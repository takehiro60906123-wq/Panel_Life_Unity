using System;
using System.Collections;
using UnityEngine;

public class EncounterFlowController : MonoBehaviour
{
    private const string PlayerHitTextMarker = "<playerhit>";
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
    private Action<BattleUnit> prepareEnemyForDeferredEntrance;
    private Action<BattleUnit> revealWaitingEnemy;
    private Action hideAllUpcomingEnemies;
    private Action<float, float> shiftUpcomingEnemies;
    private Action<Animator, bool> setMoveAnimation;

    // ============================================
    // 商店コントローラー参照
    // ============================================
    private ShopController shopController;
    private RewardDropController rewardDropController;

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
        Action<BattleUnit> prepareEnemyForDeferredEntrance,
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
        this.prepareEnemyForDeferredEntrance = prepareEnemyForDeferredEntrance;
        this.revealWaitingEnemy = revealWaitingEnemy;
        this.hideAllUpcomingEnemies = hideAllUpcomingEnemies;
        this.shiftUpcomingEnemies = shiftUpcomingEnemies;
        this.setMoveAnimation = setMoveAnimation;
    }

    /// <summary>
    /// ShopController を後から設定する。
    /// BattleBootstrapper や PanelBattleManager から呼ぶ。
    /// </summary>
    public void SetShopController(ShopController controller)
    {
        shopController = controller;
    }

    public void SetRewardDropController(RewardDropController controller)
    {
        rewardDropController = controller;
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

    public void SetupStage(bool deferInitialEnemyEntrance = false)
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
            if (deferInitialEnemyEntrance)
            {
                prepareEnemyForDeferredEntrance?.Invoke(enemyUnit);
                enemyUnit.InitializeTurn();
            }
            else
            {
                activateEnemyAsCurrent?.Invoke(enemyUnit);
            }

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
                () => battleTurnController.EnemyTurnRoutine(
                    getEnemyUnit != null ? getEnemyUnit() : null,
                    playerUnit,
                    hitEffectPrefab,
                    RequestDamageText,
                    RequestOneShotEffect,
                    RequestPlayerDefeated,
                    RequestBoardInteractable),
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

        if (currentEncounter == EncounterType.Empty
            || currentEncounter == EncounterType.Treasure
            || currentEncounter == EncounterType.Shop)
        {
            if (getIsEnemyDefeatedThisTurn == null || !getIsEnemyDefeatedThisTurn())
            {
                AdvanceEmptyTurn();
            }

            setIsEnemyDefeatedThisTurn?.Invoke(false);
            yield break;
        }

        // === 状態異常: 敵腐食のターン消費（フォールバック） ===
        // プレイヤーターン終了時に減らす。
        TickFallbackEnemyPassiveEffectsOnPlayerTurnEnd(getEnemyUnit != null ? getEnemyUnit() : null);

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit == null)
        {
            setIsEnemySpawning?.Invoke(false);

            if (stageFlowController != null
                && (stageFlowController.IsStageComplete() || stageFlowController.HasConfiguredFinalBattleBeenCleared()))
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

        yield return StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        RequestBoardInteractable(false);

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit == null || enemyUnit.IsDead())
        {
            RequestBoardInteractable(true);
            yield break;
        }

        // === 状態異常: 敵金縛りチェック（フォールバック） ===
        StatusEffectHolder fbEnemyStatus = enemyUnit.StatusEffects;
        if (fbEnemyStatus != null && fbEnemyStatus.HasEffect(StatusEffectType.Paralysis))
        {
            RequestDamageText("金縛り！", enemyUnit.transform.position + Vector3.up * 1.5f, new Color(0.8f, 0.4f, 1f));
            fbEnemyStatus.ConsumeEffectTurn(StatusEffectType.Paralysis);
            fbEnemyStatus.ConsumeEffectTurn(StatusEffectType.Slow);
            yield return new WaitForSeconds(0.25f);
            TickFallbackPlayerPassiveEffectsOnEnemyTurnEnd(playerUnit);
            RequestBoardInteractable(true);
            yield break;
        }

        bool skipCooldownBySlow = ShouldSkipFallbackEnemyCooldownProgressThisTurn(enemyUnit);
        if (!skipCooldownBySlow)
        {
            enemyUnit.TickCooldown();
        }

        if (enemyUnit.IsReadyToAttack())
        {
            enemyUnit.PlayAttackAnimation();

            yield return new WaitForSeconds(0.2f);

            bool isEvasion = UnityEngine.Random.Range(0, 100) < 15;
            Vector3 pos = playerUnit != null ? playerUnit.transform.position : Vector3.zero;

            if (isEvasion)
            {
                RequestDamageText("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int damage = Mathf.Max(1, enemyUnit.attackPower);

                // === 状態異常: プレイヤー腐食チェック（フォールバック） ===
                if (playerUnit != null && playerUnit.StatusEffects != null
                    && playerUnit.StatusEffects.HasEffect(StatusEffectType.Corrosion))
                {
                    int bonus = playerUnit.StatusEffects.GetEffectPotency(StatusEffectType.Corrosion);
                    if (bonus > 0)
                    {
                        damage += bonus;
                        RequestDamageText("腐食！", playerUnit.transform.position + Vector3.up * 2.0f, new Color(0.6f, 1f, 0.2f));
                    }
                }

                if (playerUnit != null)
                {
                    playerUnit.TakeDamage(damage);
                    ScreenShakeController.TryShake(ShakePreset.PlayerHit);

                    // === 状態異常: プレイヤー被弾解除（フォールバック） ===
                    if (playerUnit.StatusEffects != null)
                    {
                        playerUnit.StatusEffects.OnDamageReceived();
                    }
                }

                if (hitEffectPrefab != null)
                {
                    RequestOneShotEffect(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);
                }

                RequestDamageText(PlayerHitTextMarker + damage.ToString(), pos + Vector3.up * 1.15f, Color.red);

                if (playerUnit != null && playerUnit.IsDead())
                {
                    RequestPlayerDefeated();
                    yield break;
                }
            }

            enemyUnit.ResetCooldown();
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        TickFallbackPlayerPassiveEffectsOnEnemyTurnEnd(playerUnit);
        RequestBoardInteractable(true);
    }

    /// <summary>
    /// フォールバック用：敵パッシブ状態異常のターン消費。
    /// プレイヤーターン終了時に呼ぶ。
    /// </summary>
    private void TickFallbackEnemyPassiveEffectsOnPlayerTurnEnd(BattleUnit enemyUnit)
    {
        if (enemyUnit == null) return;
        StatusEffectHolder holder = enemyUnit.StatusEffects;
        if (holder == null) return;

        holder.ConsumeEffectTurn(StatusEffectType.Corrosion);
    }

    /// <summary>
    /// フォールバック用：プレイヤーパッシブ状態異常のターン消費。
    /// 敵ターン終了時に呼ぶ。
    /// </summary>
    private void TickFallbackPlayerPassiveEffectsOnEnemyTurnEnd(BattleUnit playerUnit)
    {
        if (playerUnit == null) return;
        StatusEffectHolder holder = playerUnit.StatusEffects;
        if (holder == null) return;

        holder.ConsumeEffectTurn(StatusEffectType.Corrosion);
    }


    /// <summary>
    /// フォールバック用：敵の駆動遅延。
    /// その敵ターンの cooldown 進行を止める。
    /// </summary>
    private bool ShouldSkipFallbackEnemyCooldownProgressThisTurn(BattleUnit enemyUnit)
    {
        if (enemyUnit == null) return false;

        StatusEffectHolder holder = enemyUnit.StatusEffects;
        if (holder == null || !holder.HasEffect(StatusEffectType.Slow)) return false;

        if (enemyUnit.currentCooldown > 0)
        {
            RequestDamageText("駆動遅延！", enemyUnit.transform.position + Vector3.up * 1.5f, new Color(0.45f, 0.9f, 1f));
        }

        holder.ConsumeEffectTurn(StatusEffectType.Slow);
        return true;
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
                () => EnemyRespawnRoutine(encounterType));

            PublishEncounterState(encounterType, remainingSteps);
            return;
        }

        EncounterType currentEncounter = getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Empty;
        int steps = getRemainingSteps != null ? getRemainingSteps() : 0;

        if (currentEncounter == EncounterType.Empty
            || currentEncounter == EncounterType.Treasure
            || currentEncounter == EncounterType.Shop)
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
                StartCoroutine(EnemyRespawnRoutine(currentEncounter));
            }
        }
    }

    public IEnumerator EnemyRespawnRoutine()
    {
        EncounterType previousEncounter = getCurrentEncounter != null ? getCurrentEncounter() : EncounterType.Enemy;
        yield return StartCoroutine(EnemyRespawnRoutine(previousEncounter));
    }

    public IEnumerator EnemyRespawnRoutine(EncounterType previousEncounter)
    {
        RequestBoardInteractable(false);
        setIsEnemySpawning?.Invoke(true);

        yield return new WaitForSeconds(0.05f);

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit != null && enemyUnit.IsDead())
        {
            Destroy(enemyUnit.gameObject);
            setEnemyUnit?.Invoke(null);
        }

        // 報酬パネル表示は BattleDamageResolver 側で処理する。
        // ここで二重に待つと、撃破後に進行停止しやすいので呼ばない。

        if (stageFlowController == null)
        {
            setIsEnemySpawning?.Invoke(false);
            RequestBoardInteractable(false);
            Debug.LogError("StageFlowController が取得できません。");
            yield break;
        }

        if (stageFlowController.HasConfiguredFinalBattleBeenCleared())
        {
            PublishEncounterState(EncounterType.Enemy, 0);
            setIsEnemySpawning?.Invoke(false);
            RequestBoardInteractable(false);
            RequestStageClear();
            yield break;
        }

        StageFlowController.NextEncounterPlan plan = stageFlowController.DecideNextEncounter(previousEncounter);

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
        else if (plan.encounterType == EncounterType.Treasure)
        {
            PublishEncounterState(EncounterType.Treasure, plan.steps);
            yield return StartCoroutine(EnterSafeRoomRoutine("宝箱の部屋だ！", Color.yellow));
        }
        // ============================================
        // 商店部屋
        // ============================================
        else if (plan.encounterType == EncounterType.Shop)
        {
            PublishEncounterState(EncounterType.Shop, plan.steps);
            yield return StartCoroutine(EnterShopRoutine());
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

        if (travelForwardRoutine != null)
        {
            yield return StartCoroutine(travelForwardRoutine());
        }

        yield return new WaitForSeconds(0.05f);

        setEnemyUnit?.Invoke(nextEnemy);

        if (nextEnemy != null)
        {
            activateEnemyAsCurrent?.Invoke(nextEnemy);

            // ここは1個だけで十分
            RequestDamageText("敵発見", nextEnemy.transform.position + Vector3.up * 1.6f, Color.red);

            // 次の待機敵はすぐ補充
            spawnNextEnemy?.Invoke();
        }
    }

    private IEnumerator EnterSafeRoomRoutine(string popupText, Color popupColor)
    {
        hideAllUpcomingEnemies?.Invoke();
        RequestDungeonMist(false, false);

        RequestDamageText(popupText, playerUnit.transform.position + Vector3.up * 1.5f, popupColor);
        shiftUpcomingEnemies?.Invoke(waitOffset.x, roomTravelDuration);

        if (travelForwardRoutine != null)
        {
            yield return StartCoroutine(travelForwardRoutine());
        }
    }

    // ============================================
    // 商店部屋に入る
    // ============================================
    private IEnumerator EnterShopRoutine()
    {
        // 安全部屋と同じ移動演出
        yield return StartCoroutine(EnterSafeRoomRoutine("商店を見つけた", new Color(1f, 0.85f, 0.3f)));

        yield return new WaitForSeconds(0.3f);

        // 商店UIを開く
        if (shopController != null)
        {
            bool shopOpen = true;
            shopController.OpenShop(() =>
            {
                shopOpen = false;
            });

            // 商店が閉じるまで待機（盤面操作はロック状態のまま）
            while (shopOpen)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            // ShopController が未設定の場合はスキップ
            Debug.LogWarning("ShopController が未設定です。商店をスキップします。");
            RequestDamageText("（商店準備中…）", playerUnit.transform.position + Vector3.up * 1.5f, Color.gray);
            yield return new WaitForSeconds(0.5f);
        }
    }
}