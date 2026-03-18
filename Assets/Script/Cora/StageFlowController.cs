using System.Collections.Generic;
using UnityEngine;

public class StageFlowController : MonoBehaviour
{
    public struct NextEncounterPlan
    {
        public bool isStageClear;
        public EncounterType encounterType;
        public int steps;

        public NextEncounterPlan(bool clear, EncounterType type, int stepCount)
        {
            isStageClear = clear;
            encounterType = type;
            steps = stepCount;
        }
    }

    private Transform battlePosition;
    private Vector3 waitOffset;
    private int maxFloors;
    private int maxVisibleEnemies;
    private List<BattleUnit> enemyPrefabs;

    // --- Tier Ή ---
    private StageConfig stageConfig;


    [Header("Endless 設定")]
    [SerializeField] private bool enableEndlessAfterFinalFloor = true;
    [SerializeField, Min(0f)] private float endlessHpGrowthPerBattle = 0.04f;
    [SerializeField, Min(1)] private int endlessAttackBonusEveryBattles = 8;
    [SerializeField, Min(0)] private int endlessAttackBonusStep = 1;
    [SerializeField, Min(1)] private int endlessRewardBonusEveryBattles = 10;
    [SerializeField, Min(0)] private int endlessRewardBonusStep = 1;
    private readonly Queue<BattleUnit> upcomingEnemies = new Queue<BattleUnit>();
    private int spawnedEnemyCount = 0;

    private int defeatedEnemyCount = 0;
    public int DefeatedEnemyCount => defeatedEnemyCount;
    public int TotalBattles => maxFloors;

    /// <summary>
    /// ݂̐퓬ԍi1n܂jBTier ɎgpB
    /// </summary>
    public int CurrentBattleNumber => spawnedEnemyCount;

    public void Configure(
        Transform battlePositionValue,
        Vector3 waitOffsetValue,
        int maxFloorsValue,
        int maxVisibleEnemiesValue,
        List<BattleUnit> enemyPrefabsValue)
    {
        battlePosition = battlePositionValue;
        waitOffset = waitOffsetValue;
        maxFloors = maxFloorsValue;
        maxVisibleEnemies = maxVisibleEnemiesValue;
        enemyPrefabs = enemyPrefabsValue;
    }

    /// <summary>
    /// StageConfig ݒ肷Bݒ肷 Tier x[X̓GIoLɂȂB
    /// Configure() ̌ɌĂԁB
    /// </summary>
    public void SetStageConfig(StageConfig config)
    {
        stageConfig = config;

        if (stageConfig != null && stageConfig.totalBattles > 0)
        {
            maxFloors = stageConfig.totalBattles;
        }
    }

    public bool SetupInitialStage(out BattleUnit currentEnemy, out string errorMessage)
    {
        defeatedEnemyCount = 0;
        currentEnemy = null;
        errorMessage = string.Empty;

        if (!ValidateConfiguration(out errorMessage))
        {
            return false;
        }

        upcomingEnemies.Clear();
        spawnedEnemyCount = 0;

        int initialSpawnCount = Mathf.Min(maxVisibleEnemies, maxFloors);
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnNextEnemyInternal();
        }

        if (upcomingEnemies.Count <= 0)
        {
            errorMessage = "łG܂łB";
            return false;
        }

