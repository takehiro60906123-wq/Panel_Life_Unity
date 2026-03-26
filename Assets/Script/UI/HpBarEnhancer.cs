// =============================================================
// HpBarEnhancer.cs
// HP スライダーにリッチな演出を追加する
//
// 機能:
//   1. メインバーのスムーズ減少（一気に減らずスッと減る）
//   2. ダメージトレイル（赤い残像バーが遅れて追いかける）
//   3. ダメージ時のパンチ演出（バーが揺れる）
//   4. 回復時の緑フラッシュ
//
// 使い方:
//   HP の Slider がついた GameObject にこのスクリプトをアタッチ。
//   ダメージトレイル用の Image は自動生成される。
// =============================================================
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Slider))]
public class HpBarEnhancer : MonoBehaviour
{
    [Header("スムーズ減少")]
    [Tooltip("メインバーが目標値に到達するまでの時間")]
    [SerializeField] private float mainBarDuration = 0.25f;
    [SerializeField] private Ease mainBarEase = Ease.OutQuad;

    [Header("ダメージトレイル")]
    [Tooltip("赤い残像バーの色")]
    [SerializeField] private Color trailColor = new Color(0.85f, 0.15f, 0.1f, 0.8f);
    [Tooltip("トレイルが追いかけ始めるまでの遅延")]
    [SerializeField] private float trailDelay = 0.4f;
    [Tooltip("トレイルが目標値に到達するまでの時間")]
    [SerializeField] private float trailDuration = 0.5f;
    [SerializeField] private Ease trailEase = Ease.InQuad;

    [Header("ダメージ時パンチ")]
    [SerializeField] private float damagePunchScale = 0.08f;
    [SerializeField] private float damagePunchDuration = 0.15f;
    [SerializeField] private int damagePunchVibrato = 6;

    [Header("回復フラッシュ")]
    [SerializeField] private Color healFlashColor = new Color(0.3f, 1f, 0.5f, 0.5f);
    [SerializeField] private float healFlashDuration = 0.2f;

    [Header("危機演出")]
    [Tooltip("この割合以下で危機色に変わる（0.25 = HP25%以下）")]
    [SerializeField, Range(0f, 0.5f)] private float dangerThreshold = 0.25f;
    [SerializeField] private Color dangerBarColor = new Color(0.9f, 0.25f, 0.15f, 1f);
    [SerializeField] private float dangerPulseSpeed = 1.5f;

