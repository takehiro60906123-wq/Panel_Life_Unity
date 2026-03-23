using System;
using System.Collections;
using UnityEngine;

public class BattleTurnController : MonoBehaviour
{
    private const string PlayerHitTextMarker = "<playerhit>";

    [Header("ターン進行タイミング")]
    [SerializeField] private float endPlayerTurnDelay = 0.5f;
    [SerializeField] private float enemyAttackWindupDelay = 0.2f;
    [SerializeField] private float enemyPostAttackDelay = 0.5f;
    [SerializeField] private float enemyIdleDelay = 0.25f;

    [Header("特殊攻撃設定")]
    [SerializeField] private float multiHitInterval = 0.25f;
    [SerializeField, Range(0, 100)] private int selfBuffHealChance = 50;
    [SerializeField] private int panelCorruptCount = 2;

    public Action<int> OnPanelCorruptRequested;

    private BattleSfxController battleSfxController;

    public void Configure(float endDelay, float attackWindup, float postAttack, float idleDelay)
    {
        endPlayerTurnDelay = endDelay;
        enemyAttackWindupDelay = attackWindup;
        enemyPostAttackDelay = postAttack;
        enemyIdleDelay = idleDelay;
    }

    private static bool IsSafeRoomEncounter(EncounterType encounter)
    {
        return encounter == EncounterType.Empty
            || encounter == EncounterType.Treasure
            || encounter == EncounterType.Shop;
    }

    private void TickPlayerPassiveEffectsOnEnemyTurnEnd(BattleUnit playerUnit)
    {
        if (playerUnit == null) return;
        StatusEffectHolder holder = playerUnit.StatusEffects;
        if (holder == null) return;

        holder.ConsumeEffectTurn(StatusEffectType.Corrosion);
    }

    private bool ShouldSkipEnemyCooldownProgressThisTurn(BattleUnit enemyUnit, Action<string, Vector3, Color> spawnDamageText)
    {
        if (enemyUnit == null) return false;

        StatusEffectHolder holder = enemyUnit.StatusEffects;
        if (holder == null || !holder.HasEffect(StatusEffectType.Slow)) return false;

        bool cooldownWouldProgress = enemyUnit.currentCooldown > 0;
        if (cooldownWouldProgress && spawnDamageText != null)
        {
            Vector3 textPos = enemyUnit.transform.position + Vector3.up * 1.5f;
            spawnDamageText.Invoke("駆動遅延！", textPos, new Color(0.45f, 0.9f, 1f));
        }

        holder.ConsumeEffectTurn(StatusEffectType.Slow);
        return true;
    }

    private int ApplyPlayerCorrosionBonus(int damage, BattleUnit playerUnit, Action<string, Vector3, Color> spawnDamageText)
    {
        if (playerUnit == null) return damage;
        StatusEffectHolder holder = playerUnit.StatusEffects;
        if (holder == null) return damage;
        if (!holder.HasEffect(StatusEffectType.Corrosion)) return damage;

        int bonus = holder.GetEffectPotency(StatusEffectType.Corrosion);
        if (bonus <= 0) return damage;

        damage += bonus;

        if (spawnDamageText != null)
        {
            Vector3 textPos = playerUnit.transform.position + Vector3.up * 2.0f;
            spawnDamageText.Invoke("腐食！", textPos, new Color(0.6f, 1f, 0.2f));
        }

        return damage;
    }

    public IEnumerator EndPlayerTurnRoutine(
     EncounterType currentEncounter,
     Func<bool> isEnemySpawningFunc,
     Func<bool> isEnemyDefeatedThisTurnFunc,
     Action clearEnemyDefeatedThisTurn,
     Func<BattleUnit> getEnemyUnit,
     Action onStageClear,
     Func<IEnumerator> enemyTurnRoutineFactory,
     Action onAdvanceSafeRoomTurn,
     Action<bool> setBoardInteractable)
    {
        yield return new WaitForSeconds(endPlayerTurnDelay);

        while (isEnemySpawningFunc != null && isEnemySpawningFunc())
        {
            yield return null;
        }

        if (IsSafeRoomEncounter(currentEncounter))
        {
            bool defeatedThisTurn = isEnemyDefeatedThisTurnFunc != null && isEnemyDefeatedThisTurnFunc();

            if (!defeatedThisTurn)
            {
                onAdvanceSafeRoomTurn?.Invoke();
            }
            else
            {
                setBoardInteractable?.Invoke(true);
            }

            clearEnemyDefeatedThisTurn?.Invoke();
            yield break;
        }

        if (isEnemyDefeatedThisTurnFunc != null && isEnemyDefeatedThisTurnFunc())
        {
            clearEnemyDefeatedThisTurn?.Invoke();
            setBoardInteractable?.Invoke(true);
            yield break;
        }

        BattleUnit currentEnemy = getEnemyUnit != null ? getEnemyUnit() : null;

        if (currentEnemy == null)
        {
            onStageClear?.Invoke();
            yield break;
        }

        if (enemyTurnRoutineFactory != null)
        {
            yield return enemyTurnRoutineFactory();
        }
        else
        {
            setBoardInteractable?.Invoke(true);
        }
    }

