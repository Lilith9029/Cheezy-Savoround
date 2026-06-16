using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class DailyRewardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI rewardAmountText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI infoLabel;

    [Header("7 Day Slots - Claimed Overlays (Day 1 -> Day 7)")]
    [SerializeField] private List<GameObject> claimedOverlays = new List<GameObject>();

    private int _lastSeconds = -1;

    private void Start()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        DailyRewardManager.OnRewardStatusUpdated += RefreshUI;
    }

    private void OnDestroy()
    {
        DailyRewardManager.OnRewardStatusUpdated -= RefreshUI;
    }

    private void OnEnable()
    {
        _lastSeconds = -1;
        // Trigger a fresh status check. CheckStatusRoutine() will immediately
        // broadcast OfflineChecking → RefreshUI() so we don't need a manual call here.
        if (DailyRewardManager.Instance != null)
            DailyRewardManager.Instance.RefreshStatus();
        else
            RefreshUI(); // fallback if manager not ready yet
    }

    private void Update()
    {
        if (DailyRewardManager.Instance != null && DailyRewardManager.Instance.CurrentState == DailyRewardState.Cooldown)
        {
            TimeSpan cd = DailyRewardManager.Instance.CooldownRemaining;
            int totalSecs = (int)cd.TotalSeconds;
            if (totalSecs != _lastSeconds)
            {
                _lastSeconds = totalSecs;
                UpdateCooldownDisplay(cd);
            }
        }
    }

    public void RefreshUI()
    {
        if (DailyRewardManager.Instance == null) return;

        // 1. Cập nhật text phần thưởng hiện tại
        DailyRewardItem currentReward = DailyRewardManager.Instance.GetCurrentReward();
        if (rewardAmountText != null && currentReward != null)
        {
            switch (currentReward.rewardType)
            {
                case DailyRewardType.Gold:
                    rewardAmountText.text = $"+{currentReward.amount} GOLD";
                    break;
                case DailyRewardType.Booster:
                    rewardAmountText.text = $"+{currentReward.amount} {currentReward.rewardId.ToUpper()}";
                    break;
                case DailyRewardType.AllBoostersPackage:
                    rewardAmountText.text = "POWER-UP PACKAGE";
                    break;
                case DailyRewardType.Skin:
                    rewardAmountText.text = "SPECIAL CRUST PLATE";
                    break;
            }
        }

        // 2. Cập nhật trạng thái nút và text
        switch (DailyRewardManager.Instance.CurrentState)
        {
            case DailyRewardState.OfflineChecking:
                if (statusText != null) statusText.text = "ĐANG KIỂM TRA...";
                if (infoLabel != null) infoLabel.text = "Đang xác minh thời gian, vui lòng chờ...";
                if (claimButton != null) claimButton.interactable = false;
                break;

            case DailyRewardState.ReadyToClaim:
                if (statusText != null) statusText.text = "READY TO CLAIM!";
                if (infoLabel != null) infoLabel.text = "Claim your daily rewards to accumulate gold and boosters!";
                if (claimButton != null) claimButton.interactable = true;
                break;

            case DailyRewardState.Cooldown:
                UpdateCooldownDisplay(DailyRewardManager.Instance.CooldownRemaining);
                if (claimButton != null) claimButton.interactable = false;
                break;

            case DailyRewardState.CheatingDetected:
                if (statusText != null) statusText.text = "SECURITY WARNING!";
                if (infoLabel != null) infoLabel.text = "<color=red>Time manipulation detected! Please restore standard network clock.</color>";
                if (claimButton != null) claimButton.interactable = false;
                break;
        }

        // 3. Bật overlay cho các ngày đã claim
        int claimedUpTo = UserDataManager.Instance != null
            ? UserDataManager.Instance.DailyRewardIndex
            : 0;

        for (int i = 0; i < claimedOverlays.Count; i++)
        {
            if (claimedOverlays[i] == null) continue;
            claimedOverlays[i].SetActive(i < claimedUpTo);
        }
    }

    private void UpdateCooldownDisplay(TimeSpan remaining)
    {
        if (statusText != null) statusText.text = "CLAIMED TODAY";
        if (infoLabel != null)
            infoLabel.text = $"Next reward available in:\n<b>{remaining.Hours:D2}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s</b>";
    }

    private void OnClaimClicked()
    {
        if (DailyRewardManager.Instance == null) return;

        bool success = DailyRewardManager.Instance.ClaimReward(out string msg);

        if (infoLabel != null)
            infoLabel.text = success ? $"<color=green>{msg}</color>" : $"<color=red>{msg}</color>";

        RefreshUI();
    }
}