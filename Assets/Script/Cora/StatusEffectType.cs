// =============================================================
// StatusEffectType.cs
// 状態異常の種類定義
// =============================================================

/// <summary>
/// 状態異常の種類。
/// </summary>
public enum StatusEffectType
{
    None = 0,

    /// <summary>
    /// 金縛り：Xターン行動不能。被弾で解除。
    /// </summary>
    Paralysis,

    /// <summary>
    /// 駆動遅延：行動テンポが悪化する。
    /// 敵にかかると、その敵ターンの cooldown 進行をスキップ。
    /// プレイヤーにかかると、プレイヤーターン終了時に敵の cooldown が追加で1進む。
    /// </summary>
    Slow,

    /// <summary>
    /// 腐食：Xターン被ダメージ増加。potency 分の追加ダメージ。
    /// </summary>
    Corrosion,
}
