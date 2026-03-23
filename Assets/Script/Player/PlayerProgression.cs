using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;

    [SerializeField] private PlayerCombatController playerCombatController;

    [SerializeField]
    private int[] expTable = { 0, 8, 18, 32, 50, 72, 98, 128, 162, 200 };

    [Header("レベルアップ調整")]
    [SerializeField, Range(0.85f, 1.10f)] private float expRequirementScale = 0.96f;
    [SerializeField] private int levelUpMaxHpGain = 2;
    [SerializeField] private int levelUpHealFlat = 4;
    [SerializeField, Range(0f, 0.25f)] private float levelUpHealPercentOfMaxHp = 0.08f;

    public int Level => level;
    public int CurrentExp => currentExp;

    private void Awake()
    {
        // 旧デフォルト値を使っている既存シーンの自動移行
        if (levelUpHealFlat == 2)
        {
            levelUpHealFlat = 4;
        }

        if (playerCombatController == null)
        {
            playerCombatController = FindObjectOfType<PlayerCombatController>();
        }
    }

    public void Initialize(int startLevel, int startExp)
    {
        level = Mathf.Max(1, startLevel);
        currentExp = Mathf.Max(0, startExp);
    }

    public bool AddExp(int amount, BattleUnit unit)
    {
        Debug.Log($"[PlayerProgression] AddExp amount={amount}  before={currentExp}  level={level}");
        if (amount <= 0) return false;

        currentExp += amount;
        bool leveledUp = false;

        while (level < expTable.Length && currentExp >= GetScaledRequiredExp(level))
        {
            level++;

            // V: HP+2AHP+2
            if (unit != null)
            {
                unit.IncreaseMaxHP(levelUpMaxHpGain, false);

                int healAmount = Mathf.Max(
                    levelUpHealFlat,
                    Mathf.CeilToInt(unit.maxHP * levelUpHealPercentOfMaxHp));

                unit.Heal(healAmount);
            }

            // 2xƂɋߐڍU+1
            // Lv1-2: 2_, Lv3-4: 3_, Lv5-6: 4_...
            if (level % 2 == 1)
            {
                playerCombatController?.AddLevelAttackBonus(1);
            }

            Debug.Log($"x{level}ɂȂBőHP+2AHP+2");
            leveledUp = true;
        }
        Debug.Log($"[PlayerProgression] after currentExp={currentExp}  level={level}  progress={GetExpProgress01()}");
        return leveledUp;
    }

    public float GetExpProgress01()
    {
        if (expTable == null || expTable.Length == 0)
            return 0f;

        if (level >= expTable.Length)
            return 1f;

        int prevRequired = level <= 1 ? 0 : GetScaledRequiredExp(level - 1);
        int nextRequired = GetScaledRequiredExp(level);

        int range = nextRequired - prevRequired;
        if (range <= 0) return 1f;

        return Mathf.Clamp01((currentExp - prevRequired) / (float)range);
    }

    private int GetScaledRequiredExp(int tableIndex)
    {
        if (expTable == null || expTable.Length == 0)
            return 0;

        tableIndex = Mathf.Clamp(tableIndex, 0, expTable.Length - 1);

        int raw = expTable[tableIndex];
        return Mathf.Max(0, Mathf.RoundToInt(raw * expRequirementScale));
    }
}