    public IEnumerator EnemyTurnRoutine(
     BattleUnit enemyUnit,
     BattleUnit playerUnit,
     GameObject hitEffectPrefab,
     Action<string, Vector3, Color> spawnDamageText,
     Action<GameObject, Vector3, float> spawnOneShotEffect,
     Action onGameOver,
     Action<bool> setBoardInteractable)
    {
        if (enemyUnit == null)
        {
            setBoardInteractable?.Invoke(true);
            yield break;
        }

        if (battleSfxController == null)
        {
            battleSfxController = FindObjectOfType<BattleSfxController>();
        }

        battleSfxController?.PlayTurnShift();

        StatusEffectHolder enemyStatusHolder = enemyUnit.StatusEffects;
        if (enemyStatusHolder != null && enemyStatusHolder.HasEffect(StatusEffectType.Paralysis))
        {
            Vector3 paralysisTextPos = enemyUnit.transform.position + Vector3.up * 1.5f;
            spawnDamageText?.Invoke("金縛り！", paralysisTextPos, new Color(0.8f, 0.4f, 1f));

            enemyStatusHolder.ConsumeEffectTurn(StatusEffectType.Paralysis);
            enemyStatusHolder.ConsumeEffectTurn(StatusEffectType.Slow);

            yield return new WaitForSeconds(enemyIdleDelay);
            TickPlayerPassiveEffectsOnEnemyTurnEnd(playerUnit);
            setBoardInteractable?.Invoke(true);
            yield break;
        }

        bool enemySlowSkippedCooldown = ShouldSkipEnemyCooldownProgressThisTurn(enemyUnit, spawnDamageText);
        if (!enemySlowSkippedCooldown)
        {
            enemyUnit.TickCooldown();
        }

        if (enemyUnit.IsReadyToAttack())
        {
            EnemyAttackPattern pattern = enemyUnit.attackPattern;

            if (pattern == EnemyAttackPattern.HeavyHit && !enemyUnit.isChargingHeavyHit)
            {
                enemyUnit.isChargingHeavyHit = true;

                enemyUnit.PlayChargeAnimation();

                Vector3 chargePos = enemyUnit.transform.position;
                spawnDamageText?.Invoke("…力を溜めている", chargePos + Vector3.up * 2.0f, new Color(1f, 0.6f, 0.2f));

                enemyUnit.ResetCooldown();
                yield return new WaitForSeconds(0.7f);
                TickPlayerPassiveEffectsOnEnemyTurnEnd(playerUnit);
                setBoardInteractable?.Invoke(true);
                yield break;
            }

            if (pattern == EnemyAttackPattern.SelfBuff)
            {
                bool choosesHeal = UnityEngine.Random.Range(0, 100) < selfBuffHealChance
                                   && enemyUnit.CurrentHP < enemyUnit.maxHP;

                if (choosesHeal)
                {
                    enemyUnit.PlayEnemyHealAnimation();

                    int healAmount = Mathf.Max(1, enemyUnit.attackPower * 2);
                    enemyUnit.Heal(healAmount);

                    Vector3 healPos = enemyUnit.transform.position;
                    spawnDamageText?.Invoke($"回復 +{healAmount}", healPos + Vector3.up * 1.5f, Color.green);

                    enemyUnit.ResetCooldown();
                    yield return new WaitForSeconds(enemyPostAttackDelay);
                    TickPlayerPassiveEffectsOnEnemyTurnEnd(playerUnit);
                    setBoardInteractable?.Invoke(true);
                    yield break;
                }
            }

            if (pattern == EnemyAttackPattern.HeavyHit && enemyUnit.isChargingHeavyHit)
            {
                enemyUnit.PlayHeavyAttackAnimation();
            }
            else
            {
                enemyUnit.PlayAttackAnimation();
            }

            yield return new WaitForSeconds(enemyAttackWindupDelay);

            int baseDamage = Mathf.Max(1, enemyUnit.attackPower);

            if (pattern == EnemyAttackPattern.HeavyHit && enemyUnit.isChargingHeavyHit)
            {
                baseDamage = Mathf.Max(1, enemyUnit.attackPower + 3);
                enemyUnit.isChargingHeavyHit = false;
            }

            bool isEvasion = UnityEngine.Random.Range(0, 100) < 15;
            Vector3 pos = playerUnit != null ? playerUnit.transform.position : Vector3.zero;

            if (isEvasion)
            {
                spawnDamageText?.Invoke("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int finalDamage = baseDamage;
                finalDamage = ApplyPlayerCorrosionBonus(finalDamage, playerUnit, spawnDamageText);

                battleSfxController?.PlayPlayerHit();
                if (playerUnit != null) playerUnit.TakeDamage(finalDamage);

                if (playerUnit != null && playerUnit.StatusEffects != null)
                {
                    playerUnit.StatusEffects.OnDamageReceived();
                }

                if (hitEffectPrefab != null)
                    spawnOneShotEffect?.Invoke(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);

                spawnDamageText?.Invoke(PlayerHitTextMarker + finalDamage.ToString(), pos + Vector3.up * 1.15f, Color.red);

                if (playerUnit != null && playerUnit.IsDead())
                {
                    onGameOver?.Invoke();
                    yield break;
                }
            }

            if (pattern == EnemyAttackPattern.MultiHit)
            {
                yield return new WaitForSeconds(multiHitInterval);

                if (playerUnit != null && !playerUnit.IsDead())
                {
                    enemyUnit.PlayAttackAnimation();
                    yield return new WaitForSeconds(enemyAttackWindupDelay);

                    int hit2Damage = Mathf.Max(1, enemyUnit.attackPower);
                    bool hit2Evasion = UnityEngine.Random.Range(0, 100) < 15;
                    Vector3 pos2 = playerUnit.transform.position;

                    if (hit2Evasion)
                    {
                        spawnDamageText?.Invoke("Miss", pos2 + Vector3.up * 1.5f, Color.gray);
                    }
                    else
                    {
                        int finalDmg2 = hit2Damage;
                        finalDmg2 = ApplyPlayerCorrosionBonus(finalDmg2, playerUnit, spawnDamageText);

                        battleSfxController?.PlayPlayerHit();
                        playerUnit.TakeDamage(finalDmg2);

                        if (playerUnit.StatusEffects != null)
                        {
                            playerUnit.StatusEffects.OnDamageReceived();
                        }

                        if (hitEffectPrefab != null)
                            spawnOneShotEffect?.Invoke(hitEffectPrefab, pos2 + Vector3.up * 0.5f, 0.7f);

                        spawnDamageText?.Invoke(PlayerHitTextMarker + finalDmg2.ToString(), pos2 + Vector3.up * 1.35f, Color.red);

                        if (playerUnit.IsDead())
                        {
                            onGameOver?.Invoke();
                            yield break;
                        }
                    }
                }
            }

            if (pattern == EnemyAttackPattern.PanelCorrupt)
            {
                yield return new WaitForSeconds(0.15f);

                enemyUnit.PlayCorruptSkillAnimation();

                Vector3 corruptPos = enemyUnit.transform.position;
                spawnDamageText?.Invoke("盤面汚染!", corruptPos + Vector3.up * 2.0f, new Color(0.7f, 0.2f, 0.8f));

                yield return new WaitForSeconds(0.35f);

                OnPanelCorruptRequested?.Invoke(panelCorruptCount);

                yield return new WaitForSeconds(0.3f);
            }

            enemyUnit.ResetCooldown();
            yield return new WaitForSeconds(enemyPostAttackDelay);
        }
        else
        {
            yield return new WaitForSeconds(enemyIdleDelay);
        }

        TickPlayerPassiveEffectsOnEnemyTurnEnd(playerUnit);
        setBoardInteractable?.Invoke(true);
    }

    public void AdvanceEmptyTurn(
        EncounterType currentEncounter,
        ref int remainingSteps,
        BattleUnit playerUnit,
        Action updateEncounterUI,
        Action<string, Vector3, Color> spawnDamageText,
        Action<bool> setBoardInteractable,
        Func<IEnumerator> nextRoomRoutineFactory)
    {
        if (!IsSafeRoomEncounter(currentEncounter))
        {
            return;
        }

        remainingSteps--;
        updateEncounterUI?.Invoke();

        Vector3 basePos = playerUnit != null ? playerUnit.transform.position : Vector3.zero;

        if (remainingSteps > 0)
        {
            spawnDamageText?.Invoke($"あと {remainingSteps} ターン", basePos + Vector3.up * 1.5f, Color.white);
            setBoardInteractable?.Invoke(true);
        }
        else
        {
            spawnDamageText?.Invoke("次の部屋へ！", basePos + Vector3.up * 1.5f, Color.cyan);

            if (nextRoomRoutineFactory != null)
            {
                StartCoroutine(nextRoomRoutineFactory());
            }
        }
    }
}
