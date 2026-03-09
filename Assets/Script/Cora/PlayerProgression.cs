using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;

    [SerializeField]
    private int[] expTable = { 0, 10, 30, 60, 100, 150, 220, 300, 400, 500 };

    public int Level => level;
    public int CurrentExp => currentExp;

    public void Initialize(int startLevel, int startExp)
    {
        level = Mathf.Max(1, startLevel);
        currentExp = Mathf.Max(0, startExp);
    }

    public bool AddExp(int amount, BattleUnit unit)
    {
        if (amount <= 0) return false;

        currentExp += amount;
        bool leveledUp = false;

        while (level < expTable.Length && currentExp >= expTable[level])
        {
            level++;

            int hpIncrease = Random.Range(4, 6);
            unit.IncreaseMaxHP(hpIncrease, true);

            Debug.Log($"レベルアップ！ Lv{level} になった！ 最大HPが {hpIncrease} 上がった！");
            leveledUp = true;
        }

        return leveledUp;
    }
}