        currentEnemy = upcomingEnemies.Dequeue();
        return true;
    }

    public IEnumerable<BattleUnit> GetUpcomingEnemies()
    {
        return upcomingEnemies;
    }

    public BattleUnit DequeueNextEnemy()
    {
        if (upcomingEnemies.Count <= 0) return null;
        return upcomingEnemies.Dequeue();
    }

    public void SpawnNextEnemyIfPossible()
    {
        if (!ValidateConfiguration(out _)) return;
        SpawnNextEnemyInternal();
    }

    public void SpawnNextEnemyAfter(Vector3 anchorPosition)
    {
        if (!ValidateConfiguration(out _)) return;
        if (!enableEndlessAfterFinalFloor && spawnedEnemyCount >= maxFloors) return;

        BattleUnit prefabToSpawn = PickEnemyPrefab();
        if (prefabToSpawn == null) return;

        int battleNumber = spawnedEnemyCount + 1;
        Vector3 spawnPos = anchorPosition + waitOffset;

        BattleUnit lastQueuedEnemy = null;
        foreach (BattleUnit enemy in upcomingEnemies)
        {
            if (enemy != null)
            {
                lastQueuedEnemy = enemy;
            }
        }

        if (lastQueuedEnemy != null)
        {
            spawnPos = lastQueuedEnemy.transform.position + waitOffset;
        }

        BattleUnit newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        ApplyBattleScaling(newEnemy, battleNumber);
        upcomingEnemies.Enqueue(newEnemy);
        spawnedEnemyCount++;
    }

    public NextEncounterPlan DecideNextEncounter(EncounterType previousEncounter)
    {
        if (IsStageClear())
        {
            return new NextEncounterPlan(true, EncounterType.Enemy, 0);
        }

        bool forceEnemy =
            previousEncounter == EncounterType.Empty ||
            previousEncounter == EncounterType.Treasure ||
            previousEncounter == EncounterType.Shop;

        int roll = forceEnemy ? 0 : Random.Range(0, 100);

        if (forceEnemy || roll < 65)
        {
            return new NextEncounterPlan(false, EncounterType.Enemy, 0);
        }

        if (roll < 80)
        {
            return new NextEncounterPlan(false, EncounterType.Empty, 3);
        }

        if (roll < 90)
        {
            return new NextEncounterPlan(false, EncounterType.Treasure, 1);
        }

        return new NextEncounterPlan(false, EncounterType.Shop, 1);
    }

    private bool IsStageClear()
    {
        return upcomingEnemies.Count == 0 && spawnedEnemyCount >= maxFloors;
    }

    private bool ValidateConfiguration(out string errorMessage)
    {
        errorMessage = string.Empty;

        // StageConfig ꍇ͂̓Gv[g
        bool hasStageConfig = stageConfig != null
            && stageConfig.tiers != null
            && stageConfig.tiers.Count > 0;

        bool hasLegacyPrefabs = enemyPrefabs != null && enemyPrefabs.Count > 0;

        if (!hasStageConfig && !hasLegacyPrefabs)
        {
            errorMessage = "enemyPrefabs ݒłBGvnu1ȏo^ĂB";
            return false;
        }

        if (battlePosition == null)
        {
            errorMessage = "battlePosition ݒłB";
            return false;
        }

        if (maxFloors <= 0)
        {
            errorMessage = "maxFloors  0 ȉłB";
            return false;
        }

        return true;
    }

    private void SpawnNextEnemyInternal()
    {
        if (!enableEndlessAfterFinalFloor && spawnedEnemyCount >= maxFloors) return;
        if (battlePosition == null) return;

        BattleUnit prefabToSpawn = PickEnemyPrefab();
        if (prefabToSpawn == null) return;

        int battleNumber = spawnedEnemyCount + 1;
        Vector3 spawnPos = battlePosition.position + (waitOffset * spawnedEnemyCount);

        BattleUnit newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        ApplyBattleScaling(newEnemy, battleNumber);
        upcomingEnemies.Enqueue(newEnemy);
        spawnedEnemyCount++;
    }

    /// <summary>
    /// StageConfig  Tier IoAȂΏ]̃_B
    /// </summary>
    private BattleUnit PickEnemyPrefab()
    {
        // Tier x[XiStageConfig j
        if (stageConfig != null && stageConfig.tiers != null && stageConfig.tiers.Count > 0)
        {
            int battleNumber = spawnedEnemyCount + 1;
            BattleUnit picked = stageConfig.PickEnemyPrefab(battleNumber);
            if (picked != null) return picked;
        }

        // tH[obNF]̃_Io
        if (enemyPrefabs != null && enemyPrefabs.Count > 0)
        {
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        }

        return null;
    }

    public BattleUnit TakeNextEnemyOrSpawn(Vector3 anchorPosition)
    {
        // ܓ|GԂAKw1i߂
        defeatedEnemyCount++;

        if (upcomingEnemies.Count == 0 && (enableEndlessAfterFinalFloor || spawnedEnemyCount < maxFloors))
        {
            SpawnNextEnemyAfter(anchorPosition);
        }

        if (upcomingEnemies.Count > 0)
        {
            return upcomingEnemies.Dequeue();
        }

        return null;
    }

    public bool IsStageComplete()
    {
        if (enableEndlessAfterFinalFloor) return false;
        return upcomingEnemies.Count == 0 && spawnedEnemyCount >= maxFloors;
    }

    public bool IsEndlessBattleNumber(int battleNumber)
    {
        return enableEndlessAfterFinalFloor && battleNumber > TotalBattles;
    }

    private void ApplyBattleScaling(BattleUnit unit, int battleNumber)
    {
        if (unit == null) return;
        if (!IsEndlessBattleNumber(battleNumber)) return;

        int endlessIndex = battleNumber - TotalBattles;
        float hpMultiplier = 1f + (endlessHpGrowthPerBattle * endlessIndex);
        int attackBonus = endlessAttackBonusEveryBattles > 0
            ? (endlessIndex / endlessAttackBonusEveryBattles) * Mathf.Max(0, endlessAttackBonusStep)
            : 0;
        int rewardBonus = endlessRewardBonusEveryBattles > 0
            ? (endlessIndex / endlessRewardBonusEveryBattles) * Mathf.Max(0, endlessRewardBonusStep)
            : 0;

        unit.ApplyEncounterScaling(hpMultiplier, attackBonus, rewardBonus);
    }
}