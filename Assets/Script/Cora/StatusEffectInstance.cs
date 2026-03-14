// =============================================================
// StatusEffectInstance.cs
// 状態異常インスタンス（1個分のデータ）
// Plain C# クラス。MonoBehaviour ではない。
// 配置先: Assets/Scripts/Battle/ など既存スクリプトと同階層
// =============================================================

/// <summary>
/// BattleUnit にかかっている状態異常1個分。
/// StatusEffectHolder の内部リストで保持される。
/// </summary>
[System.Serializable]
public class StatusEffectInstance
{
    /// <summary>状態異常の種類</summary>
    public StatusEffectType type;

    /// <summary>残りターン数。0以下になったら自動解除。</summary>
    public int remainingTurns;

    /// <summary>被弾時に解除されるか</summary>
    public bool removeOnDamage;

    /// <summary>
    /// 汎用数値（毒ダメージ量、鈍足割合など将来用）。
    /// 金縛りでは使わない。
    /// </summary>
    public int potency;

    public StatusEffectInstance(StatusEffectType type, int turns, bool removeOnDamage, int potency = 0)
    {
        this.type = type;
        this.remainingTurns = turns;
        this.removeOnDamage = removeOnDamage;
        this.potency = potency;
    }

    /// <summary>
    /// 残りターンを1減らす。0以下になったら true を返す（解除対象）。
    /// </summary>
    public bool TickAndCheckExpired()
    {
        remainingTurns--;
        return remainingTurns <= 0;
    }

    /// <summary>
    /// まだ有効か（remainingTurns が 1 以上）。
    /// </summary>
    public bool IsActive()
    {
        return remainingTurns > 0;
    }
}
