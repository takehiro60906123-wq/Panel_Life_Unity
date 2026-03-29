// =============================================================
// EpilogueController.cs
// ステージクリア後のエピローグ表示
//
// 黒画面に1行ずつテキストがフェードインする演出。
// Canvas・テキスト全て動的生成。Inspector でテキスト内容を編集可能。
//
// 配置:
//   PanelBattleManager と同じ GameObject に AddComponent。
//
// 使い方（PanelBattleManager から）:
//   yield return epilogueController.PlayEpilogue();
// =============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class EpilogueController : MonoBehaviour
{
    [Header("エピローグテキスト")]
    [Tooltip("1要素 = 1行。空行は間として扱う。")]
    [SerializeField, TextArea(2, 4)] private string[] epilogueLines =
    {
        "神骸は沈黙していた。",
        "かつてこの地を焼いた力の片鱗すら、もう残っていない。",
        "",
        "しかし——地の底で、かすかな振動を感じた。",
        "",
        "あれは、鼓動だったのか。"
    };

    [Header("表示タイミング")]
    [SerializeField] private float initialDelay = 0.8f;
    [SerializeField] private float lineFadeInDuration = 0.5f;
    [SerializeField] private float lineHoldDuration = 2.0f;
    [SerializeField] private float lineFadeOutDuration = 0.35f;
    [SerializeField] private float interLineDelay = 0.4f;
    [SerializeField] private float emptyLinePause = 0.8f;
    [SerializeField] private float endDelay = 1.0f;

    [Header("テキスト見た目")]
    [SerializeField] private float fontSize = 30f;
    [SerializeField] private Color textColor = new Color(0.85f, 0.82f, 0.75f, 1f);
    [SerializeField] private TMP_FontAsset customFont;

    [Header("タップで早送り")]
    [SerializeField] private bool allowFastForward = true;
    [SerializeField] private float fastForwardHoldDuration = 1.2f;
    [SerializeField] private float fastForwardFadeOutDuration = 0.15f;

    // 内部
    private GameObject epilogueRoot;
    private CanvasGroup rootCanvasGroup;
    private Image blackBackground;
    private RectTransform textContainer;
    private readonly List<TMP_Text> lineTexts = new List<TMP_Text>();
    private bool isPlaying;
    private bool fastForwardRequested;

    // =============================================================
    // 公開 API
    // =============================================================

    /// <summary>
    /// エピローグを再生する。コルーチンとして yield return で呼ぶ。
    /// </summary>
    public IEnumerator PlayEpilogue()
    {
        if (isPlaying) yield break;
        if (epilogueLines == null || epilogueLines.Length == 0) yield break;

        isPlaying = true;
        fastForwardRequested = false;

        CreateEpilogueUI();
        ClearExistingLineTexts();
        epilogueRoot.SetActive(true);
        rootCanvasGroup.alpha = 1f;

        // 黒背景フェードイン
        blackBackground.DOKill();
        Color bgColor = blackBackground.color;
        bgColor.a = 0f;
        blackBackground.color = bgColor;
        blackBackground.DOFade(1f, 0.3f);
        yield return new WaitForSeconds(0.3f);

        yield return new WaitForSeconds(initialDelay);

        // 1行ずつ表示
        for (int i = 0; i < epilogueLines.Length; i++)
        {
            string line = epilogueLines[i];

            // 空行 = 間（pause）
            if (string.IsNullOrWhiteSpace(line))
            {
                yield return WaitRealtime(emptyLinePause);
                continue;
            }

            TMP_Text lineText = CreateLineText(line);
            LayoutRebuilder.ForceRebuildLayoutImmediate(textContainer);

            // フェードイン
            yield return FadeText(lineText, 0f, 1f, lineFadeInDuration);

            // 保持（早送り対応）
            float holdTime = fastForwardRequested ? fastForwardHoldDuration : lineHoldDuration;
            yield return WaitRealtimeWithTapCheck(holdTime);

            // 以前の節は消さずに残す

            // 行間
            if (i < epilogueLines.Length - 1)
            {
                yield return WaitRealtime(interLineDelay);
            }
        }

        yield return WaitRealtime(endDelay);

        // 全体フェードアウト（既に黒画面なのでそのまま終了）
        isPlaying = false;
        // epilogueRoot は次のシーン遷移で破棄されるので残しておく
    }

    /// <summary>
    /// エピローグが有効かどうか（テキストが設定されているか）
    /// </summary>
    public bool HasEpilogue()
    {
        if (epilogueLines == null || epilogueLines.Length == 0) return false;

        for (int i = 0; i < epilogueLines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(epilogueLines[i])) return true;
        }

        return false;
    }

    // =============================================================
    // UI 動的生成
    // =============================================================

    private void CreateEpilogueUI()
    {
        if (epilogueRoot != null) return;

        // Canvas（最前面）
        epilogueRoot = new GameObject("EpilogueCanvas");

        Canvas canvas = epilogueRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9990; // TransitionManager(9999) の直下

        CanvasScaler scaler = epilogueRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        epilogueRoot.AddComponent<GraphicRaycaster>();

        rootCanvasGroup = epilogueRoot.AddComponent<CanvasGroup>();
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = true;

        // 黒背景
        GameObject bgObj = new GameObject("EpilogueBlackBG", typeof(RectTransform));
        bgObj.transform.SetParent(epilogueRoot.transform, false);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        blackBackground = bgObj.AddComponent<Image>();
        blackBackground.color = new Color(0f, 0f, 0f, 1f);
        blackBackground.raycastTarget = true;

        // テキストコンテナ（節を積み上げる）
        GameObject textContainerObj = new GameObject("EpilogueTextContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        textContainerObj.transform.SetParent(epilogueRoot.transform, false);

        textContainer = textContainerObj.GetComponent<RectTransform>();
        textContainer.anchorMin = new Vector2(0.1f, 0.2f);
        textContainer.anchorMax = new Vector2(0.9f, 0.8f);
        textContainer.offsetMin = Vector2.zero;
        textContainer.offsetMax = Vector2.zero;
        textContainer.pivot = new Vector2(0.5f, 0.5f);

        VerticalLayoutGroup layout = textContainerObj.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 36f;

        ContentSizeFitter fitter = textContainerObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        epilogueRoot.SetActive(false);
    }


    private TMP_Text CreateLineText(string line)
    {
        GameObject textObj = new GameObject("EpilogueLine", typeof(RectTransform));
        textObj.transform.SetParent(textContainer, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TMP_Text lineText = textObj.AddComponent<TextMeshProUGUI>();
        lineText.text = line;
        lineText.fontSize = fontSize;
        lineText.color = textColor;
        lineText.alignment = TextAlignmentOptions.Center;
        lineText.enableWordWrapping = true;
        lineText.overflowMode = TextOverflowModes.Overflow;
        lineText.raycastTarget = false;
        lineText.alpha = 0f;

        if (customFont != null)
        {
            lineText.font = customFont;
        }

        lineTexts.Add(lineText);
        return lineText;
    }


    private void ClearExistingLineTexts()
    {
        for (int i = lineTexts.Count - 1; i >= 0; i--)
        {
            if (lineTexts[i] != null)
            {
                Destroy(lineTexts[i].gameObject);
            }
        }

        lineTexts.Clear();
    }

    // =============================================================
    // ユーティリティ
    // =============================================================

    private IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
    {
        if (text == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));
            // ease-out
            float eased = 1f - (1f - t) * (1f - t);
            text.alpha = Mathf.Lerp(from, to, eased);
            yield return null;
        }
        text.alpha = to;
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitRealtimeWithTapCheck(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (allowFastForward && !fastForwardRequested)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    fastForwardRequested = true;
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (epilogueRoot != null)
        {
            Destroy(epilogueRoot);
        }
    }
}
