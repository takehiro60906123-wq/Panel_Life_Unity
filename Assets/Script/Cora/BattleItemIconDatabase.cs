using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BattleItemIconEntry
{
    public BattleItemType itemType = BattleItemType.None;
    public Sprite icon;
}

public class BattleItemIconDatabase : MonoBehaviour
{
    [SerializeField] private List<BattleItemIconEntry> entries = new List<BattleItemIconEntry>();

    private Dictionary<BattleItemType, Sprite> cachedLookup;

    private void Awake()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        cachedLookup = null;
    }

    public void RebuildCache()
    {
        cachedLookup = new Dictionary<BattleItemType, Sprite>();

        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            BattleItemIconEntry entry = entries[i];
            if (entry == null) continue;
            if (entry.itemType == BattleItemType.None) continue;

            cachedLookup[entry.itemType] = entry.icon;
        }
    }

    public Sprite GetIcon(BattleItemType itemType)
    {
        if (cachedLookup == null)
        {
            RebuildCache();
        }

        if (cachedLookup != null && cachedLookup.TryGetValue(itemType, out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    public BattleItemData ApplyIcon(BattleItemData item)
    {
        if (item == null)
        {
            return null;
        }

        item.icon = GetIcon(item.itemType);
        return item;
    }

    public BattleItemData CreatePreset(BattleItemType itemType)
    {
        BattleItemData item = BattleItemData.CreatePreset(itemType);
        return ApplyIcon(item);
    }
}
