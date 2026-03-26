using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreenController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Transform exitPoint;

    [Header("Subordinate")]
    [SerializeField] private Transform subordinateRoot;
    [SerializeField] private Animator subordinateAnimator;
    [SerializeField] private SpriteRenderer subordinateSpriteRenderer;
    [SerializeField] private Transform subordinateExitPoint;

    [Header("UI")]
    [SerializeField] private TMP_Text tapToStartText;
    [SerializeField] private TMP_Text playerCommentText;
    [SerializeField] private CanvasGroup playerCommentCanvasGroup;
    [SerializeField] private TMP_Text subordinateCommentText;
    [SerializeField] private CanvasGroup subordinateCommentCanvasGroup;
    [SerializeField] private Image fadeOverlay;

    [Header("Camera")]
    [SerializeField] private HomeCameraController homeCameraController;

    [Header("Scene")]
    [SerializeField] private string battleSceneName = "Battle";

    [Header("Animation")]
    [SerializeField] private string runStateName = "RUN";
    [SerializeField] private string subordinateRunStateName = "RUN";
    [SerializeField] private float commentFadeDuration = 0.15f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float subordinateRunSpeed = 3.7f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float homeFadeInDuration = 0.6f;

    [Header("Idle Dialogue")]
    [SerializeField, TextArea(1, 3)] private string[] subordinateIdleLines =
    {
        "……嫌な音ですね。",
        "まだお戻りになれます。深入りはお勧めしません。",
        "それで、あの鼓動を追うおつもりですか。",
        "お供はいたしますが、褒められた趣味とは思えません。",
        "先に申し上げておきます。私は反対しました。"
    };
    [SerializeField] private float idleDialogueStartDelay = 0.6f;
    [SerializeField] private float idleDialogueHoldDuration = 1.9f;
    [SerializeField] private float idleDialogueInterval = 0.9f;

    [Header("Departure Dialogue")]
    [SerializeField] private string playerDepartureLine = "行くか";
    [SerializeField] private string subordinateSighLine = "……はぁ";
    [SerializeField] private float playerDepartureHoldDuration = 0.65f;
    [SerializeField] private float subordinateSighDelay = 0.25f;
    [SerializeField] private float subordinateSighHoldDuration = 0.55f;
    [SerializeField] private float subordinateFollowDelay = 0.05f;

    [Header("Sound")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip uiDecideSe;
    [SerializeField] private FootstepLoopPlayer footstepLoopPlayer;
    [SerializeField] private FootstepLoopPlayer subordinateFootstepLoopPlayer;

    private bool isSequenceStarted;
    private bool isOpening = true;
    private bool leaderReachedExit;
    private bool subordinateReachedExit = true;
    private Coroutine idleDialogueCoroutine;

    private void Awake()
    {
        if (footstepLoopPlayer == null && playerRoot != null)
        {
            footstepLoopPlayer = playerRoot.GetComponentInChildren<FootstepLoopPlayer>(true);
        }

        if (subordinateFootstepLoopPlayer == null && subordinateRoot != null)
        {
            subordinateFootstepLoopPlayer = subordinateRoot.GetComponentInChildren<FootstepLoopPlayer>(true);
        }

        if (homeCameraController == null)
        {
            homeCameraController = FindObjectOfType<HomeCameraController>();
        }

        ResetCommentVisual(playerCommentText, playerCommentCanvasGroup);
        ResetCommentVisual(subordinateCommentText, subordinateCommentCanvasGroup);

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 1f;
            fadeOverlay.color = c;
            fadeOverlay.raycastTarget = false;
        }
    }

    private void Start()
    {
        StartCoroutine(HomeFadeInRoutine());
    }

    private void OnDisable()
    {
        footstepLoopPlayer?.EndLoop();
        subordinateFootstepLoopPlayer?.EndLoop();
    }

    private void Update()
    {
        if (isOpening || isSequenceStarted)
        {
            return;
        }

        if (HasStartInput())
        {
            StartCoroutine(BeginHomeDepartureRoutine());
        }
    }

    private bool HasStartInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private IEnumerator HomeFadeInRoutine()
    {
        if (fadeOverlay == null)
        {
            isOpening = false;
            StartIdleDialogueLoop();
            yield break;
        }

        float time = 0f;
        Color c = fadeOverlay.color;

        while (time < homeFadeInDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / Mathf.Max(0.01f, homeFadeInDuration));
            c.a = 1f - t;
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeOverlay.color = c;
        isOpening = false;
        StartIdleDialogueLoop();
    }

    private void StartIdleDialogueLoop()
    {
        if (idleDialogueCoroutine != null)
        {
            StopCoroutine(idleDialogueCoroutine);
        }

        if (!HasIdleDialogue())
        {
            return;
        }

        idleDialogueCoroutine = StartCoroutine(IdleDialogueLoopRoutine());
    }

    private bool HasIdleDialogue()
    {
        if (subordinateIdleLines == null || subordinateIdleLines.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < subordinateIdleLines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(subordinateIdleLines[i]))
            {
                return true;
            }
        }

        return false;
    }

    // 変更後
    private IEnumerator IdleDialogueLoopRoutine()
    {
        if (idleDialogueStartDelay > 0f)
        {
            yield return new WaitForSeconds(idleDialogueStartDelay);
        }

        for (int i = 0; i < subordinateIdleLines.Length; i++)
        {
            if (isSequenceStarted) yield break;

            string line = subordinateIdleLines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                yield break;
            }

            yield return StartCoroutine(ShowSubordinateCommentRoutine(line, idleDialogueHoldDuration));

            if (isSequenceStarted)
            {
                yield break;
            }

            if (idleDialogueInterval > 0f)
            {
                yield return new WaitForSeconds(idleDialogueInterval);
            }
        }
    }

    private string GetNextIdleLine(ref int index)
    {
        if (subordinateIdleLines == null || subordinateIdleLines.Length == 0)
        {
            return string.Empty;
        }

        int checkedCount = 0;
        while (checkedCount < subordinateIdleLines.Length)
        {
            string line = subordinateIdleLines[index];
            index = (index + 1) % subordinateIdleLines.Length;
            checkedCount++;

            if (!string.IsNullOrWhiteSpace(line))
            {
                return line;
            }
        }

        return string.Empty;
    }

    private IEnumerator BeginHomeDepartureRoutine()
    {
        if (isSequenceStarted)
        {
            yield break;
        }

        isSequenceStarted = true;

        if (idleDialogueCoroutine != null)
        {
            StopCoroutine(idleDialogueCoroutine);
            idleDialogueCoroutine = null;
        }

        ResetCommentVisual(subordinateCommentText, subordinateCommentCanvasGroup);

        if (seSource != null && uiDecideSe != null)
        {
            seSource.PlayOneShot(uiDecideSe);
        }

        if (tapToStartText != null)
        {
            tapToStartText.gameObject.SetActive(false);
        }

        yield return StartCoroutine(ShowPlayerCommentRoutine(playerDepartureLine, playerDepartureHoldDuration));

        FaceTowardExit(playerRoot, exitPoint, playerSpriteRenderer);
        PlayRunDirect(playerAnimator, runStateName);
        footstepLoopPlayer?.BeginLoop();
        leaderReachedExit = false;
        subordinateReachedExit = subordinateRoot == null;

        StartCoroutine(RunToExitRoutine(playerRoot, exitPoint, runSpeed, footstepLoopPlayer, () => leaderReachedExit = true));

        if (subordinateRoot != null)
        {
            yield return new WaitForSeconds(subordinateSighDelay);
            yield return StartCoroutine(ShowSubordinateCommentRoutine(subordinateSighLine, subordinateSighHoldDuration));

            if (subordinateFollowDelay > 0f)
            {
                yield return new WaitForSeconds(subordinateFollowDelay);
            }

            FaceTowardExit(subordinateRoot, GetSubordinateExitPoint(), subordinateSpriteRenderer);
            PlayRunDirect(subordinateAnimator, subordinateRunStateName);
            subordinateFootstepLoopPlayer?.BeginLoop();
            StartCoroutine(RunToExitRoutine(subordinateRoot, GetSubordinateExitPoint(), subordinateRunSpeed, subordinateFootstepLoopPlayer, () => subordinateReachedExit = true));
        }

        yield return new WaitUntil(() => leaderReachedExit && subordinateReachedExit);
        yield return StartCoroutine(FadeOutRoutine());

        SceneTransitionManager.TransitionToSceneWithTips(battleSceneName, TransitionType.DiamondIris);

    }

    private Transform GetSubordinateExitPoint()
    {
        return subordinateExitPoint != null ? subordinateExitPoint : exitPoint;
    }

    private void FaceTowardExit(Transform actorRoot, Transform targetExit, SpriteRenderer sprite)
    {
        if (actorRoot == null || targetExit == null || sprite == null)
        {
            return;
        }

        float deltaX = targetExit.position.x - actorRoot.position.x;
        if (Mathf.Abs(deltaX) <= 0.001f)
        {
            return;
        }

        sprite.flipX = deltaX < 0f;
    }

    private void PlayRunDirect(Animator animatorRef, string stateName)
    {
        if (animatorRef == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        animatorRef.Play(stateName, 0, 0f);
    }

    private IEnumerator ShowPlayerCommentRoutine(string line, float holdDuration)
    {
        yield return StartCoroutine(ShowCommentRoutine(playerCommentText, playerCommentCanvasGroup, line, holdDuration));
    }

    private IEnumerator ShowSubordinateCommentRoutine(string line, float holdDuration)
    {
        yield return StartCoroutine(ShowCommentRoutine(subordinateCommentText, subordinateCommentCanvasGroup, line, holdDuration));
    }

    private IEnumerator ShowCommentRoutine(TMP_Text targetText, CanvasGroup targetCanvasGroup, string line, float holdDuration)
    {
        if (targetText == null || string.IsNullOrWhiteSpace(line))
        {
            yield break;
        }

        targetText.gameObject.SetActive(true);
        targetText.text = line;
        SetTextAlpha(targetText, 1f);

        if (targetCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(targetCanvasGroup, 1f, commentFadeDuration));
        }
        else
        {
            SetTextAlpha(targetText, 1f);
        }

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        if (targetCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(targetCanvasGroup, 0f, commentFadeDuration));
        }
        else
        {
            SetTextAlpha(targetText, 0f);
        }

        targetText.text = string.Empty;
        targetText.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup targetCanvasGroup, float targetAlpha, float duration)
    {
        if (targetCanvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            targetCanvasGroup.alpha = targetAlpha;
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
    }

    private IEnumerator RunToExitRoutine(Transform actorRoot, Transform targetExit, float moveSpeed, FootstepLoopPlayer loopPlayer, System.Action onReached)
    {
        if (actorRoot == null || targetExit == null)
        {
            onReached?.Invoke();
            yield break;
        }

        while (Vector3.Distance(actorRoot.position, targetExit.position) > 0.05f)
        {
            actorRoot.position = Vector3.MoveTowards(actorRoot.position, targetExit.position, moveSpeed * Time.deltaTime);
            yield return null;
        }

        actorRoot.position = targetExit.position;
        loopPlayer?.EndLoop();
        onReached?.Invoke();
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadeOverlay == null)
        {
            yield break;
        }

        float time = 0f;
        Color c = fadeOverlay.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / Mathf.Max(0.01f, fadeDuration));
            c.a = t;
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeOverlay.color = c;
    }

    private void ResetCommentVisual(TMP_Text targetText, CanvasGroup targetCanvasGroup)
    {
        if (targetText != null)
        {
            targetText.text = string.Empty;
            SetTextAlpha(targetText, 1f);
            targetText.gameObject.SetActive(false);
        }

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = 0f;
            targetCanvasGroup.interactable = false;
            targetCanvasGroup.blocksRaycasts = false;
        }
    }

    private void SetTextAlpha(TMP_Text targetText, float alpha)
    {
        if (targetText == null)
        {
            return;
        }

        Color c = targetText.color;
        c.a = alpha;
        targetText.color = c;
    }
}
