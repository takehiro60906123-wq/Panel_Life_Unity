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

        enemyUnit.currentCooldown--;
        enemyUnit.UpdateTurnUI();

        if (enemyUnit.currentCooldown <= 0)
        {
            if (enemyUnit.animator != null)
            {
                enemyUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(enemyAttackWindupDelay);

            bool isEvasion = UnityEngine.Random.Range(0, 100) < 15;
            bool isCritical = UnityEngine.Random.Range(0, 100) < 10;

            Vector3 pos = playerUnit != null ? playerUnit.transform.position : Vector3.zero;

            if (isEvasion)
            {
                spawnDamageText?.Invoke("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int damage = isCritical ? 3 : 1;

                if (playerUnit != null)
                {
                    playerUnit.TakeDamage(damage);
                }

                if (hitEffectPrefab != null)
                {
                    spawnOneShotEffect?.Invoke(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);
                }

                Color textColor = isCritical ? Color.yellow : Color.red;
                string textStr = isCritical ? $"CRITICAL!\n{damage}" : damage.ToString();
                spawnDamageText?.Invoke(textStr, pos + Vector3.up * 1.5f, textColor);

                if (playerUnit != null && playerUnit.IsDead())
                {
                    onGameOver?.Invoke();
                    yield break;
                }
            }

            enemyUnit.currentCooldown = enemyUnit.attackInterval;
            enemyUnit.UpdateTurnUI();

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