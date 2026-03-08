using System;
using System.Collections;
using UnityEngine;

public class EncounterFlowController : MonoBehaviour
{
    private BattleEventHub battleEventHub;
    private StageFlowController stageFlowController;
    private BattleTurnController battleTurnController;
    private BattleUnit playerUnit;
    private GameObject hitEffectPrefab;
    private Transform battlePosition;
    private Vector3 waitOffset;
    private float roomTravelDuration;
    private float enemyRevealDuration;

    private Func<BattleUnit> getEnemyUnit;
    private Action<BattleUnit> setEnemyUnit;

    private Func<EncounterType> getCurrentEncounter;
    private Func<int> getRemainingSteps;

    private Func<bool> getIsEnemySpawning;
    private Action<bool> setIsEnemySpawning;
    private Func<bool> getIsEnemyDefeatedThisTurn;
    private Action<bool> setIsEnemyDefeatedThisTurn;

    private Func<IEnumerator> travelForwardRoutine;
    private Action spawnNextEnemy;
    private Action refreshUpcomingEnemyStandbyVisuals;
    private Action<BattleUnit> activateEnemyAsCurrent;
    private Action<BattleUnit> revealWaitingEnemy;
    private Action hideAllUpcomingEnemies;
    private Action<float, float> shiftUpcomingEnemies;
    private Action<Animator, bool> setMoveAnimation;

    public void Initialize(
        BattleEventHub battleEventHub,
        StageFlowController stageFlowController,
        BattleTurnController battleTurnController,
        BattleUnit playerUnit,
        GameObject hitEffectPrefab,
        Transform battlePosition,
        Vector3 waitOffset,
        float roomTravelDuration,
        float enemyRevealDuration,
        Func<BattleUnit> getEnemyUnit,
        Action<BattleUnit> setEnemyUnit,
        Func<EncounterType> getCurrentEncounter,
        Func<int> getRemainingSteps,
        Func<bool> getIsEnemySpawning,
        Action<bool> setIsEnemySpawning,
        Func<bool> getIsEnemyDefeatedThisTurn,
        Action<bool> setIsEnemyDefeatedThisTurn,
        Func<IEnumerator> travelForwardRoutine,
        Action spawnNextEnemy,
        Action refreshUpcomingEnemyStandbyVisuals,
        Action<BattleUnit> activateEnemyAsCurrent,
        Action<BattleUnit> revealWaitingEnemy,
        Action hideAllUpcomingEnemies,
        Action<float, float> shiftUpcomingEnemies,
        Action<Animator, bool> setMoveAnimation)
    {
        this.battleEventHub = battleEventHub;
        this.stageFlowController = stageFlowController;
        this.battleTurnController = battleTurnController;
        this.playerUnit = playerUnit;
        this.hitEffectPrefab = hitEffectPrefab;
        this.battlePosition = battlePosition;
        this.waitOffset = waitOffset;
        this.roomTravelDuration = roomTravelDuration;
        this.enemyRevealDuration = enemyRevealDuration;
        this.getEnemyUnit = getEnemyUnit;
        this.setEnemyUnit = setEnemyUnit;
        this.getCurrentEncounter = getCurrentEncounter;
        this.getRemainingSteps = getRemainingSteps;
        this.getIsEnemySpawning = getIsEnemySpawning;
        this.setIsEnemySpawning = setIsEnemySpawning;
        this.getIsEnemyDefeatedThisTurn = getIsEnemyDefeatedThisTurn;
        this.setIsEnemyDefeatedThisTurn = setIsEnemyDefeatedThisTurn;
        this.travelForwardRoutine = travelForwardRoutine;
        this.spawnNextEnemy = spawnNextEnemy;
        this.refreshUpcomingEnemyStandbyVisuals = refreshUpcomingEnemyStandbyVisuals;
        this.activateEnemyAsCurrent = activateEnemyAsCurrent;
        this.revealWaitingEnemy = revealWaitingEnemy;
        this.hideAllUpcomingEnemies = hideAllUpcomingEnemies;
        this.shiftUpcomingEnemies = shiftUpcomingEnemies;
        this.setMoveAnimation = setMoveAnimation;
    }

