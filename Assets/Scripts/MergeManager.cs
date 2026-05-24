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

        while (true)
        {
            MoveResult best = FindBestMove();
            if (best == null) break;

            ExecuteMove(best);
            yield return new WaitForSeconds(0.15f);
        }

        _isProcessing = false;
    }

    // Find Best Move (Single-Direction Scan)

    private MoveResult FindBestMove()
    {
        MoveResult best = null;
        Cell[] allCells = FindObjectsByType<Cell>(FindObjectsSortMode.None);

        foreach (Cell cell in allCells)
        {
            if (!cell.IsOccupied) continue;
            PizzaPlate plate = cell.currentPlate.GetComponent<PizzaPlate>();
            if (plate == null) continue;

            foreach (Cell neighborCell in _gridManager.GetNeighbors(cell.x, cell.z))
            {
                if (!neighborCell.IsOccupied) continue;
                PizzaPlate neighbor = neighborCell.currentPlate.GetComponent<PizzaPlate>();
                if (neighbor == null) continue;

                // Only consider one direction from current plate to neighbor.
                // The reverse direction will be handled when the loop reaches the neighbor cell.
                EvaluatePair(plate, neighbor, ref best);
            }
        }

        return best;
    }

    private void EvaluatePair(PizzaPlate sender, PizzaPlate receiver, ref MoveResult best)
    {
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

        // 200: Newly placed plate + same color
        if (receiver == _lastPlacedPlate && receiverHasType) return 200;

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

        // Check and clear plates after data synchronization
        CheckAndClear(move.Sender);
        CheckAndClear(move.Receiver);
    }

    private void CheckAndClear(PizzaPlate plate)
    {
        if (plate == null) return;
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