using System;
using System.Collections;
using UnityEngine;

public class BattleTurnController : MonoBehaviour
{
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

    public void Configure(float endDelay, float attackWindup, float postAttack, float idleDelay)
    {
        endPlayerTurnDelay = endDelay;
        enemyAttackWindupDelay = attackWindup;
        enemyPostAttackDelay = postAttack;
        enemyIdleDelay = idleDelay;
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

        if (currentEncounter == EncounterType.Empty || currentEncounter == EncounterType.Treasure)
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

        enemyUnit.TickCooldown();

        if (enemyUnit.IsReadyToAttack())
        {
            EnemyAttackPattern pattern = enemyUnit.attackPattern;

            // === HeavyHit 溜めターン ===
            if (pattern == EnemyAttackPattern.HeavyHit && !enemyUnit.isChargingHeavyHit)
            {
                enemyUnit.isChargingHeavyHit = true;

                Vector3 chargePos = enemyUnit.transform.position;
                spawnDamageText?.Invoke("…力を溜めている", chargePos + Vector3.up * 2.0f, new Color(1f, 0.6f, 0.2f));

                enemyUnit.ResetCooldown();
                yield return new WaitForSeconds(0.5f);
                setBoardInteractable?.Invoke(true);
                yield break;
            }

            // === SelfBuff 回復ターン（50%） ===
            if (pattern == EnemyAttackPattern.SelfBuff)
            {
                bool choosesHeal = UnityEngine.Random.Range(0, 100) < selfBuffHealChance
                                   && enemyUnit.CurrentHP < enemyUnit.maxHP;

                if (choosesHeal)
                {
                    int healAmount = Mathf.Max(1, enemyUnit.attackPower * 2);
                    enemyUnit.Heal(healAmount);

                    Vector3 healPos = enemyUnit.transform.position;
                    spawnDamageText?.Invoke($"回復 +{healAmount}", healPos + Vector3.up * 1.5f, Color.green);

                    enemyUnit.ResetCooldown();
                    yield return new WaitForSeconds(enemyPostAttackDelay);
                    setBoardInteractable?.Invoke(true);
                    yield break;
                }
            }

            // === 攻撃（演出つき） ===
            enemyUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(enemyAttackWindupDelay);

            int baseDamage = Mathf.Max(1, enemyUnit.attackPower);

            if (pattern == EnemyAttackPattern.HeavyHit && enemyUnit.isChargingHeavyHit)
            {
                baseDamage = Mathf.Max(1, enemyUnit.attackPower * 2);
                enemyUnit.isChargingHeavyHit = false;
            }

            // 1発目
            bool isEvasion = UnityEngine.Random.Range(0, 100) < 15;
            bool isCritical = (pattern != EnemyAttackPattern.HeavyHit) && UnityEngine.Random.Range(0, 100) < 10;
            Vector3 pos = playerUnit != null ? playerUnit.transform.position : Vector3.zero;

            if (isEvasion)
            {
                spawnDamageText?.Invoke("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int finalDamage = isCritical ? baseDamage * 2 : baseDamage;

                if (playerUnit != null) playerUnit.TakeDamage(finalDamage);

                if (hitEffectPrefab != null)
                    spawnOneShotEffect?.Invoke(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);

                Color textColor = isCritical ? Color.yellow : Color.red;
                string textStr = isCritical ? $"CRITICAL!\n{finalDamage}" : finalDamage.ToString();
                spawnDamageText?.Invoke(textStr, pos + Vector3.up * 1.5f, textColor);

                if (playerUnit != null && playerUnit.IsDead())
                {
                    onGameOver?.Invoke();
                    yield break;
                }
            }

            // === MultiHit 2発目 ===
            if (pattern == EnemyAttackPattern.MultiHit)
            {
                yield return new WaitForSeconds(multiHitInterval);

                if (playerUnit != null && !playerUnit.IsDead())
                {
                    enemyUnit.PlayAttackAnimation();
                    yield return new WaitForSeconds(enemyAttackWindupDelay);

                    int hit2Damage = Mathf.Max(1, enemyUnit.attackPower);
                    bool hit2Evasion = UnityEngine.Random.Range(0, 100) < 15;
                    bool hit2Crit = UnityEngine.Random.Range(0, 100) < 10;
                    Vector3 pos2 = playerUnit.transform.position;

                    if (hit2Evasion)
                    {
                        spawnDamageText?.Invoke("Miss", pos2 + Vector3.up * 1.5f, Color.gray);
                    }
                    else
                    {
                        int finalDmg2 = hit2Crit ? hit2Damage * 2 : hit2Damage;
                        playerUnit.TakeDamage(finalDmg2);

                        if (hitEffectPrefab != null)
                            spawnOneShotEffect?.Invoke(hitEffectPrefab, pos2 + Vector3.up * 0.5f, 0.7f);

                        Color col2 = hit2Crit ? Color.yellow : Color.red;
                        string txt2 = hit2Crit ? $"CRITICAL!\n{finalDmg2}" : finalDmg2.ToString();
                        spawnDamageText?.Invoke(txt2, pos2 + Vector3.up * 1.5f, col2);

                        if (playerUnit.IsDead())
                        {
                            onGameOver?.Invoke();
                            yield break;
                        }
                    }
                }
            }

            // === PanelCorrupt 盤面汚染 ===
            if (pattern == EnemyAttackPattern.PanelCorrupt)
            {
                yield return new WaitForSeconds(0.15f);

                Vector3 corruptPos = enemyUnit.transform.position;
                spawnDamageText?.Invoke("盤面汚染!", corruptPos + Vector3.up * 2.0f, new Color(0.7f, 0.2f, 0.8f));

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
        if (currentEncounter != EncounterType.Empty && currentEncounter != EncounterType.Treasure)
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