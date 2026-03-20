using DG.Tweening;
using UnityEngine;

public class PlayerPowerupFeedbackPresenter : MonoBehaviour
{
    [Header("対象")]
    [SerializeField] private Transform targetRoot;
    [SerializeField] private Transform effectAnchor;

    [Header("追加エフェクト（任意）")]
    [SerializeField] private GameObject scanPulsePrefab;
    [SerializeField] private GameObject boostBurstPrefab;

    [Header("点滅")]
    [SerializeField] private float flashInDuration = 0.09f;
    [SerializeField] private float flashOutDuration = 0.18f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.55f;

    [Header("本体反応")]
    [SerializeField] private Vector3 punch = new Vector3(0.10f, 0f, 0f);
    [SerializeField] private float punchDuration = 0.16f;
    [SerializeField] private int punchVibrato = 7;
    [SerializeField] private float punchElasticity = 0.8f;

    private SpriteRenderer[] cachedRenderers;
    private Sequence currentSequence;

    private void Awake()
    {
        AutoBind();
        CacheRenderers();
    }

    private void OnValidate()
    {
        if (targetRoot == null)
        {
            targetRoot = transform;
        }

        if (effectAnchor == null)
        {
            effectAnchor = targetRoot;
        }
    }

    private void AutoBind()
    {
        if (targetRoot == null)
        {
            targetRoot = transform;
        }

        if (effectAnchor == null)
        {
            effectAnchor = targetRoot;
        }
    }

    private void CacheRenderers()
    {
        if (targetRoot == null)
        {
            cachedRenderers = new SpriteRenderer[0];
            return;
        }

        cachedRenderers = targetRoot.GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void PlayItemUseFeedback(BattleItemType itemType)
    {
        switch (itemType)
        {
            case BattleItemType.FieldBandage:
                PlayFeedback(new Color(0.45f, 1f, 0.65f), 1f);
                break;
            case BattleItemType.ActivationCell:
                PlayFeedback(new Color(0.45f, 0.95f, 1f), 1f);
                break;
            case BattleItemType.AttackOil:
                PlayFeedback(new Color(1f, 0.75f, 0.25f), 1.05f);
                break;
            default:
                PlayFeedback(new Color(0.9f, 0.95f, 1f), 1f);
                break;
        }
    }

    public void PlayWeaponEquipFeedback(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                PlayFeedback(new Color(1f, 0.82f, 0.35f), 1.1f);
                break;
            case WeaponType.GreatSword:
                PlayFeedback(new Color(1f, 0.62f, 0.22f), 1.15f);
                break;
            default:
                PlayFeedback(new Color(1f, 1f, 1f), 1f);
                break;
        }
    }

    public void PlayGunEquipFeedback(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                PlayFeedback(new Color(0.55f, 0.9f, 1f), 1f);
                break;
            case GunType.Shotgun:
                PlayFeedback(new Color(0.55f, 0.85f, 1f), 1.1f);
                break;
            case GunType.MachineGun:
                PlayFeedback(new Color(0.5f, 1f, 0.9f), 1.08f);
                break;
            case GunType.Rifle:
                PlayFeedback(new Color(0.72f, 0.9f, 1f), 1.12f);
                break;
            default:
                PlayFeedback(new Color(0.9f, 0.95f, 1f), 1f);
                break;
        }
    }

    private void PlayFeedback(Color flashColor, float effectScale)
    {
        AutoBind();
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            CacheRenderers();
        }

        if (targetRoot == null)
        {
            return;
        }

        currentSequence?.Kill();
        targetRoot.DOKill(false);

        Color[] baseColors = new Color[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            SpriteRenderer sr = cachedRenderers[i];
            if (sr == null) continue;

            sr.DOKill(false);
            baseColors[i] = sr.color;
        }

        SpawnOptionalEffect(scanPulsePrefab, effectScale);
        SpawnOptionalEffect(boostBurstPrefab, effectScale);

        currentSequence = DOTween.Sequence();
        currentSequence.Join(targetRoot.DOPunchPosition(punch, punchDuration, punchVibrato, punchElasticity));
        currentSequence.Append(DOVirtual.Float(0f, 1f, flashInDuration, t => ApplyFlash(baseColors, flashColor, t * flashStrength)));
        currentSequence.Append(DOVirtual.Float(1f, 0f, flashOutDuration, t => ApplyFlash(baseColors, flashColor, t * flashStrength)));
        currentSequence.OnComplete(() => RestoreColors(baseColors));
        currentSequence.OnKill(() => RestoreColors(baseColors));
    }

    private void ApplyFlash(Color[] baseColors, Color flashColor, float blend)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            SpriteRenderer sr = cachedRenderers[i];
            if (sr == null) continue;

            Color baseColor = i < baseColors.Length ? baseColors[i] : sr.color;
            float alpha = baseColor.a;
            Color mixed = Color.Lerp(baseColor, flashColor, Mathf.Clamp01(blend));
            mixed.a = alpha;
            sr.color = mixed;
        }
    }

    private void RestoreColors(Color[] baseColors)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            SpriteRenderer sr = cachedRenderers[i];
            if (sr == null) continue;

            if (i < baseColors.Length)
            {
                sr.color = baseColors[i];
            }
        }
    }

    private void SpawnOptionalEffect(GameObject prefab, float effectScale)
    {
        if (prefab == null) return;

        Transform anchor = effectAnchor != null ? effectAnchor : targetRoot;
        if (anchor == null) return;

        GameObject instance = Instantiate(prefab, anchor.position, Quaternion.identity);
        instance.transform.localScale *= Mathf.Max(0.1f, effectScale);
        Destroy(instance, 1.5f);
    }
}
