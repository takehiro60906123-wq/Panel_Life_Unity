using System;
using System.Collections;
using System.Globalization;
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


[Header("Damage Text - Player Hit")]
[SerializeField] private float playerHitWorldOffsetY = -0.95f;
[SerializeField] private float playerHitLocalOffsetY = -0.90f;
[SerializeField] private float playerHitEntryDrop = 0.02f;
[SerializeField] private float playerHitRiseHeight = 0.10f;
[SerializeField] private float playerHitSideDrift = 0.00f;
[SerializeField] private float playerHitPopDuration = 0.18f;
[SerializeField] private float playerHitDriftDuration = 1.00f;
[SerializeField] private float playerHitHoldBeforeFade = 1.00f;
[SerializeField] private float playerHitFadeDuration = 1.05f;
[SerializeField] private float playerHitStartScale = 0.90f;
[SerializeField] private float playerHitPeakScale = 1.05f;
[SerializeField] private float playerHitFinalScale = 1.00f;

    [Header("Damage Text - Overlink Hit")]
    [SerializeField] private float overlinkHitEntryDrop = 0.14f;
    [SerializeField] private float overlinkHitRiseHeight = 1.95f;
    [SerializeField] private float overlinkHitSideDrift = 0.32f;
    [SerializeField] private float overlinkHitPopDuration = 0.14f;
    [SerializeField] private float overlinkHitDriftDuration = 0.60f;
    [SerializeField] private float overlinkHitHoldBeforeFade = 0.24f;
    [SerializeField] private float overlinkHitFadeDuration = 0.30f;
    [SerializeField] private float overlinkHitStartScale = 0.76f;
    [SerializeField] private float overlinkHitPeakScale = 1.50f;
    [SerializeField] private float overlinkHitFinalScale = 1.05f;
    [SerializeField] private float overlinkHitPunchScale = 0.22f;
    [SerializeField] private float overlinkHitPunchPositionY = 0.10f;
    [SerializeField] private float overlinkHitRotateZ = 7f;

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

    [Header("Damage Text - Overlink")]
    [SerializeField] private float overlinkEntryDrop = 0.16f;
    [SerializeField] private float overlinkRiseHeight = 2.05f;
    [SerializeField] private float overlinkSideDrift = 0.18f;
    [SerializeField] private float overlinkPopDuration = 0.14f;
    [SerializeField] private float overlinkDriftDuration = 0.66f;
    [SerializeField] private float overlinkHoldBeforeFade = 0.34f;
    [SerializeField] private float overlinkFadeDuration = 0.34f;
    [SerializeField] private float overlinkStartScale = 0.82f;
    [SerializeField] private float overlinkPeakScale = 1.72f;
    [SerializeField] private float overlinkFinalScale = 1.08f;
    [SerializeField] private float overlinkPunchScale = 0.34f;
    [SerializeField] private float overlinkPunchPositionY = 0.18f;
    [SerializeField] private float overlinkRotateZ = 9f;
    [SerializeField] private float overlinkViewportAnchorX = 0.86f;
    [SerializeField] private float overlinkViewportAnchorY = 0.90f;
    [SerializeField] private float overlinkViewportStartX = 1.16f;
    [SerializeField] private float overlinkViewportExitX = 1.08f;
    [SerializeField] private float overlinkSlideInDuration = 0.20f;
    [SerializeField] private float overlinkSettleDuration = 0.10f;
    [SerializeField] private float overlinkHoldDuration = 0.34f;
    [SerializeField] private float overlinkSlideOutDuration = 0.20f;

    [Header("Damage Text - Global")]
    [SerializeField] private float secondaryRiseRatio = 0.52f;
    [SerializeField] private float secondarySideRatio = 0.38f;
    [SerializeField] private float randomStartX = 0.08f;
    [SerializeField] private float criticalColorLift = 0.18f;
    [SerializeField] private float rewardColorLift = 0.08f;

    private EffectPoolManager effectPoolManager;

    private const string OverlinkImpactTextMarker = "<ovl>";
    private const string PlayerHitTextMarker = "<playerhit>";

    [Header("Overlink UI (Preplaced)")]
    [SerializeField] private RectTransform overlinkBannerRoot;
    [SerializeField] private CanvasGroup overlinkBannerCanvasGroup;
    [SerializeField] private TMP_Text overlinkBannerMainText;
    [SerializeField] private float overlinkBannerEnterOffsetX = 360f;
    [SerializeField] private float overlinkBannerEnterOffsetY = 0f;
    [SerializeField] private float overlinkBannerEnterDuration = 0.18f;
    [SerializeField] private float overlinkBannerSettleDuration = 0.10f;
    [SerializeField] private float overlinkBannerHoldDuration = 0.52f;
    [SerializeField] private float overlinkBannerExitDuration = 0.18f;
    [SerializeField] private float overlinkBannerStartScale = 0.90f;
    [SerializeField] private float overlinkBannerPeakScale = 1.08f;
    [SerializeField] private float overlinkBannerEndScale = 1.00f;
    [SerializeField] private float overlinkBannerPunchScale = 0.16f;

    [Header("Overlink Cutin Image (Optional)")]
    [SerializeField] private RectTransform overlinkCutinImage;
    [SerializeField] private CanvasGroup overlinkCutinCanvasGroup;
    [SerializeField] private Vector2 overlinkCutinStartOffset = new Vector2(-500f, 0f);
    [SerializeField] private float overlinkCutinFadeInDuration = 0.04f;
    [SerializeField] private float overlinkCutinMoveDuration = 0.12f;
    [SerializeField] private float overlinkCutinHoldDuration = 0.05f;
    [SerializeField] private float overlinkCutinFadeOutDuration = 0.08f;

    [Header("Total Damage UI (Preplaced)")]
    [SerializeField] private RectTransform totalDamageRoot;
    [SerializeField] private CanvasGroup totalDamageCanvasGroup;
    [SerializeField] private TMP_Text totalDamageValueText;
    [SerializeField] private float totalDamageEnterOffsetY = 42f;
    [SerializeField] private float totalDamageEnterDuration = 0.14f;
    [SerializeField] private float totalDamageSettleDuration = 0.10f;
    [SerializeField] private float totalDamageHoldDuration = 0.60f;
    [SerializeField] private float totalDamageExitDuration = 0.18f;
    [SerializeField] private float totalDamageStartScale = 0.82f;
    [SerializeField] private float totalDamagePeakScale = 1.12f;
    [SerializeField] private float totalDamageEndScale = 1.00f;
    [SerializeField] private float totalDamagePunchScale = 0.15f;
    [SerializeField] private float totalDamageResetGap = 0.65f;
    [SerializeField] private float totalDamageCountDuration = 0.12f;

    private Vector2 overlinkBannerBaseAnchoredPos;
    private Vector3 overlinkBannerBaseScale = Vector3.one;
    private bool hasOverlinkBannerPose;
    private Vector2 overlinkCutinBaseAnchoredPos;
    private bool hasOverlinkCutinPose;
    private Sequence overlinkCutinSequence;

    private Vector2 totalDamageBaseAnchoredPos;
    private Vector3 totalDamageBaseScale = Vector3.one;
    private bool hasTotalDamagePose;
    private Tween totalDamageHideTween;
    private Tween totalDamageCountTween;
    private int accumulatedDamageTotal;
    private int displayedDamageTotal;
    private float lastDamageRegisteredTime = -999f;

    private void Awake()
    {
        CacheUiAnchors();
        HideOverlinkBannerImmediate();
        HideOverlinkCutinImmediate();
        HideTotalDamageImmediate();
    }

    private void CacheUiAnchors()
    {
        if (overlinkBannerRoot != null && !hasOverlinkBannerPose)
        {
            overlinkBannerBaseAnchoredPos = overlinkBannerRoot.anchoredPosition;
            overlinkBannerBaseScale = overlinkBannerRoot.localScale;
            hasOverlinkBannerPose = true;
        }

        if (overlinkCutinImage != null && !hasOverlinkCutinPose)
        {
            overlinkCutinBaseAnchoredPos = overlinkCutinImage.anchoredPosition;
            hasOverlinkCutinPose = true;
        }

        if (totalDamageRoot != null && !hasTotalDamagePose)
        {
            totalDamageBaseAnchoredPos = totalDamageRoot.anchoredPosition;
            totalDamageBaseScale = totalDamageRoot.localScale;
            hasTotalDamagePose = true;
        }

        if (overlinkBannerRoot != null && overlinkBannerCanvasGroup == null)
        {
            overlinkBannerCanvasGroup = overlinkBannerRoot.GetComponent<CanvasGroup>();
            if (overlinkBannerCanvasGroup == null)
            {
                overlinkBannerCanvasGroup = overlinkBannerRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (overlinkCutinImage != null && overlinkCutinCanvasGroup == null)
        {
            overlinkCutinCanvasGroup = overlinkCutinImage.GetComponent<CanvasGroup>();
            if (overlinkCutinCanvasGroup == null)
            {
                overlinkCutinCanvasGroup = overlinkCutinImage.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (totalDamageRoot != null && totalDamageCanvasGroup == null)
        {
            totalDamageCanvasGroup = totalDamageRoot.GetComponent<CanvasGroup>();
            if (totalDamageCanvasGroup == null)
            {
                totalDamageCanvasGroup = totalDamageRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private enum DamageTextStyle
    {
        Normal,
        Critical,
        OverlinkImpact,
        PlayerHit,
        Miss,
        Guard,
        Reward,
        Overlink
    }

    private sealed class DamageTextPoseCache : MonoBehaviour
    {
        public bool captured;
        public Vector3 rootLocalScale;
        public Vector3 textLocalPosition;
        public Vector3 textLocalScale;
        public Quaternion textLocalRotation;
        public FontStyles fontStyle;

        public void Capture(Transform root, Transform textTransform)
        {
            if (captured) return;

            rootLocalScale = root.localScale;

            if (textTransform != null)
            {
                textLocalPosition = textTransform.localPosition;
                textLocalScale = textTransform.localScale;
                textLocalRotation = textTransform.localRotation;
                TMP_Text tmp = textTransform.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    fontStyle = tmp.fontStyle;
                }
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

                TMP_Text tmp = textTransform.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontStyle = fontStyle;
                }
            }
        }
    }

    private void OnDisable()
    {
        totalDamageHideTween?.Kill();
        totalDamageCountTween?.Kill();
        HideOverlinkBannerImmediate();
        HideOverlinkCutinImmediate();
        HideTotalDamageImmediate();
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

        bool isOverlinkImpact = TryConsumeMarker(ref text, OverlinkImpactTextMarker);
        bool isPlayerHit = TryConsumeMarker(ref text, PlayerHitTextMarker);

        DamageTextStyle style = ResolveDamageTextStyle(text);
        if (isOverlinkImpact && style != DamageTextStyle.Overlink)
        {
            style = DamageTextStyle.OverlinkImpact;
        }
        else if (isPlayerHit && style == DamageTextStyle.Normal)
        {
            style = DamageTextStyle.PlayerHit;
        }

        MotionProfile profile = BuildMotionProfile(style);

        Vector3 adjustedPosition = position;
        if (style == DamageTextStyle.PlayerHit)
        {
            adjustedPosition += Vector3.up * playerHitWorldOffsetY;
        }

        Color baseColor = color;
        baseColor.a = 1f;

        if (style == DamageTextStyle.Overlink)
        {
            ReturnPooledObject(damageTextPrefab, textObj);
            ShowOverlinkBanner(text);
            return;
        }

        tmp.text = text;
        tmp.fontStyle = (style == DamageTextStyle.Overlink || style == DamageTextStyle.OverlinkImpact) ? FontStyles.Bold : poseCache.fontStyle;
        tmp.color = GetStartColor(baseColor, style);
        tmp.alpha = 1f;

        RegisterTotalDamageIfNeeded(style, text);

        float side;
        if (style == DamageTextStyle.Overlink || style == DamageTextStyle.OverlinkImpact)
        {
            side = UnityEngine.Random.Range(-profile.sideDrift * 0.35f, profile.sideDrift * 0.35f);
        }
        else if (style == DamageTextStyle.PlayerHit)
        {
            side = UnityEngine.Random.Range(-profile.sideDrift, profile.sideDrift);
        }
        else
        {
            side = UnityEngine.Random.Range(-profile.sideDrift, profile.sideDrift);
        }

        float startX = (style == DamageTextStyle.Overlink || style == DamageTextStyle.PlayerHit)
            ? 0f
            : UnityEngine.Random.Range(-randomStartX, randomStartX);

        root.position = adjustedPosition + new Vector3(startX, -profile.entryDrop, 0f);
        if (style == DamageTextStyle.PlayerHit)
        {
            textTransform.localPosition = poseCache.textLocalPosition + new Vector3(0f, playerHitLocalOffsetY, 0f);
        }
        textTransform.localScale = poseCache.textLocalScale * profile.startScale;

        Vector3 midPos = adjustedPosition + new Vector3(side * secondarySideRatio, profile.riseHeight * secondaryRiseRatio, 0f);
        Vector3 endPos = adjustedPosition + new Vector3(side, profile.riseHeight, 0f);

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

        if (style == DamageTextStyle.Overlink)
        {
            seq.Insert(0f, textTransform.DOPunchScale(Vector3.one * overlinkPunchScale, profile.popDuration + 0.18f, 7, 0.85f));
            seq.Insert(0f, root.DOPunchPosition(new Vector3(0f, overlinkPunchPositionY, 0f), profile.popDuration + 0.14f, 6, 0.8f));
            seq.Insert(0f, textTransform.DOLocalRotate(new Vector3(0f, 0f, overlinkRotateZ), profile.popDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Insert(profile.popDuration * 0.5f, textTransform.DOLocalRotate(Vector3.zero, profile.popDuration * 0.6f).SetEase(Ease.InOutSine));
        }
        else if (style == DamageTextStyle.OverlinkImpact)
        {
            seq.Insert(0f, textTransform.DOPunchScale(Vector3.one * overlinkHitPunchScale, profile.popDuration + 0.14f, 7, 0.85f));
            seq.Insert(0f, root.DOPunchPosition(new Vector3(0f, overlinkHitPunchPositionY, 0f), profile.popDuration + 0.10f, 6, 0.8f));
            seq.Insert(0f, textTransform.DOLocalRotate(new Vector3(0f, 0f, overlinkHitRotateZ), profile.popDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Insert(profile.popDuration * 0.5f, textTransform.DOLocalRotate(Vector3.zero, profile.popDuration * 0.6f).SetEase(Ease.InOutSine));
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

        if (style == DamageTextStyle.Overlink)
        {
            seq.Insert(profile.popDuration + 0.02f,
                textTransform.DOScale(poseCache.textLocalScale * (profile.finalScale * 1.10f), 0.09f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));
        }
        else if (style == DamageTextStyle.OverlinkImpact)
        {
            seq.Insert(profile.popDuration + 0.02f,
                textTransform.DOScale(poseCache.textLocalScale * (profile.finalScale * 1.08f), 0.08f)
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

            poseCache.ResetPose(root, textTransform, adjustedPosition);
            ReturnPooledObject(damageTextPrefab, textObj);
        });
    }

    private void ShowOverlinkBanner(string text)
    {
        CacheUiAnchors();
        if (overlinkBannerRoot == null)
        {
            return;
        }


        overlinkBannerRoot.gameObject.SetActive(true);
        overlinkBannerRoot.DOKill();
        if (overlinkBannerMainText != null)
        {
            overlinkBannerMainText.DOKill();
            overlinkBannerMainText.color = Color.white;
            overlinkBannerMainText.gameObject.SetActive(true);
            overlinkBannerMainText.text = text;
            overlinkBannerMainText.alpha = 1f;
        }

        if (overlinkBannerCanvasGroup != null)
        {
            overlinkBannerCanvasGroup.DOKill();
            overlinkBannerCanvasGroup.alpha = 0f;
        }

        overlinkBannerRoot.anchoredPosition = overlinkBannerBaseAnchoredPos + new Vector2(overlinkBannerEnterOffsetX, overlinkBannerEnterOffsetY);
        overlinkBannerRoot.localScale = overlinkBannerBaseScale * overlinkBannerStartScale;

        PlayOverlinkCutin();

        Sequence seq = DOTween.Sequence();
        seq.Append(overlinkBannerRoot.DOAnchorPos(overlinkBannerBaseAnchoredPos, overlinkBannerEnterDuration).SetEase(Ease.OutCubic));
        if (overlinkBannerCanvasGroup != null)
        {
            seq.Join(overlinkBannerCanvasGroup.DOFade(1f, overlinkBannerEnterDuration * 0.85f).SetEase(Ease.OutQuad));
        }
        seq.Join(overlinkBannerRoot.DOScale(overlinkBannerBaseScale * overlinkBannerPeakScale, overlinkBannerEnterDuration).SetEase(Ease.OutBack));
        seq.Append(overlinkBannerRoot.DOScale(overlinkBannerBaseScale * overlinkBannerEndScale, overlinkBannerSettleDuration).SetEase(Ease.OutQuad));
        seq.Join(overlinkBannerRoot.DOPunchScale(Vector3.one * overlinkBannerPunchScale, overlinkBannerSettleDuration + 0.10f, 6, 0.85f));
        seq.AppendInterval(overlinkBannerHoldDuration);
        seq.Append(overlinkBannerRoot.DOAnchorPos(overlinkBannerBaseAnchoredPos + new Vector2(overlinkBannerEnterOffsetX * 0.45f, overlinkBannerEnterOffsetY), overlinkBannerExitDuration).SetEase(Ease.InQuad));
        if (overlinkBannerCanvasGroup != null)
        {
            seq.Join(overlinkBannerCanvasGroup.DOFade(0f, overlinkBannerExitDuration).SetEase(Ease.InQuad));
        }
        seq.OnComplete(HideOverlinkBannerImmediate);
    }

    private void PlayOverlinkCutin()
    {
        CacheUiAnchors();
        if (overlinkCutinImage == null)
        {
            return;
        }

        overlinkCutinSequence?.Kill();
        overlinkCutinImage.DOKill();
        overlinkCutinImage.gameObject.SetActive(true);
        overlinkCutinImage.anchoredPosition = overlinkCutinBaseAnchoredPos + overlinkCutinStartOffset;

        if (overlinkCutinCanvasGroup != null)
        {
            overlinkCutinCanvasGroup.DOKill();
            overlinkCutinCanvasGroup.alpha = 0f;
        }

        overlinkCutinSequence = DOTween.Sequence();

        if (overlinkCutinCanvasGroup != null)
        {
            overlinkCutinSequence.Append(
                overlinkCutinCanvasGroup
                    .DOFade(1f, overlinkCutinFadeInDuration)
                    .SetEase(Ease.OutQuad));
        }
        else
        {
            overlinkCutinSequence.AppendInterval(0f);
        }

        overlinkCutinSequence.Join(
            overlinkCutinImage
                .DOAnchorPos(overlinkCutinBaseAnchoredPos, overlinkCutinMoveDuration)
                .SetEase(Ease.OutCubic));

        overlinkCutinSequence.AppendInterval(overlinkCutinHoldDuration);

        if (overlinkCutinCanvasGroup != null)
        {
            overlinkCutinSequence.Append(
                overlinkCutinCanvasGroup
                    .DOFade(0f, overlinkCutinFadeOutDuration)
                    .SetEase(Ease.InQuad));
        }
        else
        {
            overlinkCutinSequence.AppendInterval(overlinkCutinFadeOutDuration);
        }

        overlinkCutinSequence.OnComplete(HideOverlinkCutinImmediate);
        overlinkCutinSequence.OnKill(() => overlinkCutinSequence = null);
    }

    private void HideOverlinkCutinImmediate()
    {
        overlinkCutinSequence?.Kill();
        overlinkCutinSequence = null;

        if (overlinkCutinImage == null)
        {
            return;
        }

        overlinkCutinImage.DOKill();
        overlinkCutinImage.anchoredPosition = overlinkCutinBaseAnchoredPos;

        if (overlinkCutinCanvasGroup != null)
        {
            overlinkCutinCanvasGroup.DOKill();
            overlinkCutinCanvasGroup.alpha = 0f;
        }

        overlinkCutinImage.gameObject.SetActive(false);
    }

    private void HideOverlinkBannerImmediate()
    {
        if (overlinkBannerRoot == null)
        {
            return;
        }

        overlinkBannerRoot.DOKill();
        overlinkBannerRoot.anchoredPosition = overlinkBannerBaseAnchoredPos;
        overlinkBannerRoot.localScale = overlinkBannerBaseScale;

        if (overlinkBannerCanvasGroup != null)
        {
            overlinkBannerCanvasGroup.DOKill();
            overlinkBannerCanvasGroup.alpha = 0f;
        }

        if (overlinkBannerMainText != null)
        {
            overlinkBannerMainText.DOKill();
            overlinkBannerMainText.color = Color.white;
            overlinkBannerMainText.alpha = 1f;
        }

        overlinkBannerRoot.gameObject.SetActive(false);
    }

    private bool TryConsumeMarker(ref string text, string marker)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(marker))
        {
            return false;
        }

        if (!text.StartsWith(marker, StringComparison.Ordinal))
        {
            return false;
        }

        text = text.Substring(marker.Length);
        return true;
    }

    private void RegisterTotalDamageIfNeeded(DamageTextStyle style, string text)
    {
        if (style != DamageTextStyle.Normal && style != DamageTextStyle.Critical && style != DamageTextStyle.OverlinkImpact)
        {
            return;
        }

        if (!TryExtractDamageValue(style, text, out int damageValue) || damageValue <= 0)
        {
            return;
        }

        CacheUiAnchors();
        if (totalDamageRoot == null || totalDamageValueText == null)
        {
            return;
        }

        float now = Time.time;
        bool startFresh = now - lastDamageRegisteredTime > totalDamageResetGap;
        if (startFresh)
        {
            accumulatedDamageTotal = 0;
            displayedDamageTotal = 0;
            ShowTotalDamageBannerFresh();
        }
        else
        {
            EnsureTotalDamageBannerVisible();
        }

        accumulatedDamageTotal += damageValue;
        lastDamageRegisteredTime = now;

        if (totalDamageCountTween != null && totalDamageCountTween.IsActive())
        {
            totalDamageCountTween.Kill();
        }

        int startValue = displayedDamageTotal;
        int targetValue = accumulatedDamageTotal;
        totalDamageCountTween = DOVirtual.Int(startValue, targetValue, totalDamageCountDuration, value =>
        {
            displayedDamageTotal = value;
            UpdateTotalDamageText(value);
        }).SetEase(Ease.OutQuad);

        if (totalDamageValueText != null)
        {
            Transform valueTransform = totalDamageValueText.transform;
            valueTransform.DOKill();
            valueTransform.localScale = Vector3.one;
            valueTransform.DOPunchScale(Vector3.one * totalDamagePunchScale, totalDamageSettleDuration + 0.08f, 6, 0.85f);
        }

        if (totalDamageHideTween != null && totalDamageHideTween.IsActive())
        {
            totalDamageHideTween.Kill();
        }

        totalDamageHideTween = DOVirtual.DelayedCall(totalDamageHoldDuration, HideTotalDamageAnimated);
    }

    private void ShowTotalDamageBannerFresh()
    {
        totalDamageRoot.gameObject.SetActive(true);
        totalDamageRoot.DOKill();
        if (totalDamageCanvasGroup != null)
        {
            totalDamageCanvasGroup.DOKill();
            totalDamageCanvasGroup.alpha = 0f;
        }

        totalDamageRoot.anchoredPosition = totalDamageBaseAnchoredPos + new Vector2(0f, totalDamageEnterOffsetY);
        totalDamageRoot.localScale = totalDamageBaseScale * totalDamageStartScale;
        UpdateTotalDamageText(0);

        Sequence seq = DOTween.Sequence();
        seq.Append(totalDamageRoot.DOAnchorPos(totalDamageBaseAnchoredPos, totalDamageEnterDuration).SetEase(Ease.OutCubic));
        if (totalDamageCanvasGroup != null)
        {
            seq.Join(totalDamageCanvasGroup.DOFade(1f, totalDamageEnterDuration * 0.85f).SetEase(Ease.OutQuad));
        }
        seq.Join(totalDamageRoot.DOScale(totalDamageBaseScale * totalDamagePeakScale, totalDamageEnterDuration).SetEase(Ease.OutBack));
        seq.Append(totalDamageRoot.DOScale(totalDamageBaseScale * totalDamageEndScale, totalDamageSettleDuration).SetEase(Ease.OutQuad));
    }

    private void EnsureTotalDamageBannerVisible()
    {
        totalDamageRoot.gameObject.SetActive(true);
        totalDamageRoot.anchoredPosition = totalDamageBaseAnchoredPos;
        totalDamageRoot.localScale = totalDamageBaseScale;
        if (totalDamageCanvasGroup != null)
        {
            totalDamageCanvasGroup.alpha = 1f;
        }
    }

    private void HideTotalDamageAnimated()
    {
        if (totalDamageRoot == null)
        {
            return;
        }

        totalDamageRoot.DOKill();
        if (totalDamageCanvasGroup != null)
        {
            totalDamageCanvasGroup.DOKill();
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(totalDamageRoot.DOAnchorPos(totalDamageBaseAnchoredPos + new Vector2(0f, totalDamageEnterOffsetY * 0.45f), totalDamageExitDuration).SetEase(Ease.InQuad));
        if (totalDamageCanvasGroup != null)
        {
            seq.Join(totalDamageCanvasGroup.DOFade(0f, totalDamageExitDuration).SetEase(Ease.InQuad));
        }
        seq.Join(totalDamageRoot.DOScale(totalDamageBaseScale * totalDamageStartScale, totalDamageExitDuration).SetEase(Ease.InQuad));
        seq.OnComplete(HideTotalDamageImmediate);
    }

    private void HideTotalDamageImmediate()
    {
        if (totalDamageRoot == null)
        {
            return;
        }

        totalDamageRoot.DOKill();
        totalDamageRoot.anchoredPosition = totalDamageBaseAnchoredPos;
        totalDamageRoot.localScale = totalDamageBaseScale;
        if (totalDamageCanvasGroup != null)
        {
            totalDamageCanvasGroup.DOKill();
            totalDamageCanvasGroup.alpha = 0f;
        }
        totalDamageRoot.gameObject.SetActive(false);
        accumulatedDamageTotal = 0;
        displayedDamageTotal = 0;
        UpdateTotalDamageText(0);
    }

    private void UpdateTotalDamageText(int value)
    {
        if (totalDamageValueText == null)
        {
            return;
        }

        totalDamageValueText.color = Color.white;
        totalDamageValueText.text = value.ToString(CultureInfo.InvariantCulture);
    }

    private bool TryExtractDamageValue(DamageTextStyle style, string text, out int value)
    {
        value = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (style == DamageTextStyle.Normal)
        {
            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        if (style == DamageTextStyle.Critical)
        {
            string[] lines = text.Split('\n');
            string last = lines[lines.Length - 1].Trim();
            return int.TryParse(last, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        return false;
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

            case DamageTextStyle.OverlinkImpact:
                return new MotionProfile(
                    overlinkHitEntryDrop,
                    overlinkHitRiseHeight,
                    overlinkHitSideDrift,
                    overlinkHitPopDuration,
                    overlinkHitDriftDuration,
                    overlinkHitHoldBeforeFade,
                    overlinkHitFadeDuration,
                    overlinkHitStartScale,
                    overlinkHitPeakScale,
                    overlinkHitFinalScale);

            case DamageTextStyle.PlayerHit:
                return new MotionProfile(
                    playerHitEntryDrop,
                    playerHitRiseHeight,
                    playerHitSideDrift,
                    playerHitPopDuration,
                    playerHitDriftDuration,
                    playerHitHoldBeforeFade,
                    playerHitFadeDuration,
                    playerHitStartScale,
                    playerHitPeakScale,
                    playerHitFinalScale);

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

            case DamageTextStyle.Overlink:
                return new MotionProfile(
                    overlinkEntryDrop,
                    overlinkRiseHeight,
                    overlinkSideDrift,
                    overlinkPopDuration,
                    overlinkDriftDuration,
                    overlinkHoldBeforeFade,
                    overlinkFadeDuration,
                    overlinkStartScale,
                    overlinkPeakScale,
                    overlinkFinalScale);

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

        if (normalized.IndexOf("OVERLINK", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("BOOST", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return DamageTextStyle.Overlink;
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
            case DamageTextStyle.OverlinkImpact:
                return LiftColor(baseColor, criticalColorLift);
            case DamageTextStyle.Reward:
                return LiftColor(baseColor, rewardColorLift);
            case DamageTextStyle.Overlink:
                return Color.white;
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
