using System.Collections;
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
    [SerializeField] private float blinkSpeed = 1.8f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float tapFloatSpeed = 1.25f;
    [SerializeField] private float tapFloatDistance = 6f;

    [Header("Intro Animation")]
    [SerializeField] private float titleFadeDelay = 0.25f;
    [SerializeField] private float titleFadeDuration = 0.7f;
    [SerializeField] private float tapAppearDelay = 0.9f;

    [Header("Background Motion")]
    [SerializeField] private float zoomScaleMin = 1.00f;
    [SerializeField] private float zoomScaleMax = 1.03f;
    [SerializeField] private float zoomSpeed = 0.18f;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float blackHoldDuration = 0.12f;

    [Header("Sound")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip uiDecideSe;

    private bool isTransitioning = false;
    private bool canBlinkTapText = false;
    private Vector2 tapBaseAnchoredPosition;
    private Vector3 backgroundBaseScale = Vector3.one;

    private void Awake()
    {
        if (tapToStartText != null)
        {
            tapBaseAnchoredPosition = tapToStartText.rectTransform.anchoredPosition;

            Color c = tapToStartText.color;
            c.a = 0f;
            tapToStartText.color = c;
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
    }

    private void Start()
    {
        StartCoroutine(PlayIntroRoutine());
    }

    private void Update()
    {
        UpdateBackgroundZoom();

        if (isTransitioning)
        {
            return;
        }

        if (canBlinkTapText)
        {
            UpdateTapBlink();
        }

        if (HasStartInput())
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    private IEnumerator PlayIntroRoutine()
    {
        if (titleCanvasGroup != null)
        {
            yield return new WaitForSeconds(titleFadeDelay);
            yield return StartCoroutine(FadeCanvasGroupRoutine(titleCanvasGroup, 0f, 1f, titleFadeDuration));
        }

        yield return new WaitForSeconds(tapAppearDelay);

        if (tapToStartText != null)
        {
            Color c = tapToStartText.color;
            c.a = maxAlpha;
            tapToStartText.color = c;
        }

        canBlinkTapText = true;
    }

    private void UpdateBackgroundZoom()
    {
        if (backgroundRectTransform == null)
        {
            return;
        }

        float t = Mathf.PingPong(Time.time * zoomSpeed, 1f);
        float scale = Mathf.Lerp(zoomScaleMin, zoomScaleMax, t);
        backgroundRectTransform.localScale = backgroundBaseScale * scale;
    }

    private void UpdateTapBlink()
    {
        if (tapToStartText == null)
        {
            return;
        }

        Color c = tapToStartText.color;
        float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        c.a = Mathf.Lerp(minAlpha, maxAlpha, blink);
        tapToStartText.color = c;

        float floatOffset = Mathf.Sin(Time.time * tapFloatSpeed * Mathf.PI * 2f) * tapFloatDistance;
        tapToStartText.rectTransform.anchoredPosition = tapBaseAnchoredPosition + new Vector2(0f, floatOffset);
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

    private IEnumerator StartGameRoutine()
    {
        isTransitioning = true;
        canBlinkTapText = false;

        if (seSource != null && uiDecideSe != null)
        {
            seSource.PlayOneShot(uiDecideSe);
        }

        if (tapToStartText != null)
        {
            tapToStartText.gameObject.SetActive(false);
        }

        yield return StartCoroutine(FadeOutRoutine());

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blackHoldDuration);
        }

        SceneManager.LoadScene(homeSceneName);
    }

    private IEnumerator FadeCanvasGroupRoutine(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        float time = 0f;
        group.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        group.alpha = to;
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