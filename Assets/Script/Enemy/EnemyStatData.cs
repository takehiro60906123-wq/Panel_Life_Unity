// =============================================================
// EnemyStatData.cs
// 敵の基本ステータスを定義する ScriptableObject
// 新規ファイル：Assets/Scripts/Battle/ などに配置
//
// 使い方：
//   Project ウィンドウ右クリック → Create → Battle → Enemy Stat Data
//   33体分を作成し、enemyId と各パラメータを設定する。
//   敵プレハブの BattleUnit に参照をセットするか、
//   StageFlowController でデータ駆動にする。
// =============================================================
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData_000", menuName = "Battle/Enemy Stat Data")]
public class EnemyStatData : ScriptableObject
{
    [Header("識別")]
    [Tooltip("素材番号に対応 (1〜33)")]
    public int enemyId;
    public string enemyName;

    [Header("種別")]
    public EnemyType enemyType = EnemyType.Normal;
    public EnemyAttackPattern attackPattern = EnemyAttackPattern.Normal;

    [Header("基本ステータス")]
    public int baseHP = 5;
    public int attackPower = 1;

    [Tooltip("攻撃間隔（ターン数）。1 = 毎ターン攻撃")]
    public int attackInterval = 2;

    [Tooltip("敵出現直後の初回行動までの猶予。-1 なら attackInterval と同じ")]
    public int initialAttackCooldown = -1;

    [Tooltip("外殻の最大HP。0なら外殻なし")]
    public int baseShellHp = 0;

    [Tooltip("撃破時の経験値")]
    public int expYield = 2;

    [Tooltip("撃破時のコイン")]
    public int coinYield = 3;

    [Header("モンスターパネルによるレベルアップ係数")]
    [Tooltip("パネル1連結あたりの最大HP増加量")]
    public int hpPerLevel = 2;

    [Tooltip("パネル1連結あたりのHP回復量")]
    public int healPerLevel = 1;

    [Tooltip("パネル1連結あたりの経験値ボーナス")]
    public int expPerLevel = 1;

    [Header("ビジュアル")]
    [Tooltip("UI やパネル表示用のポートレート")]
    public Sprite portrait;

    /// <summary>
    /// BattleUnit に基本ステータスを適用する。
    /// 敵生成時に呼び出すことで、データ駆動でステータスを設定できる。
    /// </summary>
    public void ApplyTo(BattleUnit unit)
    {
        if (unit == null) return;

        unit.maxHP = baseHP;
        unit.attackPower = attackPower;
        unit.attackInterval = attackInterval;
        unit.initialAttackCooldown = initialAttackCooldown;
        unit.SetMaxShell(baseShellHp, true);
        unit.expYield = expYield;
        unit.coinYield = coinYield;
        unit.enemyType = enemyType;
        unit.attackPattern = attackPattern;

        // HP を満タンにして UI を更新
        unit.Respawn();
    }
}
