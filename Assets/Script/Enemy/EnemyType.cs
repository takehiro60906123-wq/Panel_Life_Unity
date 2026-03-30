// =============================================================
// EnemyType.cs
// 敵カテゴリと攻撃パターンの定義
// 新規ファイル：Assets/Scripts/Battle/ などに配置
// =============================================================

/// <summary>
/// 敵の種別。銃や近接との相性に影響する。
/// </summary>
public enum EnemyType
{
    /// 通常の敵。特殊な耐性や弱点なし。
    Normal,

    /// 浮遊敵。近接ダメージ半減、銃が有効。
    Floating,

    /// 装甲敵。小ダメージ（1〜2）を軽減。大きい一撃が有効。
    Armored,

    /// 突撃敵。攻撃間隔が短い。ショットガンの遅延が刺さる。
    Rushing,

    /// 遠距離敵。放置が危険。ライフル・ピストルで優先処理。
    Ranged,

    /// 中ボス。演出はボス級だが逃走可能。
    MiniBoss,

    /// ボス敵。演出上の大物枠。逃走不可。ライフルやショットガンが有効候補。
    Boss
}

/// <summary>
/// EnemyType のヘルパー。ボス級判定を一元化する。
/// </summary>
public static class EnemyTypeHelper
{
    /// <summary>
    /// ボス級の敵かどうか（Boss + MiniBoss）。
    /// 演出・銃相性・スキャナー表示など、ボスと同格に扱いたい箇所で使う。
    /// 逃走判定には使わないこと（逃走不可は Boss のみ）。
    /// </summary>
    public static bool IsBossClass(this EnemyType type)
    {
        return type == EnemyType.Boss || type == EnemyType.MiniBoss;
    }
}

/// <summary>
/// 敵の攻撃パターン。将来の特殊攻撃差別化に使用。
/// </summary>
public enum EnemyAttackPattern
{
    /// 通常の単発攻撃（現在の処理そのまま）
    Normal,

    /// 低頻度だが高ダメージ
    HeavyHit,

    /// 複数回の弱い攻撃
    MultiHit,

    /// 盤面にモンスターパネルを追加生成
    PanelCorrupt,

    /// ダメージ＋継続ダメージ（毒・腐食）
    Poison,

    /// 自己強化（回復・加速）
    SelfBuff
}
