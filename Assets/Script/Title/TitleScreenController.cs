using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text tapToStartText;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField] private RectTransform backgroundRectTransform;

    [Header("Scene")]
    [SerializeField] private string homeSceneName = "Home";

    [Header("Tap Animation")]
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float blinkDuration = 0.75f;
    [SerializeField] private float tapFloatDistance = 6f;
    [SerializeField] private float tapFloatDuration = 1.1f;

    [Header("Intro Animation")]
    [SerializeField] private float titleFadeDelay = 0.25f;
    [SerializeField] private float titleFadeDuration = 0.7f;
    [SerializeField] private float tapAppearDelay = 0.9f;

    [Header("Background Motion")]
    [SerializeField] private float zoomScaleMin = 1.00f;
    [SerializeField] private float zoomScaleMax = 1.03f;
    [SerializeField] private float zoomDuration = 5.5f;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float blackHoldDuration = 0.12f;
    [SerializeField] private float titleFadeOutForPrologueDuration = 0.28f;
    [SerializeField, Range(0f, 1f)] private float prologuePanelAlpha = 1f;

    [Header("Sound")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip uiDecideSe;

    [Header("Prologue")]
    [SerializeField] private bool enableFirstLaunchPrologue = true;
    [SerializeField, TextArea(2, 4)] private string[] prologueLines =
    {
        "この地では、時折、\n巨大な鼓動のような音が響くという。",
        "文明に終焉をもたらした巨人は、\nかつて恐ろしい鼓動を鳴らしたとも伝えられている。",
        "その噂を聞き、\n彼女はこの地を訪れた。"
    };
    [SerializeField] private float prologueLineFadeDuration = 0.42f;
    [SerializeField] private float prologueInterLineDelay = 2.05f;
    [SerializeField] private float prologueFastInterLineDelay = 0.6f;
    [SerializeField] private float prologueFadeDuration = 0.25f;
    [SerializeField] private float fastForwardHoldThreshold = 0.22f;
    [SerializeField] private float prologueTextFontSize = 28f;
    [SerializeField] private float prologueHintFontSize = 18f;
    [SerializeField] private float prologueLineSpacing = 40f;
    [SerializeField] private TMP_FontAsset prologueFont;
    [SerializeField] private string prologueFastForwardHint = "長押しで早送り";
    [SerializeField] private string prologueContinueHint = "タップで進む";

    [Header("Prologue Layout")]
    [SerializeField] private Vector2 prologueTextAreaSize = new Vector2(720f, 420f);
    [SerializeField] private Vector2 prologueTextAreaOffset = new Vector2(0f, 30f);
    [SerializeField] private Vector2 prologueHintOffset = new Vector2(0f, -300f);
    [SerializeField] private Vector2 prologueHintSize = new Vector2(520f, 70f);

    private bool isTransitioning;
    private bool isShowingPrologue;
    private bool canBlinkTapText;
    private bool isPointerHeld;
    private float pointerHeldStartTime = -1f;

    private Vector2 tapBaseAnchoredPosition;
    private Vector3 backgroundBaseScale = Vector3.one;

    private GameObject runtimePrologueRoot;
    private CanvasGroup runtimePrologueCanvasGroup;
    private RectTransform runtimePrologueLinesRoot;
    private TMP_Text runtimePrologueHintText;
    private readonly List<TMP_Text> runtimePrologueLineTexts = new();

    private Tween tapFadeTween;
    private Tween tapFloatTween;
    private Tween backgroundZoomTween;
    private Tween hintBlinkTween;

    private void Awake()
    {
        if (tapToStartText != null)
        {
            tapBaseAnchoredPosition = tapToStartText.rectTransform.anchoredPosition;
            SetTextAlpha(tapToStartText, 0f);
        }

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.raycastTarget = false;
        }

        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 0f;
            titleCanvasGroup.interactable = false;
            titleCanvasGroup.blocksRaycasts = false;
        }

        if (backgroundRectTransform != null)
        {
            backgroundBaseScale = backgroundRectTransform.localScale;
        }

        BuildRuntimePrologueOverlay();
    }

    private IEnumerator Start()
    {
        StartBackgroundZoom();
        yield return PlayIntroRoutine();
    }

    private void Update()
    {
        UpdatePointerHoldState();

        if (isShowingPrologue)
        {
            return;
        }

        if (isTransitioning)
        {
            return;
        }

        if (HasStartInputDown())
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    private IEnumerator PlayIntroRoutine()
    {
        if (titleCanvasGroup != null)
        {
            yield return titleCanvasGroup
                .DOFade(1f, titleFadeDuration)
                .SetDelay(titleFadeDelay)
                .SetUpdate(true)
                .WaitForCompletion();

            titleCanvasGroup.interactable = true;
            titleCanvasGroup.blocksRaycasts = true;
        }

        yield return new WaitForSecondsRealtime(tapAppearDelay);

        if (tapToStartText != null)
        {
            SetTextAlpha(tapToStartText, maxAlpha);
            canBlinkTapText = true;
            StartTapLoop();
        }
    }

    private IEnumerator StartGameRoutine()
    {
        if (isTransitioning)
        {
            yield break;
        }

        isTransitioning = true;
        canBlinkTapText = false;
        StopTapLoop();

        if (seSource != null && uiDecideSe != null)
        {
            seSource.PlayOneShot(uiDecideSe);
        }

        if (tapToStartText != null)
        {
            tapToStartText.gameObject.SetActive(false);
        }

        if (ShouldShowPrologue())
        {
            yield return FadeTitleForPrologueRoutine();
            yield return PlayPrologueRoutine();
        }

        if (fadeOverlay != null && fadeOverlay.color.a < 0.999f)
        {
            yield return fadeOverlay
                .DOFade(1f, fadeDuration)
                .WaitForCompletion();
        }

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blackHoldDuration);
        }

        SceneManager.LoadScene(homeSceneName);
    }

    private bool ShouldShowPrologue()
    {
        if (!enableFirstLaunchPrologue || prologueLines == null || prologueLines.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < prologueLines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(prologueLines[i]))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator FadeTitleForPrologueRoutine()
    {
        Tween titleTween = null;
        Tween overlayTween = null;

        if (titleCanvasGroup != null)
        {
            titleTween = titleCanvasGroup
                .DOFade(0f, titleFadeOutForPrologueDuration)
                .SetUpdate(true);
        }

        if (fadeOverlay != null)
        {
            overlayTween = fadeOverlay
                .DOFade(prologuePanelAlpha, titleFadeOutForPrologueDuration)
                .SetUpdate(true);
        }

        if (titleTween != null)
        {
            yield return titleTween.WaitForCompletion();
            titleCanvasGroup.interactable = false;
            titleCanvasGroup.blocksRaycasts = false;
        }
        else if (overlayTween != null)
        {
            yield return overlayTween.WaitForCompletion();
        }
    }

    private IEnumerator PlayPrologueRoutine()
    {
        if (runtimePrologueCanvasGroup == null || runtimePrologueLinesRoot == null)
        {
            yield break;
        }

        isShowingPrologue = true;
        runtimePrologueRoot.SetActive(true);
        ClearRuntimePrologueLines();
        SetPrologueHintText(prologueFastForwardHint);

        runtimePrologueCanvasGroup.alpha = 0f;
        yield return runtimePrologueCanvasGroup
            .DOFade(1f, prologueFadeDuration)
            .SetUpdate(true)
            .WaitForCompletion();

        List<string> validLines = CollectValidPrologueLines();

        for (int i = 0; i < validLines.Count; i++)
        {
            TMP_Text lineText = CreateRuntimePrologueLine(validLines[i]);
            if (lineText == null)
            {
                continue;
            }

            yield return lineText
                .DOFade(1f, prologueLineFadeDuration)
                .SetUpdate(true)
                .WaitForCompletion();

            bool isLastLine = i >= validLines.Count - 1;
            if (!isLastLine)
            {
                yield return WaitForAdvanceOrDurationRoutine();
            }
        }

        SetPrologueHintText(prologueContinueHint);
        yield return WaitUntilAdvanceInputRoutine();

        yield return runtimePrologueCanvasGroup
            .DOFade(0f, prologueFadeDuration)
            .SetUpdate(true)
            .WaitForCompletion();

        ClearRuntimePrologueLines();
        runtimePrologueRoot.SetActive(false);
        isShowingPrologue = false;
    }

    private List<string> CollectValidPrologueLines()
    {
        List<string> validLines = new();

        for (int i = 0; i < prologueLines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(prologueLines[i]))
            {
                validLines.Add(prologueLines[i]);
            }
        }

        return validLines;
    }

    private IEnumerator WaitForAdvanceOrDurationRoutine()
    {
        float elapsed = 0f;

        while (true)
        {
            if (HasStartInputDown())
            {
                yield break;
            }

            float target = IsFastForwardHeld() ? prologueFastInterLineDelay : prologueInterLineDelay;
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= target)
            {
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitUntilAdvanceInputRoutine()
    {
        while (!HasStartInputDown())
        {
            yield return null;
        }
    }

    private void StartTapLoop()
    {
        if (!canBlinkTapText || tapToStartText == null)
        {
            return;
        }

        StopTapLoop();

        tapFadeTween = tapToStartText
            .DOFade(minAlpha, blinkDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);

        tapFloatTween = tapToStartText.rectTransform
            .DOAnchorPosY(tapBaseAnchoredPosition.y + tapFloatDistance, tapFloatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void StopTapLoop()
    {
        tapFadeTween?.Kill();
        tapFloatTween?.Kill();

        if (tapToStartText != null)
        {
            tapToStartText.rectTransform.anchoredPosition = tapBaseAnchoredPosition;
        }
    }

    private void StartBackgroundZoom()
    {
        if (backgroundRectTransform == null)
        {
            return;
        }

        backgroundRectTransform.localScale = backgroundBaseScale * zoomScaleMin;

        backgroundZoomTween?.Kill();
        backgroundZoomTween = backgroundRectTransform
            .DOScale(backgroundBaseScale * zoomScaleMax, zoomDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void UpdatePointerHoldState()
    {
        bool heldNow = HasAnyPointerHeldNow();

        if (heldNow && !isPointerHeld)
        {
            pointerHeldStartTime = Time.unscaledTime;
        }
        else if (!heldNow)
        {
            pointerHeldStartTime = -1f;
        }

        isPointerHeld = heldNow;
    }

    private bool HasAnyPointerHeldNow()
    {
        if (Input.GetMouseButton(0))
        {
            return true;
        }

        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
        }

        return false;
    }

    private bool IsFastForwardHeld()
    {
        if (!isPointerHeld || pointerHeldStartTime < 0f)
        {
            return false;
        }

        return Time.unscaledTime - pointerHeldStartTime >= fastForwardHoldThreshold;
    }

    private bool HasStartInputDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private void BuildRuntimePrologueOverlay()
    {
        if (tapToStartText == null || runtimePrologueRoot != null)
        {
            return;
        }

        runtimePrologueRoot = new GameObject("TitlePrologueOverlay");

        Canvas canvas = runtimePrologueRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = runtimePrologueRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        runtimePrologueRoot.AddComponent<GraphicRaycaster>();
        runtimePrologueCanvasGroup = runtimePrologueRoot.AddComponent<CanvasGroup>();
        runtimePrologueCanvasGroup.alpha = 0f;
        runtimePrologueCanvasGroup.interactable = false;
        runtimePrologueCanvasGroup.blocksRaycasts = false;

        RectTransform rootRect = runtimePrologueRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject linesRootObject = new GameObject("PrologueLinesRoot", typeof(RectTransform));
        linesRootObject.transform.SetParent(runtimePrologueRoot.transform, false);
        runtimePrologueLinesRoot = linesRootObject.GetComponent<RectTransform>();
        runtimePrologueLinesRoot.anchorMin = new Vector2(0.5f, 0.5f);
        runtimePrologueLinesRoot.anchorMax = new Vector2(0.5f, 0.5f);
        runtimePrologueLinesRoot.pivot = new Vector2(0.5f, 0.5f);
        runtimePrologueLinesRoot.anchoredPosition = prologueTextAreaOffset;
        runtimePrologueLinesRoot.sizeDelta = prologueTextAreaSize;

        runtimePrologueHintText = CreateClonedText(
            "PrologueHintText",
            runtimePrologueRoot.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            prologueHintOffset,
            prologueHintSize);

        if (runtimePrologueHintText != null)
        {
            runtimePrologueHintText.alignment = TextAlignmentOptions.Center;
            runtimePrologueHintText.enableWordWrapping = false;
            runtimePrologueHintText.fontSize = prologueHintFontSize;
            Color hintColor = runtimePrologueHintText.color;
            hintColor.a = 0.9f;
            runtimePrologueHintText.color = hintColor;
        }

        ApplyPrologueFont(runtimePrologueHintText);
        runtimePrologueRoot.SetActive(false);
    }

    private TMP_Text CreateRuntimePrologueLine(string line)
    {
        if (runtimePrologueLinesRoot == null)
        {
            return null;
        }

        GameObject textObject = new GameObject($"PrologueLine_{runtimePrologueLineTexts.Count}", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(runtimePrologueLinesRoot, false);

        TextMeshProUGUI lineText = textObject.GetComponent<TextMeshProUGUI>();
        RectTransform rect = lineText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        if (tapToStartText != null)
        {
            lineText.color = tapToStartText.color;
            lineText.fontStyle = tapToStartText.fontStyle;
            lineText.characterSpacing = tapToStartText.characterSpacing;
            lineText.wordSpacing = tapToStartText.wordSpacing;
        }
        else
        {
            lineText.color = Color.white;
        }

        lineText.raycastTarget = false;
        lineText.alignment = TextAlignmentOptions.Center;
        lineText.enableWordWrapping = true;
        lineText.overflowMode = TextOverflowModes.Overflow;
        lineText.fontSize = prologueTextFontSize;
        lineText.lineSpacing = 12f;
        lineText.text = line;

        ApplyPrologueFont(lineText);

        float width = runtimePrologueLinesRoot.sizeDelta.x;
        float height = Mathf.Max(90f, lineText.GetPreferredValues(line, width, 0f).y + 12f);
        rect.sizeDelta = new Vector2(width, height);

        SetTextAlpha(lineText, 0f);
        runtimePrologueLineTexts.Add(lineText);
        RefreshRuntimePrologueLineLayout();
        return lineText;
    }

    private void RefreshRuntimePrologueLineLayout()
    {
        int count = runtimePrologueLineTexts.Count;
        if (count <= 0)
        {
            return;
        }

        float totalHeight = 0f;

        for (int i = 0; i < count; i++)
        {
            TMP_Text lineText = runtimePrologueLineTexts[i];
            if (lineText != null)
            {
                totalHeight += lineText.rectTransform.sizeDelta.y;
            }
        }

        totalHeight += Mathf.Max(0, count - 1) * prologueLineSpacing;
        float currentY = totalHeight * 0.5f;

        for (int i = 0; i < count; i++)
        {
            TMP_Text lineText = runtimePrologueLineTexts[i];
            if (lineText == null)
            {
                continue;
            }

            RectTransform rect = lineText.rectTransform;
            float height = rect.sizeDelta.y;
            currentY -= height * 0.5f;
            rect.anchoredPosition = new Vector2(0f, currentY);
            currentY -= height * 0.5f + prologueLineSpacing;
        }
    }

    private TMP_Text CreateClonedText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        if (tapToStartText == null || parent == null)
        {
            return null;
        }

        TMP_Text clone = Instantiate(tapToStartText, parent);
        clone.name = objectName;
        clone.gameObject.SetActive(true);
        clone.raycastTarget = false;

        RectTransform rect = clone.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        Color c = clone.color;
        c.a = 1f;
        clone.color = c;
        return clone;
    }

    private void ClearRuntimePrologueLines()
    {
        for (int i = 0; i < runtimePrologueLineTexts.Count; i++)
        {
            if (runtimePrologueLineTexts[i] != null)
            {
                Destroy(runtimePrologueLineTexts[i].gameObject);
            }
        }

        runtimePrologueLineTexts.Clear();
    }

    private void ApplyPrologueFont(TMP_Text textComponent)
    {
        if (textComponent == null || prologueFont == null)
        {
            return;
        }

        textComponent.font = prologueFont;
        textComponent.fontSharedMaterial = prologueFont.material;
        textComponent.SetAllDirty();
        textComponent.ForceMeshUpdate();
    }

    private void SetTextAlpha(TMP_Text textComponent, float alpha)
    {
        if (textComponent == null)
        {
            return;
        }

        Color c = textComponent.color;
        c.a = alpha;
        textComponent.color = c;
    }

    private void SetPrologueHintText(string hint)
    {
        if (runtimePrologueHintText == null)
        {
            return;
        }

        runtimePrologueHintText.text = hint;
        runtimePrologueHintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));

        hintBlinkTween?.Kill();

        if (runtimePrologueHintText.gameObject.activeSelf)
        {
            SetTextAlpha(runtimePrologueHintText, 0.9f);
            hintBlinkTween = runtimePrologueHintText
                .DOFade(0.35f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
    }

    private void OnDestroy()
    {
        tapFadeTween?.Kill();
        tapFloatTween?.Kill();
        backgroundZoomTween?.Kill();
        hintBlinkTween?.Kill();

        if (runtimePrologueRoot != null)
        {
            Destroy(runtimePrologueRoot);
            runtimePrologueRoot = null;
        }
    }
}
