using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class AchievementConfig
{
    public string id;
    public string title;
    public string description;
    public int targetValue;
    public int reward;
}

[Serializable]
public class AchievementConfigList
{
    public List<AchievementConfig> achievements;
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private List<AchievementConfig> _achievements = new List<AchievementConfig>();
    public List<AchievementConfig> Achievements => _achievements;

    public static event Action<string, int, int> OnAchievementProgressUpdated;
    public static event Action<string> OnAchievementUnlocked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadAchievementsFromJson();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        PizzaPlate.OnPlateCleared += HandlePlateCleared;
        ComboAudioPlayer.OnComboAchieved += HandleComboAchieved;
        GameManager.OnGoldChanged += HandleGoldChanged;
        GameManager.OnLevelProgressChanged += HandleLevelChanged;
    }

    private void OnDestroy()
    {
        PizzaPlate.OnPlateCleared -= HandlePlateCleared;
        ComboAudioPlayer.OnComboAchieved -= HandleComboAchieved;
        GameManager.OnGoldChanged -= HandleGoldChanged;
        GameManager.OnLevelProgressChanged -= HandleLevelChanged;
    }

    private void LoadAchievementsFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("achievements");
        if (jsonFile == null)
        {
            Debug.LogError("[AchievementManager] achievements.json not found in Resources!");
            return;
        }

        AchievementConfigList configList = JsonUtility.FromJson<AchievementConfigList>(jsonFile.text);
        if (configList != null)
        {
            _achievements = configList.achievements;
            EnsureSaveDataForAll();
            Debug.Log($"[AchievementManager] Loaded {_achievements.Count} achievements.");
        }
    }

    private void EnsureSaveDataForAll()
    {
        if (UserDataManager.Instance == null) return;
        foreach (var config in _achievements)
        {
            if (UserDataManager.Instance.GetAchievement(config.id) == null)
            {
                UserDataManager.Instance.AddAchievement(config.id);
            }
        }
    }

    private void HandlePlateCleared(PizzaPlate plate)
    {
        UpdateProgress("bloom_pizzas_10", 1, true);
        UpdateProgress("bloom_pizzas_50", 1, true);
        UpdateProgress("bloom_pizzas_100", 1, true);
        UpdateProgress("bloom_pizzas_500", 1, true);
    }

    private void HandleComboAchieved(int comboCount)
    {
        UpdateProgress("max_combo_3", comboCount, false);
        UpdateProgress("max_combo_5", comboCount, false);
        UpdateProgress("max_combo_10", comboCount, false);
    }

    private void HandleGoldChanged(int newGold)
    {
        UpdateProgress("earn_gold_500", newGold, false);
        UpdateProgress("earn_gold_1000", newGold, false);
        UpdateProgress("earn_gold_5000", newGold, false);
    }

    private void HandleLevelChanged(int level, float percent)
    {
        UpdateProgress("reach_level_5", level, false);
        UpdateProgress("reach_level_10", level, false);
        UpdateProgress("reach_level_25", level, false);
    }

    public void TrackBoosterUsed()
    {
        UpdateProgress("use_booster_10", 1, true);
        UpdateProgress("use_booster_50", 1, true);
    }

    public void TrackDailyStreak(int streakCount)
    {
        UpdateProgress("daily_streak_3", streakCount, false);
        UpdateProgress("daily_streak_7", streakCount, false);
    }

    private void UpdateProgress(string id, int value, bool isRelative)
    {
        if (UserDataManager.Instance == null) return;

        var config = _achievements.Find(a => a.id == id);
        if (config == null) return;

        var saveData = UserDataManager.Instance.GetAchievement(id);
        if (saveData == null) return;
        if (saveData.isUnlocked) return;

        int newProgress = isRelative
            ? saveData.currentProgress + value
            : value;

        if (!isRelative && newProgress <= saveData.currentProgress) return;

        newProgress = Mathf.Min(newProgress, config.targetValue);
        bool nowUnlocked = newProgress >= config.targetValue;

        UserDataManager.Instance.UpdateAchievementProgress(id, newProgress, nowUnlocked);
        OnAchievementProgressUpdated?.Invoke(id, newProgress, config.targetValue);

        if (nowUnlocked)
        {
            OnAchievementUnlocked?.Invoke(id);
            if (GameManager.Instance != null)
                GameManager.Instance.AddGold(config.reward);
            Debug.Log($"[AchievementManager] Unlocked: {config.title} → +{config.reward} gold!");
        }
    }
}