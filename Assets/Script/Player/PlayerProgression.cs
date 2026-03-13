using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;

    [SerializeField] private PlayerCombatController playerCombatController;

    [SerializeField]
    private int[] expTable = { 0, 8, 18, 32, 50, 72, 98, 128, 162, 200 };

    public int Level => level;
    public int CurrentExp => currentExp;

    private void Awake()
    {
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

        while (level < expTable.Length && currentExp >= expTable[level])
        {
            level++;

            // シレン寄り: HP+2、現HPも+2
            if (unit != null)
            {
                unit.IncreaseMaxHP(2, false);
                unit.Heal(2);
            }

            // 2レベルごとに近接攻撃+1
            // Lv1-2: 2ダメ, Lv3-4: 3ダメ, Lv5-6: 4ダメ...
            if (level % 2 == 1)
            {
                playerCombatController?.AddLevelAttackBonus(1);
            }

            Debug.Log($"レベル{level}になった。最大HP+2、HP+2");
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

        int prevRequired = level <= 1 ? 0 : expTable[level - 1];
        int nextRequired = expTable[level];

        int range = nextRequired - prevRequired;
        if (range <= 0) return 1f;

        return Mathf.Clamp01((currentExp - prevRequired) / (float)range);
    }
}