using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MergeManager : MonoBehaviour
{
    private GridManager _gridManager;
    private bool _isProcessing = false;
    private PizzaPlate _lastPlacedPlate;
    private readonly string[] _uniqueTypesBuffer = new string[6];

    public bool IsProcessing => _isProcessing;

    private void Start()
    {
        _gridManager = FindFirstObjectByType<GridManager>();
    }

    public void CheckMerges(Cell startCell)
    {
        if (startCell == null || !startCell.IsOccupied) return;

        PizzaPlate plate = startCell.currentPlate.GetComponent<PizzaPlate>();
        if (plate != null) _lastPlacedPlate = plate;

        if (!_isProcessing)
            StartCoroutine(ProcessMergesLoop());
    }

    // Global Processing Loop

    private IEnumerator ProcessMergesLoop()
    {
        _isProcessing = true;

        // Reset combo audio count at the start of a new placement turn
        if (ComboAudioPlayer.Instance != null)
            ComboAudioPlayer.Instance.ResetCombo();

        // Change state to CheckingCombo when loop begins
        if (GameManager.Instance != null)
            GameManager.Instance.ChangeState(GameState.CheckingCombo);

        while (true)
        {
            MoveResult best = FindBestMove();
            if (best == null) break;

            // Change state to Animating during execution
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Animating);

            // Execute the data move first
            ExecuteMove(best);
            
            // Wait for Bezier animation to finish BEFORE checking/clearing
            // (0.35s Bezier + small buffer = 0.45s to be safe)
            yield return new WaitForSeconds(0.45f);

            // Now safe to check and clear plates after animation has landed
            CheckAndClear(best.Sender);
            CheckAndClear(best.Receiver);

            // Small pause to let clear VFX register before scanning again
            yield return new WaitForSeconds(0.05f);

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.CheckingCombo);
        }

        _isProcessing = false;

        yield return new WaitForSeconds(0.35f);

        CheckGameOver();

        // If not Game Over, return to Playing state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.GameOver)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    private void CheckGameOver()
    {
        // If there is at least one free cell, the game is not over
        if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
        if (_gridManager != null && _gridManager.AllCells != null)
        {
            foreach (Cell cell in _gridManager.AllCells)
            {
                if (cell != null && !cell.IsOccupied)
                {
                    return;
                }
            }
        }

        // If the grid is completely full and no automatic merges can be executed, trigger Game Over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
    }

    // Find Best Move (Single-Direction Scan)

    private MoveResult FindBestMove()
    {
        PizzaPlate bestSender = null;
        PizzaPlate bestReceiver = null;
        string bestSliceType = null;
        int bestPriority = -1;

        if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
        if (_gridManager == null || _gridManager.AllCells == null) return null;

        foreach (Cell cell in _gridManager.AllCells)
        {
            if (cell == null) continue;
            if (!cell.IsOccupied) continue;
            if (cell.currentPlate == null) continue; // may have been cleared this frame
            PizzaPlate plate = cell.currentPlate.GetComponent<PizzaPlate>();
            if (plate == null || plate.IsBeingCleared) continue;

            foreach (Cell neighborCell in _gridManager.GetNeighbors(cell.x, cell.z))
            {
                if (neighborCell == null) continue;
                if (!neighborCell.IsOccupied) continue;
                if (neighborCell.currentPlate == null) continue;
                PizzaPlate neighbor = neighborCell.currentPlate.GetComponent<PizzaPlate>();
                if (neighbor == null || neighbor.IsBeingCleared) continue;

                // Only consider one direction from current plate to neighbor.
                EvaluatePair(plate, neighbor, ref bestSender, ref bestReceiver, ref bestSliceType, ref bestPriority);
            }
        }

        if (bestSender != null)
        {
            return new MoveResult(bestSender, bestReceiver, bestSliceType, bestPriority);
        }
        return null;
    }

    private void EvaluatePair(PizzaPlate sender, PizzaPlate receiver, ref PizzaPlate bestSender, ref PizzaPlate bestReceiver, ref string bestSliceType, ref int bestPriority)
    {
        // Skip plates that are in the process of being destroyed
        if (sender == null || sender.IsBeingCleared) return;
        if (receiver == null || receiver.IsBeingCleared) return;
        if (receiver.IsFull) return;
        if (receiver.SliceCount == 0) return; // Block plates waiting to be cleared

        int typeCount = sender.GetUniqueTypesNonAlloc(_uniqueTypesBuffer);
        for (int i = 0; i < typeCount; i++)
        {
            string type = _uniqueTypesBuffer[i];
            if (sender.GetCountByType(type) == 0) continue;

            int priority = CalcPriority(sender, receiver, type);
            if (priority < 0) continue;

            if (IsCandidateBetter(sender, receiver, type, priority, bestSender, bestReceiver, bestSliceType, bestPriority))
            {
                bestSender = sender;
                bestReceiver = receiver;
                bestSliceType = type;
                bestPriority = priority;
            }
        }
    }

    private bool IsCandidateBetter(PizzaPlate sender, PizzaPlate receiver, string sliceType, int priority,
                                   PizzaPlate bestSender, PizzaPlate bestReceiver, string bestSliceType, int bestPriority)
    {
        if (bestSender == null) return true;

        if (priority != bestPriority)
            return priority > bestPriority;

        int thisSenderTypes = sender.GetUniqueTypesCount();
        int bestSenderTypes = bestSender.GetUniqueTypesCount();
        if (thisSenderTypes != bestSenderTypes)
            return thisSenderTypes > bestSenderTypes;

        bool thisNew = (receiver == _lastPlacedPlate);
        bool bestNew = (bestReceiver == _lastPlacedPlate);
        if (thisNew != bestNew) return thisNew;

        return receiver.GetCountByType(sliceType) > bestReceiver.GetCountByType(bestSliceType);
    }

    // Priority Calculation Table (Conflict-Free Rule-Based Scoring)

    private int CalcPriority(PizzaPlate sender, PizzaPlate receiver, string type)
    {
        bool receiverHasType = receiver.GetCountByType(type) > 0;

        // 300+: Complete the plate
        int completionScore = CompletionScore(sender, receiver, type);
        if (completionScore > 0) return completionScore;

        // 100: Color Separation
        if (WillReduceSenderColors(sender, receiver, type)) return 100;

        // 50: Consolidation
        if (receiverHasType)
        {
            int slotsEmpty = 6 - receiver.SliceCount;
            int senderHas = sender.GetCountByType(type);

            // STRUCTURE PROTECTION: The receiving plate must have enough empty slots to take ALL of this color from the sending plate
            if (slotsEmpty >= senderHas && senderHas > 0)
            {
                return 50;
            }
        }

        return -1;
    }

    private int CompletionScore(PizzaPlate sender, PizzaPlate receiver, string type)
    {
        int receiverHas = receiver.GetCountByType(type);
        int senderHas = sender.GetCountByType(type);
        int slotsEmpty = 6 - receiver.SliceCount;

        bool pureReceiver = (receiverHas == receiver.SliceCount);
        if (!pureReceiver) return 0;

        if (slotsEmpty <= 0 || senderHas < slotsEmpty) return 0;

        return 300 + receiverHas;
    }

    private bool WillReduceSenderColors(PizzaPlate sender, PizzaPlate receiver, string type)
    {
        // Condition for clearing: sender has only 1 slice of this color and is mixing multiple colors
        bool canReduce = sender.GetCountByType(type) == 1 && sender.GetUniqueTypesCount() > 1;
        if (!canReduce) return false;

        // Only move color to receiver if the receiver is also collecting this color
        return receiver.GetCountByType(type) > 0;
    }

    // Execute Move (Batch Move)

    private void ExecuteMove(MoveResult move)
    {
        // Safety check: plates might have been cleared between FindBestMove and ExecuteMove
        if (move.Sender == null || move.Sender.IsBeingCleared) return;
        if (move.Receiver == null || move.Receiver.IsBeingCleared) return;

        int slotsEmpty = 6 - move.Receiver.SliceCount;
        int senderHas = move.Sender.GetCountByType(move.SliceType);

        // Calculate the maximum number of slices that can be moved in one action
        int amountToMove = Mathf.Min(slotsEmpty, senderHas);

        if (amountToMove > 0 && ComboAudioPlayer.Instance != null)
            ComboAudioPlayer.Instance.PlaySliceMove();

        // Move all valid slices at once
        for (int i = 0; i < amountToMove; i++)
        {
            PizzaSlice slice = move.Sender.RemoveSliceByType(move.SliceType);
            if (slice == null) break;

            move.Receiver.AddSlice(slice);
        }
        // NOTE: CheckAndClear is now called AFTER the animation delay in ProcessMergesLoop
    }

    private void CheckAndClear(PizzaPlate plate)
    {
        if (plate == null || plate.IsBeingCleared) return;

        if (plate.IsFull && plate.IsAllSameType())
        {
            plate.ClearPlate(true);
        }
        else if (plate.SliceCount == 0)
        {
            plate.ClearPlate(false);
        }
    }

    // MoveResult Helper

    private class MoveResult
    {
        public PizzaPlate Sender;
        public PizzaPlate Receiver;
        public string SliceType;
        public int Priority;

        public MoveResult(PizzaPlate sender, PizzaPlate receiver, string type, int priority)
        {
            Sender = sender;
            Receiver = receiver;
            SliceType = type;
            Priority = priority;
        }
    }

    /// <summary>
    /// Stops the merge loop immediately (used before restart).
    /// </summary>
    public void CancelProcessing()
    {
        StopAllCoroutines();
        _isProcessing = false;
    }

    /// <summary>
    /// Deletes up to 3 random plates from the grid, allowing the player to resume.
    /// </summary>
    public void DeleteThreeRandomPlates()
    {
        if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
        if (_gridManager == null || _gridManager.AllCells == null) return;
        List<Cell> occupiedCells = new List<Cell>();
        
        foreach (Cell cell in _gridManager.AllCells)
        {
            if (cell != null && cell.IsOccupied)
            {
                occupiedCells.Add(cell);
            }
        }

        int countToDelete = Mathf.Min(3, occupiedCells.Count);
        for (int i = 0; i < countToDelete; i++)
        {
            int randomIndex = Random.Range(0, occupiedCells.Count);
            Cell targetCell = occupiedCells[randomIndex];
            occupiedCells.RemoveAt(randomIndex);

            if (targetCell.currentPlate != null)
            {
                if (ObjectPooler.Instance != null)
                {
                    ObjectPooler.Instance.SpawnFromPool("PizzaExplosion", targetCell.currentPlate.transform.position, Quaternion.identity);
                }
                
                Destroy(targetCell.currentPlate);
                targetCell.currentPlate = null;
            }
        }

        // Refill hold slots if they are empty
        if (HoldSlotsManager.Instance != null)
        {
            HoldSlotsManager.Instance.CheckAndRefillSlots();
        }

        // Switch state back to Playing
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }
    }
}