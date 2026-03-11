// =============================================================
// StageConfig.cs
// 階層ごとの敵プール定義
// Assets/Scripts/Battle/ などに配置
//
// 使い方：
//   Project ウィンドウ右クリック → Create → Battle → Stage Config
//   PanelBattleManager の Inspector に設定する。
// =============================================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageConfig", menuName = "Battle/Stage Config")]
public class StageConfig : ScriptableObject
{
    [Header("総戦闘数")]
    [Tooltip("この設定を使う場合、PanelBattleManager の maxFloors より優先される")]
    public int totalBattles = 25;

    [Header("階層定義")]
    public List<StageTier> tiers = new List<StageTier>();

    /// <summary>
    /// 現在の戦闘番号（1始まり）に応じて、敵プレハブを1体選んで返す。
    /// </summary>
    public BattleUnit PickEnemyPrefab(int battleNumber)
    {
        if (tiers == null || tiers.Count == 0) return null;

        // 該当する Tier を探す（最後にマッチしたものを使う）
        StageTier activeTier = tiers[0];
        for (int i = 0; i < tiers.Count; i++)
        {
            if (battleNumber >= tiers[i].startBattle)
            {
                activeTier = tiers[i];
            }
        }

        if (activeTier == null) return null;

        return activeTier.PickRandom();
    }
}

[System.Serializable]
public class StageTier
{
    [Tooltip("この Tier が始まる戦闘番号（1始まり）")]
    public int startBattle = 1;

    public string tierName = "序盤";

    [Tooltip("この Tier で出現する敵エントリ")]
    public List<StageTierEntry> entries = new List<StageTierEntry>();

    /// <summary>
    /// 重み付きランダムで敵プレハブを1体選ぶ。
    /// </summary>
    public BattleUnit PickRandom()
    {
        if (entries == null || entries.Count == 0) return null;

        int totalWeight = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].enemyPrefab == null) continue;
            totalWeight += Mathf.Max(0, entries[i].weight);
        }

        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].enemyPrefab == null) continue;

            cumulative += Mathf.Max(0, entries[i].weight);
            if (roll < cumulative)
            {
                return entries[i].enemyPrefab;
            }
        }

        return entries[0].enemyPrefab;
    }
}

[System.Serializable]
public class StageTierEntry
{
    public BattleUnit enemyPrefab;

    [Tooltip("出現重み。大きいほど出やすい")]
    public int weight = 10;
}
