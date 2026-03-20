using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardRewardTutorialPanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private string defaultConfirmText = "了解";

    private Action onConfirm;
    private bool confirmBound;
    private bool showRequestedBeforeAwake;

    private void Awake()
    {
        EnsureBound();

        // 非アクティブ開始のモーダルを Show() で初回表示したとき、
        // SetActive(true) に伴って Awake() が走り、ここで HideImmediate() すると
        // 表示要求を自分で打ち消してしまう。
        if (!showRequestedBeforeAwake)
        {
            HideImmediate();
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null && confirmBound)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmBound = false;
        }
    }

    public void Show(string title, string body, Action onConfirm)
    {
        this.onConfirm = onConfirm;
        showRequestedBeforeAwake = true;
        EnsureBound();

        GameObject targetRoot = rootObject != null ? rootObject : gameObject;
        targetRoot.SetActive(true);

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? string.Empty : title;
        }

        if (bodyText != null)
        {
            bodyText.text = string.IsNullOrEmpty(body) ? string.Empty : body;
        }

        if (confirmButtonText != null)
        {
            confirmButtonText.text = string.IsNullOrEmpty(defaultConfirmText) ? "OK" : defaultConfirmText;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void Hide()
    {
        onConfirm = null;
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

    private void HandleConfirmClicked()
    {
        Action callback = onConfirm;
        Hide();
        callback?.Invoke();
    }

    private void EnsureBound()
    {
        if (confirmButton == null || confirmBound)
        {
            return;
        }

        confirmButton.onClick.RemoveListener(HandleConfirmClicked);
        confirmButton.onClick.AddListener(HandleConfirmClicked);
        confirmBound = true;
    }
}
