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

    [Header("敵タイプ補正")]
    [Tooltip("装甲敵：この値以下のダメージを1軽減（最低0）")]
    [SerializeField] private int armorThreshold = 2;

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
    [SerializeField] private float postDefeatRespawnDelay = 0.05f;
    [SerializeField] private float damageTextDelay = 0.04f;

    // --- 銃ダメージフラグ：銃攻撃前にtrueにセットされる ---
    private bool nextDamageIsGun;
    private bool nextDamageUseHeavyReaction;

    /// <summary>
    /// 次のダメージが銃由来かどうかをセットする。
    /// PanelBattleManager.ExecuteGunHit から呼ばれる。
    /// DamageEnemy 内で参照後に自動リセット。
    /// </summary>
    public void SetNextDamageIsGun(bool value)
    {
        nextDamageIsGun = value;
    }

    public void SetNextDamageUseHeavyReaction(bool value)
    {
        nextDamageUseHeavyReaction = value;
    }

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

        bool isGun = nextDamageIsGun;
        bool useHeavyReaction = nextDamageUseHeavyReaction;

        nextDamageIsGun = false;
        nextDamageUseHeavyReaction = false;

        bool isEvasion = UnityEngine.Random.Range(0, 100) < evasionRate;
        bool isCritical = UnityEngine.Random.Range(0, 100) < criticalRate;
        Vector3 enemyPos = enemyUnit.transform.position;

        if (isEvasion)
        {
            battleEventHub?.RaiseDamageTextRequested("Miss", enemyPos + Vector3.up * damageTextHeight, Color.gray);
            return;
        }

        int finalDamage = isCritical ? baseDamage * criticalMultiplier : baseDamage;

        // ==============================================
        // 敵タイプ補正
        // ==============================================
        finalDamage = ApplyEnemyTypeModifier(finalDamage, enemyUnit, isGun);

        enemyUnit.TakeDamage(finalDamage, useHeavyReaction);
        battleEventHub?.RaiseOneShotEffectRequested(hitEffectPrefab, enemyPos + Vector3.up * hitEffectHeight, hitEffectReturnDelay);

        // --- テキスト表示 ---
        string text;
        Color textColor;

        if (finalDamage <= 0)
        {
            text = "GUARD";
            textColor = Color.gray;
        }
        else if (isCritical)
        {
            text = $"CRITICAL!\n{finalDamage}";
            textColor = Color.yellow;
        }
        else
        {
            text = finalDamage.ToString();
            textColor = Color.white;
        }

        StartCoroutine(ShowDamageTextDelayed(text, enemyPos + Vector3.up * damageTextHeight, textColor, damageTextDelay));
        if (!enemyUnit.IsDead())
        {
            return;
        }

        HandleEnemyDefeat(enemyUnit);
    }

 

    // ==============================================
    // 敵タイプによるダメージ補正
    // ==============================================

    private int ApplyEnemyTypeModifier(int damage, BattleUnit enemyUnit, bool isGun)
    {
        if (enemyUnit == null) return damage;

        EnemyType type = enemyUnit.enemyType;

        switch (type)
        {
            case EnemyType.Floating:
                // 浮遊敵：近接ダメージ半減（銃はそのまま）
                if (!isGun)
                {
                    damage = Mathf.Max(1, damage / 2);
                }
                break;

            case EnemyType.Armored:
                // 装甲敵：小ダメージ（閾値以下）を1軽減。ただし最低1は通す
                if (damage <= armorThreshold)
                {
                    damage = Mathf.Max(1, damage - 1);
                }
                break;
        }

        return damage;
    }

    // ==============================================

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

        // ここを追加
        if (panelBattleManager == null)
        {
            panelBattleManager = GetComponent<PanelBattleManager>();
        }

        panelBattleManager?.RefreshPlayerExpUI();

        if (isLevelUp)
        {
            if (playerUnit != null)
            {
                battleEventHub?.RaiseOneShotEffectRequested(levelUpEffectPrefab, playerUnit.transform.position, 1.2f);
            }

            battleEventHub?.RaiseLevelUpTextRequested(levelUpTextDelay);
        }
    }

    private IEnumerator ShowDamageTextDelayed(string text, Vector3 position, Color color, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        battleEventHub?.RaiseDamageTextRequested(text, position, color);
    }
}