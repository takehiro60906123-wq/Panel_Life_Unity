using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class BattleEffectController : MonoBehaviour
{
    [Header("Damage Text - Normal")]
    [SerializeField] private float normalEntryDrop = 0.10f;
    [SerializeField] private float normalRiseHeight = 1.55f;
    [SerializeField] private float normalSideDrift = 0.42f;
    [SerializeField] private float normalPopDuration = 0.11f;
    [SerializeField] private float normalDriftDuration = 0.46f;
    [SerializeField] private float normalHoldBeforeFade = 0.20f;
    [SerializeField] private float normalFadeDuration = 0.28f;
    [SerializeField] private float normalStartScale = 0.62f;
    [SerializeField] private float normalPeakScale = 1.18f;
    [SerializeField] private float normalFinalScale = 1.00f;

    [Header("Damage Text - Critical")]
    [SerializeField] private float criticalEntryDrop = 0.14f;
    [SerializeField] private float criticalRiseHeight = 1.90f;
    [SerializeField] private float criticalSideDrift = 0.52f;
    [SerializeField] private float criticalPopDuration = 0.13f;
    [SerializeField] private float criticalDriftDuration = 0.62f;
    [SerializeField] private float criticalHoldBeforeFade = 0.26f;
    [SerializeField] private float criticalFadeDuration = 0.34f;
    [SerializeField] private float criticalStartScale = 0.72f;
    [SerializeField] private float criticalPeakScale = 1.34f;
    [SerializeField] private float criticalFinalScale = 1.06f;

    [Header("Damage Text - Miss / Guard")]
    [SerializeField] private float utilityEntryDrop = 0.05f;
    [SerializeField] private float utilityRiseHeight = 1.10f;
    [SerializeField] private float utilitySideDrift = 0.28f;
    [SerializeField] private float utilityPopDuration = 0.10f;
    [SerializeField] private float utilityDriftDuration = 0.36f;
    [SerializeField] private float utilityHoldBeforeFade = 0.16f;
    [SerializeField] private float utilityFadeDuration = 0.24f;
    [SerializeField] private float utilityStartScale = 0.88f;
    [SerializeField] private float utilityPeakScale = 1.04f;
    [SerializeField] private float utilityFinalScale = 1.00f;

    [Header("Damage Text - Reward / Info")]
    [SerializeField] private float rewardEntryDrop = 0.08f;
    [SerializeField] private float rewardRiseHeight = 1.35f;
    [SerializeField] private float rewardSideDrift = 0.34f;
    [SerializeField] private float rewardPopDuration = 0.12f;
    [SerializeField] private float rewardDriftDuration = 0.54f;
    [SerializeField] private float rewardHoldBeforeFade = 0.30f;
    [SerializeField] private float rewardFadeDuration = 0.30f;
    [SerializeField] private float rewardStartScale = 0.72f;
    [SerializeField] private float rewardPeakScale = 1.16f;
    [SerializeField] private float rewardFinalScale = 1.00f;

    [Header("Damage Text - Global")]
    [SerializeField] private float secondaryRiseRatio = 0.52f;
    [SerializeField] private float secondarySideRatio = 0.38f;
    [SerializeField] private float randomStartX = 0.08f;
    [SerializeField] private float criticalColorLift = 0.18f;
    [SerializeField] private float rewardColorLift = 0.08f;

    private EffectPoolManager effectPoolManager;

    private enum DamageTextStyle
    {
        Normal,
        Critical,
        Miss,
        Guard,
        Reward
    }

    private sealed class DamageTextPoseCache : MonoBehaviour
    {
        public bool captured;
        public Vector3 rootLocalScale;
        public Vector3 textLocalPosition;
        public Vector3 textLocalScale;
        public Quaternion textLocalRotation;

        public void Capture(Transform root, Transform textTransform)
        {
            if (captured) return;

            rootLocalScale = root.localScale;

            if (textTransform != null)
            {
                textLocalPosition = textTransform.localPosition;
                textLocalScale = textTransform.localScale;
                textLocalRotation = textTransform.localRotation;
            }

            captured = true;
        }

        public void ResetPose(Transform root, Transform textTransform, Vector3 worldPosition)
        {
            root.position = worldPosition;
            root.rotation = Quaternion.identity;
            root.localScale = rootLocalScale == Vector3.zero ? Vector3.one : rootLocalScale;

            if (textTransform != null)
            {
                textTransform.localPosition = textLocalPosition;
                textTransform.localScale = textLocalScale == Vector3.zero ? Vector3.one : textLocalScale;
                textTransform.localRotation = textLocalRotation == Quaternion.identity ? Quaternion.identity : textLocalRotation;
            }
        }
    }

    public void Configure(EffectPoolManager poolManager)
    {
        effectPoolManager = poolManager;
    }

    public void SpawnDamageText(GameObject damageTextPrefab, string text, Vector3 position, Color color)
    {
        if (damageTextPrefab == null) return;

        GameObject textObj = GetPooledObject(damageTextPrefab, position, Quaternion.identity);
        if (textObj == null) return;

        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
        {
            ReturnPooledObject(damageTextPrefab, textObj);
            return;
        }

        Transform root = textObj.transform;
        Transform textTransform = tmp.transform;

        root.DOKill();
        textTransform.DOKill();
        tmp.DOKill();

        DamageTextPoseCache poseCache = textObj.GetComponent<DamageTextPoseCache>();
        if (poseCache == null)
        {
            poseCache = textObj.AddComponent<DamageTextPoseCache>();
        }

        poseCache.Capture(root, textTransform);
        poseCache.ResetPose(root, textTransform, position);

        DamageTextStyle style = ResolveDamageTextStyle(text);
        MotionProfile profile = BuildMotionProfile(style);

        Color baseColor = color;
        baseColor.a = 1f;

        tmp.text = text;
        tmp.color = GetStartColor(baseColor, style);
        tmp.alpha = 1f;

        float side = UnityEngine.Random.Range(-profile.sideDrift, profile.sideDrift);
        float startX = UnityEngine.Random.Range(-randomStartX, randomStartX);

        root.position = position + new Vector3(startX, -profile.entryDrop, 0f);
        textTransform.localScale = poseCache.textLocalScale * profile.startScale;

        Vector3 midPos = position + new Vector3(side * secondarySideRatio, profile.riseHeight * secondaryRiseRatio, 0f);
        Vector3 endPos = position + new Vector3(side, profile.riseHeight, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(root.DOMove(midPos, profile.popDuration).SetEase(Ease.OutCubic));
        seq.Join(textTransform.DOScale(poseCache.textLocalScale * profile.peakScale, profile.popDuration).SetEase(Ease.OutBack));

        if (style == DamageTextStyle.Critical)
        {
            seq.Join(DOVirtual.Color(tmp.color, baseColor, profile.popDuration, c => tmp.color = c).SetEase(Ease.OutQuad));
        }
        else
        {
            seq.Join(DOVirtual.Color(tmp.color, baseColor, Mathf.Min(0.08f, profile.popDuration), c => tmp.color = c).SetEase(Ease.OutQuad));
        }

        seq.Append(root.DOMove(endPos, profile.driftDuration).SetEase(Ease.OutQuad));
        seq.Join(textTransform.DOScale(poseCache.textLocalScale * profile.finalScale, profile.driftDuration * 0.55f).SetEase(Ease.OutQuad));

        if (style == DamageTextStyle.Critical)
        {
            seq.Insert(profile.popDuration + 0.04f,
                textTransform.DOScale(poseCache.textLocalScale * (profile.finalScale * 1.05f), 0.08f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));
        }

        seq.Insert(profile.popDuration + profile.holdBeforeFade, tmp.DOFade(0f, profile.fadeDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (tmp != null)
            {
                tmp.alpha = 1f;
                tmp.color = baseColor;
            }

            poseCache.ResetPose(root, textTransform, position);
            ReturnPooledObject(damageTextPrefab, textObj);
        });
    }

    private readonly struct MotionProfile
    {
        public readonly float entryDrop;
        public readonly float riseHeight;
        public readonly float sideDrift;
        public readonly float popDuration;
        public readonly float driftDuration;
        public readonly float holdBeforeFade;
        public readonly float fadeDuration;
        public readonly float startScale;
        public readonly float peakScale;
        public readonly float finalScale;

        public MotionProfile(
            float entryDrop,
            float riseHeight,
            float sideDrift,
            float popDuration,
            float driftDuration,
            float holdBeforeFade,
            float fadeDuration,
            float startScale,
            float peakScale,
            float finalScale)
        {
            this.entryDrop = entryDrop;
            this.riseHeight = riseHeight;
            this.sideDrift = sideDrift;
            this.popDuration = popDuration;
            this.driftDuration = driftDuration;
            this.holdBeforeFade = holdBeforeFade;
            this.fadeDuration = fadeDuration;
            this.startScale = startScale;
            this.peakScale = peakScale;
            this.finalScale = finalScale;
        }
    }

    private MotionProfile BuildMotionProfile(DamageTextStyle style)
    {
        switch (style)
        {
            case DamageTextStyle.Critical:
                return new MotionProfile(
                    criticalEntryDrop,
                    criticalRiseHeight,
                    criticalSideDrift,
                    criticalPopDuration,
                    criticalDriftDuration,
                    criticalHoldBeforeFade,
                    criticalFadeDuration,
                    criticalStartScale,
                    criticalPeakScale,
                    criticalFinalScale);

            case DamageTextStyle.Miss:
            case DamageTextStyle.Guard:
                return new MotionProfile(
                    utilityEntryDrop,
                    utilityRiseHeight,
                    utilitySideDrift,
                    utilityPopDuration,
                    utilityDriftDuration,
                    utilityHoldBeforeFade,
                    utilityFadeDuration,
                    utilityStartScale,
                    utilityPeakScale,
                    utilityFinalScale);

            case DamageTextStyle.Reward:
                return new MotionProfile(
                    rewardEntryDrop,
                    rewardRiseHeight,
                    rewardSideDrift,
                    rewardPopDuration,
                    rewardDriftDuration,
                    rewardHoldBeforeFade,
                    rewardFadeDuration,
                    rewardStartScale,
                    rewardPeakScale,
                    rewardFinalScale);

            default:
                return new MotionProfile(
                    normalEntryDrop,
                    normalRiseHeight,
                    normalSideDrift,
                    normalPopDuration,
                    normalDriftDuration,
                    normalHoldBeforeFade,
                    normalFadeDuration,
                    normalStartScale,
                    normalPeakScale,
                    normalFinalScale);
        }
    }

    private DamageTextStyle ResolveDamageTextStyle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return DamageTextStyle.Normal;
        }

        string normalized = text.Trim();

        if (normalized.IndexOf("CRITICAL", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return DamageTextStyle.Critical;
        }

        if (string.Equals(normalized, "Miss", StringComparison.OrdinalIgnoreCase))
        {
            return DamageTextStyle.Miss;
        }

        if (normalized.IndexOf("GUARD", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return DamageTextStyle.Guard;
        }

        if (normalized.IndexOf("EXP", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("LEVEL UP", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("ITEM", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("SCRAP", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("GUN +", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return DamageTextStyle.Reward;
        }

        return DamageTextStyle.Normal;
    }

    private Color GetStartColor(Color baseColor, DamageTextStyle style)
    {
        switch (style)
        {
            case DamageTextStyle.Critical:
                return LiftColor(baseColor, criticalColorLift);
            case DamageTextStyle.Reward:
                return LiftColor(baseColor, rewardColorLift);
            default:
                return baseColor;
        }
    }

    private static Color LiftColor(Color c, float amount)
    {
        return new Color(
            Mathf.Clamp01(c.r + amount),
            Mathf.Clamp01(c.g + amount),
            Mathf.Clamp01(c.b + amount),
            c.a);
    }

    public void SpawnOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay)
    {
        SpawnOneShotEffect(prefab, position, rotation, returnDelay, Vector3.one);
    }

    public void SpawnOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay, Vector3 scale)
    {
        if (prefab == null) return;

        GameObject effectObj = GetPooledObject(prefab, position, rotation);
        if (effectObj == null) return;

        effectObj.transform.position = position;
        effectObj.transform.rotation = rotation;
        effectObj.transform.localScale = scale;

        StartCoroutine(ReturnPooledObjectAfterDelay(prefab, effectObj, returnDelay));
    }

    public void SpawnMagicBullet(GameObject magicBulletPrefab, Vector3 start, Vector3 target, Action onHit)
    {
        if (magicBulletPrefab == null) return;

        GameObject bullet = GetPooledObject(magicBulletPrefab, start, Quaternion.identity);
        if (bullet == null) return;

        bullet.transform.DOMove(target, 0.15f).SetEase(Ease.Linear).OnComplete(() =>
        {
            ReturnPooledObject(magicBulletPrefab, bullet);
            onHit?.Invoke();
        });
    }

    public void SpawnEnergyOrb(
        GameObject energyOrbPrefab,
        GameObject absorbEffectPrefab,
        Vector3 startPos,
        Vector3 target,
        float duration,
        float delay,
        Color energyColor)
    {
        if (energyOrbPrefab == null) return;

        GameObject orb = GetPooledObject(energyOrbPrefab, startPos, Quaternion.identity);
        if (orb == null) return;

        orb.transform.DOKill();
        orb.transform.position = startPos;
        orb.transform.rotation = Quaternion.identity;
        ApplyTintToEffectHierarchy(orb, energyColor);

        Vector3 baseScale = Vector3.one * UnityEngine.Random.Range(0.9f, 1.08f);
        orb.transform.localScale = baseScale;

        Vector3 delta = target - startPos;
        float distance = Mathf.Max(0.01f, delta.magnitude);
        Vector3 dir = delta / distance;
        Vector3 side = Vector3.Cross(dir, Vector3.forward).normalized;
        if (side.sqrMagnitude < 0.001f)
        {
            side = Vector3.right;
        }

        float sideSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float arcHeight = Mathf.Clamp(distance * 0.24f, 0.35f, 1.25f);
        float sideAmount = Mathf.Clamp(distance * 0.18f, 0.12f, 0.55f) * sideSign;

        Vector3 point1 = startPos + Vector3.up * (arcHeight * 0.45f) + side * sideAmount;
        Vector3 point2 = Vector3.Lerp(startPos, target, 0.5f) + Vector3.up * arcHeight + side * (-sideAmount * 0.55f);
        Vector3 point3 = Vector3.Lerp(startPos, target, 0.82f) + Vector3.up * (arcHeight * 0.18f);
        Vector3[] path = new Vector3[] { startPos, point1, point2, point3, target };

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);
        seq.Append(orb.transform.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InQuad));
        seq.Join(orb.transform.DOScale(baseScale * 1.08f, duration * 0.38f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            ReturnPooledObject(energyOrbPrefab, orb);

            if (absorbEffectPrefab != null)
            {
                SpawnColoredOneShotEffect(absorbEffectPrefab, target, Quaternion.identity, 0.8f, energyColor);
            }
        });
    }


    private void SpawnColoredOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay, Color tint)
    {
        if (prefab == null) return;

        GameObject effectObj = GetPooledObject(prefab, position, rotation);
        if (effectObj == null) return;

        effectObj.transform.position = position;
        effectObj.transform.rotation = rotation;
        effectObj.transform.localScale = prefab.transform.localScale;
        ApplyTintToEffectHierarchy(effectObj, tint);

        StartCoroutine(ReturnPooledObjectAfterDelay(prefab, effectObj, returnDelay));
    }

    private void ApplyTintToEffectHierarchy(GameObject rootObj, Color tint)
    {
        if (rootObj == null) return;

        ParticleSystem[] systems = rootObj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in systems)
        {
            var main = ps.main;
            Color c = tint;
            c.a *= Mathf.Clamp01(c.a);
            main.startColor = new ParticleSystem.MinMaxGradient(c);

            var trails = ps.trails;
            if (trails.enabled)
            {
                trails.colorOverTrail = new ParticleSystem.MinMaxGradient(tint);
            }
        }

        TrailRenderer[] trailRenderers = rootObj.GetComponentsInChildren<TrailRenderer>(true);
        foreach (TrailRenderer tr in trailRenderers)
        {
            tr.startColor = tint;
            Color endColor = tint;
            endColor.a *= 0.15f;
            tr.endColor = endColor;
        }

        SpriteRenderer[] spriteRenderers = rootObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            Color c = tint;
            c.a = sr.color.a > 0f ? sr.color.a : tint.a;
            sr.color = c;
        }

        LineRenderer[] lineRenderers = rootObj.GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer lr in lineRenderers)
        {
            lr.startColor = tint;
            Color endColor = tint;
            endColor.a *= 0.15f;
            lr.endColor = endColor;
        }
    }

    public IEnumerator SpawnExpTextWithDelay(GameObject damageTextPrefab, int exp, Vector3 spawnPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnDamageText(damageTextPrefab, $"+{exp} EXP", spawnPos, Color.green);
    }

    public IEnumerator SpawnLevelUpTextWithDelay(GameObject damageTextPrefab, Transform targetTransform, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (targetTransform == null) yield break;

        SpawnDamageText(
            damageTextPrefab,
            "LEVEL UP!",
            targetTransform.position + Vector3.up * 1.5f,
            Color.yellow);
    }

    public void SpawnItemGetOrb(
        GameObject orbPrefab,
        GameObject absorbEffectPrefab,
        Vector3 startPos,
        Vector3 targetPos,
        float duration,
        System.Action onArrive)
    {
        if (orbPrefab == null)
        {
            onArrive?.Invoke();
            return;
        }

        GameObject orb = GetPooledObject(orbPrefab, startPos, Quaternion.identity);
        if (orb == null)
        {
            onArrive?.Invoke();
            return;
        }

        Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.3f) + Vector3.up * 0.8f;
        Vector3[] path = new Vector3[] { startPos, midPoint, targetPos };

        orb.transform.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                ReturnPooledObject(orbPrefab, orb);

                if (absorbEffectPrefab != null)
                {
                    SpawnOneShotEffect(absorbEffectPrefab, targetPos, Quaternion.identity, 0.6f);
                }

                onArrive?.Invoke();
            });
    }

    private GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (effectPoolManager == null)
        {
            return Instantiate(prefab, position, rotation);
        }

        return effectPoolManager.GetPooledObject(prefab, position, rotation);
    }

    private void ReturnPooledObject(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;

        if (effectPoolManager == null)
        {
            Destroy(obj);
            return;
        }

        effectPoolManager.ReturnPooledObject(prefab, obj);
    }

    private IEnumerator ReturnPooledObjectAfterDelay(GameObject prefab, GameObject obj, float delay)
    {
        if (obj == null) yield break;

        if (effectPoolManager == null)
        {
            yield return new WaitForSeconds(delay);
            Destroy(obj);
            yield break;
        }

        yield return effectPoolManager.ReturnPooledObjectAfterDelay(prefab, obj, delay);
    }
}
