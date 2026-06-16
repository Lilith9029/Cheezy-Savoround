using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public enum DailyRewardState
{
    ReadyToClaim,
    Cooldown,
    OfflineChecking,
    CheatingDetected
}

[Serializable]
public enum DailyRewardType
{
    Gold,
    Booster,
    Skin,
    AllBoostersPackage
}

[Serializable]
public class DailyRewardItem
{
    public string name = "Day";
    public DailyRewardType rewardType = DailyRewardType.Gold;
    public string rewardId = "gold"; // "gold", "cutter", "sauce", "board", "trash", "cyber_neon"
    public int amount = 100;
    public Sprite icon;
}

[Serializable]
public class WorldTimeResponse
{
    public long unixtime;
}

public class DailyRewardManager : MonoBehaviour
{
    public static DailyRewardManager Instance { get; private set; }

    [ContextMenu("DEBUG - Reset Daily Reward")]
    public void DebugResetDailyReward()
    {
        if (UserDataManager.Instance == null) return;
        UserDataManager.Instance.SetLastClaimedTime("");
        UserDataManager.Instance.SetDailyRewardIndex(0);
        Debug.Log("[DEBUG] Daily reward reset! Restart game to apply.");
    }

    [Header("Daily Reward Settings")]
    [SerializeField] private string timeApiUrl = "https://worldtimeapi.org/api/timezone/Etc/UTC";
    [SerializeField] private List<DailyRewardItem> rewards = new List<DailyRewardItem>
    {
        new DailyRewardItem { name = "Day 1", rewardType = DailyRewardType.Gold, rewardId = "gold", amount = 100 },
        new DailyRewardItem { name = "Day 2", rewardType = DailyRewardType.Booster, rewardId = "sauce", amount = 1 },
        new DailyRewardItem { name = "Day 3", rewardType = DailyRewardType.Gold, rewardId = "gold", amount = 150 },
        new DailyRewardItem { name = "Day 4", rewardType = DailyRewardType.Booster, rewardId = "trash", amount = 1 },
        new DailyRewardItem { name = "Day 5", rewardType = DailyRewardType.Gold, rewardId = "gold", amount = 200 },
        new DailyRewardItem { name = "Day 6", rewardType = DailyRewardType.AllBoostersPackage, rewardId = "all_boosters", amount = 1 },
        new DailyRewardItem { name = "Day 7", rewardType = DailyRewardType.Skin, rewardId = "cyber_neon", amount = 1 }
    };

    public static event Action OnRewardStatusUpdated;

    // Start in OfflineChecking so UI shows a loading state until the first status check completes.
    private DailyRewardState _currentState = DailyRewardState.OfflineChecking;
    private TimeSpan _cooldownRemaining = TimeSpan.Zero;
    private bool _isChecking = false;

    public DailyRewardState CurrentState => _currentState;
    public TimeSpan CooldownRemaining => _cooldownRemaining;
    public int RewardAmount => GetCurrentReward() != null ? GetCurrentReward().amount : 100;
    public List<DailyRewardItem> Rewards => rewards;

    public int CurrentRewardIndex
    {
        get
        {
            if (UserDataManager.Instance == null) return 0;
            int index = UserDataManager.Instance.DailyRewardIndex;
            if (index >= rewards.Count)
            {
                if (_currentState == DailyRewardState.ReadyToClaim)
                {
                    UserDataManager.Instance.SetDailyRewardIndex(0);
                    return 0;
                }
            }
            return index;
        }
    }

    public DailyRewardItem GetCurrentReward()
    {
        if (rewards == null || rewards.Count == 0) return null;
        int index = UserDataManager.Instance != null ? UserDataManager.Instance.DailyRewardIndex : 0;
        if (index >= rewards.Count) index = rewards.Count - 1;
        return rewards[index];
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            /*DontDestroyOnLoad(gameObject);*/
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.SetLastSessionTime(DateTime.UtcNow.ToString("o"));
        }

