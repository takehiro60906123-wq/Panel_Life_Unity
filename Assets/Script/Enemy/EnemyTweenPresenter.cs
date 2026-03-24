using DG.Tweening;
using UnityEngine;

public class EnemyTweenPresenter : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform visualRoot;

    [Header("通常攻撃演出")]
    [SerializeField] private float attackDirectionX = -1f;
    [SerializeField] private float attackWindupDistance = 0.14f;
    [SerializeField] private float attackLungeDistance = 0.42f;
    [SerializeField] private float attackHopY = 0.06f;
    [SerializeField] private float attackWindupDuration = 0.07f;
    [SerializeField] private float attackLungeDuration = 0.11f;
    [SerializeField] private float attackRecoverDuration = 0.12f;
    [SerializeField] private float attackPunchScale = 0.16f;
    [SerializeField] private float attackPunchRotation = 16f;
    [SerializeField] private Color attackFlashColor = new Color(1f, 0.95f, 0.8f, 1f);
    [SerializeField] private float attackFlashDuration = 0.10f;

    [Header("被弾演出")]
    [SerializeField] private float hitBackDistance = 0.12f;
    [SerializeField] private float hitBackDuration = 0.05f;
    [SerializeField] private float hitReturnDuration = 0.08f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.85f, 0.85f, 1f);
    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("強被弾演出（Shotgun）")]
    [SerializeField] private float heavyHitBackDistance = 0.22f;
    [SerializeField] private float heavyHitBackDuration = 0.045f;
    [SerializeField] private float heavyHitReturnDuration = 0.09f;
    [SerializeField] private float heavyHitPunchScale = 0.18f;
    [SerializeField] private float heavyHitPunchRotation = 10f;
    [SerializeField] private Color heavyHitFlashColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float heavyHitFlashDuration = 0.07f;
    [SerializeField] private float heavyHitCooldown = 0.09f;

    private float lastHeavyHitTime = -999f;

    [Header("死亡演出")]
    [SerializeField] private float deathLiftY = 0.08f;
    [SerializeField] private float deathDropY = 0.22f;
    [SerializeField] private float deathDuration = 0.22f;
    [SerializeField] private float deathEndScale = 0.12f;
    [SerializeField] private float deathSquashX = 1.08f;
    [SerializeField] private float deathSquashY = 0.72f;
    [SerializeField] private float deathFlashDuration = 0.07f;
    [SerializeField] private Color deathFlashColor = new Color(1f, 1f, 1f, 1f);

    [Header("銃撃破演出")]
    [SerializeField] private float gunDeathBlowbackDistance = 0.55f;
    [SerializeField] private float gunDeathBlowbackDuration = 0.10f;
    [SerializeField] private float gunDeathSpinAngle = 360f;
    [SerializeField] private float gunDeathFadeDuration = 0.18f;
    [SerializeField] private float gunDeathEndScale = 0.65f;
    [SerializeField] private Color gunDeathFlashColor = new Color(1f, 0.95f, 0.85f, 1f);

    [Header("ライフル撃破演出")]
    [SerializeField] private float rifleDeathPauseDuration = 0.08f;
    [SerializeField] private float rifleDeathCrumbleDuration = 0.16f;
    [SerializeField] private float rifleDeathSplitOffsetX = 0.12f;
    [SerializeField] private Color rifleDeathFlashColor = new Color(1f, 1f, 0.92f, 1f);

    [Header("オーバーリンク撃破演出")]
    [SerializeField] private float overlinkDeathExpandScale = 1.55f;
    [SerializeField] private float overlinkDeathExpandDuration = 0.08f;
    [SerializeField] private float overlinkDeathShrinkDuration = 0.12f;
    [SerializeField] private float overlinkDeathEndScale = 0.08f;
    [SerializeField] private Color overlinkDeathFlashColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private float overlinkDeathShakeIntensity = 0.06f;

    [Header("溜め演出（HeavyHit）")]
    [SerializeField] private float chargePulseDuration = 0.35f;
    [SerializeField] private float chargeScaleUp = 1.18f;
    [SerializeField] private Color chargeFlashColor = new Color(1f, 0.6f, 0.2f, 1f);
    [SerializeField] private int chargeShakeVibrato = 12;
    [SerializeField] private float chargeShakeStrength = 0.04f;

    [Header("攻撃予兆パルス")]
    [SerializeField] private Color dangerPulseColor = new Color(1f, 0.25f, 0.18f, 1f);
    [SerializeField] private float dangerPulseInterval = 0.55f;
    [SerializeField] private float dangerPulseScaleAmount = 1.06f;
    [SerializeField] private float dangerPulseBobY = 0.025f;

