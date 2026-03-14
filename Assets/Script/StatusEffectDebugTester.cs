using UnityEngine;

/// <summary>
/// 状態異常の手動テスト用。
/// シーン上の適当な GameObject に付け、playerUnit / enemyUnit を Inspector で割り当てて使う。
/// </summary>
public class StatusEffectDebugTester : MonoBehaviour
{
    [Header("対象")]
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUnit enemyUnit;

    [Header("キー設定")]
    [SerializeField] private KeyCode applyEnemyParalysisKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode applyPlayerParalysisKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode applyEnemyCorrosionKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode applyPlayerCorrosionKey = KeyCode.Alpha4;
    [SerializeField] private KeyCode applyEnemyComboKey = KeyCode.Alpha5;
    [SerializeField] private KeyCode applyEnemySlowKey = KeyCode.Alpha6;
    [SerializeField] private KeyCode applyPlayerSlowKey = KeyCode.Alpha7;
    [SerializeField] private KeyCode clearAllKey = KeyCode.Alpha0;

    [Header("金縛りテスト設定")]
    [SerializeField] private int enemyParalysisTurns = 3;
    [SerializeField] private int playerParalysisTurns = 2;

    [Header("腐食テスト設定")]
    [SerializeField] private int enemyCorrosionTurns = 3;
    [SerializeField] private int enemyCorrosionPotency = 2;
    [SerializeField] private int playerCorrosionTurns = 2;
    [SerializeField] private int playerCorrosionPotency = 1;

    [Header("駆動遅延テスト設定")]
    [SerializeField] private int enemySlowTurns = 1;
    [SerializeField] private int playerSlowTurns = 1;

    private void Update()
    {
        if (Input.GetKeyDown(applyEnemyParalysisKey))
        {
            ApplyParalysis(enemyUnit, enemyParalysisTurns, "敵");
        }

        if (Input.GetKeyDown(applyPlayerParalysisKey))
        {
            ApplyParalysis(playerUnit, playerParalysisTurns, "プレイヤー");
        }

        if (Input.GetKeyDown(applyEnemyCorrosionKey))
        {
            ApplyCorrosion(enemyUnit, enemyCorrosionTurns, enemyCorrosionPotency, "敵");
        }

        if (Input.GetKeyDown(applyPlayerCorrosionKey))
        {
            ApplyCorrosion(playerUnit, playerCorrosionTurns, playerCorrosionPotency, "プレイヤー");
        }

        if (Input.GetKeyDown(applyEnemyComboKey))
        {
            ApplyParalysis(enemyUnit, enemyParalysisTurns, "敵");
            ApplyCorrosion(enemyUnit, enemyCorrosionTurns, enemyCorrosionPotency, "敵");
            Debug.Log("[StatusEffectDebugTester] 敵に金縛り + 腐食を同時付与");
        }

        if (Input.GetKeyDown(applyEnemySlowKey))
        {
            ApplySlow(enemyUnit, enemySlowTurns, "敵");
        }

        if (Input.GetKeyDown(applyPlayerSlowKey))
        {
            ApplySlow(playerUnit, playerSlowTurns, "プレイヤー");
        }

        if (Input.GetKeyDown(clearAllKey))
        {
            ClearAll(playerUnit, "プレイヤー");
            ClearAll(enemyUnit, "敵");
        }
    }

    private void ApplyParalysis(BattleUnit unit, int turns, string label)
    {
        if (!ValidateUnit(unit, label)) return;

        unit.StatusEffects.ApplyEffect(
            StatusEffectType.Paralysis,
            turns,
            removeOnDamage: true,
            potency: 0);

        Debug.Log($"[StatusEffectDebugTester] {label}に金縛りを付与 ({turns}T)");
    }

    private void ApplyCorrosion(BattleUnit unit, int turns, int potency, string label)
    {
        if (!ValidateUnit(unit, label)) return;

        unit.StatusEffects.ApplyEffect(
            StatusEffectType.Corrosion,
            turns,
            removeOnDamage: false,
            potency: potency);

        Debug.Log($"[StatusEffectDebugTester] {label}に腐食を付与 ({turns}T, potency={potency})");
    }

    private void ApplySlow(BattleUnit unit, int turns, string label)
    {
        if (!ValidateUnit(unit, label)) return;

        unit.StatusEffects.ApplyEffect(
            StatusEffectType.Slow,
            turns,
            removeOnDamage: false,
            potency: 0);

        Debug.Log($"[StatusEffectDebugTester] {label}に駆動遅延を付与 ({turns}T)");
    }

    private void ClearAll(BattleUnit unit, string label)
    {
        if (!ValidateUnit(unit, label)) return;

        unit.StatusEffects.ClearAll();
        Debug.Log($"[StatusEffectDebugTester] {label}の状態異常を全解除");
    }

    private bool ValidateUnit(BattleUnit unit, string label)
    {
        if (unit == null)
        {
            Debug.LogWarning($"[StatusEffectDebugTester] {label} BattleUnit が未設定です");
            return false;
        }

        if (unit.StatusEffects == null)
        {
            Debug.LogWarning($"[StatusEffectDebugTester] {label} StatusEffects が取得できません");
            return false;
        }

        return true;
    }
}