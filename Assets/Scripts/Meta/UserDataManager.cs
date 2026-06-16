using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

[Serializable]
public class AchievementSaveData
{
    public string id;
    public int currentProgress;
    public bool isUnlocked;
}

[Serializable]
public class BoosterSaveData
{
    public string id;
    public int count;
}

[Serializable]
public class UserSaveData
{
    public int gold = 0;
    public List<string> purchasedSkins = new List<string> { "default" };
    public string equippedSkin = "default";
    public string lastClaimedTimeUTC = "";
    public string lastSessionTimeUTC = "";
    public int dailyRewardIndex = 0; // Days claimed in current 7-day cycle (0 to 7)
    public List<AchievementSaveData> achievements = new List<AchievementSaveData>();
    public List<BoosterSaveData> boosters = new List<BoosterSaveData>();
}

// UserDataManager must load save data BEFORE AchievementManager (-5) and UIManager subscribe
[DefaultExecutionOrder(-10)]
public class UserDataManager : MonoBehaviour
{
    [ContextMenu("DEBUG - Clear All Save Data")]
    public void DebugClearAllData()
    {
        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
            Debug.Log("[DEBUG] Save file deleted!");
        }
        InitializeDefaultData();
    }

    public static UserDataManager Instance { get; private set; }

    private UserSaveData _data = new UserSaveData();
    private string _savePath;

    public int Gold => _data.gold;
    public List<string> PurchasedSkins => _data.purchasedSkins;
    public string EquippedSkin => _data.equippedSkin;
    public string LastClaimedTimeUTC => _data.lastClaimedTimeUTC;
    public string LastSessionTimeUTC => _data.lastSessionTimeUTC;
    public int DailyRewardIndex => _data.dailyRewardIndex;

    public static event Action OnDataLoaded;
    public static event Action OnDataSaved;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            /*DontDestroyOnLoad(gameObject);*/
            _savePath = Path.Combine(Application.persistentDataPath, "userdata.json");
            Debug.Log(Application.persistentDataPath);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveSessionTime();
        SaveData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSessionTime();
            SaveData();
        }
    }

    private void SaveSessionTime()
    {
        _data.lastSessionTimeUTC = DateTime.UtcNow.ToString("o");
    }

    public void LoadData()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                Debug.Log($"[UserDataManager] Loaded JSON: {json}");
                _data = JsonUtility.FromJson<UserSaveData>(json);
            }
            else
            {
                InitializeDefaultData();
                SaveData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] Error loading data: {e.Message}");
            InitializeDefaultData();
        }

        // Validate lists
        if (_data.purchasedSkins == null || _data.purchasedSkins.Count == 0)
        {
            _data.purchasedSkins = new List<string> { "default" };
        }
        if (string.IsNullOrEmpty(_data.equippedSkin))
        {
            _data.equippedSkin = "default";
        }
        if (_data.achievements == null)
        {
            _data.achievements = new List<AchievementSaveData>();
        }
        if (_data.boosters == null)
        {
            _data.boosters = new List<BoosterSaveData>();
        }
        EnsureBoosterInitialized("cutter", 3);
        EnsureBoosterInitialized("sauce", 3);
        EnsureBoosterInitialized("board", 3);
        EnsureBoosterInitialized("trash", 3);
 
        OnDataLoaded?.Invoke();
    }

    private void EnsureBoosterInitialized(string id, int defaultCount)
    {
        if (_data.boosters == null) _data.boosters = new List<BoosterSaveData>();
        if (!_data.boosters.Exists(b => b.id == id))
        {
            _data.boosters.Add(new BoosterSaveData { id = id, count = defaultCount });
        }
    }

    public void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_savePath, json);
            OnDataSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] Error saving data: {e.Message}");
        }
    }

    private void InitializeDefaultData()
    {
        // Achievement list is intentionally empty here — AchievementManager.EnsureSaveDataForAll()
        // will populate it from achievements.json after both managers have initialised.
        _data = new UserSaveData
        {
            gold = 50, // Starting gold
            purchasedSkins = new List<string> { "default" },
            equippedSkin = "default",
            lastClaimedTimeUTC = "",
            lastSessionTimeUTC = DateTime.UtcNow.ToString("o"),
            achievements = new List<AchievementSaveData>(),
            boosters = new List<BoosterSaveData>()
        };
    }

    public void SetGold(int amount)
    {
        if (_data.gold != amount)
        {
            _data.gold = amount;
            SaveData();
        }
    }

    public void AddGold(int amount)
    {
        _data.gold += amount;
        SaveData();
    }

    public bool HasSkin(string skinId)
    {
        return _data.purchasedSkins.Contains(skinId);
    }

    public void PurchaseSkin(string skinId, int cost)
    {
        if (_data.purchasedSkins.Contains(skinId)) return;

        if (cost > 0 && _data.gold < cost) return;

        if (cost > 0) _data.gold -= cost;
        _data.purchasedSkins.Add(skinId);
        SaveData();
    }

    public void EquipSkin(string skinId)
    {
        if (_data.purchasedSkins.Contains(skinId))
        {
            _data.equippedSkin = skinId;
            SaveData();
        }
    }

    public void SetLastClaimedTime(string utcTimeString)
    {
        _data.lastClaimedTimeUTC = utcTimeString;
        SaveData();
    }

    public void SetLastSessionTime(string utcTimeString)
    {
        _data.lastSessionTimeUTC = utcTimeString;
        SaveData();
    }

    public void SetDailyRewardIndex(int index)
    {
        Debug.Log($"[UserDataManager] SetDailyRewardIndex called: {_data.dailyRewardIndex} → {index}\n{System.Environment.StackTrace}");
        _data.dailyRewardIndex = index;
        SaveData();
    }

    public AchievementSaveData GetAchievement(string id)
    {
        return _data.achievements.Find(a => a.id == id);
    }

    public void UpdateAchievementProgress(string id, int progress, bool isUnlocked)
    {
        var ach = GetAchievement(id);
        if (ach != null)
        {
            ach.currentProgress = progress;
            ach.isUnlocked = isUnlocked;
            SaveData();
        }
    }

    public int GetBoosterCount(string id)
    {
        var booster = _data.boosters.Find(b => b.id == id);
        return booster != null ? booster.count : 0;
    }

    public void AddBooster(string id, int amount = 1)
    {
        var booster = _data.boosters.Find(b => b.id == id);
        if (booster == null)
        {
            booster = new BoosterSaveData { id = id, count = amount };
            _data.boosters.Add(booster);
        }
        else
        {
            booster.count += amount;
        }
        SaveData();
    }

    public void AddAchievement(string id)
    {
        if (_data.achievements == null)
            _data.achievements = new List<AchievementSaveData>();

        if (!_data.achievements.Exists(a => a.id == id))
        {
            _data.achievements.Add(new AchievementSaveData { id = id, currentProgress = 0, isUnlocked = false });
            SaveData();
        }
    }
}
