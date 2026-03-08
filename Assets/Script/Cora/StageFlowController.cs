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

    private readonly Queue<BattleUnit> upcomingEnemies = new Queue<BattleUnit>();
    private int spawnedEnemyCount = 0;

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

    public bool SetupInitialStage(out BattleUnit currentEnemy, out string errorMessage)
    {
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
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        BattleUnit prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

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

        bool forceEnemy = previousEncounter == EncounterType.Empty || previousEncounter == EncounterType.Treasure;
        int roll = forceEnemy ? 0 : Random.Range(0, 100);

        if (forceEnemy || roll < 70)
        {
            return new NextEncounterPlan(false, EncounterType.Enemy, 0);
        }

        if (roll < 90)
        {
            return new NextEncounterPlan(false, EncounterType.Empty, 3);
        }

        return new NextEncounterPlan(false, EncounterType.Treasure, 1);
    }

    private bool IsStageClear()
    {
        return upcomingEnemies.Count == 0 && spawnedEnemyCount >= maxFloors;
    }

    private bool ValidateConfiguration(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            errorMessage = "enemyPrefabs が未設定です。敵プレハブを1体以上登録してください。";
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
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;
        if (battlePosition == null) return;

        BattleUnit prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Vector3 spawnPos = battlePosition.position + (waitOffset * spawnedEnemyCount);

        BattleUnit newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        upcomingEnemies.Enqueue(newEnemy);
        spawnedEnemyCount++;
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

        return null;
    }

    public bool IsStageComplete()
    {
        return upcomingEnemies.Count == 0 && spawnedEnemyCount >= maxFloors;
    }
}