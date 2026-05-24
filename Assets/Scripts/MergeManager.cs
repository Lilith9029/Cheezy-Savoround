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

    // ─── Vòng lặp quét toàn cục ─────────────────────────────────────────────────

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

    // ─── Tìm nước đi tối ưu nhất (Quét chuẩn 1 chiều) ───────────────────────────

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

                // CHỈ XÉT MỘT CHIỀU xuôi từ đĩa hiện tại sang hàng xóm.
                // Chiều ngược lại tự động tính khi vòng lặp duyệt tới neighborCell.
                EvaluatePair(plate, neighbor, ref best);
            }
        }

        return best;
    }

    private void EvaluatePair(PizzaPlate sender, PizzaPlate receiver, ref MoveResult best)
    {
        if (receiver.IsFull) return;
        if (receiver.SliceCount == 0) return; // Chặn đĩa đang chờ hủy biến mất

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

    // ─── Bảng Tính Điểm Chuẩn Hóa Chống Xung Đột Quy Tắc ───────────────────────

    private int CalcPriority(PizzaPlate sender, PizzaPlate receiver, string type)
    {
        bool receiverHasType = receiver.GetCountByType(type) > 0;

        // Bậc 300+: Ăn điểm hoàn thành đĩa
        int completionScore = CompletionScore(sender, receiver, type);
        if (completionScore > 0) return completionScore;

        // Bậc 200: Đĩa mới đặt + có chung màu
        if (receiver == _lastPlacedPlate && receiverHasType) return 200;

        // Bậc 100: Bóc tách đĩa tạp (Color Separation)
        if (WillReduceSenderColors(sender, receiver, type)) return 100;

        // Bậc 50: Gom nhóm thông thường (Consolidation)
        if (receiverHasType)
        {
            int slotsEmpty = 6 - receiver.SliceCount;
            int senderHas = sender.GetCountByType(type);

            // BẢO VỆ CẤU TRÚC: Đĩa nhận phải có đủ chỗ trống lấy HẾT màu này từ đĩa gửi
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
        // Điều kiện dọn sạch: sender chỉ còn đúng 1 slice màu này và đang mix nhiều màu
        bool canReduce = sender.GetCountByType(type) == 1 && sender.GetTypesPresent().Count > 1;
        if (!canReduce) return false;

        // Chỉ dọn màu sang nếu đĩa nhận cũng đang gom loại màu này
        return receiver.GetCountByType(type) > 0;
    }

    // ─── Thực thi nước đi: Di chuyển hàng loạt bảo toàn dữ liệu ─────────────────

    private void ExecuteMove(MoveResult move)
    {
        int slotsEmpty = 6 - move.Receiver.SliceCount;
        int senderHas = move.Sender.GetCountByType(move.SliceType);

        // Tính toán số lượng tối đa có thể gom đi trong 1 hành động
        int amountToMove = Mathf.Min(slotsEmpty, senderHas);

        // Chuyển toàn bộ số lượng hợp lệ ngay lập tức
        for (int i = 0; i < amountToMove; i++)
        {
            PizzaSlice slice = move.Sender.RemoveSliceByType(move.SliceType);
            if (slice == null) break;

            move.Receiver.AddSlice(slice);
        }

        // Sau khi đồng bộ dữ liệu xong mới kiểm tra dọn đĩa an toàn
        CheckAndClear(move.Sender);
        CheckAndClear(move.Receiver);
    }

    private void CheckAndClear(PizzaPlate plate)
    {
        if (plate == null) return;
        if (plate.SliceCount == 0 || (plate.IsFull && plate.IsAllSameType()))
            plate.ClearPlate();
    }

    // ─── Bộ So Sánh Tie-Break (Ưu tiên đĩa Mix lên hàng đầu) ─────────────────────

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

            // ĐẶC BIỆT: Ưu tiên lọc đĩa MIX (nhiều màu) trước để dọn sạch bàn cờ
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