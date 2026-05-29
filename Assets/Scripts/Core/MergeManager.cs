using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MergeManager : MonoBehaviour
{
    private GridManager _gridManager;
    private bool _isProcessing = false;
    private PizzaPlate _lastPlacedPlate;

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

        // Check if game is over
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
        Cell[] allCells = FindObjectsByType<Cell>(FindObjectsSortMode.None);
        foreach (Cell cell in allCells)
        {
            if (cell != null && !cell.IsOccupied)
            {
                return;
            }
        }

        // If the grid is completely full and no automatic merges can be executed, trigger Game Over
        Debug.LogWarning("[MergeManager] Grid is fully occupied with no merges left! Game Over.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
    }

    // Find Best Move (Single-Direction Scan)

    private MoveResult FindBestMove()
    {
        MoveResult best = null;
        Cell[] allCells = FindObjectsByType<Cell>(FindObjectsSortMode.None);

        foreach (Cell cell in allCells)
        {
            if (!cell.IsOccupied) continue;
            if (cell.currentPlate == null) continue; // may have been cleared this frame
            PizzaPlate plate = cell.currentPlate.GetComponent<PizzaPlate>();
            if (plate == null || plate.IsBeingCleared) continue;

            foreach (Cell neighborCell in _gridManager.GetNeighbors(cell.x, cell.z))
            {
                if (!neighborCell.IsOccupied) continue;
                if (neighborCell.currentPlate == null) continue;
                PizzaPlate neighbor = neighborCell.currentPlate.GetComponent<PizzaPlate>();
                if (neighbor == null || neighbor.IsBeingCleared) continue;

                // Only consider one direction from current plate to neighbor.
                // The reverse direction will be handled when the loop reaches the neighbor cell.
                EvaluatePair(plate, neighbor, ref best);
            }
        }

        return best;
    }

    private void EvaluatePair(PizzaPlate sender, PizzaPlate receiver, ref MoveResult best)
    {
        // Skip plates that are in the process of being destroyed
        if (sender == null || sender.IsBeingCleared) return;
        if (receiver == null || receiver.IsBeingCleared) return;
        if (receiver.IsFull) return;
        if (receiver.SliceCount == 0) return; // Block plates waiting to be cleared

        foreach (string type in sender.GetTypesPresent())
        {
            if (sender.GetCountByType(type) == 0) continue;

            int priority = CalcPriority(sender, receiver, type);
            if (priority < 0) continue;

            var candidate = new MoveResult(sender, receiver, type, priority);
            if (best == null || candidate.IsBetterThan(best, _lastPlacedPlate))
                best = candidate;
        }
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
        bool canReduce = sender.GetCountByType(type) == 1 && sender.GetTypesPresent().Count > 1;
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
        if (plate.SliceCount == 0 || (plate.IsFull && plate.IsAllSameType()))
            plate.ClearPlate();
    }

    // Tie-Break Comparison (Prioritize Mixed Plates)

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

        public bool IsBetterThan(MoveResult other, PizzaPlate lastPlaced)
        {
            if (Priority != other.Priority)
                return Priority > other.Priority;

            // SPECIAL: Prioritize filtering MIX plates (multiple colors) first to clear the board
            int thisSenderTypes = Sender.GetTypesPresent().Count;
            int otherSenderTypes = other.Sender.GetTypesPresent().Count;
            if (thisSenderTypes != otherSenderTypes)
                return thisSenderTypes > otherSenderTypes;

            bool thisNew = (Receiver == lastPlaced);
            bool otherNew = (other.Receiver == lastPlaced);
            if (thisNew != otherNew) return thisNew;

            return Receiver.GetCountByType(SliceType) > other.Receiver.GetCountByType(other.SliceType);
        }
    }
}