    // 内部
    private Slider slider;
    private RectTransform sliderRect;
    private Image fillImage;
    private Color fillBaseColor;
    private Image trailImage;
    private RectTransform trailRect;
    private float lastKnownValue;
    private float trailValue;
    private bool initialized;
    private Tween mainBarTween;
    private Tween trailTween;
    private Tween punchTween;
    private Tween dangerPulseTween;
    private bool isDangerPulsing;
    private Vector3 baseScale;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        sliderRect = GetComponent<RectTransform>();
        if (sliderRect != null)
        {
            baseScale = sliderRect.localScale;
        }
        Initialize();
    }

    private void Initialize()
    {
        if (initialized || slider == null) return;
        initialized = true;

        // Fill Image を探す
        Transform fillArea = slider.fillRect;
        if (fillArea != null)
        {
            fillImage = fillArea.GetComponent<Image>();
            if (fillImage != null)
            {
                fillBaseColor = fillImage.color;
            }
        }

        lastKnownValue = slider.value;
        trailValue = slider.value;

        CreateTrailImage();
    }

    private void OnDestroy()
    {
        mainBarTween?.Kill();
        trailTween?.Kill();
        punchTween?.Kill();
        dangerPulseTween?.Kill();
    }

    // =============================================================
    // ダメージトレイル Image 自動生成
    // =============================================================

    private void CreateTrailImage()
    {
        if (slider.fillRect == null) return;

        // Fill Image の親を取得
        Transform fillParent = slider.fillRect.parent;
        if (fillParent == null) return;

        // トレイル用 Image を Fill の直前に挿入
        GameObject trailObj = new GameObject("DamageTrail", typeof(RectTransform), typeof(Image));
        trailObj.transform.SetParent(fillParent, false);

        // Fill の手前（後ろに見える位置）に配置
        int fillIndex = slider.fillRect.GetSiblingIndex();
        trailObj.transform.SetSiblingIndex(Mathf.Max(0, fillIndex));

        trailRect = trailObj.GetComponent<RectTransform>();
        // Fill と同じ Anchor/Pivot 設定をコピー
        RectTransform fillRef = slider.fillRect as RectTransform;
        if (fillRef != null)
        {
            trailRect.anchorMin = fillRef.anchorMin;
            trailRect.anchorMax = fillRef.anchorMax;
            trailRect.pivot = fillRef.pivot;
            trailRect.offsetMin = fillRef.offsetMin;
            trailRect.offsetMax = fillRef.offsetMax;
            trailRect.sizeDelta = fillRef.sizeDelta;
            trailRect.anchoredPosition = fillRef.anchoredPosition;
        }

        trailImage = trailObj.GetComponent<Image>();
        trailImage.color = trailColor;
        trailImage.raycastTarget = false;
        trailImage.type = Image.Type.Filled;
        trailImage.fillMethod = Image.FillMethod.Horizontal;
        trailImage.fillAmount = 1f;

        // Fill Image のスプライトをコピー（見た目を揃える）
        if (fillImage != null && fillImage.sprite != null)
        {
            trailImage.sprite = fillImage.sprite;
            trailImage.type = fillImage.type;
            if (fillImage.type == Image.Type.Filled)
            {
                trailImage.fillMethod = fillImage.fillMethod;
                trailImage.fillOrigin = fillImage.fillOrigin;
            }
        }
    }

    // =============================================================
    // メイン: 値の変化を検知してアニメーション
    // =============================================================

    private void LateUpdate()
    {
        if (slider == null) return;
        if (!initialized) Initialize();

        float currentValue = slider.value;

        // 値が変わった
        if (!Mathf.Approximately(currentValue, lastKnownValue))
        {
            float delta = currentValue - lastKnownValue;

            if (delta < 0f)
            {
                // ── ダメージ ──
                OnDamage(lastKnownValue, currentValue);
            }
            else
            {
                // ── 回復 ──
                OnHeal(currentValue);
            }

            lastKnownValue = currentValue;
        }

        // 危機判定
        UpdateDangerState();
    }

    private void OnDamage(float fromValue, float toValue)
    {
        // メインバーは即座に目標値へ（Slider.value は既にセットされている）
        // → Slider の value は BattleUnitView が直接セットするので、
        //   ここでは追加演出だけ行う

        // トレイルバーのアニメーション
        AnimateTrail(toValue);

        // パンチ演出
        PlayDamagePunch();
    }

    private void OnHeal(float toValue)
    {
        // トレイルも即座に合わせる（回復時は残像不要）
        trailValue = toValue;
        UpdateTrailVisual(toValue);

        trailTween?.Kill();

        // 緑フラッシュ
        PlayHealFlash();
    }

    // =============================================================
    // ダメージトレイル
    // =============================================================

    private void AnimateTrail(float targetValue)
    {
        trailTween?.Kill();

        // トレイルは現在のメインバー値から遅れて減る
        float startTrailValue = trailValue;

        trailTween = DOTween.To(
                () => trailValue,
                x =>
                {
                    trailValue = x;
                    UpdateTrailVisual(x);
                },
                targetValue,
                trailDuration)
            .SetDelay(trailDelay)
            .SetEase(trailEase);
    }

    private void UpdateTrailVisual(float value)
    {
        if (trailImage == null || slider == null) return;

        float ratio = Mathf.Clamp01(value / Mathf.Max(0.001f, slider.maxValue));

        if (trailImage.type == Image.Type.Filled)
        {
            trailImage.fillAmount = ratio;
        }
        else
        {
            // Filled じゃない場合は anchorMax で制御
            if (trailRect != null)
            {
                trailRect.anchorMax = new Vector2(ratio, trailRect.anchorMax.y);
            }
        }
    }

    // =============================================================
    // ダメージパンチ
    // =============================================================

    private void PlayDamagePunch()
    {
        if (sliderRect == null) return;

        punchTween?.Kill();
        sliderRect.localScale = baseScale;

        punchTween = sliderRect.DOPunchScale(
            baseScale * damagePunchScale,
            damagePunchDuration,
            damagePunchVibrato,
            0.8f)
            .OnKill(() =>
            {
                if (sliderRect != null) sliderRect.localScale = baseScale;
            });
    }

    // =============================================================
    // 回復フラッシュ
    // =============================================================

    private void PlayHealFlash()
    {
        if (fillImage == null) return;

        fillImage.DOKill();
        fillImage.color = healFlashColor;
        fillImage.DOColor(fillBaseColor, healFlashDuration).SetEase(Ease.OutQuad);
    }

    // =============================================================
    // 危機演出（HP低い時に脈動）
    // =============================================================

    private void UpdateDangerState()
    {
        if (fillImage == null || slider == null) return;

        float ratio = slider.value / Mathf.Max(0.001f, slider.maxValue);
        bool inDanger = ratio <= dangerThreshold && ratio > 0f;

        if (inDanger && !isDangerPulsing)
        {
            isDangerPulsing = true;
            fillImage.DOKill();
            fillImage.color = dangerBarColor;

            dangerPulseTween = fillImage
                .DOColor(fillBaseColor, dangerPulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else if (!inDanger && isDangerPulsing)
        {
            isDangerPulsing = false;
            dangerPulseTween?.Kill();
            fillImage.DOKill();
            fillImage.color = fillBaseColor;
        }
    }
}