    private void PublishEncounterState(EncounterType encounterType, int steps)
    {
        battleEventHub?.RaiseEncounterStateChanged(encounterType, steps);
    }

    private void RequestBoardInteractable(bool isInteractable)
    {
        battleEventHub?.RaiseBoardInteractableRequested(isInteractable);
    }

    private void RequestDungeonMist(bool isBattle, bool immediate)
    {
        battleEventHub?.RaiseDungeonMistRequested(isBattle, immediate);
    }

    private void RequestDamageText(string text, Vector3 position, Color color)
    {
        battleEventHub?.RaiseDamageTextRequested(text, position, color);
    }

    private void RequestOneShotEffect(GameObject prefab, Vector3 position, float returnDelay)
    {
        battleEventHub?.RaiseOneShotEffectRequested(prefab, position, returnDelay);
    }

    private void RequestStageClear()
    {
        battleEventHub?.RaiseStageClearRequested();
    }

    private void RequestPlayerDefeated()
    {
        battleEventHub?.RaisePlayerDefeatedRequested();
    }

    public void SetupStage()
    {
        if (stageFlowController == null)
        {
            Debug.LogError("StageFlowController が取得できません。");
            setEnemyUnit?.Invoke(null);
            RequestBoardInteractable(false);
            return;
        }

        if (!stageFlowController.SetupInitialStage(out BattleUnit initialEnemy, out string errorMessage))
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                if (errorMessage.Contains("0 以下"))
                {
                    Debug.LogWarning(errorMessage);
                }
                else
                {
                    Debug.LogError(errorMessage);
                }
            }

