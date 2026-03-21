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

    [Header("UI")]
    [SerializeField] private TMP_Text tapToStartText;
    [SerializeField] private TMP_Text commentText;
    [SerializeField] private CanvasGroup commentCanvasGroup;
    [SerializeField] private Image fadeOverlay;

    [Header("Scene")]
    [SerializeField] private string battleSceneName = "Battle";

    [Header("Animation")]
    [SerializeField] private string runStateName = "RUN";
    [SerializeField] private float standUpWait = 0.8f;
    [SerializeField] private float commentFadeDuration = 0.15f;
    [SerializeField] private float commentHoldDuration = 0.7f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float homeFadeInDuration = 0.6f;

    private bool isSequenceStarted = false;
    private bool isOpening = true;

    private void Awake()
    {
        if (commentText != null)
        {
            commentText.text = "行くか";
        }

        if (commentCanvasGroup != null)
        {
            commentCanvasGroup.alpha = 0f;
            commentCanvasGroup.interactable = false;
            commentCanvasGroup.blocksRaycasts = false;
        }

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

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            return true;
        }

        return false;
    }

    private IEnumerator HomeFadeInRoutine()
    {
        if (fadeOverlay == null)
        {
            isOpening = false;
            yield break;
        }

        float time = 0f;
        Color c = fadeOverlay.color;

        while (time < homeFadeInDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / homeFadeInDuration);
            c.a = 1f - t;
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeOverlay.color = c;
        isOpening = false;
    }

    private IEnumerator BeginHomeDepartureRoutine()
    {
        isSequenceStarted = true;

        if (tapToStartText != null)
        {
            tapToStartText.gameObject.SetActive(false);
        }

        yield return StartCoroutine(FadeComment(1f, commentFadeDuration));
        yield return new WaitForSeconds(commentHoldDuration);
        yield return StartCoroutine(FadeComment(0f, commentFadeDuration));

        FaceTowardExit();
        PlayRunDirect();

        yield return StartCoroutine(RunToExitRoutine());
        yield return StartCoroutine(FadeOutRoutine());

        SceneManager.LoadScene(battleSceneName);
    }

    private void FaceTowardExit()
    {
        if (playerRoot == null || exitPoint == null || playerSpriteRenderer == null)
        {
            return;
        }

        float deltaX = exitPoint.position.x - playerRoot.position.x;

        if (Mathf.Abs(deltaX) <= 0.001f)
        {
            return;
        }

        // 元絵が「右向き」ならこれ
        playerSpriteRenderer.flipX = deltaX < 0f;

        // もし逆向きになるなら、こっちに変える
        // playerSpriteRenderer.flipX = deltaX > 0f;
    }

    private void PlayRunDirect()
    {
        if (playerAnimator == null)
        {
            return;
        }

        playerAnimator.Play(runStateName, 0, 0f);
    }

    private IEnumerator FadeComment(float targetAlpha, float duration)
    {
        if (commentCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = commentCanvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            commentCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        commentCanvasGroup.alpha = targetAlpha;
    }

    private IEnumerator RunToExitRoutine()
    {
        if (playerRoot == null || exitPoint == null)
        {
            yield break;
        }

        while (Vector3.Distance(playerRoot.position, exitPoint.position) > 0.05f)
        {
            playerRoot.position = Vector3.MoveTowards(
                playerRoot.position,
                exitPoint.position,
                runSpeed * Time.deltaTime
            );

            yield return null;
        }

        playerRoot.position = exitPoint.position;
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
            float t = Mathf.Clamp01(time / fadeDuration);
            c.a = t;
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeOverlay.color = c;
    }
}
