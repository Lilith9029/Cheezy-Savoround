using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AchievementUI : MonoBehaviour
{
    public static AchievementUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform container;
    [SerializeField] private GameObject templateItem;
    [SerializeField] private Button closeButton;

    [Header("Scroll Settings")]
    [SerializeField] private RectTransform scrollViewport;

    private List<GameObject> _instantiatedItems = new List<GameObject>();
    private ScrollRect _scrollRect;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        SetupScrollRect();

        AchievementManager.OnAchievementProgressUpdated += HandleProgressUpdated;
        UserDataManager.OnDataLoaded += RefreshUI;
    }

    private void OnDestroy()
    {
        AchievementManager.OnAchievementProgressUpdated -= HandleProgressUpdated;
        UserDataManager.OnDataLoaded -= RefreshUI;
    }

    private void OnEnable()
    {
        RefreshUI();
        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void SetupScrollRect()
    {
        _scrollRect = GetComponentInChildren<ScrollRect>();

        if (_scrollRect == null && container != null)
        {
            GameObject scrollObj = new GameObject("ScrollRect", typeof(RectTransform));
            scrollObj.transform.SetParent(container.parent, false);

            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = container.anchorMin;
            scrollRect.anchorMax = container.anchorMax;
            scrollRect.offsetMin = container.offsetMin;
            scrollRect.offsetMax = container.offsetMax;
            scrollRect.sizeDelta = container.sizeDelta;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRT = viewportObj.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportObj.GetComponent<Mask>().showMaskGraphic = false;

            container.SetParent(viewportObj.transform, false);
            container.anchorMin = new Vector2(0, 1);
            container.anchorMax = new Vector2(1, 1);
            container.pivot = new Vector2(0.5f, 1f);
            container.anchoredPosition = Vector2.zero;

            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.content = container;
            _scrollRect.viewport = viewportRT;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 30f;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            ContentSizeFitter fitter = container.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void HandleProgressUpdated(string id, int progress, int target)
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (AchievementManager.Instance == null || UserDataManager.Instance == null) return;

        foreach (var item in _instantiatedItems)
            Destroy(item);
        _instantiatedItems.Clear();

        if (templateItem == null || container == null) return;
        templateItem.SetActive(false);

        var achievements = new List<AchievementConfig>(AchievementManager.Instance.Achievements);
        achievements.Sort((a, b) =>
        {
            var saveA = UserDataManager.Instance.GetAchievement(a.id);
            var saveB = UserDataManager.Instance.GetAchievement(b.id);
            bool unlockedA = saveA != null && saveA.isUnlocked;
            bool unlockedB = saveB != null && saveB.isUnlocked;
            return unlockedA.CompareTo(unlockedB);
        });

        foreach (var config in achievements)
        {
            GameObject newItem = Instantiate(templateItem, container);
            newItem.SetActive(true);
            _instantiatedItems.Add(newItem);

            AchievementSaveData saveData = UserDataManager.Instance.GetAchievement(config.id);
            int currentVal = saveData?.currentProgress ?? 0;
            bool isUnlocked = saveData?.isUnlocked ?? false;

            TextMeshProUGUI titleText = FindComponentByName<TextMeshProUGUI>(newItem, "TitleText");
            TextMeshProUGUI descText = FindComponentByName<TextMeshProUGUI>(newItem, "DescriptionText");
            TextMeshProUGUI progressText = FindComponentByName<TextMeshProUGUI>(newItem, "ProgressText");
            Slider progressBar = FindComponentByName<Slider>(newItem, "ProgressBar");
            Image checkmarkImage = FindComponentByName<Image>(newItem, "CheckmarkImage");

            if (titleText != null) titleText.text = config.title;
            if (descText != null) descText.text = config.description;

            if (progressBar != null)
            {
                progressBar.minValue = 0;
                progressBar.maxValue = config.targetValue;
                progressBar.value = currentVal;
            }

            if (progressText != null)
            {
                progressText.text = isUnlocked
                    ? $"COMPLETED! (+{config.reward} Gold)"
                    : $"{currentVal} / {config.targetValue}";

                progressText.color = isUnlocked
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : Color.white;
            }

            if (checkmarkImage != null)
                checkmarkImage.gameObject.SetActive(isUnlocked);
        }

        // Force rebuild layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    private T FindComponentByName<T>(GameObject root, string name) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        foreach (var c in components)
        {
            if (c.gameObject.name == name) return c;
        }
        return null;
    }
}