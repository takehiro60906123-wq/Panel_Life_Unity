using System;
using System.Collections;
using UnityEngine;

public class BattleDamageResolver : MonoBehaviour
{
    [Header("判定設定")]
    [Range(0, 100)][SerializeField] private int evasionRate = 10;
    [Range(0, 100)][SerializeField] private int criticalRate = 20;
    [SerializeField] private int criticalMultiplier = 2;

    [Header("表示設定")]
    [SerializeField] private float hitEffectReturnDelay = 0.7f;
    [SerializeField] private float expTextDelay = 0.4f;
    [SerializeField] private float levelUpTextDelay = 1.05f;
    [SerializeField] private float damageTextHeight = 1.5f;
    [SerializeField] private float hitEffectHeight = 0.5f;
    [SerializeField] private float expTextHeight = 1.0f;

    private BattleEventHub battleEventHub;
    private BattleUnit playerUnit;
    private Func<BattleUnit> getEnemyUnit;
    private Func<bool> getIsEnemySpawning;
    private Action<bool> setIsEnemyDefeatedThisTurn;

    private GameObject hitEffectPrefab;
    private GameObject levelUpEffectPrefab;
    private Func<IEnumerator> enemyRespawnRoutineFactory;

    private bool isSubscribed;
    private PanelBattleManager panelBattleManager;
    [SerializeField] private float postDefeatRespawnDelay = 0.55f;


    public void Initialize(
        BattleEventHub battleEventHub,
        BattleUnit playerUnit,
        Func<BattleUnit> getEnemyUnit,
        Func<bool> getIsEnemySpawning,
        Action<bool> setIsEnemyDefeatedThisTurn,
        GameObject hitEffectPrefab,
        GameObject levelUpEffectPrefab,
        Func<IEnumerator> enemyRespawnRoutineFactory)
    {
        BindEventHub(battleEventHub);
        this.playerUnit = playerUnit;
        this.getEnemyUnit = getEnemyUnit;
        this.getIsEnemySpawning = getIsEnemySpawning;
        this.setIsEnemyDefeatedThisTurn = setIsEnemyDefeatedThisTurn;
        this.hitEffectPrefab = hitEffectPrefab;
        this.levelUpEffectPrefab = levelUpEffectPrefab;
        this.enemyRespawnRoutineFactory = enemyRespawnRoutineFactory;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void BindEventHub(BattleEventHub newHub)
    {
        if (battleEventHub == newHub)
        {
            Subscribe();
            return;
        }

        Unsubscribe();
        battleEventHub = newHub;
        Subscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed || battleEventHub == null)
        {
            return;
        }

        battleEventHub.EnemyDamageRequested += DamageEnemy;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || battleEventHub == null)
        {
            return;
        }

        battleEventHub.EnemyDamageRequested -= DamageEnemy;
        isSubscribed = false;
    }

    public void DamageEnemy(int baseDamage)
    {
        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit == null) return;
        if (enemyUnit.IsDead()) return;
        if (getIsEnemySpawning != null && getIsEnemySpawning()) return;

        bool isEvasion = UnityEngine.Random.Range(0, 100) < evasionRate;
        bool isCritical = UnityEngine.Random.Range(0, 100) < criticalRate;
        Vector3 enemyPos = enemyUnit.transform.position;

        if (isEvasion)
        {
            battleEventHub?.RaiseDamageTextRequested("Miss", enemyPos + Vector3.up * damageTextHeight, Color.gray);
            return;
        }

        int finalDamage = isCritical ? baseDamage * criticalMultiplier : baseDamage;

        enemyUnit.TakeDamage(finalDamage);
        battleEventHub?.RaiseOneShotEffectRequested(hitEffectPrefab, enemyPos + Vector3.up * hitEffectHeight, hitEffectReturnDelay);

        Color textColor = isCritical ? Color.yellow : Color.white;
        string text = isCritical ? $"CRITICAL!\n{finalDamage}" : finalDamage.ToString();
        battleEventHub?.RaiseDamageTextRequested(text, enemyPos + Vector3.up * damageTextHeight, textColor);

        if (!enemyUnit.IsDead())
        {
            return;
        }

        HandleEnemyDefeat(enemyUnit);
    }

    private void HandleEnemyDefeat(BattleUnit defeatedEnemy)
    {
        StartCoroutine(HandleEnemyDefeatSequenceRoutine(defeatedEnemy));
    }

    private IEnumerator HandleEnemyDefeatSequenceRoutine(BattleUnit defeatedEnemy)
    {
        if (defeatedEnemy == null) yield break;

        setIsEnemyDefeatedThisTurn?.Invoke(true);
        battleEventHub?.RaiseEnemyDefeated(defeatedEnemy);

        if (panelBattleManager == null)
        {
            panelBattleManager = GetComponent<PanelBattleManager>();
        }

        if (panelBattleManager != null)
        {
            yield return StartCoroutine(panelBattleManager.PlayEnemyDefeatSequence(defeatedEnemy, () =>
            {
                ApplyEnemyDefeatRewards(defeatedEnemy);
            }));
        }
        else
        {
            ApplyEnemyDefeatRewards(defeatedEnemy);
        }

        yield return new WaitForSeconds(postDefeatRespawnDelay);

        if (enemyRespawnRoutineFactory != null)
        {
            IEnumerator routine = enemyRespawnRoutineFactory();
            if (routine != null)
            {
                StartCoroutine(routine);
            }
        }
    }

    private void ApplyEnemyDefeatRewards(BattleUnit defeatedEnemy)
    {
        Vector3 expTextPos = defeatedEnemy.transform.position + Vector3.up * expTextHeight;
        battleEventHub?.RaiseExpTextRequested(defeatedEnemy.expYield, expTextPos, expTextDelay);

        bool isLevelUp = false;
        if (playerUnit != null)
        {
            isLevelUp = playerUnit.AddExp(defeatedEnemy.expYield);
        }

        if (isLevelUp)
        {
            if (playerUnit != null)
            {
                battleEventHub?.RaiseOneShotEffectRequested(levelUpEffectPrefab, playerUnit.transform.position, 1.2f);
            }

            battleEventHub?.RaiseLevelUpTextRequested(levelUpTextDelay);
        }
    }
}