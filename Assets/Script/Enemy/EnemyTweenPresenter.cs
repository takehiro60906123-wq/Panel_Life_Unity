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

    [Header("死亡演出")]
    [SerializeField] private float deathDropY = 0.18f;
    [SerializeField] private float deathDuration = 0.30f;
    [SerializeField] private float deathEndScale = 0.15f;

    [Header("溜め演出（HeavyHit）")]
    [SerializeField] private float chargePulseDuration = 0.35f;
    [SerializeField] private float chargeScaleUp = 1.18f;
    [SerializeField] private Color chargeFlashColor = new Color(1f, 0.6f, 0.2f, 1f);
    [SerializeField] private int chargeShakeVibrato = 12;
    [SerializeField] private float chargeShakeStrength = 0.04f;

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
    // 被弾（既存）
    // =============================================================

    public void PlayHitTween()
    {
        EnsureSetup();
        KillTweens();

        Vector3 hitPos = baseLocalPos + new Vector3(-attackDirectionX * hitBackDistance, 0f, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(hitPos, hitBackDuration).SetEase(Ease.OutQuad));
        seq.Append(visualRoot.DOLocalMove(baseLocalPos, hitReturnDuration).SetEase(Ease.InQuad));

        FlashColor(hitFlashColor, hitFlashDuration);
    }

    // =============================================================
    // 死亡（既存）
    // =============================================================

    public void PlayDeathTween()
    {
        EnsureSetup();
        KillTweens();

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(baseLocalPos + new Vector3(0f, -deathDropY, 0f), deathDuration).SetEase(Ease.InQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * deathEndScale, deathDuration).SetEase(Ease.InBack));

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;
            sr.DOFade(0f, deathDuration);
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
}