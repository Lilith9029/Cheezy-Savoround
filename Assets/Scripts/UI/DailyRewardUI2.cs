using UnityEngine;
using System.Collections.Generic;

public class DailyRewardUI2 : MonoBehaviour
{
    [Header("Day Slot Overlays (Day 1 -> Day 7)")]
    [SerializeField] private List<GameObject> claimedOverlays = new List<GameObject>(); // 7 phần tử

    private void OnEnable()
    {
        DailyRewardManager.OnRewardStatusUpdated += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        DailyRewardManager.OnRewardStatusUpdated -= RefreshUI;
    }

    public void RefreshUI()
    {
        if (DailyRewardManager.Instance == null) return;

        // ... các code cũ ...

        // 3. Bật overlay cho các ngày đã claim
        int claimedUpTo = UserDataManager.Instance != null
            ? UserDataManager.Instance.DailyRewardIndex
            : 0;

        // THÊM DÒNG NÀY ĐỂ DEBUG
        Debug.Log($"[DailyRewardUI] claimedUpTo = {claimedUpTo} | overlays count = {claimedOverlays.Count}");

        for (int i = 0; i < claimedOverlays.Count; i++)
        {
            if (claimedOverlays[i] == null)
            {
                Debug.LogWarning($"[DailyRewardUI] Overlay slot {i} is NULL!");
                continue;
            }

            bool shouldShow = i < claimedUpTo;
            Debug.Log($"[DailyRewardUI] Overlay[{i}] = {claimedOverlays[i].name} → SetActive({shouldShow})");
            claimedOverlays[i].SetActive(shouldShow);
        }
    }
}