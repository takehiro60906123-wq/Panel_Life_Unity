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

            default:
                yield return StartCoroutine(FinishTurn());
                break;
        }
    }

    private IEnumerator PlayMeleeAttack(int count)
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

            yield return new WaitForSeconds(0.12f);
            battleEventHub?.RaiseEnemyDamageRequested(1);
            yield return new WaitForSeconds(0.08f);
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

    private IEnumerator FinishTurn()
    {
        if (endPlayerTurnRoutine != null)
        {
            yield return StartCoroutine(endPlayerTurnRoutine());
        }

        isProcessing = false;
    }
}