using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardRewardDetailPanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image accentImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;

    private bool showRequestedBeforeAwake;

    private void Awake()
    {
        // 長押し詳細も同じライフサイクル事故を避ける。
        if (!showRequestedBeforeAwake)
        {
            HideImmediate();
        }
    }

    public void Show(string title, string detail, Sprite icon, Color accentColor)
    {
        showRequestedBeforeAwake = true;

        GameObject targetRoot = rootObject != null ? rootObject : gameObject;
        targetRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? "-" : title;
        }

        if (detailText != null)
        {
            detailText.text = string.IsNullOrEmpty(detail) ? string.Empty : detail;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.enabled = icon != null;
        }

        if (accentImage != null)
        {
            accentImage.color = accentColor;
        }
    }

    public void Hide()
    {
        showRequestedBeforeAwake = false;

        GameObject targetRoot = rootObject != null ? rootObject : gameObject;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        targetRoot.SetActive(false);
    }

    public void HideImmediate()
    {
        Hide();
    }
}