[Header("スキル演出（PanelCorrupt / SelfBuff）")]
    [SerializeField] private float skillPulseDuration = 0.3f;
    [SerializeField] private float skillScaleUp = 1.12f;
    [SerializeField] private Color corruptFlashColor = new Color(0.7f, 0.2f, 0.8f, 1f);
    [SerializeField] private Color healFlashColor = new Color(0.3f, 1f, 0.5f, 1f);

    [Header("重撃発射演出")]
    [SerializeField] private float heavyLungeDistance = 0.6f;
    [SerializeField] private float heavyLungeDuration = 0.14f;
    [SerializeField] private float heavyRecoverDuration = 0.18f;
    [SerializeField] private Color heavyFlashColor = new Color(1f, 0.4f, 0.1f, 1f);

    private SpriteRenderer[] spriteRenderers;
    private Color[] baseColors;
    private Vector3 baseLocalPos;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private bool cached;
    private Sequence dangerPulseSequence;
    private Tween[] dangerPulseColorTweens;
    private bool isDangerPulsePlaying;

    private void Awake()
    {
        EnsureSetup();
        ResetVisualsImmediate();
    }

    public void EnsureSetup()
    {
        if (cached) return;

        if (visualRoot == null)
        {
            Transform unitRoot = transform.Find("UnitRoot");
            if (unitRoot != null)
            {
                visualRoot = unitRoot;
            }
            else if (transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }
            else
            {
                visualRoot = transform;
            }
        }

        spriteRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            baseColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }

        baseLocalPos = visualRoot.localPosition;
        baseLocalScale = visualRoot.localScale;
        baseLocalRotation = visualRoot.localRotation;
        cached = true;
    }

    public void ResetVisualsImmediate()
    {
        EnsureSetup();
        StopDangerPulseInternal();
        KillTweens();

        if (visualRoot != null)
        {
            visualRoot.localPosition = baseLocalPos;
            visualRoot.localScale = baseLocalScale;
            visualRoot.localRotation = baseLocalRotation;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;

            Color c = baseColors[i];
            c.a = 1f;
            spriteRenderers[i].color = c;
        }
    }

    public void PlayIdleReset()
    {
        ResetVisualsImmediate();
    }

    // =============================================================
    // 通常攻撃（既存）
    // =============================================================

    public void PlayAttackTween()
    {
        EnsureSetup();
        KillTweens();

        Vector3 windupPos = baseLocalPos + new Vector3(-attackDirectionX * attackWindupDistance, -attackHopY * 0.35f, 0f);
        Vector3 lungePos = baseLocalPos + new Vector3(attackDirectionX * attackLungeDistance, attackHopY, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(windupPos, attackWindupDuration).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * 0.93f, attackWindupDuration).SetEase(Ease.OutQuad));

        seq.Append(visualRoot.DOLocalMove(lungePos, attackLungeDuration).SetEase(Ease.OutExpo));
        seq.Join(visualRoot.DOScale(baseLocalScale * 1.12f, attackLungeDuration).SetEase(Ease.OutBack));
        seq.Join(visualRoot.DOPunchRotation(new Vector3(0f, 0f, attackPunchRotation * -attackDirectionX), attackLungeDuration + 0.04f, 5, 0.8f));

        seq.Append(visualRoot.DOLocalMove(baseLocalPos, attackRecoverDuration).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale, attackRecoverDuration).SetEase(Ease.OutQuad));

        FlashColor(attackFlashColor, attackFlashDuration);
    }

    // =============================================================
    // 溜め演出 — HeavyHit 準備ターン
    // 体が膨らんで震える → オレンジに光る
    // =============================================================

    public void PlayChargeTween()
    {
        EnsureSetup();
        KillTweens();

        Sequence seq = DOTween.Sequence();

        // 膨張
        seq.Append(visualRoot.DOScale(baseLocalScale * chargeScaleUp, chargePulseDuration * 0.5f).SetEase(Ease.OutQuad));

        // 震え
        seq.Append(visualRoot.DOShakePosition(chargePulseDuration, chargeShakeStrength, chargeShakeVibrato, 90f, false, true));

        // 元に戻る
        seq.Append(visualRoot.DOScale(baseLocalScale, chargePulseDuration * 0.3f).SetEase(Ease.InQuad));
        seq.Join(visualRoot.DOLocalMove(baseLocalPos, chargePulseDuration * 0.3f).SetEase(Ease.InQuad));

        FlashColor(chargeFlashColor, chargePulseDuration);
    }

    // =============================================================
    // 重撃発射 — HeavyHit 攻撃ターン
    // 通常より大きく突っ込む → 赤オレンジに光る
    // =============================================================

    public void PlayHeavyAttackTween()
    {
        EnsureSetup();
        KillTweens();

        Vector3 windupPos = baseLocalPos + new Vector3(-attackDirectionX * attackWindupDistance * 1.5f, -attackHopY * 0.5f, 0f);
        Vector3 lungePos = baseLocalPos + new Vector3(attackDirectionX * heavyLungeDistance, attackHopY * 1.5f, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(windupPos, attackWindupDuration * 1.2f).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * 0.88f, attackWindupDuration * 1.2f).SetEase(Ease.OutQuad));

        seq.Append(visualRoot.DOLocalMove(lungePos, heavyLungeDuration).SetEase(Ease.OutExpo));
        seq.Join(visualRoot.DOScale(baseLocalScale * 1.25f, heavyLungeDuration).SetEase(Ease.OutBack));
        seq.Join(visualRoot.DOPunchRotation(new Vector3(0f, 0f, attackPunchRotation * 1.5f * -attackDirectionX), heavyLungeDuration + 0.06f, 6, 0.9f));

        seq.Append(visualRoot.DOLocalMove(baseLocalPos, heavyRecoverDuration).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale, heavyRecoverDuration).SetEase(Ease.OutQuad));

        FlashColor(heavyFlashColor, heavyLungeDuration + 0.05f);
    }

    // =============================================================
    // スキル演出 — PanelCorrupt（盤面汚染）
    // 紫に脈動 → 波動を放つ感じ
    // =============================================================

    public void PlayCorruptSkillTween()
    {
        EnsureSetup();
        KillTweens();

        Sequence seq = DOTween.Sequence();

        // 収縮
        seq.Append(visualRoot.DOScale(baseLocalScale * 0.9f, skillPulseDuration * 0.3f).SetEase(Ease.InQuad));

        // 膨張（波動放出）
        seq.Append(visualRoot.DOScale(baseLocalScale * skillScaleUp, skillPulseDuration * 0.3f).SetEase(Ease.OutBack));

        // 戻る
        seq.Append(visualRoot.DOScale(baseLocalScale, skillPulseDuration * 0.4f).SetEase(Ease.OutQuad));

        FlashColor(corruptFlashColor, skillPulseDuration);
    }

    // =============================================================
    // 回復演出 — SelfBuff
    // 緑に光って少し浮く
    // =============================================================

    public void PlayHealTween()
    {
        EnsureSetup();
        KillTweens();

        Sequence seq = DOTween.Sequence();

        // 少し浮く
        seq.Append(visualRoot.DOLocalMove(baseLocalPos + Vector3.up * 0.08f, skillPulseDuration * 0.4f).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * 1.06f, skillPulseDuration * 0.4f).SetEase(Ease.OutQuad));

        // 戻る
        seq.Append(visualRoot.DOLocalMove(baseLocalPos, skillPulseDuration * 0.6f).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale, skillPulseDuration * 0.6f).SetEase(Ease.OutQuad));

        FlashColor(healFlashColor, skillPulseDuration);
    }


    // =============================================================
    // 攻撃予兆パルス — クールダウン1以下で開始するループ演出
    // 赤く脈動 + 微かに前傾する → 「次に殴ってくる」が映像で伝わる
    // =============================================================

    public void PlayDangerPulseTween()
    {
        if (isDangerPulsePlaying) return;

        EnsureSetup();
        StopDangerPulseInternal();

        isDangerPulsePlaying = true;

        float halfInterval = dangerPulseInterval * 0.5f;

        if (visualRoot != null)
        {
            dangerPulseSequence = DOTween.Sequence();
            dangerPulseSequence.Append(
                visualRoot.DOScale(baseLocalScale * dangerPulseScaleAmount, halfInterval)
                    .SetEase(Ease.InOutSine));
            dangerPulseSequence.Join(
                visualRoot.DOLocalMove(
                    baseLocalPos + new Vector3(attackDirectionX * dangerPulseBobY, dangerPulseBobY * 0.5f, 0f),
                    halfInterval)
                    .SetEase(Ease.InOutSine));
            dangerPulseSequence.Append(
                visualRoot.DOScale(baseLocalScale, halfInterval)
                    .SetEase(Ease.InOutSine));
            dangerPulseSequence.Join(
                visualRoot.DOLocalMove(baseLocalPos, halfInterval)
                    .SetEase(Ease.InOutSine));
            dangerPulseSequence.SetLoops(-1, LoopType.Restart);
            dangerPulseSequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        if (spriteRenderers == null || spriteRenderers.Length == 0)
        {
            dangerPulseColorTweens = null;
            return;
        }

        dangerPulseColorTweens = new Tween[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color original = (baseColors != null && i < baseColors.Length) ? baseColors[i] : sr.color;
            Color pulse = Color.Lerp(original, dangerPulseColor, 0.45f);

            Sequence colorSeq = DOTween.Sequence();
            colorSeq.Append(sr.DOColor(pulse, halfInterval).SetEase(Ease.InOutSine));
            colorSeq.Append(sr.DOColor(original, halfInterval).SetEase(Ease.InOutSine));
            colorSeq.SetLoops(-1, LoopType.Restart);
            colorSeq.SetLink(sr.gameObject, LinkBehaviour.KillOnDestroy);
            dangerPulseColorTweens[i] = colorSeq;
        }
    }

    public void StopDangerPulse()
    {
        if (!isDangerPulsePlaying) return;
        StopDangerPulseInternal();
    }

    private void StopDangerPulseInternal()
    {
        isDangerPulsePlaying = false;

        if (dangerPulseSequence != null && dangerPulseSequence.IsActive())
        {
            dangerPulseSequence.Kill();
        }
        dangerPulseSequence = null;

        if (dangerPulseColorTweens != null)
        {
            for (int i = 0; i < dangerPulseColorTweens.Length; i++)
            {
                Tween tween = dangerPulseColorTweens[i];
                if (tween != null && tween.IsActive())
                {
                    tween.Kill();
                }
            }
        }
        dangerPulseColorTweens = null;

        if (spriteRenderers != null && baseColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer sr = spriteRenderers[i];
                if (sr == null) continue;

                Color c = i < baseColors.Length ? baseColors[i] : sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = baseLocalPos;
            visualRoot.localScale = baseLocalScale;
            visualRoot.localRotation = baseLocalRotation;
        }
    }

    // =============================================================
    // 被弾（既存）
    // =============================================================

    public void PlayHitTween()
    {
        EnsureSetup();
        KillTweens();

        Vector3 hitPos = baseLocalPos + new Vector3(-attackDirectionX * (hitBackDistance * 1.35f), 0f, 0f);

        Sequence seq = DOTween.Sequence();

        // 被弾直後に少し縮む
        seq.Append(
            visualRoot.DOScale(baseLocalScale * 0.92f, hitBackDuration * 0.75f)
                .SetEase(Ease.OutQuad)
        );

        // ノックバック
        seq.Join(
            visualRoot.DOLocalMove(hitPos, hitBackDuration)
                .SetEase(Ease.OutQuad)
        );

        // 細かい揺れ
        seq.Join(
            visualRoot.DOShakePosition(
                hitBackDuration + hitReturnDuration,
                new Vector3(0.08f, 0.025f, 0f),
                20,
                90f,
                false,
                true
            )
        );

        // 元の位置に戻る
        seq.Append(
            visualRoot.DOLocalMove(baseLocalPos, hitReturnDuration * 0.9f)
                .SetEase(Ease.OutCubic)
        );

        seq.Join(
            visualRoot.DOScale(baseLocalScale, hitReturnDuration)
                .SetEase(Ease.OutBack)
        );

        // 白めの強いフラッシュ
        FlashColor(new Color(1f, 1f, 1f, 1f), 0.08f);
    }

    // =============================================================
    // 死亡（既存）
    // =============================================================

    public void PlayDeathTween()
    {
        EnsureSetup();
        KillTweens();

        Vector3 liftPos = baseLocalPos + new Vector3(0f, deathLiftY, 0f);
        Vector3 dropPos = baseLocalPos + new Vector3(0f, -deathDropY, 0f);
        Vector3 squashScale = new Vector3(
            baseLocalScale.x * deathSquashX,
            baseLocalScale.y * deathSquashY,
            baseLocalScale.z
        );
        Vector3 endScale = baseLocalScale * deathEndScale;

        Sequence seq = DOTween.Sequence();

        // 倒れ始めに一瞬だけ浮いて、白く飛ばす
        seq.Append(
            visualRoot.DOLocalMove(liftPos, deathDuration * 0.18f)
                .SetEase(Ease.OutQuad)
        );

        seq.Join(
            visualRoot.DOScale(squashScale, deathDuration * 0.18f)
                .SetEase(Ease.OutQuad)
        );

        FlashColor(deathFlashColor, deathFlashDuration);

        // 下へ落ちながら潰れて消える
        seq.Append(
            visualRoot.DOLocalMove(dropPos, deathDuration * 0.82f)
                .SetEase(Ease.InQuad)
        );

        seq.Join(
            visualRoot.DOScale(endScale, deathDuration * 0.82f)
                .SetEase(Ease.InBack)
        );

        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, attackDirectionX * -10f),
                deathDuration * 0.35f,
                4,
                0.8f
            )
        );

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            sr.DOKill();

            Color c = sr.color;
            c.a = 1f;
            sr.color = c;

            sr.DOFade(0f, deathDuration * 0.72f)
                .SetDelay(deathDuration * 0.10f)
                .SetEase(Ease.OutQuad);
        }
    }

    public void PlayGunDeathTween()
    {
        EnsureSetup();
        KillTweens();

        FlashColor(gunDeathFlashColor, 0.06f);

        Vector3 blowbackTarget = baseLocalPos + new Vector3(
            -attackDirectionX * gunDeathBlowbackDistance,
            0.12f,
            0f);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(0.02f);

        seq.Append(
            visualRoot.DOLocalMove(blowbackTarget, gunDeathBlowbackDuration)
                .SetEase(Ease.OutQuad));

        seq.Join(
            visualRoot.DORotate(
                new Vector3(0f, 0f, -attackDirectionX * gunDeathSpinAngle),
                gunDeathBlowbackDuration + gunDeathFadeDuration,
                RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad));

        seq.Join(
            visualRoot.DOScale(baseLocalScale * gunDeathEndScale, gunDeathFadeDuration)
                .SetDelay(gunDeathBlowbackDuration * 0.5f)
                .SetEase(Ease.InBack));

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            sr.DOKill();
            sr.DOFade(0f, gunDeathFadeDuration)
                .SetDelay(gunDeathBlowbackDuration * 0.3f)
                .SetEase(Ease.OutQuad);
        }
    }

    public void PlayRifleDeathTween()
    {
        EnsureSetup();
        KillTweens();

        FlashColor(rifleDeathFlashColor, rifleDeathPauseDuration + 0.04f);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(rifleDeathPauseDuration);

        seq.Append(
            visualRoot.DOLocalMove(
                baseLocalPos + new Vector3(-attackDirectionX * rifleDeathSplitOffsetX, 0f, 0f),
                0.04f)
                .SetEase(Ease.OutQuad));

        Vector3 crumblePos = baseLocalPos + new Vector3(
            -attackDirectionX * rifleDeathSplitOffsetX,
            -deathDropY * 1.2f,
            0f);

        seq.Append(
            visualRoot.DOLocalMove(crumblePos, rifleDeathCrumbleDuration)
                .SetEase(Ease.InQuad));

        seq.Join(
            visualRoot.DOScale(baseLocalScale * deathEndScale, rifleDeathCrumbleDuration)
                .SetEase(Ease.InQuad));

        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, -attackDirectionX * 5f),
                rifleDeathCrumbleDuration,
                2,
                0.5f));

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            sr.DOKill();
            sr.DOFade(0f, rifleDeathCrumbleDuration * 0.8f)
                .SetDelay(rifleDeathPauseDuration + 0.04f)
                .SetEase(Ease.OutQuad);
        }
    }

    public void PlayOverlinkDeathTween()
    {
        EnsureSetup();
        KillTweens();

        FlashColor(overlinkDeathFlashColor, overlinkDeathExpandDuration + 0.04f);

        Sequence seq = DOTween.Sequence();
        seq.Append(
            visualRoot.DOScale(baseLocalScale * overlinkDeathExpandScale, overlinkDeathExpandDuration)
                .SetEase(Ease.OutBack));

        seq.Join(
            visualRoot.DOShakePosition(
                overlinkDeathExpandDuration,
                overlinkDeathShakeIntensity,
                20,
                90f,
                false,
                true));

        seq.Append(
            visualRoot.DOScale(baseLocalScale * overlinkDeathEndScale, overlinkDeathShrinkDuration)
                .SetEase(Ease.InBack));

        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, attackDirectionX * 25f),
                overlinkDeathShrinkDuration,
                6,
                0.9f));

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            sr.DOKill();

            Color flashColor = overlinkDeathFlashColor;
            flashColor.a = 1f;
            sr.color = flashColor;

            sr.DOFade(0f, overlinkDeathShrinkDuration * 0.85f)
                .SetDelay(overlinkDeathExpandDuration)
                .SetEase(Ease.InQuad);
        }
    }

    // =============================================================
    // ユーティリティ
    // =============================================================

    private void FlashColor(Color flashColor, float duration)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color original = baseColors[i];
            sr.color = flashColor;
            sr.DOColor(original, duration);
        }
    }

    private void KillTweens()
    {
        StopDangerPulseInternal();

        if (visualRoot != null)
        {
            visualRoot.DOKill();
        }

        if (spriteRenderers == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].DOKill();
        }
    }

    public void PlayHeavyHitTween()
    {
        EnsureSetup();

        if (Time.time - lastHeavyHitTime < heavyHitCooldown)
        {
            return;
        }

        lastHeavyHitTime = Time.time;

        KillTweens();

        Vector3 hitPos = baseLocalPos + new Vector3(-attackDirectionX * heavyHitBackDistance, 0f, 0f);

        Sequence seq = DOTween.Sequence();

        seq.Append(
            visualRoot.DOLocalMove(hitPos, heavyHitBackDuration)
                .SetEase(Ease.OutQuad)
        );

        seq.Join(
            visualRoot.DOShakePosition(
                heavyHitBackDuration + heavyHitReturnDuration,
                new Vector3(0.10f, 0.03f, 0f),
                22,
                90f,
                false,
                true
            )
        );

        seq.Join(
            visualRoot.DOPunchScale(
                new Vector3(heavyHitPunchScale, heavyHitPunchScale, 0f),
                heavyHitBackDuration + 0.03f,
                8,
                0.85f
            )
        );

        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, heavyHitPunchRotation * -attackDirectionX),
                heavyHitBackDuration + 0.04f,
                6,
                0.85f
            )
        );

        seq.Append(
            visualRoot.DOLocalMove(baseLocalPos, heavyHitReturnDuration)
                .SetEase(Ease.OutCubic)
        );

        FlashColor(heavyHitFlashColor, heavyHitFlashDuration);
    }
}