            setEnemyUnit?.Invoke(null);
            RequestBoardInteractable(false);
            return;
        }

        setEnemyUnit?.Invoke(initialEnemy);
        refreshUpcomingEnemyStandbyVisuals?.Invoke();

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit != null)
        {
            enemyUnit.transform.localScale = Vector3.one;
            activateEnemyAsCurrent?.Invoke(enemyUnit);
            PublishEncounterState(EncounterType.Enemy, 0);
            RequestDungeonMist(true, false);
        }
        else
        {
            Debug.LogWarning("生成できる敵がいませんでした。");
            RequestBoardInteractable(false);
        }
    }

    public IEnumerator EndPlayerTurnRoutine()
    {
        if (battleTurnController != null)
        {
            yield return battleTurnController.EndPlayerTurnRoutine(
                getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Enemy,
                () => getIsEnemySpawning != null && getIsEnemySpawning(),
                () => getIsEnemyDefeatedThisTurn != null && getIsEnemyDefeatedThisTurn(),
                () => setIsEnemyDefeatedThisTurn?.Invoke(false),
                () => getEnemyUnit != null ? getEnemyUnit() : null,
                RequestStageClear,
                EnemyTurnRoutine,
                AdvanceEmptyTurn,
                RequestBoardInteractable);

            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        while (getIsEnemySpawning != null && getIsEnemySpawning())
        {
            yield return null;
        }

        EncounterType currentEncounter = getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Enemy;

        if (currentEncounter == EncounterType.Empty || currentEncounter == EncounterType.Treasure)
        {
            if (getIsEnemyDefeatedThisTurn == null || !getIsEnemyDefeatedThisTurn())
            {
                AdvanceEmptyTurn();
            }

            setIsEnemyDefeatedThisTurn?.Invoke(false);
            yield break;
        }

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit == null)
        {
            setIsEnemySpawning?.Invoke(false);

            if (stageFlowController != null && stageFlowController.IsStageComplete())
            {
                RequestStageClear();
            }
            else
            {
                Debug.LogError("次の敵取得に失敗しました。ステージは未完了です。");
                RequestBoardInteractable(true);
            }

            yield break;
        }

        if (getIsEnemyDefeatedThisTurn != null && getIsEnemyDefeatedThisTurn())
        {
            setIsEnemyDefeatedThisTurn?.Invoke(false);
            yield break;
        }

        yield return StartCoroutine(EnemyTurnRoutine());
    }

    public IEnumerator EnemyTurnRoutine()
    {
        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        if (battleTurnController != null)
        {
            yield return battleTurnController.EnemyTurnRoutine(
                enemyUnit,
                playerUnit,
                hitEffectPrefab,
                RequestDamageText,
                RequestOneShotEffect,
                RequestPlayerDefeated,
                RequestBoardInteractable);

            yield break;
        }

        if (enemyUnit == null)
        {
            RequestBoardInteractable(true);
            yield break;
        }

        enemyUnit.currentCooldown--;
        enemyUnit.UpdateTurnUI();

        if (enemyUnit.currentCooldown <= 0)
        {
            if (enemyUnit.animator != null)
            {
                enemyUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.2f);

            bool isEvasion = UnityEngine.Random.Range(0, 100) < 15;
            bool isCritical = UnityEngine.Random.Range(0, 100) < 10;
            Vector3 pos = playerUnit.transform.position;

            if (isEvasion)
            {
                RequestDamageText("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int damage = isCritical ? 3 : 1;
                playerUnit.TakeDamage(damage);

                RequestOneShotEffect(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);

                Color textColor = isCritical ? Color.yellow : Color.red;
                string textStr = isCritical ? $"CRITICAL!\n{damage}" : damage.ToString();
                RequestDamageText(textStr, pos + Vector3.up * 1.5f, textColor);

                if (playerUnit.IsDead())
                {
                    RequestPlayerDefeated();
                    yield break;
                }
            }

            enemyUnit.currentCooldown = enemyUnit.attackInterval;
            enemyUnit.UpdateTurnUI();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        RequestBoardInteractable(true);
    }

    public void AdvanceEmptyTurn()
    {
        if (battleTurnController != null)
        {
            int remainingSteps = getRemainingSteps != null ? getRemainingSteps() : 0;
            EncounterType encounterType = getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Empty;

            battleTurnController.AdvanceEmptyTurn(
                encounterType,
                ref remainingSteps,
                playerUnit,
                () => PublishEncounterState(encounterType, remainingSteps),
                RequestDamageText,
                RequestBoardInteractable,
                EnemyRespawnRoutine);

            PublishEncounterState(encounterType, remainingSteps);
            return;
        }

        EncounterType currentEncounter = getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Empty;
        int steps = getRemainingSteps != null ? getRemainingSteps() : 0;

        if (currentEncounter == EncounterType.Empty || currentEncounter == EncounterType.Treasure)
        {
            steps--;
            PublishEncounterState(currentEncounter, steps);

            if (steps > 0)
            {
                RequestDamageText($"あと {steps} ターン", playerUnit.transform.position + Vector3.up * 1.5f, Color.white);
                RequestBoardInteractable(true);
            }
            else
            {
                RequestDamageText("次の部屋へ！", playerUnit.transform.position + Vector3.up * 1.5f, Color.cyan);
                StartCoroutine(EnemyRespawnRoutine());
            }
        }
    }

    public IEnumerator EnemyRespawnRoutine()
    {
        RequestBoardInteractable(false);
        setIsEnemySpawning?.Invoke(true);

        EncounterType prevEncounter = getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Enemy;

        yield return new WaitForSeconds(0.9f);

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit != null && enemyUnit.IsDead())
        {
            Destroy(enemyUnit.gameObject);
            setEnemyUnit?.Invoke(null);
        }

        if (stageFlowController == null)
        {
            setIsEnemySpawning?.Invoke(false);
            RequestBoardInteractable(false);
            Debug.LogError("StageFlowController が取得できません。");
            yield break;
        }

        StageFlowController.NextEncounterPlan plan = stageFlowController.DecideNextEncounter(prevEncounter);

        if (plan.isStageClear)
        {
            PublishEncounterState(EncounterType.Enemy, 0);
            setIsEnemySpawning?.Invoke(false);
            RequestBoardInteractable(false);
            RequestStageClear();
            yield break;
        }

        if (plan.encounterType == EncounterType.Enemy)
        {
            PublishEncounterState(EncounterType.Enemy, 0);
            yield return StartCoroutine(MoveToNextEnemyRoutine());

            if ((getEnemyUnit == null ? null : getEnemyUnit()) == null)
            {
                setIsEnemySpawning?.Invoke(false);
                RequestBoardInteractable(false);
                RequestStageClear();
                yield break;
            }
        }
        else if (plan.encounterType == EncounterType.Empty)
        {
            PublishEncounterState(EncounterType.Empty, plan.steps);
            yield return StartCoroutine(EnterSafeRoomRoutine("平和な部屋だ", Color.white));
        }
        else
        {
            PublishEncounterState(EncounterType.Treasure, plan.steps);
            yield return StartCoroutine(EnterSafeRoomRoutine("宝箱の部屋だ！", Color.yellow));
        }

        setIsEnemySpawning?.Invoke(false);
        RequestBoardInteractable(true);
    }

    private IEnumerator MoveToNextEnemyRoutine()
    {
        hideAllUpcomingEnemies?.Invoke();
        RequestDungeonMist(true, false);

        BattleUnit currentEnemy = getEnemyUnit != null ? getEnemyUnit() : null;
        BattleUnit nextEnemy = null;

        if (stageFlowController != null && currentEnemy != null)
        {
            nextEnemy = stageFlowController.TakeNextEnemyOrSpawn(currentEnemy.transform.position);
        }
        else if (stageFlowController != null)
        {
            nextEnemy = stageFlowController.TakeNextEnemyOrSpawn(
                battlePosition != null ? battlePosition.position : Vector3.zero);
        }

        refreshUpcomingEnemyStandbyVisuals?.Invoke();

        if (nextEnemy != null)
        {
            revealWaitingEnemy?.Invoke(nextEnemy);
            RequestDamageText("敵の気配…", nextEnemy.transform.position + Vector3.up * 1.5f, Color.red);
            yield return new WaitForSeconds(enemyRevealDuration);
        }

        RequestDamageText("先へ走る…", playerUnit.transform.position + Vector3.up * 1.5f, Color.white);
        setMoveAnimation?.Invoke(playerUnit.animator, true);

        if (travelForwardRoutine != null)
        {
            yield return StartCoroutine(travelForwardRoutine());
        }

        setMoveAnimation?.Invoke(playerUnit.animator, false);
        setEnemyUnit?.Invoke(nextEnemy);

        if (nextEnemy != null)
        {
            activateEnemyAsCurrent?.Invoke(nextEnemy);
            RequestDamageText("敵に到達した！", nextEnemy.transform.position + Vector3.up * 1.5f, Color.red);
            spawnNextEnemy?.Invoke();
        }
    }

    private IEnumerator EnterSafeRoomRoutine(string popupText, Color popupColor)
    {
        hideAllUpcomingEnemies?.Invoke();
        RequestDungeonMist(false, false);

        RequestDamageText(popupText, playerUnit.transform.position + Vector3.up * 1.5f, popupColor);
        setMoveAnimation?.Invoke(playerUnit.animator, true);
        shiftUpcomingEnemies?.Invoke(waitOffset.x, roomTravelDuration);

        if (travelForwardRoutine != null)
        {
            yield return StartCoroutine(travelForwardRoutine());
        }

        setMoveAnimation?.Invoke(playerUnit.animator, false);
    }
}