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
    private RewardDropController rewardDropController;
    private Func<int> getCurrentRewardBattleNumber;
    [SerializeField] private float postDefeatRespawnDelay = 0.05f;
    [SerializeField] private float damageTextDelay = 0.04f;

    [Header("外殻表示")]
    [SerializeField] private Color shellDamageTextColor = new Color(0.72f, 0.87f, 1f, 1f);
    [SerializeField] private Color shellBreakTextColor = new Color(0.95f, 0.96f, 1f, 1f);

    private bool nextDamageIsGun;
    private bool nextDamageUseHeavyReaction;
    private bool nextDamageUseOverlinkTextBoost;

    private const string OverlinkImpactTextMarker = "<ovl>";

    private bool hasQueuedSuccessfulEnemyHitStatusEffect;
    private StatusEffectType queuedSuccessfulEnemyHitStatusEffectType = StatusEffectType.None;
    private int queuedSuccessfulEnemyHitStatusEffectTurns;
    private bool queuedSuccessfulEnemyHitStatusEffectRemoveOnDamage;
    private int queuedSuccessfulEnemyHitStatusEffectPotency;
    private int queuedSuccessfulEnemyHitStatusEffectChancePercent = 100;

    private BattleSfxController battleSfxController;

    public void SetNextDamageIsGun(bool value)
    {
        nextDamageIsGun = value;
    }

    public void SetNextDamageUseHeavyReaction(bool value)
    {
        nextDamageUseHeavyReaction = value;
    }

    public void SetNextDamageUseOverlinkTextBoost(bool value)
    {
        nextDamageUseOverlinkTextBoost = value;
    }

    public void QueueNextSuccessfulEnemyHitStatusEffect(
        StatusEffectType type,
        int turns,
        bool removeOnDamage,
        int potency = 0,
        int chancePercent = 100)
    {
        if (type == StatusEffectType.None || turns <= 0)
        {
            ClearQueuedSuccessfulEnemyHitStatusEffect();
            return;
        }

        hasQueuedSuccessfulEnemyHitStatusEffect = true;
        queuedSuccessfulEnemyHitStatusEffectType = type;
        queuedSuccessfulEnemyHitStatusEffectTurns = turns;
        queuedSuccessfulEnemyHitStatusEffectRemoveOnDamage = removeOnDamage;
        queuedSuccessfulEnemyHitStatusEffectPotency = potency;
        queuedSuccessfulEnemyHitStatusEffectChancePercent = Mathf.Clamp(chancePercent, 0, 100);
    }

    public void ClearQueuedSuccessfulEnemyHitStatusEffect()
    {
        hasQueuedSuccessfulEnemyHitStatusEffect = false;
        queuedSuccessfulEnemyHitStatusEffectType = StatusEffectType.None;
        queuedSuccessfulEnemyHitStatusEffectTurns = 0;
        queuedSuccessfulEnemyHitStatusEffectRemoveOnDamage = false;
        queuedSuccessfulEnemyHitStatusEffectPotency = 0;
        queuedSuccessfulEnemyHitStatusEffectChancePercent = 100;
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
        battleSfxController = FindObjectOfType<BattleSfxController>();
    }

    public void SetRewardDropController(RewardDropController controller, Func<int> getCurrentRewardBattleNumber)
    {
        rewardDropController = controller;
        this.getCurrentRewardBattleNumber = getCurrentRewardBattleNumber;
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
        if (battleSfxController == null)
        {
            battleSfxController = FindObjectOfType<BattleSfxController>();
        }

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit == null) return;
        if (enemyUnit.IsDead()) return;
        if (getIsEnemySpawning != null && getIsEnemySpawning()) return;

        bool isGun = nextDamageIsGun;
        bool useHeavyReaction = nextDamageUseHeavyReaction;
        bool useOverlinkTextBoost = nextDamageUseOverlinkTextBoost;

        nextDamageIsGun = false;
        nextDamageUseHeavyReaction = false;
        nextDamageUseOverlinkTextBoost = false;

        bool isEvasion = UnityEngine.Random.Range(0, 100) < evasionRate;
        bool isCritical = UnityEngine.Random.Range(0, 100) < criticalRate;
        Vector3 enemyPos = enemyUnit.transform.position;

        if (isEvasion)
        {
            battleEventHub?.RaiseDamageTextRequested("Miss", enemyPos + Vector3.up * damageTextHeight, Color.gray);
            return;
        }

        int finalDamage = isCritical ? baseDamage * criticalMultiplier : baseDamage;

        finalDamage = ApplyEnemyTypeModifier(finalDamage, enemyUnit, isGun);

        int corrosionBonus = 0;
        if (enemyUnit.StatusEffects != null && enemyUnit.StatusEffects.HasEffect(StatusEffectType.Corrosion))
        {
            corrosionBonus = enemyUnit.StatusEffects.GetEffectPotency(StatusEffectType.Corrosion);
            if (corrosionBonus > 0)
            {
                finalDamage += corrosionBonus;
            }
        }

        int shellAbsorbedDamage = 0;
        bool shellBrokenThisHit = false;
        if (finalDamage > 0 && enemyUnit.HasActiveShell)
        {
            finalDamage = enemyUnit.ApplyShellDamage(finalDamage, out shellAbsorbedDamage, out shellBrokenThisHit);
        }

        if (finalDamage > 0)
        {
            battleSfxController?.PlayEnemyHit();
        }

        enemyUnit.TakeDamage(finalDamage, useHeavyReaction);

        TryApplyQueuedSuccessfulEnemyHitStatusEffect(enemyUnit);

        if (enemyUnit.StatusEffects != null)
        {
            enemyUnit.StatusEffects.OnDamageReceived();
        }

        battleEventHub?.RaiseOneShotEffectRequested(hitEffectPrefab, enemyPos + Vector3.up * hitEffectHeight, hitEffectReturnDelay);

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

        if (useOverlinkTextBoost && finalDamage > 0)
        {
            text = OverlinkImpactTextMarker + text;
        }

        StartCoroutine(ShowDamageTextDelayed(text, enemyPos + Vector3.up * damageTextHeight, textColor, damageTextDelay));

        if (shellAbsorbedDamage > 0)
        {
            StartCoroutine(ShowDamageTextDelayed(
                $"外殻 -{shellAbsorbedDamage}",
                enemyPos + Vector3.up * (damageTextHeight + 0.42f),
                shellDamageTextColor,
                damageTextDelay));
        }

        if (shellBrokenThisHit)
        {
            StartCoroutine(ShowDamageTextDelayed(
                "外殻破壊",
                enemyPos + Vector3.up * (damageTextHeight + 0.84f),
                shellBreakTextColor,
                damageTextDelay + 0.03f));
        }

        if (corrosionBonus > 0)
        {
            StartCoroutine(ShowDamageTextDelayed(
                "腐食！",
                enemyPos + Vector3.up * (damageTextHeight + 0.5f),
                new Color(0.6f, 1f, 0.2f),
                damageTextDelay));
        }

        if (!enemyUnit.IsDead())
        {
            return;
        }

        HandleEnemyDefeat(enemyUnit);
    }

    private void TryApplyQueuedSuccessfulEnemyHitStatusEffect(BattleUnit enemyUnit)
    {
        if (!hasQueuedSuccessfulEnemyHitStatusEffect) return;
        if (enemyUnit == null)
        {
            ClearQueuedSuccessfulEnemyHitStatusEffect();
            return;
        }

        bool shouldApply = UnityEngine.Random.Range(0, 100) < queuedSuccessfulEnemyHitStatusEffectChancePercent;
        if (!shouldApply)
        {
            ClearQueuedSuccessfulEnemyHitStatusEffect();
            return;
        }

        if (!enemyUnit.IsDead() && enemyUnit.StatusEffects != null)
        {
            enemyUnit.StatusEffects.ApplyEffect(
                queuedSuccessfulEnemyHitStatusEffectType,
                queuedSuccessfulEnemyHitStatusEffectTurns,
                queuedSuccessfulEnemyHitStatusEffectRemoveOnDamage,
                queuedSuccessfulEnemyHitStatusEffectPotency);

            string applyText = GetQueuedStatusEffectApplyText(queuedSuccessfulEnemyHitStatusEffectType);
            if (!string.IsNullOrEmpty(applyText))
            {
                battleEventHub?.RaiseDamageTextRequested(
                    applyText,
                    enemyUnit.transform.position + Vector3.up * (damageTextHeight + 0.85f),
                    GetQueuedStatusEffectApplyTextColor(queuedSuccessfulEnemyHitStatusEffectType));
            }
        }

        ClearQueuedSuccessfulEnemyHitStatusEffect();
    }

    private string GetQueuedStatusEffectApplyText(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Corrosion:
                return "装甲腐食";

            case StatusEffectType.Slow:
                return "駆動遅延";

            default:
                return null;
        }
    }

    private Color GetQueuedStatusEffectApplyTextColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Corrosion:
                return new Color(0.6f, 1f, 0.2f);

            case StatusEffectType.Slow:
                return new Color(0.45f, 0.9f, 1f);

            default:
                return Color.white;
        }
    }

    private int ApplyEnemyTypeModifier(int damage, BattleUnit enemyUnit, bool isGun)
    {
        if (enemyUnit == null) return damage;

        EnemyType type = enemyUnit.enemyType;

        switch (type)
        {
            case EnemyType.Floating:
                if (!isGun)
                {
                    damage = Mathf.Max(1, damage / 2);
                }
                break;

            case EnemyType.Armored:
                if (damage <= armorThreshold)
                {
                    damage = Mathf.Max(1, damage - 1);
                }
                break;
        }

        return damage;
    }

    private void HandleEnemyDefeat(BattleUnit defeatedEnemy)
    {
        StartCoroutine(HandleEnemyDefeatSequenceRoutine(defeatedEnemy));
    }

    private IEnumerator HandleEnemyDefeatSequenceRoutine(BattleUnit defeatedEnemy)
    {
        if (defeatedEnemy == null) yield break;

        if (panelBattleManager == null)
        {
            panelBattleManager = GetComponent<PanelBattleManager>();
        }

        StageFlowController stageFlowController = panelBattleManager != null
            ? panelBattleManager.stageFlowController
            : GetComponent<StageFlowController>();

        stageFlowController?.RegisterEnemyDefeated();

        setIsEnemyDefeatedThisTurn?.Invoke(true);
        battleEventHub?.RaiseEnemyDefeated(defeatedEnemy);

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

        if (rewardDropController != null && getCurrentRewardBattleNumber != null)
        {
            int rewardBattleNumber = Mathf.Max(1, getCurrentRewardBattleNumber.Invoke());
            yield return rewardDropController.TryPresentBoardRewardRoutine(rewardBattleNumber);
        }

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
        if (panelBattleManager == null)
        {
            panelBattleManager = GetComponent<PanelBattleManager>();
        }

        panelBattleManager?.panelBoardController?.PlayDefeatCelebration();

        int gainedCoins = panelBattleManager != null
            ? panelBattleManager.CalculateEnemyCoinReward(defeatedEnemy)
            : 0;

        if (gainedCoins > 0)
        {
            panelBattleManager.AddCoins(gainedCoins);

            Vector3 coinTextPos = defeatedEnemy.transform.position
                                  + Vector3.up * (expTextHeight + 0.45f);

            battleEventHub?.RaiseDamageTextRequested(
                $"+{gainedCoins}G",
                coinTextPos,
                new Color(1f, 0.9f, 0.2f));
        }

        Vector3 expTextPos = defeatedEnemy.transform.position + Vector3.up * expTextHeight;
        battleEventHub?.RaiseExpTextRequested(defeatedEnemy.expYield, expTextPos, expTextDelay);

        bool isLevelUp = false;
        if (playerUnit != null)
        {
            isLevelUp = playerUnit.AddExp(defeatedEnemy.expYield);
        }

        panelBattleManager?.RefreshPlayerExpUI();

        if (isLevelUp)
        {
            if (playerUnit != null)
            {
                battleEventHub?.RaiseOneShotEffectRequested(
                    levelUpEffectPrefab,
                    playerUnit.transform.position,
                    1.2f);
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
