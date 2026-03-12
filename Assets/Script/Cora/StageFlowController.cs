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

    // --- Tier 対応 ---
    private StageConfig stageConfig;

    private readonly Queue<BattleUnit> upcomingEnemies = new Queue<BattleUnit>();
    private int spawnedEnemyCount = 0;

    private int defeatedEnemyCount = 0;
    public int DefeatedEnemyCount => defeatedEnemyCount;
    public int TotalBattles => maxFloors;

    /// <summary>
    /// 現在の戦闘番号（1始まり）。Tier 判定に使用。
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
    /// StageConfig を設定する。設定すると Tier ベースの敵選出が有効になる。
    /// Configure() の後に呼ぶ。
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
            errorMessage = "生成できる敵がいませんでした。";
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
        if (spawnedEnemyCount >= maxFloors) return;

        BattleUnit prefabToSpawn = PickEnemyPrefab();
        if (prefabToSpawn == null) return;

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

        // StageConfig がある場合はそちらの敵プールを使う
        bool hasStageConfig = stageConfig != null
            && stageConfig.tiers != null
            && stageConfig.tiers.Count > 0;

        bool hasLegacyPrefabs = enemyPrefabs != null && enemyPrefabs.Count > 0;

        if (!hasStageConfig && !hasLegacyPrefabs)
        {
            errorMessage = "enemyPrefabs が未設定です。敵プレハブを1つ以上登録してください。";
            return false;
        }

        if (battlePosition == null)
        {
            errorMessage = "battlePosition が未設定です。";
            return false;
        }

        if (maxFloors <= 0)
        {
            errorMessage = "maxFloors が 0 以下です。";
            return false;
        }

        return true;
    }

    private void SpawnNextEnemyInternal()
    {
        if (spawnedEnemyCount >= maxFloors) return;
        if (battlePosition == null) return;

        BattleUnit prefabToSpawn = PickEnemyPrefab();
        if (prefabToSpawn == null) return;

        Vector3 spawnPos = battlePosition.position + (waitOffset * spawnedEnemyCount);

        BattleUnit newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        upcomingEnemies.Enqueue(newEnemy);
        spawnedEnemyCount++;
    }

    /// <summary>
    /// StageConfig があれば Tier から選出、なければ従来のランダム。
    /// </summary>
    private BattleUnit PickEnemyPrefab()
    {
        // Tier ベース（StageConfig あり）
        if (stageConfig != null && stageConfig.tiers != null && stageConfig.tiers.Count > 0)
        {
            int battleNumber = spawnedEnemyCount + 1;
            BattleUnit picked = stageConfig.PickEnemyPrefab(battleNumber);
            if (picked != null) return picked;
        }

        // フォールバック：従来のランダム選出
        if (enemyPrefabs != null && enemyPrefabs.Count > 0)
        {
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        }

        return null;
    }

    public BattleUnit TakeNextEnemyOrSpawn(Vector3 anchorPosition)
    {
        if (upcomingEnemies.Count == 0 && spawnedEnemyCount < maxFloors)
        {
            SpawnNextEnemyAfter(anchorPosition);
        }

        if (upcomingEnemies.Count > 0)
        {
            return upcomingEnemies.Dequeue();
        }
        defeatedEnemyCount++;
        return null;
    }

    public bool IsStageComplete()
    {
        return upcomingEnemies.Count == 0 && spawnedEnemyCount >= maxFloors;
    }
}