using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PanelActionController : MonoBehaviour
{
    private BattleEventHub battleEventHub;
    private PanelBoardController panelBoardController;
    private BattleUnit playerUnit;
    private Func<BattleUnit> getEnemyUnit;
    private Func<IEnumerator> endPlayerTurnRoutine;

    private bool isProcessing;
    [Header("近接DoTween演出")]
    [SerializeField] private string playerMeleeVisualRootName = "PlayerVisual";
    [SerializeField] private float meleeAttackDirectionX = 1f;
    [SerializeField] private float meleeWindupDistance = 0.08f;
    [SerializeField] private float meleeLungeDistance = 0.26f;
    [SerializeField] private float meleeHopY = 0.05f;
    [SerializeField] private float meleeWindupDuration = 0.05f;
    [SerializeField] private float meleeLungeDuration = 0.08f;
    [SerializeField] private float meleeRecoverDuration = 0.10f;
    [SerializeField] private float meleeHitScale = 1.10f;
    [SerializeField] private float meleePunchRotation = 10f;
    [SerializeField] private float meleeComboInterval = 0.03f;

    public void Initialize(
        BattleEventHub battleEventHub,
        PanelBoardController panelBoardController,
        BattleUnit playerUnit,
        Func<BattleUnit> getEnemyUnit,
        Func<IEnumerator> endPlayerTurnRoutine)
    {
        this.battleEventHub = battleEventHub;
        this.panelBoardController = panelBoardController;
        this.playerUnit = playerUnit;
        this.getEnemyUnit = getEnemyUnit;
        this.endPlayerTurnRoutine = endPlayerTurnRoutine;
        isProcessing = false;
    }

    public void OnPanelClicked(int row, int col)
    {
        if (isProcessing) return;
        if (panelBoardController == null) return;

        PanelType clickedType = panelBoardController.GetPanelType(row, col);
        if (clickedType == PanelType.None) return;

        isProcessing = true;
        battleEventHub?.RaiseBoardInteractableRequested(false);

        List<Vector2Int> chain = panelBoardController.FindChain(row, col, clickedType);

        if (clickedType == PanelType.Sword)
        {
            chain.AddRange(panelBoardController.GetAdjacentLevelPanels(chain));
        }

        StartCoroutine(CollectEnergyAndAttack(clickedType, chain));
        panelBoardController.ClearChainPanels(chain);

        DOVirtual.DelayedCall(0.25f, () =>
        {
            if (panelBoardController != null)
            {
                panelBoardController.DropAndFillPanels();
            }
        });
    }

    private IEnumerator CollectEnergyAndAttack(PanelType type, List<Vector2Int> chain)
    {
        if (playerUnit == null || panelBoardController == null)
        {
            isProcessing = false;
            yield break;
        }

        Vector3 targetPos = playerUnit.transform.position + Vector3.up * 0.5f;
        float flyDuration = 0.5f;
        float delay = 0f;

        foreach (Vector2Int pos in chain)
        {
            Vector3 panelWorldPos = panelBoardController.GetPanelWorldPosition(pos.x, pos.y);
            battleEventHub?.RaiseEnergyOrbRequested(panelWorldPos, targetPos, flyDuration, delay);
            delay += 0.04f;
        }

        yield return new WaitForSeconds(flyDuration + delay + 0.2f);
        yield return ExecutePanelAction(type, chain.Count);
    }

    private IEnumerator ExecutePanelAction(PanelType type, int chainCount)
    {
        switch (type)
        {
            case PanelType.Sword:
                yield return StartCoroutine(PlayMeleeAttack(chainCount));
                break;

            case PanelType.Magic:
                yield return StartCoroutine(PlayMagicAttack(chainCount));
                break;

            case PanelType.Heal:
                if (playerUnit != null) playerUnit.Heal(chainCount);
                yield return StartCoroutine(FinishTurn());
                break;

            case PanelType.Coin:
                battleEventHub?.RaiseCoinsGained(chainCount * 10);
                yield return StartCoroutine(FinishTurn());
                break;

            case PanelType.LvUp:
                yield return StartCoroutine(PlayEnemyLevelUp(chainCount));
                break;

            default:
                yield return StartCoroutine(FinishTurn());
                break;
        }
    }

    private Transform ResolvePlayerMeleeVisualRoot()
    {
        if (playerUnit == null) return null;

        Transform t = playerUnit.transform.Find(playerMeleeVisualRootName);
        if (t != null) return t;

        t = playerUnit.transform.Find("VisualRoot");
        if (t != null) return t;

        t = playerUnit.transform.Find("UnitRoot");
        if (t != null) return t;

        for (int i = 0; i < playerUnit.transform.childCount; i++)
        {
            Transform child = playerUnit.transform.GetChild(i);
            if (child.name.Contains("Canvas")) continue;

            if (child.GetComponentInChildren<SpriteRenderer>(true) != null ||
                child.GetComponentInChildren<Animator>(true) != null)
            {
                return child;
            }
        }

        return playerUnit.transform;
    }

    private IEnumerator PlaySingleMeleeHitTween()
    {
        Transform visualRoot = ResolvePlayerMeleeVisualRoot();

        if (visualRoot == null)
        {
            yield return new WaitForSeconds(0.18f);
            yield break;
        }

        visualRoot.DOKill();

        Vector3 baseLocalPos = visualRoot.localPosition;
        Vector3 baseLocalScale = visualRoot.localScale;
        Quaternion baseLocalRotation = visualRoot.localRotation;

        Vector3 windupPos = baseLocalPos + new Vector3(-meleeAttackDirectionX * meleeWindupDistance, -meleeHopY * 0.35f, 0f);
        Vector3 strikePos = baseLocalPos + new Vector3(meleeAttackDirectionX * meleeLungeDistance, meleeHopY, 0f);

        float totalDuration = meleeWindupDuration + meleeLungeDuration + meleeRecoverDuration;
        float hitMoment = meleeWindupDuration + (meleeLungeDuration * 0.75f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(windupPos, meleeWindupDuration).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * 0.94f, meleeWindupDuration).SetEase(Ease.OutQuad));

        seq.Append(visualRoot.DOLocalMove(strikePos, meleeLungeDuration).SetEase(Ease.OutExpo));
        seq.Join(visualRoot.DOScale(baseLocalScale * meleeHitScale, meleeLungeDuration).SetEase(Ease.OutBack));
        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, -meleePunchRotation * Mathf.Sign(meleeAttackDirectionX)),
                meleeLungeDuration + 0.03f,
                4,
                0.8f));

        seq.Append(visualRoot.DOLocalMove(baseLocalPos, meleeRecoverDuration).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale, meleeRecoverDuration).SetEase(Ease.OutQuad));

        yield return new WaitForSeconds(hitMoment);

        battleEventHub?.RaiseEnemyDamageRequested(1);

        yield return new WaitForSeconds(Mathf.Max(0f, totalDuration - hitMoment));

        visualRoot.localPosition = baseLocalPos;
        visualRoot.localScale = baseLocalScale;
        visualRoot.localRotation = baseLocalRotation;
    }

    private IEnumerator PlayMeleeAttack(int count)
    {
        count = Mathf.Max(1, count);

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        if (enemyUnit == null || enemyUnit.IsDead())
        {
            yield return StartCoroutine(PlaySingleMeleeHitTween());
            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            yield return StartCoroutine(PlaySingleMeleeHitTween());

            if (i < count - 1)
            {
                yield return new WaitForSeconds(meleeComboInterval);
            }
        }

        BattleUnitView playerView = playerUnit != null ? playerUnit.GetComponent<BattleUnitView>() : null;
        if (playerView != null)
        {
            playerView.PlayIdle();
        }

        yield return StartCoroutine(FinishTurn());
    }

    private IEnumerator PlayMagicAttack(int count)
    {
        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        if (enemyUnit == null || enemyUnit.IsDead())
        {
            if (playerUnit != null && playerUnit.animator != null)
            {
                playerUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.4f);
            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit != null && playerUnit.animator != null)
            {
                playerUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.08f);
            battleEventHub?.RaiseMagicBulletRequested();
            yield return new WaitForSeconds(0.12f);
        }

        yield return StartCoroutine(FinishTurn());
    }

    // =============================================================
    // LvUp パネル → 敵レベルアップ
    // =============================================================

    private IEnumerator PlayEnemyLevelUp(int chainCount)
    {
        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        // 敵がいない or 死亡中 → 効果なしでターン終了
        if (enemyUnit == null || enemyUnit.IsDead())
        {
            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        // 敵レベルアップ適用
        BattleUnit.EnemyLevelUpResult result = enemyUnit.EnemyLevelUp(chainCount);

        // --- 演出 ---
        Vector3 enemyPos = enemyUnit.transform.position;

        // 警告テキスト（赤系）
        battleEventHub?.RaiseDamageTextRequested(
            "LEVEL UP!",
            enemyPos + Vector3.up * 2.0f,
            new Color(1f, 0.3f, 0.3f));

        yield return new WaitForSeconds(0.35f);

        // ステータス変化テキスト（オレンジ系）
        string statText = $"HP+{result.hpGained}  EXP+{result.expBonusGained}";
        battleEventHub?.RaiseDamageTextRequested(
            statText,
            enemyPos + Vector3.up * 1.4f,
            new Color(1f, 0.7f, 0.3f));

        yield return new WaitForSeconds(0.45f);

        // ターン消費して終了
        yield return StartCoroutine(FinishTurn());
    }

    // =============================================================

    private IEnumerator FinishTurn()
    {
        if (endPlayerTurnRoutine != null)
        {
            yield return StartCoroutine(endPlayerTurnRoutine());
        }

        isProcessing = false;
    }
}