using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyRewardItemUI : MonoBehaviour
{
    [Header("UI Component Bindings")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private GameObject claimedOverlay;
    [SerializeField] private GameObject activeHighlight;

    /// <summary>
    /// Set up or update the visual display of the reward day card.
    /// </summary>
    public void Setup(string dayName, Sprite icon, string amount, bool isClaimed, bool isActiveClaimable)
    {
        // Try to automatically find components if they are not bound in Inspector
        ResolveComponents();

        if (dayText != null)
        {
            dayText.text = dayName;
            // Highlight text if this is the active claimable day
            dayText.color = isActiveClaimable ? new Color(1f, 0.82f, 0f) : Color.white; // Gold highlight for active
        }

        if (rewardIcon != null)
        {
            rewardIcon.sprite = icon;
            rewardIcon.gameObject.SetActive(icon != null);
        }

        if (amountText != null)
        {
            amountText.text = amount;
        }

        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(isClaimed);
        }

        if (activeHighlight != null)
        {
            activeHighlight.SetActive(isActiveClaimable);
        }
    }

    private void ResolveComponents()
    {
        if (dayText == null) dayText = transform.Find("DayText")?.GetComponent<TextMeshProUGUI>();
        if (rewardIcon == null) rewardIcon = transform.Find("Icon")?.GetComponent<Image>();
        if (amountText == null) amountText = transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
        if (claimedOverlay == null) claimedOverlay = transform.Find("ClaimedOverlay")?.gameObject;
        if (activeHighlight == null) activeHighlight = transform.Find("ActiveHighlight")?.gameObject;
    }
}
