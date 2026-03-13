using UnityEngine;
using UnityEngine.UI;

public class BattleBootstrapper : MonoBehaviour
{
    public void EnsureDependencies(PanelBattleManager manager)
    {
        if (manager == null)
        {
            return;
        }

        manager.battleEventHub = ResolveOrAdd(manager.battleEventHub);
        manager.SetEffectPoolManager(ResolveOrAdd(manager.GetEffectPoolManager()));

        if (manager.battleUIController == null)
        {
            manager.battleUIController = GetComponent<BattleUIController>();
        }

        if (manager.battleUIController == null)
        {
            manager.battleUIController = FindObjectOfType<BattleUIController>();
        }

        if (manager.battleUIController == null)
        {
            Debug.LogWarning("BattleUIController が見つかりません。UI表示は更新されません。");
        }

        manager.dungeonMistController = ResolveOrAdd(manager.dungeonMistController);
        manager.enemyPresentationController = ResolveOrAdd(manager.enemyPresentationController);
        manager.roomTravelController = ResolveOrAdd(manager.roomTravelController);
        manager.panelBoardController = ResolveOrAdd(manager.panelBoardController);
        manager.battleEffectController = ResolveOrAdd(manager.battleEffectController);
        manager.stageFlowController = ResolveOrAdd(manager.stageFlowController);
        manager.battleTurnController = ResolveOrAdd(manager.battleTurnController);
        manager.panelActionController = ResolveOrAdd(manager.panelActionController);
        manager.encounterFlowController = ResolveOrAdd(manager.encounterFlowController);
        manager.battleDamageResolver = ResolveOrAdd(manager.battleDamageResolver);
    }

    public bool Initialize(PanelBattleManager manager)
    {
        if (manager == null)
        {
            return false;
        }

        if (manager.boardParent == null)
        {
            Debug.LogError("boardParent が未設定です。");
            return false;
        }

        if (manager.panelPrefab == null)
        {
            Debug.LogError("panelPrefab が未設定です。");
            return false;
        }

        if (manager.battleEventHub == null)
        {
            Debug.LogError("BattleEventHub が取得できません。");
            return false;
        }

        CanvasGroup boardCanvasGroup = manager.boardParent.GetComponent<CanvasGroup>();
        if (boardCanvasGroup == null)
        {
            boardCanvasGroup = manager.boardParent.gameObject.AddComponent<CanvasGroup>();
        }
        manager.SetBoardCanvasGroup(boardCanvasGroup);

        if (manager.dungeonMistController != null)
        {
            manager.dungeonMistController.Configure(
                manager.dungeonMistRoot,
                manager.battleMistAlpha,
                manager.mistFadeDuration);
            manager.dungeonMistController.ApplyBattleState(true, true);
        }

        if (manager.enemyPresentationController != null)
        {
            manager.enemyPresentationController.Configure(
                manager.enemyRevealDuration,
                manager.roomTravelEase);
        }

        if (manager.roomTravelController != null)
        {
            manager.roomTravelController.Configure(
                manager.roomTravelDuration,
                manager.roomTravelEase);
        }

        if (manager.battleEffectController != null)
        {
            manager.battleEffectController.Configure(manager.GetEffectPoolManager());
        }

        if (manager.stageFlowController != null)
        {
            manager.stageFlowController.Configure(
                manager.battlePosition,
                manager.waitOffset,
                manager.maxFloors,
                manager.maxVisibleEnemies,
                manager.enemyPrefabs);
        }
        if (manager.stageConfig != null)
        {
            manager.stageFlowController.SetStageConfig(manager.stageConfig);
        }

        if (manager.battleTurnController != null)
        {
            manager.battleTurnController.Configure(0.5f, 0.2f, 0.5f, 0.25f);
            manager.battleTurnController.OnPanelCorruptRequested = (count) =>
            {
                if (manager.panelBoardController != null)
                {
                    manager.panelBoardController.ForceSetRandomPanels(PanelType.LvUp, count);
                }
            };
        }

        if (manager.panelBoardController == null)
        {
            Debug.LogError("PanelBoardController が取得できません。");
            return false;
        }

        if (manager.panelActionController == null)
        {
            Debug.LogError("PanelActionController が取得できません。");
            return false;
        }

        if (manager.encounterFlowController == null)
        {
            Debug.LogError("EncounterFlowController が取得できません。");
            return false;
        }

        if (manager.battleDamageResolver == null)
        {
            Debug.LogError("BattleDamageResolver が取得できません。");
            return false;
        }

        manager.battleDamageResolver.Initialize(
            manager.battleEventHub,
            manager.playerUnit,
            () => manager.enemyUnit,
            () => manager.IsEnemySpawning,
            value => manager.SetEnemyDefeatedThisTurn(value),
            manager.hitEffectPrefab,
            manager.levelUpEffectPrefab,
            () => manager.encounterFlowController != null ? manager.encounterFlowController.EnemyRespawnRoutine() : null);

        manager.panelActionController.Initialize(
    manager.battleEventHub,
    manager.panelBoardController,
    manager.playerUnit,
    manager.PlayerCombatController,
    () => manager.enemyUnit,
    manager.EndPlayerTurn,
    manager.ApplyPlayerDamageModifiers,
    manager.HandleCollectedPanelItems);

        manager.encounterFlowController.Initialize(
            manager.battleEventHub,
            manager.stageFlowController,
            manager.battleTurnController,
            manager.playerUnit,
            manager.hitEffectPrefab,
            manager.battlePosition,
            manager.waitOffset,
            manager.roomTravelDuration,
            manager.enemyRevealDuration,
            () => manager.enemyUnit,
            value => manager.enemyUnit = value,
            () => manager.currentEncounter,
            () => manager.remainingSteps,
            () => manager.IsEnemySpawning,
            value => manager.SetEnemySpawning(value),
            () => manager.IsEnemyDefeatedThisTurn,
            value => manager.SetEnemyDefeatedThisTurn(value),
            manager.TravelForward,
            manager.SpawnNextEnemy,
            () => manager.enemyPresentationController.RefreshUpcomingEnemyStandbyVisuals(
                manager.stageFlowController != null ? manager.stageFlowController.GetUpcomingEnemies() : null),
            manager.enemyPresentationController.ActivateEnemyAsCurrent,
            manager.enemyPresentationController.RevealWaitingEnemy,
            () => manager.enemyPresentationController.HideAllUpcomingEnemies(
                manager.stageFlowController != null ? manager.stageFlowController.GetUpcomingEnemies() : null),
            (deltaX, duration) => manager.enemyPresentationController.ShiftUpcomingEnemies(
                manager.stageFlowController != null ? manager.stageFlowController.GetUpcomingEnemies() : null,
                deltaX,
                duration),
            manager.enemyPresentationController.SetMoveAnimation);

        bool boardInitialized = manager.panelBoardController.Initialize(
            manager.panelPrefab,
            manager.boardParent,
            manager.panelSettings,
            manager.BoardRows,
            manager.BoardCols,
            manager.panelActionController.OnPanelClicked);

        if (!boardInitialized)
        {
            return false;
        }

        manager.SetBoardInteractable(true);
        manager.UpdateCoinUI();
        manager.UpdateEncounterUI();
        manager.panelBoardController.GenerateBoard();
        manager.encounterFlowController.SetupStage();
        manager.PrepareItemPanelForCurrentBattle();
        return true;
    }

    private T ResolveOrAdd<T>(T current) where T : Component
    {
        if (current != null)
        {
            return current;
        }

        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}