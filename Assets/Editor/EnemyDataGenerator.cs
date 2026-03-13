#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class EnemyDataGenerator
{
    private const string EnemyDataFolder = "Assets/Data/Enemies";
    private const string StageConfigPath = "Assets/Data/StageConfig_Main.asset";
    private const string PrefabFolder = "Assets/Resources/Enemy";

    private struct EnemyDef
    {
        public int id;
        public string name;
        public EnemyType type;
        public EnemyAttackPattern pattern;
        public int hp;
        public int atk;
        public int interval;
        public int exp;
        public int appearBattle;
        public int hpPerLevel;
        public int healPerLevel;
        public int expPerLevel;
    }

    // =========================================================
    // 全33体 — interval リバランス
    //
    // 方針：
    //   Rushing = 1（毎ターン。それが個性）
    //   HeavyHit = 1（溜め1ターン+攻撃1ターンの内蔵2ターン制）
    //   序盤 Normal/Armored = 2（余裕あり）
    //   中盤 = 1〜2（圧が増す）
    //   終盤 = 1（毎ターン殴ってくる地獄）
    //   SelfBuff = 2（回復ターンがあるので実質もっと遅い）
    //   PanelCorrupt = 2（汚染自体が十分脅威）
    //   蟲巣(018) = 2（ATK0なので間隔短くても痛くない）
    // =========================================================

    private static readonly EnemyDef[] AllEnemies = new EnemyDef[]
 {
    // === A. 蛾系（Floating）===
    new EnemyDef { id=1,  name="腐蛾",     type=EnemyType.Floating, pattern=EnemyAttackPattern.Normal,       hp=7,  atk=3, interval=1, exp=3,  appearBattle=1,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=2,  name="胞子蛾",   type=EnemyType.Floating, pattern=EnemyAttackPattern.Normal,       hp=12, atk=4, interval=1, exp=5,  appearBattle=7,  hpPerLevel=4, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=6,  name="蟲翼獣",   type=EnemyType.Floating, pattern=EnemyAttackPattern.Normal,       hp=24, atk=7, interval=1, exp=10, appearBattle=16, hpPerLevel=6, healPerLevel=2, expPerLevel=3 },

    // === B. 赤蟲系（Rushing）===
    new EnemyDef { id=3,  name="火蟲",     type=EnemyType.Rushing,  pattern=EnemyAttackPattern.Normal,       hp=6,  atk=4, interval=1, exp=3,  appearBattle=2,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=15, name="突撃蟲",   type=EnemyType.Rushing,  pattern=EnemyAttackPattern.MultiHit,     hp=10, atk=3, interval=1, exp=5,  appearBattle=10, hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=16, name="蟲戦車",   type=EnemyType.Rushing,  pattern=EnemyAttackPattern.MultiHit,     hp=18, atk=5, interval=1, exp=9,  appearBattle=19, hpPerLevel=5, healPerLevel=1, expPerLevel=2 },

    // === C. 岩殻系（Armored）===
    new EnemyDef { id=4,  name="岩蟲",     type=EnemyType.Armored,  pattern=EnemyAttackPattern.Normal,       hp=10, atk=3, interval=1, exp=4,  appearBattle=3,  hpPerLevel=4, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=5,  name="棘殻",     type=EnemyType.Armored,  pattern=EnemyAttackPattern.Normal,       hp=16, atk=4, interval=1, exp=6,  appearBattle=9,  hpPerLevel=4, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=7,  name="結晶獣",   type=EnemyType.Armored,  pattern=EnemyAttackPattern.HeavyHit,     hp=28, atk=6, interval=1, exp=11, appearBattle=17, hpPerLevel=6, healPerLevel=2, expPerLevel=3 },

    // === D. 多脚系 ===
    new EnemyDef { id=8,  name="蔓脚蟲",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.MultiHit,     hp=8,  atk=2, interval=1, exp=4,  appearBattle=4,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=9,  name="蠕蟲",     type=EnemyType.Normal,   pattern=EnemyAttackPattern.MultiHit,     hp=14, atk=3, interval=1, exp=6,  appearBattle=12, hpPerLevel=4, healPerLevel=1, expPerLevel=1 },

    // === E. 毛獣系 ===
    new EnemyDef { id=10, name="毛獣",     type=EnemyType.Normal,   pattern=EnemyAttackPattern.Normal,       hp=8,  atk=3, interval=1, exp=3,  appearBattle=2,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=21, name="重牙獣",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.HeavyHit,     hp=19, atk=6, interval=1, exp=9,  appearBattle=13, hpPerLevel=5, healPerLevel=1, expPerLevel=2 },
    new EnemyDef { id=22, name="暗雲獣",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.PanelCorrupt, hp=28, atk=6, interval=1, exp=11, appearBattle=20, hpPerLevel=6, healPerLevel=2, expPerLevel=3 },

    // === F. 苔系 ===
    new EnemyDef { id=11, name="苔玉",     type=EnemyType.Normal,   pattern=EnemyAttackPattern.SelfBuff,     hp=9,  atk=2, interval=1, exp=3,  appearBattle=5,  hpPerLevel=3, healPerLevel=2, expPerLevel=1 },
    new EnemyDef { id=19, name="苔塊",     type=EnemyType.Normal,   pattern=EnemyAttackPattern.SelfBuff,     hp=15, atk=3, interval=1, exp=6,  appearBattle=11, hpPerLevel=4, healPerLevel=3, expPerLevel=2 },
    new EnemyDef { id=20, name="暗苔塊",   type=EnemyType.Armored,  pattern=EnemyAttackPattern.SelfBuff,     hp=24, atk=5, interval=1, exp=9,  appearBattle=18, hpPerLevel=6, healPerLevel=3, expPerLevel=3 },

    // === G. 幽体系 ===
    new EnemyDef { id=12, name="幽体蟲",   type=EnemyType.Floating, pattern=EnemyAttackPattern.Normal,       hp=8,  atk=3, interval=1, exp=4,  appearBattle=6,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=13, name="雷光水母", type=EnemyType.Floating, pattern=EnemyAttackPattern.PanelCorrupt, hp=17, atk=5, interval=1, exp=8,  appearBattle=14, hpPerLevel=4, healPerLevel=1, expPerLevel=2 },

    // === H. 甲虫系 ===
    new EnemyDef { id=14, name="甲蟲",     type=EnemyType.Normal,   pattern=EnemyAttackPattern.Normal,       hp=9,  atk=3, interval=1, exp=4,  appearBattle=5,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=17, name="鉄甲蟲",   type=EnemyType.Armored,  pattern=EnemyAttackPattern.Normal,       hp=15, atk=4, interval=1, exp=6,  appearBattle=11, hpPerLevel=4, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=18, name="蟲巣",     type=EnemyType.Normal,   pattern=EnemyAttackPattern.PanelCorrupt, hp=22, atk=0, interval=1, exp=8,  appearBattle=20, hpPerLevel=5, healPerLevel=1, expPerLevel=2 },

    // === I. 炎植物系 ===
    new EnemyDef { id=23, name="焔蟲",     type=EnemyType.Ranged,   pattern=EnemyAttackPattern.Normal,       hp=14, atk=7, interval=1, exp=10, appearBattle=15, hpPerLevel=4, healPerLevel=1, expPerLevel=2 },
    new EnemyDef { id=24, name="蝕炎草",   type=EnemyType.Ranged,   pattern=EnemyAttackPattern.PanelCorrupt, hp=20, atk=6, interval=1, exp=10, appearBattle=21, hpPerLevel=5, healPerLevel=2, expPerLevel=3 },

    // === J. 棘獣系 ===
    new EnemyDef { id=25, name="棘紫蟲",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.Normal,       hp=11, atk=4, interval=1, exp=5,  appearBattle=8,  hpPerLevel=3, healPerLevel=1, expPerLevel=1 },
    new EnemyDef { id=26, name="大棘獣",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.Normal,       hp=18, atk=5, interval=1, exp=8,  appearBattle=14, hpPerLevel=4, healPerLevel=1, expPerLevel=2 },
    new EnemyDef { id=29, name="蒼棘獣",   type=EnemyType.Rushing,  pattern=EnemyAttackPattern.MultiHit,     hp=17, atk=5, interval=1, exp=10, appearBattle=19, hpPerLevel=5, healPerLevel=1, expPerLevel=2 },
    new EnemyDef { id=30, name="翠棘獣",   type=EnemyType.Armored,  pattern=EnemyAttackPattern.HeavyHit,     hp=32, atk=7, interval=1, exp=12, appearBattle=23, hpPerLevel=6, healPerLevel=2, expPerLevel=3 },

    // === K. 丸獣〜王蟲系 ===
    new EnemyDef { id=27, name="巨苔獣",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.SelfBuff,     hp=16, atk=3, interval=1, exp=6,  appearBattle=8,  hpPerLevel=4, healPerLevel=3, expPerLevel=2 },
    new EnemyDef { id=28, name="眼球蟲",   type=EnemyType.Ranged,   pattern=EnemyAttackPattern.Normal,       hp=13, atk=7, interval=1, exp=9,  appearBattle=15, hpPerLevel=4, healPerLevel=1, expPerLevel=2 },
    new EnemyDef { id=31, name="暗殻獣",   type=EnemyType.Armored,  pattern=EnemyAttackPattern.HeavyHit,     hp=32, atk=7, interval=1, exp=12, appearBattle=22, hpPerLevel=6, healPerLevel=2, expPerLevel=3 },
    new EnemyDef { id=32, name="赤眼獣",   type=EnemyType.Rushing,  pattern=EnemyAttackPattern.MultiHit,     hp=21, atk=6, interval=1, exp=13, appearBattle=24, hpPerLevel=5, healPerLevel=2, expPerLevel=4 },
    new EnemyDef { id=33, name="朽王蟲",   type=EnemyType.Normal,   pattern=EnemyAttackPattern.PanelCorrupt, hp=38, atk=8, interval=1, exp=18, appearBattle=25, hpPerLevel=7, healPerLevel=3, expPerLevel=4 },
 };

    // =========================================================
    // Tier 定義 — 中ボス用 Tier 追加
    //   戦闘10 = 中ボス（中盤プール上位）
    //   戦闘20 = 中ボス（終盤プール上位）
    //   戦闘25 = ラスボス固定
    // =========================================================

    private struct TierDef
    {
        public int startBattle;
        public string name;
        public int[] enemyIds;
        public int[] weights;
    }

    private static readonly TierDef[] AllTiers = new TierDef[]
    {
        new TierDef
        {
            startBattle = 1, name = "序盤",
            enemyIds = new[] { 1, 3, 4, 8, 10, 11, 12, 14 },
            weights  = new[] { 12, 10, 10, 8, 12, 8, 8, 8 }
        },
        new TierDef
        {
            startBattle = 8, name = "中盤",
            enemyIds = new[] { 2, 5, 9, 15, 17, 19, 21, 25, 26, 27, 13 },
            weights  = new[] { 10, 10, 8, 8, 10, 8, 10, 10, 8, 8, 6 }
        },
        // 戦闘10 = 中ボス（強めの敵を高確率で出す）
        new TierDef
        {
            startBattle = 10, name = "中ボス1",
            enemyIds = new[] { 21, 5, 13, 26 },
            weights  = new[] { 30, 20, 25, 25 }
        },
        // 戦闘11で通常中盤に戻す
        new TierDef
        {
            startBattle = 11, name = "中盤後半",
            enemyIds = new[] { 2, 5, 9, 15, 17, 19, 21, 25, 26, 27, 13 },
            weights  = new[] { 10, 10, 8, 8, 10, 8, 10, 10, 8, 8, 6 }
        },
        new TierDef
        {
            startBattle = 16, name = "終盤",
            enemyIds = new[] { 6, 7, 16, 18, 20, 22, 23, 24, 28, 29, 30, 31 },
            weights  = new[] { 8, 8, 8, 6, 8, 8, 8, 6, 8, 8, 6, 6 }
        },
        // 戦闘20 = 中ボス2（終盤上位を高確率）
        new TierDef
        {
            startBattle = 20, name = "中ボス2",
            enemyIds = new[] { 31, 30, 22, 7 },
            weights  = new[] { 30, 25, 25, 20 }
        },
        // 戦闘21で通常終盤に戻す
        new TierDef
        {
            startBattle = 21, name = "終盤後半",
            enemyIds = new[] { 6, 7, 16, 18, 20, 22, 23, 24, 28, 29, 30, 31 },
            weights  = new[] { 8, 8, 8, 6, 8, 8, 8, 6, 8, 8, 6, 6 }
        },
        new TierDef
        {
            startBattle = 24, name = "最終",
            enemyIds = new[] { 32, 33, 31, 30, 6 },
            weights  = new[] { 10, 15, 8, 6, 4 }
        },
    };

    [MenuItem("Tools/Battle/Generate All Enemy Data")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets", "Data");
        EnsureFolder("Assets/Data", "Enemies");

        Dictionary<int, BattleUnit> prefabMap = LoadAllEnemyPrefabs();
        Debug.Log($"[Generator] プレハブ {prefabMap.Count}体を検出 ({PrefabFolder})");

        Dictionary<int, EnemyStatData> dataMap = new Dictionary<int, EnemyStatData>();
        for (int i = 0; i < AllEnemies.Length; i++)
        {
            EnemyDef def = AllEnemies[i];
            string assetPath = $"{EnemyDataFolder}/EnemyData_{def.id:D3}_{def.name}.asset";

            EnemyStatData data = AssetDatabase.LoadAssetAtPath<EnemyStatData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyStatData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.enemyId = def.id; data.enemyName = def.name;
            data.enemyType = def.type; data.attackPattern = def.pattern;
            data.baseHP = def.hp; data.attackPower = def.atk;
            data.attackInterval = def.interval; data.expYield = def.exp;
            data.hpPerLevel = def.hpPerLevel; data.healPerLevel = def.healPerLevel;
            data.expPerLevel = def.expPerLevel;

            EditorUtility.SetDirty(data);
            dataMap[def.id] = data;
        }

        int appliedCount = 0;
        foreach (EnemyDef def in AllEnemies)
        {
            if (!prefabMap.ContainsKey(def.id)) continue;
            BattleUnit unit = prefabMap[def.id];
            unit.maxHP = def.hp; unit.attackPower = def.atk;
            unit.attackInterval = def.interval; unit.expYield = def.exp;
            unit.enemyType = def.type; unit.attackPattern = def.pattern;
            EditorUtility.SetDirty(unit.gameObject);
            appliedCount++;
        }

        StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(StageConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<StageConfig>();
            AssetDatabase.CreateAsset(config, StageConfigPath);
        }

        config.totalBattles = 25;
        config.tiers = new List<StageTier>();

        for (int t = 0; t < AllTiers.Length; t++)
        {
            TierDef tierDef = AllTiers[t];
            StageTier tier = new StageTier
            {
                startBattle = tierDef.startBattle,
                tierName = tierDef.name,
                entries = new List<StageTierEntry>()
            };

            for (int e = 0; e < tierDef.enemyIds.Length; e++)
            {
                int enemyId = tierDef.enemyIds[e];
                BattleUnit prefab = prefabMap.ContainsKey(enemyId) ? prefabMap[enemyId] : null;
                tier.entries.Add(new StageTierEntry { enemyPrefab = prefab, weight = tierDef.weights[e] });
            }

            config.tiers.Add(tier);
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("========================================");
        Debug.Log($"[Generator] 完了！ {appliedCount}体適用 / {AllTiers.Length} Tier（中ボス含む）");
        Debug.Log("========================================");
    }

    private static Dictionary<int, BattleUnit> LoadAllEnemyPrefabs()
    {
        Dictionary<int, BattleUnit> map = new Dictionary<int, BattleUnit>();
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) return map;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            BattleUnit unit = prefab.GetComponent<BattleUnit>();
            if (unit == null) continue;
            int id = ExtractId(prefab.name);
            if (id >= 1 && id <= 33 && !map.ContainsKey(id)) map[id] = unit;
        }
        return map;
    }

    private static int ExtractId(string name)
    {
        string digits = "";
        bool found = false;
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsDigit(name[i])) { digits += name[i]; found = true; }
            else if (found) break;
        }
        return int.TryParse(digits, out int r) ? r : -1;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }

    [MenuItem("Tools/Battle/Add EnemyTweenPresenter to All Enemies")]
    public static void AddTweenPresenterToAll()
    {
        Dictionary<int, BattleUnit> prefabMap = LoadAllEnemyPrefabs();
        int addedCount = 0, alreadyCount = 0;

        foreach (var kvp in prefabMap)
        {
            BattleUnit unit = kvp.Value;
            if (unit == null) continue;
            if (unit.GetComponent<EnemyTweenPresenter>() != null) { alreadyCount++; continue; }

            string prefabPath = AssetDatabase.GetAssetPath(unit.gameObject);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null) continue;

            EnemyTweenPresenter newPresenter = prefabRoot.AddComponent<EnemyTweenPresenter>();
            Transform unitRoot = prefabRoot.transform.Find("UnitRoot");
            if (unitRoot != null)
            {
                SerializedObject so = new SerializedObject(newPresenter);
                SerializedProperty prop = so.FindProperty("visualRoot");
                if (prop != null) { prop.objectReferenceValue = unitRoot; so.ApplyModifiedProperties(); }
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            addedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TweenPresenter] 追加:{addedCount}  既存:{alreadyCount}");
    }

    [MenuItem("Tools/Battle/Log All Enemy Stats")]
    public static void LogAllStats()
    {
        Debug.Log("=== 全33体 ===");
        for (int i = 0; i < AllEnemies.Length; i++)
        {
            EnemyDef e = AllEnemies[i];
            Debug.Log($"{e.id:D3} {e.name,-6} | {e.type,-9} | HP:{e.hp,2} ATK:{e.atk} INT:{e.interval} EXP:{e.exp,2} | {e.pattern,-13} | #{e.appearBattle}");
        }
    }
}
#endif