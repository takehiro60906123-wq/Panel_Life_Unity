using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyPresentationController : MonoBehaviour
{
    private float enemyRevealDuration = 0.2f;
    private Ease roomTravelEase = Ease.Linear;

    [Header("ìoèÍââèoê›íË")]
    [SerializeField] private float entranceSlideDistance = 3.5f;
    [SerializeField] private float entranceSlideDuration = 0.35f;
    [SerializeField] private Ease entranceSlideEase = Ease.OutCubic;
    [SerializeField] private float entranceFlashDuration = 0.15f;
    [SerializeField] private Color entranceFlashColor = new Color(1f, 1f, 0.8f, 1f);

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

        unit.transform.DOKill();

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            sr.DOKill();
        }

        SetEnemyVisible(unit, true);
        unit.SetUIActive(true);
        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 1f);
        unit.InitializeTurn();

        PlayEntranceAnimation(unit);
    }

    private void PlayEntranceAnimation(BattleUnit unit)
    {
        if (unit == null) return;

        Transform root = unit.transform;

        Vector3 targetPos = root.position;
        Vector3 startPos = targetPos + Vector3.right * entranceSlideDistance;
        Vector3 overPos = targetPos + Vector3.left * 0.12f;

        root.position = startPos;
        root.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Append(root.DOMove(overPos, entranceSlideDuration * 0.8f).SetEase(Ease.OutCubic));
        seq.Append(root.DOMove(targetPos, entranceSlideDuration * 0.2f).SetEase(Ease.OutQuad));

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            Color original = sr.color;
            sr.color = entranceFlashColor;
            sr.DOColor(original, entranceFlashDuration);
        }
    }

    public void RestoreEnemyColors(BattleUnit unit)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
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

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            sr.enabled = isVisible;
        }

        unit.SetUIActive(isVisible);
    }

    public void SetEnemyAlpha(BattleUnit unit, float alpha)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
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

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
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