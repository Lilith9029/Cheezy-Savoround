using UnityEngine;
using System;
using System.Collections.Generic;

public enum ShopCategory
{
    Coin,
    Skin,
    Booster
}

[Serializable]
public class CoinPackConfig
{
    public string id;
    public string name;
    public int goldAmount;
    public string priceLabel;
    public string iconKey;
}

[Serializable]
public class SkinConfig
{
    public string id;
    public string name;
    public int cost;
    public string colorHex;
    public float metallic;
    public float smoothness;
}

[Serializable]
public class BoosterConfig
{
    public string id;
    public string name;
    public int cost;
    public string iconKey;
}

[Serializable]
public class ShopConfig
{
    public List<CoinPackConfig> coinPacks;
    public List<SkinConfig> skins;
    public List<BoosterConfig> boosters;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private string configFileName = "ShopConfig";

    private List<CoinPackConfig> _coinPacks = new List<CoinPackConfig>();
    private List<SkinConfig> _skins = new List<SkinConfig>();
    private List<BoosterConfig> _boosters = new List<BoosterConfig>();

    public List<CoinPackConfig> CoinPacks => _coinPacks;
    public List<SkinConfig> Skins => _skins;
    public List<BoosterConfig> Boosters => _boosters;

    public static event Action<string> OnSkinEquipped;
    public static event Action OnShopUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            /*DontDestroyOnLoad(gameObject);*/
            LoadConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadConfig()
    {
        TextAsset configAsset = Resources.Load<TextAsset>(configFileName);
        if (configAsset == null)
        {
            return;
        }

        try
        {
            ShopConfig config = JsonUtility.FromJson<ShopConfig>(configAsset.text);
            if (config == null) return;

            if (config.coinPacks != null) _coinPacks = config.coinPacks;
            if (config.skins != null) _skins = config.skins;
            if (config.boosters != null) _boosters = config.boosters;

        }
        catch (Exception e)
        {
            Debug.LogError($"[ShopManager] Error parsing ShopConfig: {e.Message}");
        }
    }

    public int GetItemCount(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Coin: return _coinPacks.Count;
            case ShopCategory.Skin: return _skins.Count;
            case ShopCategory.Booster: return _boosters.Count;
            default: return 0;
        }
    }

    public SkinConfig GetSkinConfig(string id) => _skins.Find(s => s.id == id);
    public CoinPackConfig GetCoinPack(string id) => _coinPacks.Find(c => c.id == id);
    public BoosterConfig GetBoosterConfig(string id) => _boosters.Find(b => b.id == id);

    public bool BuyCoinPack(string id)
    {
        CoinPackConfig pack = GetCoinPack(id);
        if (pack == null || GameManager.Instance == null) return false;

        GameManager.Instance.AddGold(pack.goldAmount);
        OnShopUpdated?.Invoke();
        return true;
    }

    public bool BuySkin(string id)
    {
        SkinConfig skin = GetSkinConfig(id);
        if (skin == null || UserDataManager.Instance == null || GameManager.Instance == null) return false;

        if (UserDataManager.Instance.HasSkin(id)) return false;
        if (!GameManager.Instance.TrySpendGold(skin.cost)) return false;

        UserDataManager.Instance.PurchaseSkin(id, 0);
        OnShopUpdated?.Invoke();
        return true;
    }

    public bool EquipSkin(string id)
    {
        if (UserDataManager.Instance == null)
        {
            return false;
        }

        if (UserDataManager.Instance.HasSkin(id))
        {
            UserDataManager.Instance.EquipSkin(id);
            OnSkinEquipped?.Invoke(id);
            OnShopUpdated?.Invoke();
            return true;
        }

        return false;
    }

    public bool BuyBooster(string id)
    {
        BoosterConfig booster = GetBoosterConfig(id);
        if (booster == null || UserDataManager.Instance == null || GameManager.Instance == null) return false;
        if (!GameManager.Instance.TrySpendGold(booster.cost)) return false;

        UserDataManager.Instance.AddBooster(id, 1);
        OnShopUpdated?.Invoke();
        return true;
    }
}
