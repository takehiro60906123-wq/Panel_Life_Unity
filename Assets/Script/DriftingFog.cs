using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DriftingFogLocal : MonoBehaviour
{
    [Header("Horizontal Move (Local)")]
    [SerializeField] private float moveSpeed = 0.2f;
    [SerializeField] private float leftLocalX = -6f;
    [SerializeField] private float rightLocalX = 6f;
    [SerializeField] private bool moveToRight = true;

    [Header("Vertical Bob (Local)")]
    [SerializeField] private bool useVerticalBob = true;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 0.6f;

    [Header("Alpha Pulse")]
    [SerializeField] private bool useAlphaPulse = true;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.32f;
    [SerializeField] private float alphaPulseSpeed = 0.5f;

    [Header("Randomize")]
    [SerializeField] private bool randomizePhaseOnStart = true;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseLocalPos;
    private float bobPhase;
    private float alphaPhase;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseLocalPos = transform.localPosition;
    }

    private void Start()
    {
        if (randomizePhaseOnStart)
        {
            bobPhase = Random.Range(0f, Mathf.PI * 2f);
            alphaPhase = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        MoveHorizontalLocal();
        ApplyVerticalBobLocal();
        ApplyAlphaPulse();
    }

    private void MoveHorizontalLocal()
    {
        Vector3 localPos = transform.localPosition;
        float dir = moveToRight ? 1f : -1f;

        localPos.x += dir * moveSpeed * Time.deltaTime;

        if (moveToRight && localPos.x > rightLocalX)
        {
            localPos.x = leftLocalX;
        }
        else if (!moveToRight && localPos.x < leftLocalX)
        {
            localPos.x = rightLocalX;
        }

        transform.localPosition = localPos;
    }

    private void ApplyVerticalBobLocal()
    {
        if (!useVerticalBob) return;

        Vector3 localPos = transform.localPosition;
        localPos.y = baseLocalPos.y + Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
        transform.localPosition = localPos;
    }

    private void ApplyAlphaPulse()
    {
        if (!useAlphaPulse || spriteRenderer == null) return;

        Color c = spriteRenderer.color;
        float t = (Mathf.Sin(Time.time * alphaPulseSpeed + alphaPhase) + 1f) * 0.5f;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        spriteRenderer.color = c;
    }
}