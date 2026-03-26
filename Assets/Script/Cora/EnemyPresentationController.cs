using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyPresentationController : MonoBehaviour
{
    private float enemyRevealDuration = 0.2f;
    private Ease roomTravelEase = Ease.Linear;

    [Header("ìoèÍââèoê›íË")]
    [SerializeField] private float entranceSlideDistance = 2.4f;
    [SerializeField] private float entranceSlideDuration = 0.16f;
    [SerializeField] private Ease entranceSlideEase = Ease.OutCubic;
    [SerializeField] private float entranceOvershootDistance = 0.12f;
    [SerializeField] private float entranceStartScale = 0.92f;
    [SerializeField] private float entranceOvershootScale = 1.05f;
    [SerializeField] private float entranceStartAlpha = 0.35f;
    [SerializeField] private float entranceFlashDuration = 0.10f;
    [SerializeField] private Color entranceFlashColor = new Color(1f, 0.96f, 0.82f, 1f);
    [SerializeField] private float entranceLandingPunch = 0.08f;

    public void Configure(float enemyRevealDuration, Ease roomTravelEase)
    {
        this.enemyRevealDuration = enemyRevealDuration;
        this.roomTravelEase = roomTravelEase;
    }

    public void RefreshUpcomingEnemyStandbyVisuals(IEnumerable<BattleUnit> upcomingEnemies)
    {
        if (upcomingEnemies == null) return;

        foreach (BattleUnit unit in upcomingEnemies)
        {
            if (unit == null) continue;

            unit.transform.localScale = Vector3.one * 0.8f;
            RestoreEnemyColors(unit);
            SetEnemyAlpha(unit, 1f);
            SetEnemyVisible(unit, false);
        }
    }

    public void ActivateEnemyAsCurrent(BattleUnit unit)
    {
        if (unit == null) return;

        PrepareEnemyForEntrance(unit, true);
        unit.InitializeTurn();
        PlayEntranceAnimation(unit);
    }

    public void PrepareEnemyForDeferredEntrance(BattleUnit unit)
    {
        if (unit == null) return;

        unit.transform.DOKill();

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            sr.DOKill();
        }

        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 0f);
        SetEnemyVisible(unit, false);
        unit.transform.localScale = Vector3.one;
    }

    public void PrepareEnemyForEntrance(BattleUnit unit, bool showUI)
    {
        if (unit == null) return;

        unit.transform.DOKill();

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            sr.DOKill();
        }

        SetEnemyVisible(unit, true);
        unit.SetUIActive(showUI);
        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 1f);
    }

    public void PlayCurrentEnemyEntrance(BattleUnit unit)
    {
        if (unit == null) return;

        PrepareEnemyForEntrance(unit, true);
        PlayEntranceAnimation(unit);
    }

    private void PlayEntranceAnimation(BattleUnit unit)
    {
        if (unit == null) return;

        Transform root = unit.transform;
        root.DOKill();

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);

        Vector3 targetPos = root.position;
        Vector3 startPos = targetPos + Vector3.right * entranceSlideDistance;
        Vector3 overPos = targetPos + Vector3.left * entranceOvershootDistance;

        root.position = startPos;
        root.localScale = Vector3.one * entranceStartScale;

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;

            sr.DOKill();

            Color original = sr.color;
            original.a = 1f;

            Color startColor = entranceFlashColor;
            startColor.a = entranceStartAlpha;

            sr.color = startColor;
            sr.DOColor(original, entranceFlashDuration).SetEase(Ease.OutQuad);
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(
            root.DOMove(overPos, entranceSlideDuration * 0.78f)
                .SetEase(entranceSlideEase)
        );

        seq.Join(
            root.DOScale(Vector3.one * entranceOvershootScale, entranceSlideDuration * 0.78f)
                .SetEase(Ease.OutQuad)
        );

        seq.Append(
            root.DOMove(targetPos, entranceSlideDuration * 0.22f)
                .SetEase(Ease.OutQuad)
        );

        seq.Join(
            root.DOScale(Vector3.one, entranceSlideDuration * 0.22f)
                .SetEase(Ease.OutBack)
        );

        seq.Append(
            root.DOPunchPosition(
                new Vector3(-entranceLandingPunch, 0f, 0f),
                0.10f,
                8,
                0.75f
            )
        );

        if (unit.animator != null)
        {
            unit.animator.Play("IDLE", 0, 0f);
        }
    }

    public void RestoreEnemyColors(BattleUnit unit)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            Color c = sr.color;
            c.r = 1f;
            c.g = 1f;
            c.b = 1f;
            sr.color = c;
        }
    }

    public void SetEnemyVisible(BattleUnit unit, bool isVisible)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            sr.enabled = isVisible;
        }

        unit.SetUIActive(isVisible);
    }

    public void SetEnemyAlpha(BattleUnit unit, float alpha)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    public void RevealWaitingEnemy(BattleUnit unit)
    {
        if (unit == null) return;

        SetEnemyVisible(unit, true);
        unit.SetUIActive(false);
        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 0f);

        unit.transform.localScale = Vector3.one * 0.96f;

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            sr.DOFade(1f, enemyRevealDuration);
        }

        unit.transform.DOScale(Vector3.one, enemyRevealDuration).SetEase(Ease.OutQuad);

        if (unit.animator != null)
        {
            unit.animator.Play("IDLE", 0, 0f);
        }
    }

    public void HideAllUpcomingEnemies(IEnumerable<BattleUnit> upcomingEnemies)
    {
        if (upcomingEnemies == null) return;

        foreach (BattleUnit enemy in upcomingEnemies)
        {
            if (enemy == null) continue;
            SetEnemyVisible(enemy, false);
        }
    }

    public void ShiftUpcomingEnemies(IEnumerable<BattleUnit> upcomingEnemies, float deltaX, float duration)
    {
        if (upcomingEnemies == null) return;

        foreach (BattleUnit enemy in upcomingEnemies)
        {
            if (enemy == null) continue;
            enemy.transform.DOMoveX(enemy.transform.position.x + deltaX, duration).SetEase(roomTravelEase);
        }
    }

    public void SetMoveAnimation(Animator animator, bool isMoving)
    {
        if (animator == null) return;

        bool hasMoveBool = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "1_Move" && param.type == AnimatorControllerParameterType.Bool)
            {
                hasMoveBool = true;
                break;
            }
        }

        if (hasMoveBool)
        {
            animator.SetBool("1_Move", isMoving);
        }
        else
        {
            if (isMoving) animator.Play("MOVE", 0, 0f);
            else animator.Play("IDLE", 0, 0f);
        }
    }
}