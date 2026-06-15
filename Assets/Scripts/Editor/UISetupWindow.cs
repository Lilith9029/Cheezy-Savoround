using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public class UISetupWindow : EditorWindow
{
    [MenuItem("Cheezy Savoround/Fix Scene Duplicates")]
    public static void FixSceneDuplicates()
    {
        CleanupDuplicateGameplayObjects();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Cheezy Savoround/Setup UI")]
    public static void SetupUI()
    {
        CleanupDuplicateGameplayObjects();

        // 1. Create or Find MetaManagers GameObject
        GameObject managersObj = GameObject.Find("MetaManagers");
        if (managersObj == null)
        {
            managersObj = new GameObject("MetaManagers");
            Undo.RegisterCreatedObjectUndo(managersObj, "Create MetaManagers");
        }

        // Add manager components if not present
        if (managersObj.GetComponent<UserDataManager>() == null) managersObj.AddComponent<UserDataManager>();
        if (managersObj.GetComponent<ShopManager>() == null) managersObj.AddComponent<ShopManager>();
        if (managersObj.GetComponent<DailyRewardManager>() == null) managersObj.AddComponent<DailyRewardManager>();
        if (managersObj.GetComponent<AchievementManager>() == null) managersObj.AddComponent<AchievementManager>();
        if (managersObj.GetComponent<BoosterManager>() == null) managersObj.AddComponent<BoosterManager>();
        // GameManager & MergeManager must be on always-active objects, NOT on UI panels
        if (Object.FindFirstObjectByType<GameManager>() == null)
            managersObj.AddComponent<GameManager>();
        if (Object.FindFirstObjectByType<MergeManager>() == null)
            managersObj.AddComponent<MergeManager>();

        // 1b. Gameplay controllers must live under --- GM --- (never on UI / never duplicate GameControllers)
        EnsureGameplayControllers();

        // 2. Setup EventSystem
        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            
            System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModuleType != null)
            {
                eventSystem.AddComponent(newModuleType);
            }
            else
            {
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
        else
        {
            System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModuleType != null && eventSystem.GetComponent("InputSystemUIInputModule") == null)
            {
                var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null) DestroyImmediate(oldModule);
                eventSystem.AddComponent(newModuleType);
            }
        }

        // 3. Create or Find Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        Canvas canvas = null;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Add UIManager to Canvas
        UIManager uiManager = canvasObj.GetComponent<UIManager>();
        if (uiManager == null) uiManager = canvasObj.AddComponent<UIManager>();

        // Load Sprites and auto-fix texture import settings
        Sprite btnShopSprite = LoadSprite("Assets/UI/bt/bt shop.png");
        Sprite btnDailySprite = LoadSprite("Assets/UI/bt/bt daily.png");
        Sprite btnAchievementSprite = LoadSprite("Assets/UI/bt/bt achievement.png");
        Sprite btnReplaySprite = LoadSprite("Assets/UI/bt/bt replay.png");
        Sprite btnCoinSprite = LoadSprite("Assets/UI/bt/bt coin.png");
        Sprite btnHomeSprite = LoadSprite("Assets/UI/bt/bt home.png");
        Sprite btnPlusSprite = LoadSprite("Assets/UI/bt/bt +.png");
        Sprite btnPlaySprite = LoadSprite("Assets/UI/bt/bt start.png");
        Sprite btnHtpSprite = LoadSprite("Assets/UI/bt/bt htp.png");
        Sprite logoSprite = LoadSprite("Assets/UI/lg/logo.png");
        Sprite bgSprite = LoadSprite("Assets/UI/bg/bg.png");
        Sprite closeSprite = LoadSprite("Assets/UI/bt/bt turn off.png");
        Sprite watchAdSprite = LoadSprite("Assets/UI/bt/bt watch ad.png");
        Sprite puLoseSprite = LoadSprite("Assets/UI/pu/pu game over/pu lose.png");
        Sprite cutterSprite = LoadSprite("Assets/UI/bt/cutter.png");
        Sprite sauceSprite = LoadSprite("Assets/UI/bt/bottle of sauce.png");
        Sprite boardSprite = LoadSprite("Assets/UI/bt/bt boosters.png");
        Sprite trashSprite = LoadSprite("Assets/UI/bt/trash can.png");

        // 4. Create HUD
        GameObject hudObj = FindOrCreateChild(canvasObj, "HUD");
        RectTransform hudRect = hudObj.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;

        // HUD Top Bar
        GameObject topBar = FindOrCreateChild(hudObj, "TopBar");
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 0.9f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.sizeDelta = Vector2.zero;

        // Left Home Button
        GameObject btnHomeHUD = FindOrCreateChild(topBar, "BtnHome");
        SetupButtonGraphic(btnHomeHUD, btnHomeSprite, new Vector2(0.02f, 0.15f), new Vector2(0.12f, 0.85f));
        Button homeBtnComp = btnHomeHUD.GetComponent<Button>();

        // Score Text
        GameObject scoreTextObj = FindOrCreateChild(topBar, "ScoreText");
        TextMeshProUGUI scoreText = scoreTextObj.GetComponent<TextMeshProUGUI>();
        if (scoreText == null) scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "SCORE: 0";
        scoreText.fontSize = 42;
        scoreText.alignment = TextAlignmentOptions.Center;
        RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.15f, 0.2f);
        scoreRect.anchorMax = new Vector2(0.45f, 0.8f);
        scoreRect.sizeDelta = Vector2.zero;

        // Gold Container
        GameObject goldContainer = FindOrCreateChild(topBar, "GoldContainer");
        RectTransform goldRect = goldContainer.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0.50f, 0.2f);
        goldRect.anchorMax = new Vector2(0.85f, 0.8f);
        goldRect.sizeDelta = Vector2.zero;
        
        GameObject goldIconObj = FindOrCreateChild(goldContainer, "GoldIcon");
        Image goldIcon = goldIconObj.GetComponent<Image>();
        if (goldIcon == null) goldIcon = goldIconObj.AddComponent<Image>();
        if (btnCoinSprite != null) goldIcon.sprite = btnCoinSprite;
        RectTransform goldIconRect = goldIconObj.GetComponent<RectTransform>();
        goldIconRect.anchorMin = new Vector2(0f, 0f);
        goldIconRect.anchorMax = new Vector2(0.25f, 1f);
        goldIconRect.sizeDelta = Vector2.zero;

        GameObject goldTextObj = FindOrCreateChild(goldContainer, "GoldText");
        TextMeshProUGUI goldText = goldTextObj.GetComponent<TextMeshProUGUI>();
        if (goldText == null) goldText = goldTextObj.AddComponent<TextMeshProUGUI>();
        goldText.text = "0";
        goldText.fontSize = 42;
        goldText.alignment = TextAlignmentOptions.Left;
        RectTransform goldTextRect = goldTextObj.GetComponent<RectTransform>();
        goldTextRect.anchorMin = new Vector2(0.28f, 0f);
        goldTextRect.anchorMax = new Vector2(0.75f, 1f);
        goldTextRect.sizeDelta = Vector2.zero;

        // "+" Button for Coin Shop
        GameObject btnPlusIAP = FindOrCreateChild(goldContainer, "BtnPlus");
        SetupButtonGraphic(btnPlusIAP, btnPlusSprite, new Vector2(0.78f, 0.05f), new Vector2(0.98f, 0.95f));
        Button plusBtnComp = btnPlusIAP.GetComponent<Button>();

        // Right Replay Button
        GameObject btnReplayHUD = FindOrCreateChild(topBar, "BtnReplay");
        SetupButtonGraphic(btnReplayHUD, btnReplaySprite, new Vector2(0.88f, 0.15f), new Vector2(0.98f, 0.85f));
        Button replayBtnComp = btnReplayHUD.GetComponent<Button>();

        // Level Progression bar (below top bar)
        GameObject levelProgressObj = FindOrCreateChild(hudObj, "LevelProgressContainer");
        RectTransform lpRect = levelProgressObj.GetComponent<RectTransform>();
        lpRect.anchorMin = new Vector2(0.15f, 0.83f);
        lpRect.anchorMax = new Vector2(0.85f, 0.89f);
        lpRect.sizeDelta = Vector2.zero;

        // Level Number text
        GameObject levelTextObj = FindOrCreateChild(levelProgressObj, "LevelText");
        TextMeshProUGUI levelText = levelTextObj.GetComponent<TextMeshProUGUI>();
        if (levelText == null) levelText = levelTextObj.AddComponent<TextMeshProUGUI>();
        levelText.text = "LEVEL 1";
        levelText.fontSize = 28;
        levelText.alignment = TextAlignmentOptions.Center;
        RectTransform levelTextRect = levelTextObj.GetComponent<RectTransform>();
        levelTextRect.anchorMin = new Vector2(0f, 0.65f);
        levelTextRect.anchorMax = new Vector2(1f, 1f);
        levelTextRect.sizeDelta = Vector2.zero;

        // Level Slider
        GameObject levelSliderObj = FindOrCreateChild(levelProgressObj, "Slider");
        Slider levelSlider = levelSliderObj.GetComponent<Slider>();
        if (levelSlider == null) levelSlider = levelSliderObj.AddComponent<Slider>();
        RectTransform levelSliderRect = levelSliderObj.GetComponent<RectTransform>();
        levelSliderRect.anchorMin = new Vector2(0f, 0f);
        levelSliderRect.anchorMax = new Vector2(1f, 0.6f);
        levelSliderRect.sizeDelta = Vector2.zero;

        GameObject sliderBg = FindOrCreateChild(levelSliderObj, "Background");
        Image sBgImg = sliderBg.GetComponent<Image>();
        if (sBgImg == null) sBgImg = sliderBg.AddComponent<Image>();
        sBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform sBgRect = sliderBg.GetComponent<RectTransform>();
        sBgRect.anchorMin = Vector2.zero;
        sBgRect.anchorMax = Vector2.one;
        sBgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = FindOrCreateChild(levelSliderObj, "Fill Area");
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fill = FindOrCreateChild(fillArea, "Fill");
        Image fillImg = fill.GetComponent<Image>();
        if (fillImg == null) fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.95f, 0.7f, 0.1f); // Rich gold color
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f); // starts empty
        fillRect.sizeDelta = Vector2.zero;

        levelSlider.fillRect = fillRect;

        // HUD Bottom Toolbar (Boosters)
        GameObject toolbar = FindOrCreateChild(hudObj, "Toolbar");
        RectTransform toolbarRect = toolbar.GetComponent<RectTransform>();
        toolbarRect.anchorMin = new Vector2(0.15f, 0.02f);
        toolbarRect.anchorMax = new Vector2(0.85f, 0.12f);
        toolbarRect.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup horizLayout = toolbar.GetComponent<HorizontalLayoutGroup>();
        if (horizLayout == null) horizLayout = toolbar.AddComponent<HorizontalLayoutGroup>();
        horizLayout.spacing = 25;
        horizLayout.childAlignment = TextAnchor.MiddleCenter;
        horizLayout.childControlHeight = false;
        horizLayout.childControlWidth = false;

        SetupBoosterButton(toolbar, "Tool_Cutter", cutterSprite, "Dao Cắt");
        SetupBoosterButton(toolbar, "Tool_Sauce", sauceSprite, "Chai Sốt");
        SetupBoosterButton(toolbar, "Tool_Board", boardSprite, "Thớt Gỗ");
        SetupBoosterButton(toolbar, "Tool_Trash", trashSprite, "Thùng Rác");

        // 5. Create Main Menu Panel
        GameObject mainMenuPanelObj = FindOrCreateChild(canvasObj, "MainMenuPanel");
        RectTransform mmPanelRect = mainMenuPanelObj.GetComponent<RectTransform>();
        mmPanelRect.anchorMin = Vector2.zero;
        mmPanelRect.anchorMax = Vector2.one;
        mmPanelRect.sizeDelta = Vector2.zero;

        Image mmBg = mainMenuPanelObj.GetComponent<Image>();
        if (mmBg == null) mmBg = mainMenuPanelObj.AddComponent<Image>();
        if (bgSprite != null)
        {
            mmBg.sprite = bgSprite;
            mmBg.color = Color.white;
        }
        else
        {
            mmBg.color = new Color(0.15f, 0.1f, 0.08f);
        }

        GameObject logoObj = FindOrCreateChild(mainMenuPanelObj, "Logo");
        Image logoImg = logoObj.GetComponent<Image>();
        if (logoImg == null) logoImg = logoObj.AddComponent<Image>();
        if (logoSprite != null)
        {
            logoImg.sprite = logoSprite;
            logoImg.color = Color.white;
        }
        else
        {
            logoImg.color = Color.yellow;
        }
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.2f, 0.55f);
        logoRect.anchorMax = new Vector2(0.8f, 0.85f);
        logoRect.sizeDelta = Vector2.zero;
        
        AspectRatioFitter aspectFitter = logoObj.GetComponent<AspectRatioFitter>();
        if (aspectFitter == null) aspectFitter = logoObj.AddComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = 1.33f;

        GameObject btnPlay = FindOrCreateChild(mainMenuPanelObj, "PlayButton");
        SetupButtonGraphic(btnPlay, btnPlaySprite, new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.48f));
        Button playButtonComp = btnPlay.GetComponent<Button>();

        GameObject btnHtp = FindOrCreateChild(mainMenuPanelObj, "HowToPlayButton");
        SetupButtonGraphic(btnHtp, btnHtpSprite, new Vector2(0.32f, 0.20f), new Vector2(0.68f, 0.31f));
        Button htpButtonComp = btnHtp.GetComponent<Button>();

        // Main Menu Sidebar (Shop / Daily / Achievement)
        GameObject sidebar = FindOrCreateChild(mainMenuPanelObj, "Sidebar");
        RectTransform sidebarRect = sidebar.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(0.79f, 0f);
        sidebarRect.anchorMax = new Vector2(1f, 1f);
        sidebarRect.sizeDelta = Vector2.zero;

        Button mmShopBtnComp = SetupHUDButton(sidebar, "BtnShop", btnShopSprite, new Vector2(0f, 0.75f), new Vector2(1f, 0.95f), () => uiManager.OpenShop());
        Button mmDailyBtnComp = SetupHUDButton(sidebar, "BtnDaily", btnDailySprite, new Vector2(0f, 0.50f), new Vector2(1f, 0.70f), () => uiManager.OpenDailyReward());
        Button mmAchBtnComp = SetupHUDButton(sidebar, "BtnAchievement", btnAchievementSprite, new Vector2(0f, 0.25f), new Vector2(1f, 0.45f), () => uiManager.OpenAchievements());

        // 6. Create How To Play Panel
        GameObject htpPanelObj = FindOrCreateChild(canvasObj, "HowToPlayPanel");
        RectTransform htpPanelRect = htpPanelObj.GetComponent<RectTransform>();
        htpPanelRect.anchorMin = Vector2.zero;
        htpPanelRect.anchorMax = Vector2.one;
        htpPanelRect.sizeDelta = Vector2.zero;

        Image htpTintBg = htpPanelObj.GetComponent<Image>();
        if (htpTintBg == null) htpTintBg = htpPanelObj.AddComponent<Image>();
        htpTintBg.color = new Color(0, 0, 0, 0.75f);

        GameObject htpContainer = FindOrCreateChild(htpPanelObj, "Container");
        RectTransform htpContRect = htpContainer.GetComponent<RectTransform>();
        htpContRect.anchorMin = new Vector2(0.15f, 0.25f);
        htpContRect.anchorMax = new Vector2(0.85f, 0.75f);
        htpContRect.sizeDelta = Vector2.zero;

        Image htpContImg = htpContainer.GetComponent<Image>();
        if (htpContImg == null) htpContImg = htpContainer.AddComponent<Image>();
        htpContImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        GameObject htpTitle = FindOrCreateChild(htpContainer, "TitleText");
        SetText(htpTitle, "CÁCH CHƠI", 44, TextAlignmentOptions.Center);
        RectTransform htpTitleRect = htpTitle.GetComponent<RectTransform>();
        htpTitleRect.anchorMin = new Vector2(0.1f, 0.82f);
        htpTitleRect.anchorMax = new Vector2(0.9f, 0.95f);
        htpTitleRect.sizeDelta = Vector2.zero;

        GameObject htpInstructions = FindOrCreateChild(htpContainer, "InstructionText");
        SetText(htpInstructions, "<b>1. Kéo & Thả:</b> Kéo các đĩa pizza từ kệ thớt bên dưới lên bảng lưới.\n\n" +
                                   "<b>2. Ghép Pizza:</b> Các miếng bánh pizza cùng màu nằm cạnh nhau (ngang hoặc dọc) sẽ tự động dồn về một đĩa.\n\n" +
                                   "<b>3. Hoàn thành:</b> Tạo thành đĩa pizza đầy đủ <b>6 miếng</b> cùng loại để nổ đĩa, nhận vàng và điểm số combo!\n\n" +
                                   "<b>4. Hỗ trợ:</b> Sử dụng thanh công cụ cuối màn hình để cắt bánh, rưới sốt, di chuyển đĩa hoặc vứt bỏ đĩa kẹt.", 28, TextAlignmentOptions.Left);
        RectTransform htpInstrRect = htpInstructions.GetComponent<RectTransform>();
        htpInstrRect.anchorMin = new Vector2(0.08f, 0.18f);
        htpInstrRect.anchorMax = new Vector2(0.92f, 0.80f);
        htpInstrRect.sizeDelta = Vector2.zero;

        GameObject htpCloseBtn = SetupButton(htpContainer, "CloseButton", "ĐÓNG", new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.13f));
        Button htpCloseBtnComp = htpCloseBtn.GetComponent<Button>();

        // 7. Create Game Over Panel
        GameObject gameOverPanelObj = FindOrCreateChild(canvasObj, "GameOverPanel");
        RectTransform goPanelRect = gameOverPanelObj.GetComponent<RectTransform>();
        goPanelRect.anchorMin = Vector2.zero;
        goPanelRect.anchorMax = Vector2.one;
        goPanelRect.sizeDelta = Vector2.zero;

        Image goTintBg = gameOverPanelObj.GetComponent<Image>();
        if (goTintBg == null) goTintBg = gameOverPanelObj.AddComponent<Image>();
        goTintBg.color = new Color(0, 0, 0, 0.8f);

        GameObject goContainer = FindOrCreateChild(gameOverPanelObj, "Container");
        RectTransform goContRect = goContainer.GetComponent<RectTransform>();
        goContRect.anchorMin = new Vector2(0.15f, 0.2f);
        goContRect.anchorMax = new Vector2(0.85f, 0.8f);
        goContRect.sizeDelta = Vector2.zero;

        Image goContImg = goContainer.GetComponent<Image>();
        if (goContImg == null) goContImg = goContainer.AddComponent<Image>();
        if (puLoseSprite != null)
        {
            goContImg.sprite = puLoseSprite;
            goContImg.color = Color.white;
        }
        else
        {
            goContImg.color = new Color(0.2f, 0.08f, 0.08f, 0.95f);
        }

        GameObject goTitle = FindOrCreateChild(goContainer, "Title");
        SetText(goTitle, "THẤT BẠI", 52, TextAlignmentOptions.Center);
        RectTransform goTitleRect = goTitle.GetComponent<RectTransform>();
        goTitleRect.anchorMin = new Vector2(0.1f, 0.84f);
        goTitleRect.anchorMax = new Vector2(0.9f, 0.96f);
        goTitleRect.sizeDelta = Vector2.zero;

        GameObject scoreTextObjGO = FindOrCreateChild(goContainer, "ScoreText");
        TextMeshProUGUI scoreTextGO = scoreTextObjGO.GetComponent<TextMeshProUGUI>();
        if (scoreTextGO == null) scoreTextGO = scoreTextObjGO.AddComponent<TextMeshProUGUI>();
        scoreTextGO.text = "SCORE\n0";
        scoreTextGO.fontSize = 38;
        scoreTextGO.alignment = TextAlignmentOptions.Center;
        RectTransform scoreRectGO = scoreTextObjGO.GetComponent<RectTransform>();
        scoreRectGO.anchorMin = new Vector2(0.1f, 0.58f);
        scoreRectGO.anchorMax = new Vector2(0.9f, 0.78f);
        scoreRectGO.sizeDelta = Vector2.zero;

        GameObject bestTextObjGO = FindOrCreateChild(goContainer, "BestScoreText");
        TextMeshProUGUI bestTextGO = bestTextObjGO.GetComponent<TextMeshProUGUI>();
        if (bestTextGO == null) bestTextGO = bestTextObjGO.AddComponent<TextMeshProUGUI>();
        bestTextGO.text = "BEST SCORE\n0";
        bestTextGO.fontSize = 38;
        bestTextGO.alignment = TextAlignmentOptions.Center;
        RectTransform bestRectGO = bestTextObjGO.GetComponent<RectTransform>();
        bestRectGO.anchorMin = new Vector2(0.1f, 0.38f);
        bestRectGO.anchorMax = new Vector2(0.9f, 0.56f);
        bestRectGO.sizeDelta = Vector2.zero;

        GameObject btnWatchAd = FindOrCreateChild(goContainer, "WatchAdButton");
        SetupButtonGraphic(btnWatchAd, watchAdSprite, new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.35f));
        Button watchAdBtnComp = btnWatchAd.GetComponent<Button>();

        GameObject btnReplayGO = FindOrCreateChild(goContainer, "ReplayButton");
        SetupButtonGraphic(btnReplayGO, btnReplaySprite, new Vector2(0.55f, 0.06f), new Vector2(0.75f, 0.18f));
        Button replayGOBtnComp = btnReplayGO.GetComponent<Button>();

        GameObject btnHomeGO = FindOrCreateChild(goContainer, "HomeButton");
        SetupButtonGraphic(btnHomeGO, btnHomeSprite, new Vector2(0.25f, 0.06f), new Vector2(0.45f, 0.18f));
        Button homeGOBtnComp = btnHomeGO.GetComponent<Button>();

        GameObject adOverlayObj = FindOrCreateChild(gameOverPanelObj, "AdLoadingOverlay");
        RectTransform adOverlayRect = adOverlayObj.GetComponent<RectTransform>();
        adOverlayRect.anchorMin = Vector2.zero;
        adOverlayRect.anchorMax = Vector2.one;
        adOverlayRect.sizeDelta = Vector2.zero;

        Image adOverlayImg = adOverlayObj.GetComponent<Image>();
        if (adOverlayImg == null) adOverlayImg = adOverlayObj.AddComponent<Image>();
        adOverlayImg.color = new Color(0, 0, 0, 0.92f);

        GameObject adLoadingText = FindOrCreateChild(adOverlayObj, "LoadingText");
        SetText(adLoadingText, "ĐANG TẢI QUẢNG CÁO...\nVUI LÒNG ĐỢI GIÂY LÁT", 36, TextAlignmentOptions.Center);
        RectTransform adTextRect = adLoadingText.GetComponent<RectTransform>();
        adTextRect.anchorMin = new Vector2(0.1f, 0.4f);
        adTextRect.anchorMax = new Vector2(0.9f, 0.6f);
        adTextRect.sizeDelta = Vector2.zero;

        // 8. Create Coin Shop Panel
        GameObject coinShopPanelObj = FindOrCreateChild(canvasObj, "CoinShopPanel");
        RectTransform csPanelRect = coinShopPanelObj.GetComponent<RectTransform>();
        csPanelRect.anchorMin = Vector2.zero;
        csPanelRect.anchorMax = Vector2.one;
        csPanelRect.sizeDelta = Vector2.zero;

        Image csTintBg = coinShopPanelObj.GetComponent<Image>();
        if (csTintBg == null) csTintBg = coinShopPanelObj.AddComponent<Image>();
        csTintBg.color = new Color(0, 0, 0, 0.75f);

        GameObject csContainer = FindOrCreateChild(coinShopPanelObj, "Background");
        RectTransform csContRect = csContainer.GetComponent<RectTransform>();
        csContRect.anchorMin = new Vector2(0.15f, 0.15f);
        csContRect.anchorMax = new Vector2(0.85f, 0.85f);
        csContRect.sizeDelta = Vector2.zero;

        Image csContImg = csContainer.GetComponent<Image>();
        if (csContImg == null) csContImg = csContainer.AddComponent<Image>();
        csContImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        GameObject csTitle = FindOrCreateChild(csContainer, "Title");
        SetText(csTitle, "CỬA HÀNG VÀNG", 48, TextAlignmentOptions.Center);
        RectTransform csTitleRect = csTitle.GetComponent<RectTransform>();
        csTitleRect.anchorMin = new Vector2(0.1f, 0.85f);
        csTitleRect.anchorMax = new Vector2(0.9f, 0.95f);
        csTitleRect.sizeDelta = Vector2.zero;

        GameObject csCloseBtn = SetupButton(csContainer, "CloseButton", "Đóng", new Vector2(0.4f, 0.03f), new Vector2(0.6f, 0.09f));
        Button csCloseBtnComp = csCloseBtn.GetComponent<Button>();

        GameObject coinGrid = FindOrCreateChild(csContainer, "Grid");
        RectTransform cgRect = coinGrid.GetComponent<RectTransform>();
        cgRect.anchorMin = new Vector2(0.1f, 0.15f);
        cgRect.anchorMax = new Vector2(0.9f, 0.80f);
        cgRect.sizeDelta = Vector2.zero;

        GridLayoutGroup coinGridG = coinGrid.GetComponent<GridLayoutGroup>();
        if (coinGridG == null) coinGridG = coinGrid.AddComponent<GridLayoutGroup>();
        coinGridG.cellSize = new Vector2(240, 220);
        coinGridG.spacing = new Vector2(30, 30);
        coinGridG.startAxis = GridLayoutGroup.Axis.Horizontal;
        coinGridG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        coinGridG.constraintCount = 2;
        coinGridG.childAlignment = TextAnchor.MiddleCenter;

        GameObject card1 = SetupCoinCard(coinGrid, "Pack1", "1,000 VÀNG", "$1.99", btnCoinSprite);
        GameObject card2 = SetupCoinCard(coinGrid, "Pack2", "5,000 VÀNG", "$7.99", btnCoinSprite);
        GameObject card3 = SetupCoinCard(coinGrid, "Pack3", "25,000 VÀNG", "$29.99", btnCoinSprite);
        GameObject card4 = SetupCoinCard(coinGrid, "Pack4", "50,000 VÀNG", "$49.99", btnCoinSprite);

        // 9. Setup Shop Panel (Carousel: Coin / Skin / Booster)
        Sprite shopCoinBg = LoadSprite("Assets/UI/pu/pu shop/coin/Asset 54.png");
        Sprite shopSkinBg = LoadSprite("Assets/UI/pu/pu shop/skin/Asset 62.png");
        Sprite shopBoosterBg = LoadSprite("Assets/UI/pu/pu shop/boosters/Asset 44.png");
        Sprite btnLeftSprite = LoadSprite("Assets/UI/bt/left.png");
        Sprite btnRightSprite = LoadSprite("Assets/UI/bt/right.png");
        Sprite btnBuySprite = LoadSprite("Assets/UI/bt/bt buy.png");
        Sprite btnCloseSprite = LoadSprite("Assets/UI/bt/bt turn off.png");
        Sprite goldBarSprite = LoadSprite("Assets/UI/pu/pu shop/coin/Asset 45.png");

        GameObject shopPanelObj = CreateOverlayPanel(canvasObj, "ShopPanel");
        ShopUI shopUI = shopPanelObj.GetComponent<ShopUI>();
        if (shopUI == null) shopUI = shopPanelObj.AddComponent<ShopUI>();

        GameObject shopBg = FindOrCreateChild(shopPanelObj, "Background");
        RectTransform shopBgRect = shopBg.GetComponent<RectTransform>();
        shopBgRect.anchorMin = new Vector2(0.08f, 0.12f);
        shopBgRect.anchorMax = new Vector2(0.92f, 0.88f);
        shopBgRect.sizeDelta = Vector2.zero;
        Image shopBgImg = shopBg.GetComponent<Image>();
        if (shopBgImg == null) shopBgImg = shopBg.AddComponent<Image>();
        if (shopCoinBg != null) shopBgImg.sprite = shopCoinBg;
        shopBgImg.color = Color.white;

        GameObject shopCloseBtn = FindOrCreateChild(shopBg, "CloseButton");
        SetupButtonGraphic(shopCloseBtn, btnCloseSprite, new Vector2(0.88f, 0.88f), new Vector2(0.98f, 0.98f));
        Button shopCloseBtnComp = shopCloseBtn.GetComponent<Button>();

        GameObject goldBar = FindOrCreateChild(shopBg, "GoldBar");
        SetupButtonGraphic(goldBar, goldBarSprite, new Vector2(0.55f, 0.82f), new Vector2(0.95f, 0.9f));
        GameObject shopGoldTextObj = FindOrCreateChild(goldBar, "GoldText");
        TextMeshProUGUI shopGoldText = shopGoldTextObj.GetComponent<TextMeshProUGUI>();
        if (shopGoldText == null) shopGoldText = shopGoldTextObj.AddComponent<TextMeshProUGUI>();
        shopGoldText.text = "0";
        shopGoldText.fontSize = 28;
        shopGoldText.alignment = TextAlignmentOptions.Right;
        RectTransform gtRect = shopGoldTextObj.GetComponent<RectTransform>();
        gtRect.anchorMin = new Vector2(0.35f, 0.1f);
        gtRect.anchorMax = new Vector2(0.95f, 0.9f);
        gtRect.sizeDelta = Vector2.zero;

        GameObject priceTextObj = FindOrCreateChild(shopBg, "PriceText");
        TextMeshProUGUI priceText = priceTextObj.GetComponent<TextMeshProUGUI>();
        if (priceText == null) priceText = priceTextObj.AddComponent<TextMeshProUGUI>();
        priceText.text = "$1.99";
        priceText.fontSize = 30;
        priceText.alignment = TextAlignmentOptions.Right;
        RectTransform ptRect = priceTextObj.GetComponent<RectTransform>();
        ptRect.anchorMin = new Vector2(0.55f, 0.72f);
        ptRect.anchorMax = new Vector2(0.95f, 0.8f);
        ptRect.sizeDelta = Vector2.zero;

        GameObject itemNameObj = FindOrCreateChild(shopBg, "ItemNameText");
        SetText(itemNameObj, "Item", 26, TextAlignmentOptions.Center);
        RectTransform inRect = itemNameObj.GetComponent<RectTransform>();
        inRect.anchorMin = new Vector2(0.1f, 0.72f);
        inRect.anchorMax = new Vector2(0.5f, 0.8f);
        inRect.sizeDelta = Vector2.zero;

        GameObject itemIconObj = FindOrCreateChild(shopBg, "ItemIcon");
        Image itemIconImg = itemIconObj.GetComponent<Image>();
        if (itemIconImg == null) itemIconImg = itemIconObj.AddComponent<Image>();
        itemIconImg.color = Color.white;
        RectTransform iiRect = itemIconObj.GetComponent<RectTransform>();
        iiRect.anchorMin = new Vector2(0.28f, 0.32f);
        iiRect.anchorMax = new Vector2(0.72f, 0.68f);
        iiRect.sizeDelta = Vector2.zero;

        GameObject btnPrev = FindOrCreateChild(shopBg, "BtnPrev");
        SetupButtonGraphic(btnPrev, btnLeftSprite, new Vector2(0.02f, 0.38f), new Vector2(0.16f, 0.62f));
        Button btnPrevComp = btnPrev.GetComponent<Button>();

        GameObject btnNext = FindOrCreateChild(shopBg, "BtnNext");
        SetupButtonGraphic(btnNext, btnRightSprite, new Vector2(0.84f, 0.38f), new Vector2(0.98f, 0.62f));
        Button btnNextComp = btnNext.GetComponent<Button>();

        GameObject btnBuy = FindOrCreateChild(shopBg, "BtnBuy");
        SetupButtonGraphic(btnBuy, btnBuySprite, new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.18f));
        Button btnBuyComp = btnBuy.GetComponent<Button>();
        GameObject buyLabelObj = FindOrCreateChild(btnBuy, "Label");
        SetText(buyLabelObj, "", 1, TextAlignmentOptions.Center);
        buyLabelObj.SetActive(false);

        SerializedObject shopSer = new SerializedObject(shopUI);
        shopSer.FindProperty("backgroundImage").objectReferenceValue = shopBgImg;
        shopSer.FindProperty("itemIconImage").objectReferenceValue = itemIconImg;
        shopSer.FindProperty("itemNameText").objectReferenceValue = itemNameObj.GetComponent<TextMeshProUGUI>();
        shopSer.FindProperty("priceText").objectReferenceValue = priceText;
        shopSer.FindProperty("goldText").objectReferenceValue = shopGoldText;
        shopSer.FindProperty("prevButton").objectReferenceValue = btnPrevComp;
        shopSer.FindProperty("nextButton").objectReferenceValue = btnNextComp;
        shopSer.FindProperty("buyButton").objectReferenceValue = btnBuyComp;
        shopSer.FindProperty("closeButton").objectReferenceValue = shopCloseBtnComp;
        shopSer.FindProperty("coinBackground").objectReferenceValue = shopCoinBg;
        shopSer.FindProperty("skinBackground").objectReferenceValue = shopSkinBg;
        shopSer.FindProperty("boosterBackground").objectReferenceValue = shopBoosterBg;

        SerializedProperty iconList = shopSer.FindProperty("iconMappings");
        iconList.ClearArray();
        AddShopIconMapping(shopSer, iconList, "coin_57", LoadSprite("Assets/UI/pu/pu shop/coin/Asset 57.png"));
        AddShopIconMapping(shopSer, iconList, "coin_58", LoadSprite("Assets/UI/pu/pu shop/coin/Asset 58.png"));
        AddShopIconMapping(shopSer, iconList, "coin_59", LoadSprite("Assets/UI/pu/pu shop/coin/Asset 59.png"));
        AddShopIconMapping(shopSer, iconList, "coin_60", LoadSprite("Assets/UI/pu/pu shop/coin/Asset 60.png"));
        AddShopIconMapping(shopSer, iconList, "booster_48", LoadSprite("Assets/UI/pu/pu shop/boosters/Asset 48.png"));
        AddShopIconMapping(shopSer, iconList, "booster_49", LoadSprite("Assets/UI/pu/pu shop/boosters/Asset 49.png"));
        AddShopIconMapping(shopSer, iconList, "booster_50", LoadSprite("Assets/UI/pu/pu shop/boosters/Asset 50.png"));
        AddShopIconMapping(shopSer, iconList, "booster_51", LoadSprite("Assets/UI/pu/pu shop/boosters/Asset 51.png"));

        shopSer.ApplyModifiedProperties();

        // 10. Setup Daily Reward Panel
        GameObject dailyPanelObj = CreateOverlayPanel(canvasObj, "DailyRewardPanel");
        DailyRewardUI dailyUI = dailyPanelObj.GetComponent<DailyRewardUI>();
        if (dailyUI == null) dailyUI = dailyPanelObj.AddComponent<DailyRewardUI>();

        GameObject dailyBg = FindOrCreateChild(dailyPanelObj, "Background");
        GameObject dailyTitle = FindOrCreateChild(dailyBg, "Title");
        SetText(dailyTitle, "ĐIỂM DANH HÀNG NGÀY", 48);

        GameObject dailyStatus = FindOrCreateChild(dailyBg, "StatusText");
        SetText(dailyStatus, "SẴN SÀNG NHẬN THƯỞNG", 36, TextAlignmentOptions.Center);
        RectTransform dsRect = dailyStatus.GetComponent<RectTransform>();
        dsRect.anchorMin = new Vector2(0.1f, 0.65f);
        dailyStatus.GetComponent<RectTransform>().anchorMax = new Vector2(0.9f, 0.8f);
        dailyStatus.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject dailyAmount = FindOrCreateChild(dailyBg, "RewardAmountText");
        SetText(dailyAmount, "+100 VÀNG", 48, TextAlignmentOptions.Center);
        RectTransform daRect = dailyAmount.GetComponent<RectTransform>();
        daRect.anchorMin = new Vector2(0.1f, 0.45f);
        daRect.anchorMax = new Vector2(0.9f, 0.62f);
        daRect.sizeDelta = Vector2.zero;

        GameObject dailyInfo = FindOrCreateChild(dailyBg, "InfoLabel");
        SetText(dailyInfo, "Điểm danh hàng ngày để tích lũy vàng mua skins!", 24, TextAlignmentOptions.Center);
        RectTransform diRect = dailyInfo.GetComponent<RectTransform>();
        diRect.anchorMin = new Vector2(0.1f, 0.25f);
        diRect.anchorMax = new Vector2(0.9f, 0.42f);
        diRect.sizeDelta = Vector2.zero;

        GameObject dailyClaimBtn = SetupButton(dailyBg, "ClaimButton", "Nhận Thưởng", new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.2f));
        GameObject dailyCloseBtn = SetupButton(dailyBg, "CloseButton", "Đóng", new Vector2(0.4f, 0.03f), new Vector2(0.6f, 0.09f));

        SerializedObject dailySer = new SerializedObject(dailyUI);
        dailySer.FindProperty("statusText").objectReferenceValue = dailyStatus.GetComponent<TextMeshProUGUI>();
        dailySer.FindProperty("rewardAmountText").objectReferenceValue = dailyAmount.GetComponent<TextMeshProUGUI>();
        dailySer.FindProperty("claimButton").objectReferenceValue = dailyClaimBtn.GetComponent<Button>();
        dailySer.FindProperty("closeButton").objectReferenceValue = dailyCloseBtn.GetComponent<Button>();
        dailySer.FindProperty("infoLabel").objectReferenceValue = dailyInfo.GetComponent<TextMeshProUGUI>();
        dailySer.ApplyModifiedProperties();

        // 11. Setup Achievement Panel
        GameObject achPanelObj = CreateOverlayPanel(canvasObj, "AchievementPanel");
        AchievementUI achUI = achPanelObj.GetComponent<AchievementUI>();
        if (achUI == null) achUI = achPanelObj.AddComponent<AchievementUI>();

        GameObject achBg = FindOrCreateChild(achPanelObj, "Background");
        GameObject achTitle = FindOrCreateChild(achBg, "Title");
        SetText(achTitle, "NHIỆM VỤ TRỌN ĐỜI", 48);

        GameObject achCloseBtn = SetupButton(achBg, "CloseButton", "Đóng", new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.12f));
        GameObject achContainer = FindOrCreateChild(achBg, "Container");
        RectTransform achContainerRect = achContainer.GetComponent<RectTransform>();
        achContainerRect.anchorMin = new Vector2(0.1f, 0.2f);
        achContainerRect.anchorMax = new Vector2(0.9f, 0.8f);
        achContainerRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vLayout = achContainer.GetComponent<VerticalLayoutGroup>();
        if (vLayout == null) vLayout = achContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 15;
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandHeight = false;

        GameObject achItem = FindOrCreateChild(achContainer, "TemplateItem");
        RectTransform achItemRect = achItem.GetComponent<RectTransform>();
        achItemRect.sizeDelta = new Vector2(800, 100);
        Image itemImg = achItem.GetComponent<Image>();
        if (itemImg == null) itemImg = achItem.AddComponent<Image>();
        itemImg.color = new Color(0.25f, 0.25f, 0.25f, 0.9f);

        GameObject itemTitle = FindOrCreateChild(achItem, "TitleText");
        SetText(itemTitle, "Achievement Title", 24);
        RectTransform itRect = itemTitle.GetComponent<RectTransform>();
        itRect.anchorMin = new Vector2(0.05f, 0.6f);
        itRect.anchorMax = new Vector2(0.6f, 0.9f);
        itRect.sizeDelta = Vector2.zero;

        GameObject itemDesc = FindOrCreateChild(achItem, "DescriptionText");
        SetText(itemDesc, "Bloom 100 pizzas", 20);
        RectTransform idRect = itemDesc.GetComponent<RectTransform>();
        idRect.anchorMin = new Vector2(0.05f, 0.35f);
        idRect.anchorMax = new Vector2(0.6f, 0.58f);
        idRect.sizeDelta = Vector2.zero;

        GameObject itemProg = FindOrCreateChild(achItem, "ProgressText");
        SetText(itemProg, "0 / 100", 20, TextAlignmentOptions.Right);
        RectTransform ipRect = itemProg.GetComponent<RectTransform>();
        ipRect.anchorMin = new Vector2(0.65f, 0.6f);
        ipRect.anchorMax = new Vector2(0.95f, 0.9f);
        ipRect.sizeDelta = Vector2.zero;

        GameObject itemSliderObj = FindOrCreateChild(achItem, "ProgressBar");
        Slider slider = itemSliderObj.GetComponent<Slider>();
        if (slider == null) slider = itemSliderObj.AddComponent<Slider>();
        RectTransform isRect = itemSliderObj.GetComponent<RectTransform>();
        isRect.anchorMin = new Vector2(0.05f, 0.1f);
        isRect.anchorMax = new Vector2(0.85f, 0.28f);
        isRect.sizeDelta = Vector2.zero;
        
        GameObject achSliderBg = FindOrCreateChild(itemSliderObj, "Background");
        Image achBgImg = achSliderBg.GetComponent<Image>();
        if (achBgImg == null) achBgImg = achSliderBg.AddComponent<Image>();
        achBgImg.color = Color.gray;
        RectTransform achBgRect = achSliderBg.GetComponent<RectTransform>();
        achBgRect.anchorMin = Vector2.zero;
        achBgRect.anchorMax = Vector2.one;
        achBgRect.sizeDelta = Vector2.zero;

        GameObject achFillArea = FindOrCreateChild(itemSliderObj, "Fill Area");
        RectTransform achFillAreaRect = achFillArea.GetComponent<RectTransform>();
        achFillAreaRect.anchorMin = Vector2.zero;
        achFillAreaRect.anchorMax = Vector2.one;
        achFillAreaRect.sizeDelta = Vector2.zero;

        GameObject achFill = FindOrCreateChild(achFillArea, "Fill");
        Image achFillImg = achFill.GetComponent<Image>();
        if (achFillImg == null) achFillImg = achFill.AddComponent<Image>();
        achFillImg.color = Color.green;
        RectTransform achFillRect = achFill.GetComponent<RectTransform>();
        achFillRect.anchorMin = Vector2.zero;
        achFillRect.anchorMax = new Vector2(0.5f, 1f);
        achFillRect.sizeDelta = Vector2.zero;

        slider.fillRect = achFillRect;

        GameObject checkmark = FindOrCreateChild(achItem, "CheckmarkImage");
        Image checkImg = checkmark.GetComponent<Image>();
        if (checkImg == null) checkImg = checkmark.AddComponent<Image>();
        checkImg.color = Color.yellow;
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.88f, 0.2f);
        checkRect.anchorMax = new Vector2(0.96f, 0.8f);
        checkRect.sizeDelta = Vector2.zero;

        SerializedObject achSer = new SerializedObject(achUI);
        achSer.FindProperty("container").objectReferenceValue = achContainerRect;
        achSer.FindProperty("templateItem").objectReferenceValue = achItem;
        achSer.FindProperty("closeButton").objectReferenceValue = achCloseBtn.GetComponent<Button>();
        achSer.ApplyModifiedProperties();

        // 12. Bind all Canvas references to UIManager (Centralized Setup)
        SerializedObject uimSer = new SerializedObject(uiManager);
        uimSer.FindProperty("scoreText").objectReferenceValue = scoreText;
        uimSer.FindProperty("goldText").objectReferenceValue = goldText;
        uimSer.FindProperty("hudPanel").objectReferenceValue = hudObj;
        uimSer.FindProperty("hudHomeButton").objectReferenceValue = homeBtnComp;
        uimSer.FindProperty("hudReplayButton").objectReferenceValue = replayBtnComp;
        uimSer.FindProperty("hudPlusButton").objectReferenceValue = plusBtnComp;
        uimSer.FindProperty("shopPanel").objectReferenceValue = shopPanelObj;
        uimSer.FindProperty("dailyRewardPanel").objectReferenceValue = dailyPanelObj;
        uimSer.FindProperty("achievementPanel").objectReferenceValue = achPanelObj;
        uimSer.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanelObj;
        uimSer.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanelObj;
        uimSer.FindProperty("howToPlayPanel").objectReferenceValue = htpPanelObj;
        uimSer.FindProperty("coinShopPanel").objectReferenceValue = coinShopPanelObj;
        uimSer.FindProperty("levelProgressSlider").objectReferenceValue = levelSlider;
        uimSer.FindProperty("levelText").objectReferenceValue = levelText;
        
        // Centralized Menu refs
        uimSer.FindProperty("mmPlayButton").objectReferenceValue = playButtonComp;
        uimSer.FindProperty("mmHowToPlayButton").objectReferenceValue = htpButtonComp;
        uimSer.FindProperty("mmShopButton").objectReferenceValue = mmShopBtnComp;
        uimSer.FindProperty("mmDailyButton").objectReferenceValue = mmDailyBtnComp;
        uimSer.FindProperty("mmAchievementButton").objectReferenceValue = mmAchBtnComp;
        
        // Centralized HTP refs
        uimSer.FindProperty("htpCloseButton").objectReferenceValue = htpCloseBtnComp;

        // Centralized Game Over refs
        uimSer.FindProperty("goScoreText").objectReferenceValue = scoreTextGO;
        uimSer.FindProperty("goBestScoreText").objectReferenceValue = bestTextGO;
        uimSer.FindProperty("goHomeButton").objectReferenceValue = homeGOBtnComp;
        uimSer.FindProperty("goReplayButton").objectReferenceValue = replayGOBtnComp;
        uimSer.FindProperty("goWatchAdButton").objectReferenceValue = watchAdBtnComp;
        uimSer.FindProperty("goAdLoadingOverlay").objectReferenceValue = adOverlayObj;

        // Centralized Coin Shop refs
        uimSer.FindProperty("csBtnPack1000").objectReferenceValue = card1.GetComponent<Button>();
        uimSer.FindProperty("csBtnPack5000").objectReferenceValue = card2.GetComponent<Button>();
        uimSer.FindProperty("csBtnPack25000").objectReferenceValue = card3.GetComponent<Button>();
        uimSer.FindProperty("csBtnPack50000").objectReferenceValue = card4.GetComponent<Button>();
        uimSer.FindProperty("csCloseButton").objectReferenceValue = csCloseBtnComp;

        uimSer.ApplyModifiedProperties();

        // 13. Apply Plate prefab skin component
        string platePrefabPath = "Assets/Prefabs/SinglePlate.prefab";
        GameObject platePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(platePrefabPath);
        if (platePrefab != null)
        {
            GameObject instance = PrefabUtility.LoadPrefabContents(platePrefabPath);
            if (instance.GetComponent<PlateSkin>() == null)
            {
                instance.AddComponent<PlateSkin>();
                PrefabUtility.SaveAsPrefabAsset(instance, platePrefabPath);
            }
            PrefabUtility.UnloadPrefabContents(instance);
        }
        else
        {
        }

        // Apply clean scene changes saving
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Cheezy Savoround/Optimize Build Assets")]
    public static void OptimizeBuildAssets()
    {
        // 1. Enable GPU Instancing on all Materials in the project
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int matCount = 0;
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                matCount++;
            }
        }
        if (matCount > 0)
        {
            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.Log("[Optimize] All materials already have GPU Instancing enabled.");
        }

        // 2. Create and pack Sprite Atlas
        string atlasPath = "Assets/UI/CheezyUI.spriteatlas";
        UnityEngine.U2D.SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new UnityEngine.U2D.SpriteAtlas();
            
            /*UnityEngine.U2D.SpriteAtlasPackingSettings packSettings = new UnityEngine.U2D.SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            };
            atlas.SetPackingSettings(packSettings);

            UnityEngine.U2D.SpriteAtlasTextureSettings textureSettings = new UnityEngine.U2D.SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
            atlas.SetTextureSettings(textureSettings);*/

            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        Object uiFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/UI");
        if (uiFolder != null)
        {
            Object[] currentTargets = UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas);
            bool alreadyAdded = false;
            foreach (var target in currentTargets)
            {
                if (target == uiFolder)
                {
                    alreadyAdded = true;
                    break;
                }
            }
            if (!alreadyAdded)
            {
                UnityEditor.U2D.SpriteAtlasExtensions.Add(atlas, new Object[] { uiFolder });
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
        }

        // 3. Mark environmental assets and grid cells as Batching Static in scene
        int staticCount = 0;
        GameObject tableObj = GameObject.Find("Table");
        if (tableObj != null) { MarkStatic(tableObj, ref staticCount); }

        GameObject floorObj = GameObject.Find("Floor 1");
        if (floorObj == null) floorObj = GameObject.Find("Floor");
        if (floorObj != null) { MarkStatic(floorObj, ref staticCount); }

        GameObject lobbyObj = GameObject.Find("Lobby");
        if (lobbyObj != null) { MarkStatic(lobbyObj, ref staticCount); }

        GridManager grid = Object.FindFirstObjectByType<GridManager>();
        if (grid != null && grid.AllCells != null)
        {
            foreach (var cell in grid.AllCells)
            {
                if (cell != null)
                {
                    MarkStatic(cell.gameObject, ref staticCount);
                    if (cell.tilePrefabInstance != null)
                    {
                        MarkStatic(cell.tilePrefabInstance, ref staticCount);
                    }
                }
            }
        }

        if (staticCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("[Optimize] Static assets already configured.");
        }
    }

    private static void MarkStatic(GameObject obj, ref int count)
    {
        if (obj == null) return;
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(obj);
        if ((flags & StaticEditorFlags.BatchingStatic) == 0)
        {
            GameObjectUtility.SetStaticEditorFlags(obj, flags | StaticEditorFlags.BatchingStatic);
            count++;
        }
        // Recurse children
        foreach (Transform child in obj.transform)
        {
            MarkStatic(child.gameObject, ref count);
        }
    }

    private static void CleanupDuplicateGameplayObjects()
    {
        GameObject dupControllers = GameObject.Find("GameControllers");
        if (dupControllers != null)
        {
            Undo.DestroyObjectImmediate(dupControllers);
        }

        GameObject gmRoot = GameObject.Find("--- GM ---");
        if (gmRoot != null)
        {
            foreach (Transform child in gmRoot.transform)
            {
                if (child.name.TrimStart().TrimEnd().StartsWith("GameManager"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            if (GameObject.Find("MetaManagers")?.GetComponent<MergeManager>() != null)
            {
                MergeManager sceneMerge = gmRoot.GetComponentInChildren<MergeManager>(true);
                if (sceneMerge != null)
                {
                    Undo.DestroyObjectImmediate(sceneMerge.gameObject);
                }
            }
        }
    }

    private static void EnsureGameplayControllers()
    {
        GameObject gmRoot = GameObject.Find("--- GM ---");
        if (gmRoot == null)
        {
            gmRoot = new GameObject("--- GM ---");
            Undo.RegisterCreatedObjectUndo(gmRoot, "Create --- GM ---");
        }

        HoldSlotsManager hsm = Object.FindFirstObjectByType<HoldSlotsManager>();
        if (hsm == null)
        {
            GameObject hsmObj = new GameObject("HoldSlotsManager");
            Undo.RegisterCreatedObjectUndo(hsmObj, "Create HoldSlotsManager");
            hsmObj.transform.SetParent(gmRoot.transform);
            hsm = hsmObj.AddComponent<HoldSlotsManager>();
        }

        PlateSpawner spawner = hsm.plateSpawner;
        if (spawner == null) spawner = hsm.GetComponentInChildren<PlateSpawner>(true);
        if (spawner == null) spawner = Object.FindFirstObjectByType<PlateSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("PlateSpawner");
            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create PlateSpawner");
            spawnerObj.transform.SetParent(hsm.transform);
            spawner = spawnerObj.AddComponent<PlateSpawner>();
            hsm.plateSpawner = spawner;
        }

        if (Object.FindFirstObjectByType<GhostPlatePreview>() == null)
        {
            GameObject ghostObj = new GameObject("GhostPlatePreview");
            Undo.RegisterCreatedObjectUndo(ghostObj, "Create GhostPlatePreview");
            ghostObj.transform.SetParent(gmRoot.transform);
            ghostObj.AddComponent<GhostPlatePreview>();
        }

        if (Object.FindFirstObjectByType<ComboAudioPlayer>() == null)
        {
            GameObject audioObj = new GameObject("ComboAudioPlayer");
            Undo.RegisterCreatedObjectUndo(audioObj, "Create ComboAudioPlayer");
            audioObj.transform.SetParent(gmRoot.transform);
            audioObj.AddComponent<AudioSource>();
            audioObj.AddComponent<ComboAudioPlayer>();
        }

        if (Object.FindFirstObjectByType<ObjectPooler>() == null)
        {
            GameObject poolObj = new GameObject("ObjectPooler");
            Undo.RegisterCreatedObjectUndo(poolObj, "Create ObjectPooler");
            poolObj.transform.SetParent(gmRoot.transform);
            poolObj.AddComponent<ObjectPooler>();
        }

    }

    private static void AddShopIconMapping(SerializedObject shopSer, SerializedProperty iconList, string key, Sprite sprite)
    {
        if (sprite == null) return;
        int index = iconList.arraySize;
        iconList.InsertArrayElementAtIndex(index);
        SerializedProperty entry = iconList.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("iconKey").stringValue = key;
        entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
    }

    private static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject FindOrCreateChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) return child.gameObject;

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static Button SetupHUDButton(GameObject parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject btnObj = FindOrCreateChild(parent, name);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        if (img == null) img = btnObj.AddComponent<Image>();
        img.color = Color.white;
        if (sprite != null)
        {
            img.sprite = sprite;
        }

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClickAction);
        return btn;
    }

    private static void SetupButtonGraphic(GameObject obj, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        Image img = obj.GetComponent<Image>();
        if (img == null) img = obj.AddComponent<Image>();
        img.color = Color.white;
        if (sprite != null)
        {
            img.sprite = sprite;
        }
        else
        {
            img.color = Color.grey;
        }

        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.AddComponent<Button>();
    }

    private static GameObject SetupButton(GameObject parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject btnObj = FindOrCreateChild(parent, name);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        Image img = btnObj.GetComponent<Image>();
        if (img == null) img = btnObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.35f, 0.15f); // Beautiful orange theme button

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();

        GameObject txtObj = FindOrCreateChild(btnObj, "Text");
        SetText(txtObj, label, 24, TextAlignmentOptions.Center);
        RectTransform tRect = txtObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;

        return btnObj;
    }

    private static void SetupBoosterButton(GameObject parent, string name, Sprite sprite, string labelText)
    {
        GameObject btnObj = FindOrCreateChild(parent, name);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(110, 110); // Fixed size inside HorizontalLayoutGroup

        Image img = btnObj.GetComponent<Image>();
        if (img == null) img = btnObj.AddComponent<Image>();
        img.color = Color.white;
        if (sprite != null)
        {
            img.sprite = sprite;
        }
        else
        {
            img.color = Color.grey;
        }

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();

        // Text below item
        GameObject txtObj = FindOrCreateChild(btnObj, "Label");
        SetText(txtObj, labelText, 16, TextAlignmentOptions.Center);
        RectTransform tRect = txtObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, -0.3f);
        tRect.anchorMax = new Vector2(1f, 0f);
        tRect.sizeDelta = Vector2.zero;

        // Count badge in top-right corner (Week 4 UI optimization)
        GameObject badgeObj = FindOrCreateChild(btnObj, "CountBadge");
        TextMeshProUGUI badgeTxt = badgeObj.GetComponent<TextMeshProUGUI>();
        if (badgeTxt == null) badgeTxt = badgeObj.AddComponent<TextMeshProUGUI>();
        badgeTxt.text = "x0";
        badgeTxt.fontSize = 22;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.alignment = TextAlignmentOptions.Right;
        badgeTxt.color = Color.white;
        badgeTxt.outlineWidth = 0.2f;
        badgeTxt.outlineColor = Color.black;

        RectTransform bRect = badgeObj.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0.6f);
        bRect.anchorMax = new Vector2(0.95f, 0.95f);
        bRect.sizeDelta = Vector2.zero;
    }

    private static GameObject SetupCoinCard(GameObject parent, string name, string amount, string price, Sprite coinSprite)
    {
        GameObject card = FindOrCreateChild(parent, name);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240, 220);

        Image img = card.GetComponent<Image>();
        if (img == null) img = card.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.85f); // Dark background item card

        Button btn = card.GetComponent<Button>();
        if (btn == null) btn = card.AddComponent<Button>();

        // Gold Icon
        GameObject icon = FindOrCreateChild(card, "CoinIcon");
        Image iconImg = icon.GetComponent<Image>();
        if (iconImg == null) iconImg = icon.AddComponent<Image>();
        if (coinSprite != null) iconImg.sprite = coinSprite;
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.3f, 0.45f);
        iconRect.anchorMax = new Vector2(0.7f, 0.85f);
        iconRect.sizeDelta = Vector2.zero;

        // Amount label
        GameObject txtAmount = FindOrCreateChild(card, "AmountText");
        SetText(txtAmount, amount, 22, TextAlignmentOptions.Center);
        RectTransform aRect = txtAmount.GetComponent<RectTransform>();
        aRect.anchorMin = new Vector2(0.05f, 0.22f);
        aRect.anchorMax = new Vector2(0.95f, 0.42f);
        aRect.sizeDelta = Vector2.zero;

        // Price label
        GameObject txtPrice = FindOrCreateChild(card, "PriceText");
        SetText(txtPrice, price, 24, TextAlignmentOptions.Center);
        TextMeshProUGUI tmpPrice = txtPrice.GetComponent<TextMeshProUGUI>();
        tmpPrice.color = Color.yellow;
        RectTransform pRect = txtPrice.GetComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.05f, 0.02f);
        pRect.anchorMax = new Vector2(0.95f, 0.2f);
        pRect.sizeDelta = Vector2.zero;

        return card;
    }

    private static GameObject CreateOverlayPanel(GameObject canvas, string name)
    {
        GameObject panelObj = FindOrCreateChild(canvas, name);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image bgImg = panelObj.GetComponent<Image>();
        if (bgImg == null) bgImg = panelObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);

        GameObject bgObj = FindOrCreateChild(panelObj, "Background");
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.15f, 0.15f);
        bgRect.anchorMax = new Vector2(0.85f, 0.85f);
        bgRect.sizeDelta = Vector2.zero;

        Image containerImg = bgObj.GetComponent<Image>();
        if (containerImg == null) containerImg = bgObj.AddComponent<Image>();
        containerImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        GameObject titleObj = FindOrCreateChild(bgObj, "Title");
        SetText(titleObj, name, 48, TextAlignmentOptions.Center);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.85f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.sizeDelta = Vector2.zero;

        return panelObj;
    }

    private static void SetText(GameObject obj, string text, float fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI tmpText = obj.GetComponent<TextMeshProUGUI>();
        if (tmpText == null) tmpText = obj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = alignment;
    }
}
