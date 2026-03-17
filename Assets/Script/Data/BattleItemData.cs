using UnityEngine;

public enum BattleItemType
{
    None,
    FieldBandage,
    ShockCanister,
    ActivationCell,
    MagneticCollectorCanister,
    AttackOil
}

public enum BattleItemUseTarget
{
    Self,
    Enemy
}

[System.Serializable]
public class BattleItemDropEntry
{
    public BattleItemType itemType = BattleItemType.None;
    public int weight = 1;
}

[System.Serializable]
public class BattleItemData
{
    public BattleItemType itemType;
    public string itemName;
    public BattleItemUseTarget useTarget;
    public int power;
    public int durationTurns;
    public Sprite icon;

    public bool IsValid()
    {
        return itemType != BattleItemType.None;
    }

    public static BattleItemData CreatePreset(BattleItemType itemType)
    {
        switch (itemType)
        {
            case BattleItemType.FieldBandage:
                return new BattleItemData
                {
                    itemType = BattleItemType.FieldBandage,
                    itemName = "野戦包帯",
                    useTarget = BattleItemUseTarget.Self,
                    power = 6,
                    durationTurns = 0
                };

            case BattleItemType.ShockCanister:
                return new BattleItemData
                {
                    itemType = BattleItemType.ShockCanister,
                    itemName = "衝撃筒",
                    useTarget = BattleItemUseTarget.Enemy,
                    power = 3,
                    durationTurns = 0
                };

            case BattleItemType.ActivationCell:
                return new BattleItemData
                {
                    itemType = BattleItemType.ActivationCell,
                    itemName = "起動セル",
                    useTarget = BattleItemUseTarget.Self,
                    power = 3,
                    durationTurns = 0
                };

            case BattleItemType.MagneticCollectorCanister:
                return new BattleItemData
                {
                    itemType = BattleItemType.MagneticCollectorCanister,
                    itemName = "磁気回収筒",
                    useTarget = BattleItemUseTarget.Self,
                    power = 18,
                    durationTurns = 0
                };

            case BattleItemType.AttackOil:
                return new BattleItemData
                {
                    itemType = BattleItemType.AttackOil,
                    itemName = "攻撃油",
                    useTarget = BattleItemUseTarget.Self,
                    power = 25,
                    durationTurns = 3
                };
        }

        return null;
    }
}
