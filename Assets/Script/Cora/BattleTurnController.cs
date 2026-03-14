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

    // ============================================
    // 非戦闘エンカウンターかどうかの判定
    // ============================================
    private static bool IsSafeRoomEncounter(EncounterType encounter)
    {
        return encounter == EncounterType.Empty
            || encounter == EncounterType.Treasure
            || encounter == EncounterType.Shop;
    }

    // ============================================
    // 状態異常: プレイヤーパッシブ効果のターン消費
    // 敵ターン終了時に呼ぶ。
    // 将来パッシブ効果が増えたらここに足す。
    // ============================================
    private void TickPlayerPassiveEffectsOnEnemyTurnEnd(BattleUnit playerUnit)
    {
        if (playerUnit == null) return;
        StatusEffectHolder holder = playerUnit.StatusEffects;
        if (holder == null) return;

        holder.ConsumeEffectTurn(StatusEffectType.Corrosion);
    }


    // ============================================
    // 状態異常: 敵の駆動遅延
    // 敵ターン開始時に、現在の enemy turn の cooldown 進行を1回止める。
    // 既に攻撃可能状態なら効果は薄いが、そのターン分は消費する。
    // ============================================
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

    // ============================================
    // 状態異常: プレイヤー腐食によるダメージ増加
    // 敵攻撃のダメージ計算後、TakeDamage前に呼ぶ。
    // ============================================
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

        // ==============================================
        // 状態異常: 敵金縛りチェック
        // 金縛り中はクールダウンも進まない（完全凍結）
        // ==============================================
        StatusEffectHolder enemyStatusHolder = enemyUnit.StatusEffects;
        if (enemyStatusHolder != null && enemyStatusHolder.HasEffect(StatusEffectType.Paralysis))
        {
            Vector3 paralysisTextPos = enemyUnit.transform.position + Vector3.up * 1.5f;
            spawnDamageText?.Invoke("金縛り！", paralysisTextPos, new Color(0.8f, 0.4f, 1f));

            // 行動スキップした後に残りターンを消費
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

            // === HeavyHit 溜めターン ===
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

            // === SelfBuff 回復ターン（50%） ===
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

            // === 攻撃（パターン別演出） ===
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

                // === 状態異常: プレイヤー腐食チェック ===
                finalDamage = ApplyPlayerCorrosionBonus(finalDamage, playerUnit, spawnDamageText);

                if (playerUnit != null) playerUnit.TakeDamage(finalDamage);

                // === 状態異常: プレイヤー被弾解除 ===
                if (playerUnit != null && playerUnit.StatusEffects != null)
                {
                    playerUnit.StatusEffects.OnDamageReceived();
                }

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

                        // === 状態異常: プレイヤー腐食チェック（MultiHit 2発目） ===
                        finalDmg2 = ApplyPlayerCorrosionBonus(finalDmg2, playerUnit, spawnDamageText);

                        playerUnit.TakeDamage(finalDmg2);

                        // === 状態異常: プレイヤー被弾解除（MultiHit 2発目） ===
                        if (playerUnit.StatusEffects != null)
                        {
                            playerUnit.StatusEffects.OnDamageReceived();
                        }

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
