using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyPresentationController : MonoBehaviour
{
    private float enemyRevealDuration = 0.2f;
    private Ease roomTravelEase = Ease.Linear;

    [Header("登場演出設定")]
    [SerializeField] private float entranceDropHeight = 0.5f;
    [SerializeField] private float entranceBounceDuration = 0.4f;
    [SerializeField] private float entranceScaleStart = 0.3f;
    [SerializeField] private float entranceFlashDuration = 0.2f;
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

        SetEnemyVisible(unit, true);
        unit.SetUIActive(true);
        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 1f);
        unit.InitializeTurn();

        // --- 登場演出 ---
        PlayEntranceAnimation(unit);
    }

    /// <summary>
    /// 敵登場演出：上から落下＋バウンス＋スケール膨張＋白フラッシュ
    /// </summary>
    private void PlayEntranceAnimation(BattleUnit unit)
    {
        if (unit == null) return;

        Transform root = unit.transform;

        // 開始位置（少し上）
        Vector3 targetPos = root.position;
        root.position = targetPos + Vector3.up * entranceDropHeight;

        // 開始スケール（小さい）
        root.localScale = Vector3.one * entranceScaleStart;

        // ドロップ＋バウンス
        root.DOMove(targetPos, entranceBounceDuration).SetEase(Ease.OutBounce);

        // スケール復帰（弾ける感じ）
        root.DOScale(Vector3.one, entranceBounceDuration * 0.8f).SetEase(Ease.OutBack);

        // 白フラッシュ
        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            Color original = sr.color;
            sr.color = entranceFlashColor;
            sr.DOColor(original, entranceFlashDuration).SetDelay(entranceBounceDuration * 0.5f);
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