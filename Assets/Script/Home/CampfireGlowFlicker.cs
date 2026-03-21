using UnityEngine;

public class CampfireGlowFlicker : MonoBehaviour
{
    [Header("Glow Renderers")]
    [SerializeField] private SpriteRenderer radialGlow;
    [SerializeField] private SpriteRenderer groundGlow;

    [Header("Base Alpha")]
    [SerializeField, Range(0f, 1f)] private float radialBaseAlpha = 0.32f;
    [SerializeField, Range(0f, 1f)] private float groundBaseAlpha = 0.22f;

    [Header("Alpha Flicker Amount")]
    [SerializeField, Range(0f, 1f)] private float radialAlphaRange = 0.08f;
    [SerializeField, Range(0f, 1f)] private float groundAlphaRange = 0.05f;

    [Header("Base Scale")]
    [SerializeField] private Vector3 radialBaseScale = new Vector3(3.2f, 3.2f, 1f);
    [SerializeField] private Vector3 groundBaseScale = new Vector3(3.8f, 1.4f, 1f);

    [Header("Scale Flicker Amount")]
    [SerializeField] private float radialScaleRange = 0.18f;
    [SerializeField] private float groundScaleRangeX = 0.20f;
    [SerializeField] private float groundScaleRangeY = 0.08f;

    [Header("Flicker Speed")]
    [SerializeField] private float radialNoiseSpeed = 2.4f;
    [SerializeField] private float groundNoiseSpeed = 1.8f;

    [Header("Color")]
    [SerializeField] private Color glowColor = new Color(1.0f, 0.55f, 0.18f, 1f);

    [Header("Noise Seed")]
    [SerializeField] private float noiseSeed = 10f;

    private Vector3 radialInitialScale;
    private Vector3 groundInitialScale;

    private void Awake()
    {
        if (radialGlow != null)
        {
            radialInitialScale = radialBaseScale;
            radialGlow.transform.localScale = radialBaseScale;
            SetRendererColor(radialGlow, glowColor, radialBaseAlpha);
        }

        if (groundGlow != null)
        {
            groundInitialScale = groundBaseScale;
            groundGlow.transform.localScale = groundBaseScale;
            SetRendererColor(groundGlow, glowColor, groundBaseAlpha);
        }
    }

    private void Update()
    {
        float time = Time.time;

        UpdateRadialGlow(time);
        UpdateGroundGlow(time);
    }

    private void UpdateRadialGlow(float time)
    {
        if (radialGlow == null)
        {
            return;
        }

        float noise = Mathf.PerlinNoise(noiseSeed, time * radialNoiseSpeed);
        float centeredNoise = (noise - 0.5f) * 2f;

        float alpha = radialBaseAlpha + centeredNoise * radialAlphaRange;
        alpha = Mathf.Clamp01(alpha);

        float scaleOffset = centeredNoise * radialScaleRange;
        Vector3 scale = radialInitialScale + new Vector3(scaleOffset, scaleOffset, 0f);

        radialGlow.transform.localScale = scale;
        SetRendererColor(radialGlow, glowColor, alpha);
    }

    private void UpdateGroundGlow(float time)
    {
        if (groundGlow == null)
        {
            return;
        }

        float noise = Mathf.PerlinNoise(noiseSeed + 31.7f, time * groundNoiseSpeed);
        float centeredNoise = (noise - 0.5f) * 2f;

        float alpha = groundBaseAlpha + centeredNoise * groundAlphaRange;
        alpha = Mathf.Clamp01(alpha);

        Vector3 scale = groundInitialScale + new Vector3(
            centeredNoise * groundScaleRangeX,
            centeredNoise * groundScaleRangeY,
            0f
        );

        groundGlow.transform.localScale = scale;
        SetRendererColor(groundGlow, glowColor, alpha);
    }

    private void SetRendererColor(SpriteRenderer target, Color baseColor, float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        target.color = c;
    }
}