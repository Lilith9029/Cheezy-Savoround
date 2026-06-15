using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// UIManager runs AFTER GameManager so it can safely subscribe and receive the initial state broadcast
[DefaultExecutionOrder(10)]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Button hudHomeButton;
    [SerializeField] private Button hudReplayButton;
    [SerializeField] private Button hudPlusButton;

    [Header("Panel References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject dailyRewardPanel;
    [SerializeField] private GameObject achievementPanel;
    
    [Header("GDD Section 5 Panel References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject coinShopPanel;

    [Header("Level Progress References")]
    [SerializeField] private Slider levelProgressSlider;
    [SerializeField] private TextMeshProUGUI levelCurrentText;
    [SerializeField] private TextMeshProUGUI levelNextText;

    [Header("MainMenu Panel References")]
    [SerializeField] private Button mmPlayButton;
    [SerializeField] private Button mmHowToPlayButton;
    [SerializeField] private Button mmShopButton;
    [SerializeField] private Button mmDailyButton;
    [SerializeField] private Button mmAchievementButton;

    [Header("Replay Confirm Panel")]
    [SerializeField] private GameObject replayConfirmPanel;
    [SerializeField] private Button replayConfirmYesButton;
    [SerializeField] private Button replayConfirmNoButton;
    [SerializeField] private TextMeshProUGUI replayConfirmMessageText;
    [SerializeField] private TMP_FontAsset replayConfirmFont;

    [Header("HowToPlay Panel References")]
    [SerializeField] private Button htpCloseButton;

    [Header("GameOver Panel References")]
    [SerializeField] private TextMeshProUGUI goScoreText;
    [SerializeField] private TextMeshProUGUI goBestScoreText;
    [SerializeField] private Button goHomeButton;
    [SerializeField] private Button goReplayButton;
    [SerializeField] private Button goWatchAdButton;
    [SerializeField] private GameObject goAdLoadingOverlay;

    [Header("CoinShop Panel References")]
    [SerializeField] private Button csBtnPack1000;
    [SerializeField] private Button csBtnPack5000;
    [SerializeField] private Button csBtnPack25000;
    [SerializeField] private Button csBtnPack50000;
    [SerializeField] private Button csCloseButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetupSubCanvases();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ResolveMainMenuSidebarButtons();
        EnsureReplayConfirmPanel();

        // Close all panels at startup
        CloseAllPanels();

        // Bind Button Listeners
        InitializeListeners();

        // Subscribe to score/gold/state/level events
        GameManager.OnScoreChanged += UpdateScoreText;
        GameManager.OnGoldChanged += UpdateGoldText;
        GameManager.OnStateChanged += HandleStateChanged;
        GameManager.OnLevelProgressChanged += UpdateLevelProgress;

        // Force initial update if managers exist
        if (GameManager.Instance != null)
        {
            UpdateScoreText(GameManager.Instance.Score);
            UpdateGoldText(GameManager.Instance.Gold);
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        GameManager.OnScoreChanged -= UpdateScoreText;
        GameManager.OnGoldChanged -= UpdateGoldText;
        GameManager.OnStateChanged -= HandleStateChanged;
        GameManager.OnLevelProgressChanged -= UpdateLevelProgress;
    }

    private void InitializeListeners()
    {
        // Main Menu
        if (mmPlayButton != null) mmPlayButton.onClick.AddListener(() => GameManager.Instance.StartGame());
        if (mmHowToPlayButton != null) mmHowToPlayButton.onClick.AddListener(() => OpenHowToPlay());
        if (mmShopButton != null) mmShopButton.onClick.AddListener(() => OpenShop());
        if (mmDailyButton != null) mmDailyButton.onClick.AddListener(() => OpenDailyReward());
        if (mmAchievementButton != null) mmAchievementButton.onClick.AddListener(() => OpenAchievements());

        // How To Play
        if (htpCloseButton != null) htpCloseButton.onClick.AddListener(() => { if (howToPlayPanel != null) howToPlayPanel.SetActive(false); });

        // HUD Controls
        if (hudHomeButton != null) hudHomeButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());
        if (hudReplayButton != null) hudReplayButton.onClick.AddListener(OnReplayButtonClicked);
        if (hudPlusButton != null) hudPlusButton.onClick.AddListener(() => OpenCoinShop());

        if (replayConfirmYesButton != null) replayConfirmYesButton.onClick.AddListener(ConfirmReplay);
        if (replayConfirmNoButton != null) replayConfirmNoButton.onClick.AddListener(CloseReplayConfirm);

        // Game Over
        if (goHomeButton != null) goHomeButton.onClick.AddListener(() => { if (gameOverPanel != null) gameOverPanel.SetActive(false); GameManager.Instance.ReturnToMenu(); });
        if (goReplayButton != null) goReplayButton.onClick.AddListener(() => { if (gameOverPanel != null) gameOverPanel.SetActive(false); GameManager.Instance.StartGame(); });
        if (goWatchAdButton != null) goWatchAdButton.onClick.AddListener(() => StartCoroutine(WatchAdRoutine()));

        // Coin Shop
        if (csBtnPack1000 != null) csBtnPack1000.onClick.AddListener(() => BuyCoins(1000));
        if (csBtnPack5000 != null) csBtnPack5000.onClick.AddListener(() => BuyCoins(5000));
        if (csBtnPack25000 != null) csBtnPack25000.onClick.AddListener(() => BuyCoins(25000));
        if (csBtnPack50000 != null) csBtnPack50000.onClick.AddListener(() => BuyCoins(50000));
        if (csCloseButton != null) csCloseButton.onClick.AddListener(() => { if (coinShopPanel != null) coinShopPanel.SetActive(false); });
    }

    private void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {score}";
        }
    }

    private void UpdateGoldText(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"{gold}";
        }
    }

    private void UpdateLevelProgress(int level, float percent)
    {
        if (levelProgressSlider != null)
            levelProgressSlider.value = percent;

        if (levelCurrentText != null)
            levelCurrentText.text = $"{level}";

        if (levelNextText != null)
            levelNextText.text = $"{level + 1}";
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Menu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                if (hudPanel != null) hudPanel.SetActive(false);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                CloseReplayConfirm();
                break;

            case GameState.Playing:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(true);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                break;

            case GameState.CheckingCombo:
            case GameState.Animating:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(true);
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                break;

            case GameState.GameOver:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(true);
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                CloseReplayConfirm();
                RefreshGameOverScores();
                break;
        }

        RefreshReplayButtonState();
    }

    private void Update()
    {
        if (hudReplayButton != null && hudReplayButton.gameObject.activeInHierarchy)
        {
            RefreshReplayButtonState();
        }
    }

    private void ResolveMainMenuSidebarButtons()
    {
        if (mainMenuPanel == null) return;

        Transform sidebar = mainMenuPanel.transform.Find("Sidebar");
        if (sidebar == null) return;

        if (mmShopButton == null) mmShopButton = FindButton(sidebar, "BtnShop");
        if (mmDailyButton == null) mmDailyButton = FindButton(sidebar, "BtnDaily");
        if (mmAchievementButton == null) mmAchievementButton = FindButton(sidebar, "BtnAchievement");
    }

    private static Button FindButton(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private void EnsureReplayConfirmPanel()
    {
        if (replayConfirmPanel != null) return;

        Transform canvas = transform;
        GameObject panel = new GameObject("ReplayConfirmPanel");
        panel.transform.SetParent(canvas, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject box = new GameObject("Box");
        box.transform.SetParent(panel.transform, false);
        RectTransform boxRect = box.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.12f, 0.38f);
        boxRect.anchorMax = new Vector2(0.88f, 0.62f);
        boxRect.sizeDelta = Vector2.zero;
        Image boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.18f, 0.12f, 0.1f, 0.95f);

        GameObject msgObj = new GameObject("Message");
        msgObj.transform.SetParent(box.transform, false);
        RectTransform msgRect = msgObj.AddComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.08f, 0.45f);
        msgRect.anchorMax = new Vector2(0.92f, 0.9f);
        msgRect.sizeDelta = Vector2.zero;
        replayConfirmMessageText = msgObj.AddComponent<TextMeshProUGUI>();
        replayConfirmMessageText.text = "Replay this round?\nYour current progress will be lost.";
        replayConfirmMessageText.fontSize = 30;
        if (replayConfirmFont != null) replayConfirmMessageText.font = replayConfirmFont;
        replayConfirmMessageText.alignment = TextAlignmentOptions.Center;
        replayConfirmMessageText.color = Color.white;

        replayConfirmNoButton = CreateConfirmButton(box.transform, "BtnNo", "Cancel", new Vector2(0.08f, 0.1f), new Vector2(0.46f, 0.35f), replayConfirmFont);
        replayConfirmYesButton = CreateConfirmButton(box.transform, "BtnYes", "Replay", new Vector2(0.54f, 0.1f), new Vector2(0.92f, 0.35f), replayConfirmFont);


        replayConfirmPanel = panel;
        panel.SetActive(false);
    }

    private static Button CreateConfirmButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font = null)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.35f, 0.15f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 28;
        if (font != null) txt.font = font;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        return btn;
    }

    private void OnReplayButtonClicked()
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.CanRequestReplay())
        {
            return;
        }

        if (replayConfirmPanel != null)
        {
            replayConfirmPanel.SetActive(true);
            replayConfirmPanel.transform.SetAsLastSibling();
        }
    }

    private void ConfirmReplay()
    {
        CloseReplayConfirm();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    private void CloseReplayConfirm()
    {
        if (replayConfirmPanel != null) replayConfirmPanel.SetActive(false);
    }

    private void RefreshReplayButtonState()
    {
        if (hudReplayButton == null || GameManager.Instance == null) return;
        hudReplayButton.interactable = GameManager.Instance.CanRequestReplay();
    }

    private void RefreshGameOverScores()
    {
        int currentScore = 0;
        if (GameManager.Instance != null)
        {
            currentScore = GameManager.Instance.Score;
        }

        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }

        if (goScoreText != null)
        {
            goScoreText.text = $"{currentScore}";
        }

        if (goBestScoreText != null)
        {
            goBestScoreText.text = $"{bestScore}";
        }
    }

    private IEnumerator WatchAdRoutine()
    {
        if (goHomeButton != null) goHomeButton.interactable = false;
        if (goReplayButton != null) goReplayButton.interactable = false;
        if (goWatchAdButton != null) goWatchAdButton.interactable = false;

        if (goAdLoadingOverlay != null) goAdLoadingOverlay.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        if (goAdLoadingOverlay != null) goAdLoadingOverlay.SetActive(false);

        if (goHomeButton != null) goHomeButton.interactable = true;
        if (goReplayButton != null) goReplayButton.interactable = true;
        if (goWatchAdButton != null) goWatchAdButton.interactable = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        MergeManager mergeManager = FindFirstObjectByType<MergeManager>();
        if (mergeManager != null)
        {
            mergeManager.DeleteThreeRandomPlates();
        }
        else
        {
            if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameState.Playing);
        }
    }

    private void BuyCoins(int amount)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(amount);
        }

        if (UserDataManager.Instance != null)
        {
            UserDataManager.Instance.SaveData();
        }
    }

    public void CloseAllPanels()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (dailyRewardPanel != null) dailyRewardPanel.SetActive(false);
        if (achievementPanel != null) achievementPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (coinShopPanel != null) coinShopPanel.SetActive(false);
        CloseReplayConfirm();
    }

    public void OpenShop(ShopCategory category = ShopCategory.Coin)
    {
        CloseAllPanels();
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            if (ShopUI.Instance != null) ShopUI.Instance.Open(category);
        }
    }

    public void OpenDailyReward()
    {
        CloseAllPanels();
        if (dailyRewardPanel != null)
        {
            dailyRewardPanel.SetActive(true);
            if (DailyRewardManager.Instance != null) DailyRewardManager.Instance.RefreshStatus();
        }
    }

    public void OpenAchievements()
    {
        CloseAllPanels();
        if (achievementPanel != null)
        {
            achievementPanel.SetActive(true);
            if (AchievementUI.Instance != null) AchievementUI.Instance.RefreshUI();
        }
    }

    public void OpenHowToPlay()
    {
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    public void OpenCoinShop()
    {
        OpenShop(ShopCategory.Coin);
    }

    private void SetupSubCanvases()
    {
        // Add Canvas and GraphicRaycaster components to make these act as independent Sub-Canvases.
        // This stops modifications to HUD elements (like score/gold updates) from forcing the rebuild of popup panels (like shop/achievements) and vice versa.
        
        // Isolate dynamic elements
        if (scoreText != null && scoreText.transform.parent != null)
        {
            IsolateToSubCanvas(scoreText.transform.parent.gameObject);
        }
        
        if (levelProgressSlider != null)
        {
            IsolateToSubCanvas(levelProgressSlider.gameObject);
        }

        // Isolate panels
        IsolateToSubCanvas(shopPanel);
        IsolateToSubCanvas(dailyRewardPanel);
        IsolateToSubCanvas(achievementPanel);
        IsolateToSubCanvas(mainMenuPanel);
        IsolateToSubCanvas(gameOverPanel);
        IsolateToSubCanvas(howToPlayPanel);
        IsolateToSubCanvas(coinShopPanel);
        IsolateToSubCanvas(replayConfirmPanel);
    }

    private void IsolateToSubCanvas(GameObject obj)
    {
        if (obj == null) return;
        
        Canvas canvas = obj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = obj.AddComponent<Canvas>();
            // Since it's a child of a root canvas, it inherits render settings.
            
            // Sub-canvases containing buttons or interactive items need GraphicRaycaster
            if (obj.GetComponentInChildren<Button>(true) != null || obj.GetComponent<Button>() != null)
            {
                if (obj.GetComponent<GraphicRaycaster>() == null)
                {
                    obj.AddComponent<GraphicRaycaster>();
                }
            }
        }
    }
}
