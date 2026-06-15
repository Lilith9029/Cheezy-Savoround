using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[Serializable]
public class ShopIconMapping
{
    public string iconKey;
    public Sprite sprite;
}

/// <summary>
/// Carousel shop: Coin / Skin / Booster.
/// Wire Background, ItemIcon, PriceText, GoldText, Prev/Next/Buy/Close in Inspector.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI buyButtonLabel;

    [Header("Category Backgrounds")]
    [SerializeField] private Sprite coinBackground;
    [SerializeField] private Sprite skinBackground;
    [SerializeField] private Sprite boosterBackground;

    [Header("Icon Sprites (match iconKey in ShopConfig)")]
    [SerializeField] private List<ShopIconMapping> iconMappings = new List<ShopIconMapping>();

    private ShopCategory _category = ShopCategory.Coin;
    private int _itemIndex;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        TryResolveReferences();

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (prevButton != null)
            prevButton.onClick.AddListener(() => Navigate(-1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => Navigate(1));

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        ShopManager.OnShopUpdated += RefreshDisplay;
        UserDataManager.OnDataLoaded += RefreshDisplay;
        GameManager.OnGoldChanged += OnGoldChanged;
    }

    private void OnDestroy()
    {
        ShopManager.OnShopUpdated -= RefreshDisplay;
        UserDataManager.OnDataLoaded -= RefreshDisplay;
        GameManager.OnGoldChanged -= OnGoldChanged;
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    private void TryResolveReferences()
    {
        Transform bg = transform.Find("Background");
        if (bg == null) return;

        if (backgroundImage == null) backgroundImage = bg.GetComponent<Image>();
        if (itemIconImage == null) itemIconImage = FindChildImage(bg, "ItemIcon");
        if (itemNameText == null) itemNameText = FindChildText(bg, "ItemNameText");
        if (priceText == null) priceText = FindChildText(bg, "PriceText");
        if (goldText == null) goldText = FindChildText(bg, "GoldBar/GoldText");
        if (prevButton == null) prevButton = FindChildButton(bg, "BtnPrev");
        if (nextButton == null) nextButton = FindChildButton(bg, "BtnNext");
        if (buyButton == null) buyButton = FindChildButton(bg, "BtnBuy");
        if (closeButton == null) closeButton = FindChildButton(bg, "CloseButton");
    }

    private static TextMeshProUGUI FindChildText(Transform parent, string path)
    {
        Transform t = parent.Find(path);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Image FindChildImage(Transform parent, string path)
    {
        Transform t = parent.Find(path);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Button FindChildButton(Transform parent, string path)
    {
        Transform t = parent.Find(path);
        return t != null ? t.GetComponent<Button>() : null;
    }

    public void Open(ShopCategory category = ShopCategory.Coin, int itemIndex = 0)
    {
        _category = category;
        _itemIndex = Mathf.Max(0, itemIndex);
        ClampItemIndex();
        gameObject.SetActive(true);
        RefreshDisplay();
    }

    private void OnGoldChanged(int gold)
    {
        UpdateGoldText(gold);
        RefreshBuyButton();
    }

    private void Navigate(int direction)
    {
        if (ShopManager.Instance == null) return;

        int count = ShopManager.Instance.GetItemCount(_category);
        if (count <= 0) return;

        _itemIndex += direction;

        if (_itemIndex < 0)
        {
            int prevCategory = ((int)_category - 1 + 3) % 3;
            _category = (ShopCategory)prevCategory;
            _itemIndex = Mathf.Max(0, ShopManager.Instance.GetItemCount(_category) - 1);
        }
        else if (_itemIndex >= count)
        {
            int nextCategory = ((int)_category + 1) % 3;
            _category = (ShopCategory)nextCategory;
            _itemIndex = 0;
        }

        ClampItemIndex();
        RefreshDisplay();
    }

    private void ClampItemIndex()
    {
        if (ShopManager.Instance == null) return;
        int count = ShopManager.Instance.GetItemCount(_category);
        if (count == 0) { _itemIndex = 0; return; }
        _itemIndex = Mathf.Clamp(_itemIndex, 0, count - 1);
    }

    public void RefreshDisplay()
    {
        if (ShopManager.Instance == null) return;

        ApplyBackground();
        ApplyItemVisual();
        UpdateGoldText(GameManager.Instance != null ? GameManager.Instance.Gold : 0);
        RefreshBuyButton();
    }

    private void ApplyBackground()
    {
        if (backgroundImage == null) return;

        Sprite bg = coinBackground;
        switch (_category)
        {
            case ShopCategory.Skin: bg = skinBackground; break;
            case ShopCategory.Booster: bg = boosterBackground; break;
        }

        if (bg != null)
        {
            backgroundImage.sprite = bg;
            backgroundImage.color = Color.white;
        }
    }

    private void ApplyItemVisual()
    {
        if (ShopManager.Instance == null) return;

        string iconKey = "";
        string displayName = "";

        switch (_category)
        {
            case ShopCategory.Coin:
                if (_itemIndex < ShopManager.Instance.CoinPacks.Count)
                {
                    var pack = ShopManager.Instance.CoinPacks[_itemIndex];
                    iconKey = pack.iconKey;
                    displayName = pack.name;
                    if (priceText != null) priceText.text = pack.priceLabel;
                }
                break;

            case ShopCategory.Skin:
                if (_itemIndex < ShopManager.Instance.Skins.Count)
                {
                    var skin = ShopManager.Instance.Skins[_itemIndex];
                    iconKey = skin.id;
                    displayName = skin.name;
                    if (priceText != null)
                    {
                        bool owned = UserDataManager.Instance != null && UserDataManager.Instance.HasSkin(skin.id);
                        bool equipped = UserDataManager.Instance != null && UserDataManager.Instance.EquippedSkin == skin.id;
                        if (equipped) priceText.text = "EQUIPPED";
                        else if (owned) priceText.text = "OWNED";
                        else priceText.text = $"{skin.cost} Coins";
                    }
                }
                break;

            case ShopCategory.Booster:
                if (_itemIndex < ShopManager.Instance.Boosters.Count)
                {
                    var booster = ShopManager.Instance.Boosters[_itemIndex];
                    iconKey = booster.iconKey;
                    displayName = booster.name;
                    int owned = UserDataManager.Instance != null ? UserDataManager.Instance.GetBoosterCount(booster.id) : 0;
                    if (priceText != null)
                        priceText.text = owned > 0 ? $"x{owned} | {booster.cost} Coins" : $"{booster.cost} Coins";
                }
                break;
        }

        if (itemNameText != null) itemNameText.text = displayName;

        if (itemIconImage != null)
        {
            if (_category == ShopCategory.Skin && _itemIndex < ShopManager.Instance.Skins.Count)
            {
                var skin = ShopManager.Instance.Skins[_itemIndex];
                itemIconImage.sprite = null;
                if (ColorUtility.TryParseHtmlString(skin.colorHex, out Color c))
                {
                    itemIconImage.color = c;
                }
            }
            else
            {
                Sprite icon = GetIcon(iconKey);
                itemIconImage.sprite = icon;
                itemIconImage.color = icon != null ? Color.white : Color.gray;
            }
        }
    }

    private Sprite GetIcon(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey)) return null;
        foreach (var mapping in iconMappings)
        {
            if (mapping.iconKey == iconKey) return mapping.sprite;
        }
        return null;
    }

    private void UpdateGoldText(int gold)
    {
        if (goldText != null) goldText.text = gold.ToString();
    }

    private void RefreshBuyButton()
    {
        if (buyButton == null || ShopManager.Instance == null) return;

        bool interactable = true;
        string label = "BUY";

        switch (_category)
        {
            case ShopCategory.Coin:
                label = "BUY";
                interactable = true;
                break;

            case ShopCategory.Skin:
                if (_itemIndex < ShopManager.Instance.Skins.Count)
                {
                    var skin = ShopManager.Instance.Skins[_itemIndex];
                    bool owned = UserDataManager.Instance != null && UserDataManager.Instance.HasSkin(skin.id);
                    bool equipped = UserDataManager.Instance != null && UserDataManager.Instance.EquippedSkin == skin.id;

                    if (equipped)
                    {
                        label = "OWNED";
                        interactable = false;
                    }
                    else if (owned)
                    {
                        label = "EQUIPPED";
                        interactable = true;
                    }
                    else
                    {
                        label = "BUY";
                        interactable = GameManager.Instance != null && GameManager.Instance.Gold >= skin.cost;
                    }
                }
                break;

            case ShopCategory.Booster:
                if (_itemIndex < ShopManager.Instance.Boosters.Count)
                {
                    var booster = ShopManager.Instance.Boosters[_itemIndex];
                    label = "BUY";
                    interactable = GameManager.Instance != null && GameManager.Instance.Gold >= booster.cost;
                }
                break;
        }

        buyButton.interactable = interactable;
        if (buyButtonLabel != null) buyButtonLabel.text = label;
    }

    private void OnBuyClicked()
    {
        if (ShopManager.Instance == null) return;

        switch (_category)
        {
            case ShopCategory.Coin:
                if (_itemIndex < ShopManager.Instance.CoinPacks.Count)
                    ShopManager.Instance.BuyCoinPack(ShopManager.Instance.CoinPacks[_itemIndex].id);
                break;

            case ShopCategory.Skin:
                if (_itemIndex < ShopManager.Instance.Skins.Count)
                {
                    var skin = ShopManager.Instance.Skins[_itemIndex];
                    bool owned = UserDataManager.Instance != null && UserDataManager.Instance.HasSkin(skin.id);
                    if (owned) ShopManager.Instance.EquipSkin(skin.id);
                    else ShopManager.Instance.BuySkin(skin.id);
                }
                break;

            case ShopCategory.Booster:
                if (_itemIndex < ShopManager.Instance.Boosters.Count)
                    ShopManager.Instance.BuyBooster(ShopManager.Instance.Boosters[_itemIndex].id);
                break;
        }

        RefreshDisplay();
    }
}
