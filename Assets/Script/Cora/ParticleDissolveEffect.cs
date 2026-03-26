// =============================================================
// ParticleDissolveEffect.cs
// デスストランディング風の粒子分解エフェクト
//
// SpriteRenderer のバウンズから粒子を放出し、
// 上方向にゆっくり漂いながら消える。
//
// 使い方:
//   ParticleDissolveEffect.Play(targetTransform);
//   呼ぶだけ。ParticleSystem は自動生成・自動破棄される。
// =============================================================
using UnityEngine;

public static class ParticleDissolveEffect
{
    /// <summary>
    /// 指定した Transform の位置に粒子分解エフェクトを生成する。
    /// </summary>
    public static void Play(Transform target, DissolveSettings settings = null)
    {
        if (target == null) return;

        if (settings == null) settings = DissolveSettings.Default;

        // スプライトのバウンズを取得
        SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
        Bounds bounds;
        if (sr != null)
        {
            bounds = sr.bounds;
        }
        else
        {
            bounds = new Bounds(target.position, Vector3.one * 0.5f);
        }

        // ParticleSystem 用の GameObject を生成
        GameObject psObj = new GameObject("DissolveParticles");
        psObj.transform.position = bounds.center;

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

        // ── Main Module ──
        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(settings.lifetimeMin, settings.lifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(settings.speedMin, settings.speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.sizeMin, settings.sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(settings.colorStart, settings.colorEnd);
        main.maxParticles = settings.particleCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = settings.gravity;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.duration = settings.lifetimeMax + 0.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        // ── Emission（一斉バースト）──
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, settings.particleCount)
        });

        // ── Shape（敵のバウンズに合わせた放出範囲）──
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(
            bounds.size.x * settings.boundsScale,
            bounds.size.y * settings.boundsScale,
            0.05f);

        // ── Velocity over Lifetime（ゆっくり上昇 + ランダムな横揺れ）──
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-settings.driftX, settings.driftX);
        vel.y = new ParticleSystem.MinMaxCurve(settings.riseSpeedMin, settings.riseSpeedMax);
        vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

        // ── Noise（ふわふわ漂う動き）──
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = settings.noiseStrength;
        noise.frequency = settings.noiseFrequency;
        noise.scrollSpeed = 0.15f;
        noise.damping = true;
        noise.octaveCount = 2;

        // ── Size over Lifetime（だんだん小さくなる）──
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.3f, 0.9f);
        sizeCurve.AddKey(0.7f, 0.5f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Color over Lifetime（フェードアウト）──
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fadeGrad = new Gradient();
        fadeGrad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLife.color = new ParticleSystem.MinMaxGradient(fadeGrad);

        // ── Renderer ──
        ParticleSystemRenderer renderer = psObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sr != null ? sr.sortingOrder + 1 : 5;

        // Additive マテリアル（光る粒子感）
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.SetFloat("_Mode", 1); // Additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_ColorMode", 0);
        renderer.material = mat;

        // 再生開始
        ps.Play();

        // 自動破棄（main.stopAction = Destroy で自動破棄されるが安全策）
        Object.Destroy(psObj, main.duration + 1f);
    }
}

// =============================================================
// 設定クラス
// =============================================================
[System.Serializable]
public class DissolveSettings
{
    [Header("粒子数")]
    public int particleCount = 60;

    [Header("寿命")]
    public float lifetimeMin = 0.8f;
    public float lifetimeMax = 1.6f;

    [Header("初速")]
    public float speedMin = 0.05f;
    public float speedMax = 0.2f;

    [Header("上昇")]
    public float riseSpeedMin = 0.15f;
    public float riseSpeedMax = 0.45f;

    [Header("横揺れ")]
    public float driftX = 0.12f;

    [Header("サイズ")]
    public float sizeMin = 0.015f;
    public float sizeMax = 0.055f;

    [Header("重力（マイナスで上昇）")]
    public float gravity = -0.03f;

    [Header("色")]
    public Color colorStart = new Color(0.95f, 0.85f, 0.5f, 1f);   // 金色
    public Color colorEnd = new Color(0.7f, 0.55f, 0.3f, 0.7f);    // 暗い金

    [Header("バウンズ")]
    public float boundsScale = 0.85f;

    [Header("ノイズ")]
    public float noiseStrength = 0.25f;
    public float noiseFrequency = 0.8f;

    private static DissolveSettings defaultSettings;
    public static DissolveSettings Default
    {
        get
        {
            if (defaultSettings == null) defaultSettings = new DissolveSettings();
            return defaultSettings;
        }
    }
}
