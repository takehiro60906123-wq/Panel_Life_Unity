using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EndingSceneController : MonoBehaviour
{
    [Header("Kushana")]
    [SerializeField] private Transform kushanaRoot;
    [SerializeField] private Animator kushanaAnimator;
    [SerializeField] private SpriteRenderer kushanaSpriteRenderer;
    [SerializeField] private FootstepLoopPlayer kushanaFootstepLoopPlayer;

    [Header("Man")]
    [SerializeField] private Transform manRoot;
    [SerializeField] private Animator manAnimator;
    [SerializeField] private SpriteRenderer manSpriteRenderer;
    [SerializeField] private FootstepLoopPlayer manFootstepLoopPlayer;

    [Header("Move Points")]
    [SerializeField] private Transform kushanaStartPoint;
    [SerializeField] private Transform kushanaStopPoint;
    [SerializeField] private Transform manStartPoint;
    [SerializeField] private Transform manStopPoint;

    [Header("Background")]
    [SerializeField] private SpriteRenderer[] ruinedBackgroundRenderers;

    [Header("Dawn Environment")]
    [SerializeField] private GameObject[] dawnEnvironmentObjects;
    [SerializeField, Range(0f, 1f)] private float dawnEnvironmentShowTiming = 0.35f;

    [Header("UI")]
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private CanvasGroup titleImageCanvasGroup;
    [SerializeField] private CanvasGroup returnButtonCanvasGroup;
    [SerializeField] private Button returnHomeButton;

    [Header("Scene")]
    [SerializeField] private string homeSceneName = "Home";

    [Header("Animator")]
    [SerializeField] private string runningBoolName = "IsRunning";
    [SerializeField] private string walkStateName = "RUN";
    [SerializeField] private string idleStateName = "IDLE";
    [SerializeField] private string surprisedStateName = "IDLE";
    [SerializeField] private bool forcePlayIdleStateOnArrival = true;

    [Header("Movement")]
    [SerializeField] private float kushanaMoveSpeed = 2.6f;
    [SerializeField] private float manMoveSpeed = 2.4f;
    [SerializeField] private float arriveThreshold = 0.02f;

    [Header("Fade")]
    [SerializeField] private bool useVerticalWipeFade = true;
    [SerializeField, Range(0f, 1f)] private float startDarkAmount = 1f;
    [SerializeField, Range(0f, 1f)] private float walkDarkAmount = 1f;
    [SerializeField] private float openingDarkFadeDuration = 0.35f;
    [SerializeField] private float revealDuration = 1.8f;
    [SerializeField] private float finalFadeDuration = 0.8f;
    [SerializeField] private bool keepBackgroundVisibleBehindFade = true;

    [Header("Dialogue")]
    [SerializeField] private float dialogueFadeDuration = 0.15f;
    [SerializeField] private float lineHoldDuration = 1.35f;
    [SerializeField] private float afterEntryPause = 0.45f;
    [SerializeField] private float afterRevealPause = 0.3f;

    [Header("Typewriter")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charactersPerSecond = 24f;
    [SerializeField] private float punctuationExtraDelay = 0.08f;
    [SerializeField] private bool useUnscaledTimeForDialogue = false;

    [Header("Ending UI Timing")]
    [SerializeField] private float titleImageFadeDuration = 0.9f;
    [SerializeField] private float titleImageHoldDuration = 0.2f;
    [SerializeField] private float returnButtonFadeDuration = 0.25f;

    private bool isSequenceStarted;
    private bool kushanaReachedStop;
    private bool manReachedStop;
    private bool returnRequested;

    private void Awake()
    {
        if (kushanaFootstepLoopPlayer == null && kushanaRoot != null)
        {
            kushanaFootstepLoopPlayer = kushanaRoot.GetComponentInChildren<FootstepLoopPlayer>(true);
        }

        if (manFootstepLoopPlayer == null && manRoot != null)
        {
            manFootstepLoopPlayer = manRoot.GetComponentInChildren<FootstepLoopPlayer>(true);
        }

        if (returnHomeButton != null)
        {
            returnHomeButton.onClick.AddListener(OnPressedReturnHomeButton);
        }

        if (kushanaRoot != null && kushanaStartPoint != null)
        {
            kushanaRoot.position = kushanaStartPoint.position;
        }

        if (manRoot != null && manStartPoint != null)
        {
            manRoot.position = manStartPoint.position;
        }

        ResetDialogueVisual();
        SetCanvasGroupVisibleImmediate(titleImageCanvasGroup, false);
        SetCanvasGroupVisibleImmediate(returnButtonCanvasGroup, false);
        SetDawnEnvironmentVisible(false);

        if (keepBackgroundVisibleBehindFade)
        {
            SetRuinedBackgroundAlpha(1f);
        }
        else
        {
            SetRuinedBackgroundAlpha(0f);
        }

        ConfigureFadeOverlayForVerticalWipe();
        SetFadeAmount(startDarkAmount);
    }

    private void Start()
    {
        if (!isSequenceStarted)
        {
            StartCoroutine(PlayEndingRoutine());
        }
    }

    private void OnDisable()
    {
        kushanaFootstepLoopPlayer?.EndLoop();
        manFootstepLoopPlayer?.EndLoop();
    }

    private void OnDestroy()
    {
        if (returnHomeButton != null)
        {
            returnHomeButton.onClick.RemoveListener(OnPressedReturnHomeButton);
        }
    }

    private IEnumerator PlayEndingRoutine()
    {
        if (isSequenceStarted)
        {
            yield break;
        }

        isSequenceStarted = true;
        returnRequested = false;
        kushanaReachedStop = false;
        manReachedStop = false;

        yield return StartCoroutine(FadeOverlayRoutine(walkDarkAmount, openingDarkFadeDuration));

        FaceTowardTarget(kushanaRoot, kushanaStopPoint, kushanaSpriteRenderer);
        FaceTowardTarget(manRoot, manStopPoint, manSpriteRenderer);

        SetActorRunning(kushanaAnimator, true);
        SetActorRunning(manAnimator, true);
        kushanaFootstepLoopPlayer?.BeginLoop();
        manFootstepLoopPlayer?.BeginLoop();

        StartCoroutine(MoveActorRoutine(
            kushanaRoot,
            kushanaStopPoint,
            kushanaMoveSpeed,
            kushanaAnimator,
            kushanaFootstepLoopPlayer,
            () => kushanaReachedStop = true));

        StartCoroutine(MoveActorRoutine(
            manRoot,
            manStopPoint,
            manMoveSpeed,
            manAnimator,
            manFootstepLoopPlayer,
            () => manReachedStop = true));

        yield return new WaitUntil(() => kushanaReachedStop && manReachedStop);

        yield return WaitForSecondsCompat(afterEntryPause);

        yield return StartCoroutine(ShowLineRoutine("男", "……結局、今回も外れでしたね"));
        yield return StartCoroutine(ShowLineRoutine("男", "今回ははずれでよかったですね。"));

        yield return StartCoroutine(RevealRuinedKingdomRoutine());

        yield return WaitForSecondsCompat(afterRevealPause);

        PlayDirectState(manAnimator, surprisedStateName);

        yield return StartCoroutine(ShowLineRoutine("男", "……え"));
        yield return StartCoroutine(ShowLineRoutine("男", "な、なんだこれ……"));
        yield return StartCoroutine(ShowLineRoutine("男", "国が……崩れてる……"));

        FaceTowardTarget(kushanaRoot, manRoot, kushanaSpriteRenderer);

        yield return StartCoroutine(ShowLineRoutine("クシャナ", "そう怯えるな"));
        yield return StartCoroutine(ShowLineRoutine("クシャナ", "お前を将軍にしてやる"));

        yield return StartCoroutine(ShowLineRoutine("男", "……は？"));
        yield return StartCoroutine(ShowLineRoutine("男", "あなたは……正気じゃない"));

        yield return StartCoroutine(ShowLineRoutine("クシャナ", "今さらだな"));

        yield return StartCoroutine(FadeCanvasGroupRoutine(titleImageCanvasGroup, 1f, titleImageFadeDuration));

        if (titleImageHoldDuration > 0f)
        {
            yield return WaitForSecondsCompat(titleImageHoldDuration);
        }

        yield return StartCoroutine(FadeCanvasGroupRoutine(returnButtonCanvasGroup, 1f, returnButtonFadeDuration));
        yield return new WaitUntil(() => returnRequested);

        yield return StartCoroutine(FadeOverlayRoutine(1f, finalFadeDuration));
        SceneManager.LoadScene(homeSceneName);
    }

    private IEnumerator MoveActorRoutine(
        Transform actorRoot,
        Transform targetPoint,
        float moveSpeed,
        Animator animatorRef,
        FootstepLoopPlayer loopPlayer,
        Action onReached)
    {
        if (actorRoot == null || targetPoint == null)
        {
            StopActorMovementAnimation(animatorRef);
            loopPlayer?.EndLoop();
            onReached?.Invoke();
            yield break;
        }

        while (Vector3.Distance(actorRoot.position, targetPoint.position) > arriveThreshold)
        {
            actorRoot.position = Vector3.MoveTowards(
                actorRoot.position,
                targetPoint.position,
                moveSpeed * Time.deltaTime);

            yield return null;
        }

        actorRoot.position = targetPoint.position;

        loopPlayer?.EndLoop();
        StopActorMovementAnimation(animatorRef);
        onReached?.Invoke();
    }

    private void StopActorMovementAnimation(Animator animatorRef)
    {
        if (animatorRef == null)
        {
            return;
        }

        SetActorRunning(animatorRef, false);

        if (forcePlayIdleStateOnArrival && !string.IsNullOrWhiteSpace(idleStateName))
        {
            PlayDirectState(animatorRef, idleStateName);
            animatorRef.Update(0f);
        }
    }

    private IEnumerator RevealRuinedKingdomRoutine()
    {
        float time = 0f;
        float startFadeAmount = GetFadeAmount();
        float startBackgroundAlpha = GetRuinedBackgroundAlpha();
        bool dawnEnvironmentShown = false;

        while (time < revealDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / Mathf.Max(0.01f, revealDuration));

            SetFadeAmount(Mathf.Lerp(startFadeAmount, 0f, t));

            if (!keepBackgroundVisibleBehindFade)
            {
                SetRuinedBackgroundAlpha(Mathf.Lerp(startBackgroundAlpha, 1f, t));
            }

            if (!dawnEnvironmentShown && t >= dawnEnvironmentShowTiming)
            {
                dawnEnvironmentShown = true;
                SetDawnEnvironmentVisible(true);
            }

            yield return null;
        }

        SetFadeAmount(0f);

        if (!keepBackgroundVisibleBehindFade)
        {
            SetRuinedBackgroundAlpha(1f);
        }

        SetDawnEnvironmentVisible(true);
    }

    private IEnumerator ShowLineRoutine(string speakerName, string line)
    {
        if (lineText == null || string.IsNullOrWhiteSpace(line))
        {
            yield break;
        }

        if (nameText != null)
        {
            nameText.text = speakerName;
            nameText.gameObject.SetActive(!string.IsNullOrWhiteSpace(speakerName));
        }

        lineText.gameObject.SetActive(true);
        lineText.maxVisibleCharacters = 0;
        lineText.text = line;
        lineText.ForceMeshUpdate();

        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroupRoutine(dialogueCanvasGroup, 1f, dialogueFadeDuration));
        }

        yield return StartCoroutine(RevealLineTextRoutine(line));

        if (lineHoldDuration > 0f)
        {
            yield return WaitForSecondsCompat(lineHoldDuration);
        }

        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroupRoutine(dialogueCanvasGroup, 0f, dialogueFadeDuration));
        }

        lineText.maxVisibleCharacters = int.MaxValue;
    }

    private IEnumerator RevealLineTextRoutine(string fullLine)
    {
        if (lineText == null)
        {
            yield break;
        }

        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            lineText.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        lineText.ForceMeshUpdate();
        TMP_TextInfo textInfo = lineText.textInfo;
        int totalVisibleCharacters = textInfo.characterCount;

        if (totalVisibleCharacters <= 0)
        {
            lineText.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        float secondsPerCharacter = 1f / Mathf.Max(0.01f, charactersPerSecond);

        for (int i = 0; i < totalVisibleCharacters; i++)
        {
            lineText.maxVisibleCharacters = i + 1;

            char currentChar = GetCharacterAtVisibleIndex(textInfo, i);
            float wait = secondsPerCharacter + GetAdditionalDelayForCharacter(currentChar);
            if (wait > 0f)
            {
                yield return WaitForSecondsCompat(wait);
            }
        }

        lineText.maxVisibleCharacters = int.MaxValue;
    }

    private char GetCharacterAtVisibleIndex(TMP_TextInfo textInfo, int visibleIndex)
    {
        if (textInfo == null || visibleIndex < 0 || visibleIndex >= textInfo.characterCount)
        {
            return '\0';
        }

        return textInfo.characterInfo[visibleIndex].character;
    }

    private float GetAdditionalDelayForCharacter(char c)
    {
        if (punctuationExtraDelay <= 0f)
        {
            return 0f;
        }

        switch (c)
        {
            case '。':
            case '、':
            case '…':
            case '.':
            case ',':
            case '!':
            case '?':
            case '！':
            case '？':
                return punctuationExtraDelay;
            default:
                return 0f;
        }
    }

    private object WaitForSecondsCompat(float seconds)
    {
        if (useUnscaledTimeForDialogue)
        {
            return new WaitForSecondsRealtime(seconds);
        }

        return new WaitForSeconds(seconds);
    }

    private IEnumerator FadeCanvasGroupRoutine(CanvasGroup targetCanvasGroup, float targetAlpha, float duration)
    {
        if (targetCanvasGroup == null)
        {
            yield break;
        }

        if (targetAlpha > 0.99f)
        {
            targetCanvasGroup.gameObject.SetActive(true);
        }

        if (duration <= 0f)
        {
            targetCanvasGroup.alpha = targetAlpha;
            ApplyCanvasGroupInteractableState(targetCanvasGroup, targetAlpha > 0.99f);
            if (targetAlpha <= 0.01f)
            {
                targetCanvasGroup.gameObject.SetActive(false);
            }
            yield break;
        }

        float startAlpha = targetCanvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            targetCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        targetCanvasGroup.alpha = targetAlpha;
        ApplyCanvasGroupInteractableState(targetCanvasGroup, targetAlpha > 0.99f);

        if (targetAlpha <= 0.01f)
        {
            targetCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void ApplyCanvasGroupInteractableState(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private IEnumerator FadeOverlayRoutine(float targetAmount, float duration)
    {
        if (fadeOverlay == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetFadeAmount(targetAmount);
            yield break;
        }

        float startAmount = GetFadeAmount();
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            SetFadeAmount(Mathf.Lerp(startAmount, targetAmount, t));
            yield return null;
        }

        SetFadeAmount(targetAmount);
    }

    private void OnPressedReturnHomeButton()
    {
        returnRequested = true;
    }

    private void SetActorRunning(Animator animatorRef, bool isRunning)
    {
        if (animatorRef == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(runningBoolName))
        {
            animatorRef.SetBool(runningBoolName, isRunning);
        }

        if (isRunning && string.IsNullOrWhiteSpace(runningBoolName))
        {
            PlayDirectState(animatorRef, walkStateName);
        }
    }

    private void PlayDirectState(Animator animatorRef, string stateName)
    {
        if (animatorRef == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        animatorRef.Play(stateName, 0, 0f);
    }

    private void FaceTowardTarget(Transform actorRoot, Transform target, SpriteRenderer sprite)
    {
        if (actorRoot == null || target == null || sprite == null)
        {
            return;
        }

        float deltaX = target.position.x - actorRoot.position.x;
        if (Mathf.Abs(deltaX) <= 0.001f)
        {
            return;
        }

        sprite.flipX = deltaX < 0f;
    }

    private void ConfigureFadeOverlayForVerticalWipe()
    {
        if (fadeOverlay == null || !useVerticalWipeFade)
        {
            return;
        }

        fadeOverlay.type = Image.Type.Filled;
        fadeOverlay.fillMethod = Image.FillMethod.Vertical;
        fadeOverlay.fillOrigin = (int)Image.OriginVertical.Top;
        fadeOverlay.fillClockwise = false;

        Color c = fadeOverlay.color;
        c.a = 1f;
        fadeOverlay.color = c;
    }

    private void ResetDialogueVisual()
    {
        if (nameText != null)
        {
            nameText.text = string.Empty;
            nameText.gameObject.SetActive(false);
        }

        if (lineText != null)
        {
            lineText.text = string.Empty;
            lineText.maxVisibleCharacters = int.MaxValue;
            lineText.gameObject.SetActive(false);
        }

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
            dialogueCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void SetCanvasGroupVisibleImmediate(CanvasGroup canvasGroup, bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.gameObject.SetActive(visible);
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private void SetFadeAmount(float amount)
    {
        if (fadeOverlay == null)
        {
            return;
        }

        amount = Mathf.Clamp01(amount);

        if (useVerticalWipeFade && fadeOverlay.type == Image.Type.Filled)
        {
            fadeOverlay.fillAmount = amount;

            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            return;
        }

        Color color = fadeOverlay.color;
        color.a = amount;
        fadeOverlay.color = color;
    }

    private float GetFadeAmount()
    {
        if (fadeOverlay == null)
        {
            return 0f;
        }

        if (useVerticalWipeFade && fadeOverlay.type == Image.Type.Filled)
        {
            return fadeOverlay.fillAmount;
        }

        return fadeOverlay.color.a;
    }

    private void SetDawnEnvironmentVisible(bool visible)
    {
        if (dawnEnvironmentObjects == null)
        {
            return;
        }

        for (int i = 0; i < dawnEnvironmentObjects.Length; i++)
        {
            GameObject obj = dawnEnvironmentObjects[i];
            if (obj == null)
            {
                continue;
            }

            obj.SetActive(visible);
        }
    }

    private void SetRuinedBackgroundAlpha(float alpha)
    {
        if (ruinedBackgroundRenderers == null)
        {
            return;
        }

        for (int i = 0; i < ruinedBackgroundRenderers.Length; i++)
        {
            SpriteRenderer sr = ruinedBackgroundRenderers[i];
            if (sr == null)
            {
                continue;
            }

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private float GetRuinedBackgroundAlpha()
    {
        if (ruinedBackgroundRenderers == null || ruinedBackgroundRenderers.Length == 0)
        {
            return 0f;
        }

        SpriteRenderer sr = ruinedBackgroundRenderers[0];
        if (sr == null)
        {
            return 0f;
        }

        return sr.color.a;
    }
}
