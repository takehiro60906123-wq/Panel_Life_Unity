using System.Collections;
using UnityEngine;

public class HomeCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform focusTarget;
    [SerializeField] private Transform runTarget;
    [SerializeField] private SpriteRenderer backgroundBoundsSprite;

    [Header("Opening")]
    [SerializeField] private float openingDuration = 1.2f;
    [SerializeField] private float openingStartSize = 6.2f;
    [SerializeField] private float openingEndSize = 5.7f;

    [Header("Idle Drift")]
    [SerializeField] private float driftX = 0.05f;
    [SerializeField] private float driftY = 0.03f;
    [SerializeField] private float driftSpeedX = 0.25f;
    [SerializeField] private float driftSpeedY = 0.18f;

    [Header("Tap Focus")]
    [SerializeField] private float tapFocusSize = 5.3f;
    [SerializeField] private float tapFocusDuration = 0.35f;
    [SerializeField] private Vector2 tapFocusOffset = new Vector2(0f, 0.25f);

    [Header("Run Follow")]
    [SerializeField] private float followSmooth = 4.0f;
    [SerializeField] private float lookAheadX = 0.8f;
    [SerializeField] private float runSize = 5.1f;
    [SerializeField] private float runZoomSpeed = 3.0f;

    [Header("Clamp")]
    [SerializeField] private float clampPaddingX = 0f;
    [SerializeField] private float clampPaddingY = 0f;

    private Vector3 basePosition;
    private bool openingFinished;
    private bool runFollowActive;
    private Coroutine tapFocusCoroutine;
    private float openingTime;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        basePosition = transform.position;

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = openingStartSize;
        }

        transform.position = ClampToBackground(transform.position);
        basePosition = transform.position;
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (!openingFinished)
        {
            UpdateOpening();
            return;
        }

        if (runFollowActive)
        {
            UpdateRunFollow();
            return;
        }

        UpdateIdleDrift();
    }

    private void UpdateOpening()
    {
        openingTime += Time.deltaTime;
        float t = Mathf.Clamp01(openingTime / Mathf.Max(0.01f, openingDuration));

        Vector3 targetPos = basePosition;

        if (focusTarget != null)
        {
            targetPos = new Vector3(
                focusTarget.position.x,
                focusTarget.position.y + 0.3f,
                transform.position.z
            );
        }

        Vector3 nextPos = Vector3.Lerp(basePosition, targetPos, t * 0.35f);
        transform.position = ClampToBackground(nextPos);

        targetCamera.orthographicSize = Mathf.Lerp(openingStartSize, openingEndSize, t);

        // サイズ変化後に再clamp
        transform.position = ClampToBackground(transform.position);

        if (t >= 1f)
        {
            openingFinished = true;
            basePosition = transform.position;
        }
    }

    private void UpdateIdleDrift()
    {
        float x = Mathf.Sin(Time.time * driftSpeedX) * driftX;
        float y = Mathf.Sin(Time.time * driftSpeedY + 1.4f) * driftY;

        Vector3 drift = new Vector3(x, y, 0f);
        Vector3 nextPos = basePosition + drift;

        transform.position = ClampToBackground(nextPos);
    }

    private void UpdateRunFollow()
    {
        if (runTarget == null)
        {
            return;
        }

        Vector3 targetPos = new Vector3(
            runTarget.position.x + lookAheadX,
            basePosition.y,
            transform.position.z
        );

        Vector3 nextPos = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * followSmooth
        );

        transform.position = ClampToBackground(nextPos);

        targetCamera.orthographicSize = Mathf.Lerp(
            targetCamera.orthographicSize,
            runSize,
            Time.deltaTime * runZoomSpeed
        );

        // サイズ変化後に再clamp
        transform.position = ClampToBackground(transform.position);
    }

    public void PlayTapFocus()
    {
        if (!openingFinished || targetCamera == null)
        {
            return;
        }

        if (tapFocusCoroutine != null)
        {
            StopCoroutine(tapFocusCoroutine);
        }

        tapFocusCoroutine = StartCoroutine(TapFocusRoutine());
    }

    public void StartRunFollow()
    {
        runFollowActive = true;
    }

    private IEnumerator TapFocusRoutine()
    {
        float time = 0f;
        float startSize = targetCamera.orthographicSize;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos;

        if (focusTarget != null)
        {
            targetPos = new Vector3(
                focusTarget.position.x + tapFocusOffset.x,
                focusTarget.position.y + tapFocusOffset.y,
                startPos.z
            );
        }

        while (time < tapFocusDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / Mathf.Max(0.01f, tapFocusDuration));

            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);
            transform.position = ClampToBackground(nextPos);

            targetCamera.orthographicSize = Mathf.Lerp(startSize, tapFocusSize, t);

            // サイズ変化後に再clamp
            transform.position = ClampToBackground(transform.position);

            yield return null;
        }

        basePosition = transform.position;
        tapFocusCoroutine = null;
    }

    private Vector3 ClampToBackground(Vector3 targetPos)
    {
        if (targetCamera == null || backgroundBoundsSprite == null || !targetCamera.orthographic)
        {
            targetPos.z = transform.position.z;
            return targetPos;
        }

        Bounds bounds = backgroundBoundsSprite.bounds;

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        float minX = bounds.min.x + halfWidth + clampPaddingX;
        float maxX = bounds.max.x - halfWidth - clampPaddingX;
        float minY = bounds.min.y + halfHeight + clampPaddingY;
        float maxY = bounds.max.y - halfHeight - clampPaddingY;

        if (minX > maxX)
        {
            targetPos.x = bounds.center.x;
        }
        else
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        }

        if (minY > maxY)
        {
            targetPos.y = bounds.center.y;
        }
        else
        {
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        targetPos.z = transform.position.z;
        return targetPos;
    }
}