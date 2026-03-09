using UnityEngine;

public enum BattleItemType
{
    None,
    FieldBandage,
    ShockCanister,
    ActivationCell
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
                    power = 6
                };

            case BattleItemType.ShockCanister:
                return new BattleItemData
                {
                    itemType = BattleItemType.ShockCanister,
                    itemName = "衝撃筒",
                    useTarget = BattleItemUseTarget.Enemy,
                    power = 3
                };

            case BattleItemType.ActivationCell:
                return new BattleItemData
                {
                    itemType = BattleItemType.ActivationCell,
                    itemName = "起動セル",
                    useTarget = BattleItemUseTarget.Self,
                    power = 3
                };
        }

        return null;
    }
}