        StartCoroutine(CheckStatusRoutine());
    }

    private void Update()
    {
        if (_currentState == DailyRewardState.Cooldown && _cooldownRemaining > TimeSpan.Zero)
        {
            _cooldownRemaining -= TimeSpan.FromSeconds(Time.deltaTime);
            if (_cooldownRemaining <= TimeSpan.Zero)
            {
                _cooldownRemaining = TimeSpan.Zero;
                _currentState = DailyRewardState.ReadyToClaim;
                OnRewardStatusUpdated?.Invoke();
            }
        }
    }

    public void RefreshStatus()
    {
        if (!_isChecking)
        {
            StartCoroutine(CheckStatusRoutine());
        }
        else
        {
            // Already checking in background — broadcast current state so any
            // newly-opened UI panel renders the OfflineChecking indicator immediately.
            OnRewardStatusUpdated?.Invoke();
        }
    }

    private IEnumerator CheckStatusRoutine()
    {
        _isChecking = true;

        // Broadcast immediately so UI can show a "Checking..." state
        // instead of rendering stale Cooldown default.
        _currentState = DailyRewardState.OfflineChecking;
        OnRewardStatusUpdated?.Invoke();

        // Try to fetch secure time online
        using (UnityWebRequest webRequest = UnityWebRequest.Get(timeApiUrl))
        {
            webRequest.timeout = 5; // 5 seconds timeout
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ProcessOnlineTime(webRequest.downloadHandler.text);
            }
            else
            {
                ProcessOfflineTime();
            }
        }

        _isChecking = false;
        OnRewardStatusUpdated?.Invoke();
    }

    private void ProcessOnlineTime(string jsonResponse)
    {
        try
        {
            WorldTimeResponse response = JsonUtility.FromJson<WorldTimeResponse>(jsonResponse);
            long currentUnixTime = response.unixtime;

            // Get last claimed unix time
            long lastClaimedUnixTime = 0;
            if (UserDataManager.Instance != null && !string.IsNullOrEmpty(UserDataManager.Instance.LastClaimedTimeUTC))
            {
                long.TryParse(UserDataManager.Instance.LastClaimedTimeUTC, out lastClaimedUnixTime);
            }

            long elapsedSeconds = currentUnixTime - lastClaimedUnixTime;
            long dailyCooldown = 24 * 60 * 60; // 24 hours in seconds

            if (elapsedSeconds >= dailyCooldown)
            {
                _currentState = DailyRewardState.ReadyToClaim;
                _cooldownRemaining = TimeSpan.Zero;

                // Reset index if we finished the 7-day cycle
                if (UserDataManager.Instance != null && UserDataManager.Instance.DailyRewardIndex >= rewards.Count)
                {
                    UserDataManager.Instance.SetDailyRewardIndex(0);
                }
            }
            else
            {
                _currentState = DailyRewardState.Cooldown;
                _cooldownRemaining = TimeSpan.FromSeconds(dailyCooldown - elapsedSeconds);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[DailyRewardManager] Error parsing time response: {e.Message}");
            ProcessOfflineTime();
        }
    }

    private void ProcessOfflineTime()
    {
        if (UserDataManager.Instance == null)
        {
            _currentState = DailyRewardState.Cooldown;
            return;
        }

        DateTime currentLocalUtc = DateTime.UtcNow;

        // === FIX: Chỉ check anti-cheat nếu LastClaimedTimeUTC có giá trị ===
        // Build mới hoàn toàn (chưa claim lần nào) → bỏ qua anti-cheat
        bool hasEverClaimed = !string.IsNullOrEmpty(UserDataManager.Instance.LastClaimedTimeUTC);

        if (hasEverClaimed && !string.IsNullOrEmpty(UserDataManager.Instance.LastSessionTimeUTC))
        {
            if (DateTime.TryParse(UserDataManager.Instance.LastSessionTimeUTC, out DateTime lastSessionTime))
            {
                // === FIX: Cho phép sai lệch tối đa 60 giây để tránh false positive ===
                if (currentLocalUtc < lastSessionTime.AddSeconds(-60))
                {
                    _currentState = DailyRewardState.CheatingDetected;
                    _cooldownRemaining = TimeSpan.Zero;
                    return;
                }
            }
        }

        // Check last claimed time
        DateTime lastClaimedTime = DateTime.MinValue;
        if (hasEverClaimed)
        {
            if (long.TryParse(UserDataManager.Instance.LastClaimedTimeUTC, out long lastClaimedUnix))
            {
                lastClaimedTime = DateTimeOffset.FromUnixTimeSeconds(lastClaimedUnix).UtcDateTime;
            }
            else
            {
                DateTime.TryParse(UserDataManager.Instance.LastClaimedTimeUTC, out lastClaimedTime);
            }
        }

        // === FIX: Nếu chưa claim lần nào → ReadyToClaim ngay ===
        if (!hasEverClaimed)
        {
            _currentState = DailyRewardState.ReadyToClaim;
            _cooldownRemaining = TimeSpan.Zero;
            return;
        }

        TimeSpan elapsed = currentLocalUtc - lastClaimedTime;
        TimeSpan cooldown = TimeSpan.FromHours(24);

        if (elapsed >= cooldown)
        {
            _currentState = DailyRewardState.ReadyToClaim;
            _cooldownRemaining = TimeSpan.Zero;

            if (UserDataManager.Instance.DailyRewardIndex >= rewards.Count)
            {
                UserDataManager.Instance.SetDailyRewardIndex(0);
            }
        }
        else
        {
            _currentState = DailyRewardState.Cooldown;
            _cooldownRemaining = cooldown - elapsed;
        }
    }

    public bool ClaimReward(out string message)
    {
        message = "";

        if (_currentState != DailyRewardState.ReadyToClaim)
        {
            if (_currentState == DailyRewardState.CheatingDetected)
            {
                message = "Không thể nhận thưởng! Phát hiện gian lận thay đổi thời gian.";
            }
            else
            {
                message = $"Chưa đến giờ nhận thưởng! Còn lại: {_cooldownRemaining.Hours}h {_cooldownRemaining.Minutes}m.";
            }
            return false;
        }

        if (UserDataManager.Instance == null || GameManager.Instance == null)
        {
            message = "Lỗi hệ thống! Vui lòng thử lại sau.";
            return false;
        }

        int index = UserDataManager.Instance.DailyRewardIndex;
        if (index >= rewards.Count)
        {
            UserDataManager.Instance.SetDailyRewardIndex(0);
            index = 0;
        }

        DailyRewardItem reward = rewards[index];
        bool claimSuccess = false;

        // Give reward based on type
        if (reward.rewardType == DailyRewardType.Gold)
        {
            GameManager.Instance.AddGold(reward.amount);
            message = $"Nhận thưởng thành công! Bạn nhận được {reward.amount} vàng.";
            claimSuccess = true;
        }
        else if (reward.rewardType == DailyRewardType.Booster)
        {
            UserDataManager.Instance.AddBooster(reward.rewardId, reward.amount);
            string boosterName = reward.rewardId == "cutter" ? "Dao Cắt" :
                                 reward.rewardId == "sauce" ? "Chai Sốt" :
                                 reward.rewardId == "board" ? "Thớt Gỗ" :
                                 reward.rewardId == "trash" ? "Thùng Rác" : reward.rewardId;
            message = $"Nhận thưởng thành công! Bạn nhận được {reward.amount} {boosterName}.";
            claimSuccess = true;
        }
        else if (reward.rewardType == DailyRewardType.AllBoostersPackage)
        {
            UserDataManager.Instance.AddBooster("cutter", 1);
            UserDataManager.Instance.AddBooster("sauce", 1);
            UserDataManager.Instance.AddBooster("board", 1);
            UserDataManager.Instance.AddBooster("trash", 1);
            message = "Nhận thưởng thành công! Bạn nhận được Gói Power-up (+1 tất cả công cụ).";
            claimSuccess = true;
        }
        else if (reward.rewardType == DailyRewardType.Skin)
        {
            if (UserDataManager.Instance.HasSkin(reward.rewardId))
            {
                // Fallback reward if skin already owned
                int fallbackGold = 500;
                GameManager.Instance.AddGold(fallbackGold);
                message = $"Nhận thưởng thành công! Bạn đã sở hữu đĩa này, nhận thay thế {fallbackGold} vàng.";
            }
            else
            {
                UserDataManager.Instance.PurchaseSkin(reward.rewardId, 0);
                string skinName = reward.rewardId == "cyber_neon" ? "Cyber Neon" : "Đặc Biệt";
                message = $"Nhận thưởng thành công! Bạn mở khóa đĩa đặc biệt: {skinName}!";
            }
            claimSuccess = true;
        }

        if (claimSuccess)
        {
            // Save claim time as Unix timestamp string so online/offline checks parse it correctly.
            long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string claimTimeVal = currentUnix.ToString();
            UserDataManager.Instance.SetLastClaimedTime(claimTimeVal);

            // Advance dailyRewardIndex
            UserDataManager.Instance.SetDailyRewardIndex(index + 1);

            _currentState = DailyRewardState.Cooldown;
            _cooldownRemaining = TimeSpan.FromHours(24);

            if (AchievementManager.Instance != null)
                AchievementManager.Instance.TrackDailyStreak(UserDataManager.Instance.DailyRewardIndex);

            OnRewardStatusUpdated?.Invoke();
            return true;
        }

        message = "Lỗi hệ thống! Vui lòng thử lại sau.";
        return false;
    }
}
