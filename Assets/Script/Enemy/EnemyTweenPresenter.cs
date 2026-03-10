using DG.Tweening;
using UnityEngine;

public class EnemyTweenPresenter : MonoBehaviour
{
    [Header("ŽQÆ")]
    [SerializeField] private Transform visualRoot;

    [Header("UŒ‚‰‰o")]
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

    [Header("”í’e‰‰o")]
    [SerializeField] private float hitBackDistance = 0.12f;
    [SerializeField] private float hitBackDuration = 0.05f;
    [SerializeField] private float hitReturnDuration = 0.08f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.85f, 0.85f, 1f);
    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("Ž€–S‰‰o")]
    [SerializeField] private float deathDropY = 0.18f;
    [SerializeField] private float deathDuration = 0.30f;
    [SerializeField] private float deathEndScale = 0.15f;

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

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color original = baseColors[i];
            sr.color = attackFlashColor;
            sr.DOColor(original, attackFlashDuration);
        }
    }

    public void PlayHitTween()
    {
        EnsureSetup();
        KillTweens();

        Vector3 hitPos = baseLocalPos + new Vector3(-attackDirectionX * hitBackDistance, 0f, 0f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(hitPos, hitBackDuration).SetEase(Ease.OutQuad));
        seq.Append(visualRoot.DOLocalMove(baseLocalPos, hitReturnDuration).SetEase(Ease.InQuad));

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer sr = spriteRenderers[i];
            if (sr == null) continue;

            Color original = baseColors[i];
            sr.color = hitFlashColor;
            sr.DOColor(original, hitFlashDuration);
        }
    }

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