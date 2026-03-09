using System.Collections.Generic;
using UnityEngine;

public class BattleInventoryController : MonoBehaviour
{
    [SerializeField] private int maxSlots = 8;
    [SerializeField] private List<BattleItemData> items = new List<BattleItemData>();

    public int MaxSlots => Mathf.Max(0, maxSlots);
    public int Count => items != null ? items.Count : 0;
    public int FreeSlots => Mathf.Max(0, MaxSlots - Count);
    public IReadOnlyList<BattleItemData> Items => items;

    private void Awake()
    {
        if (items == null)
        {
            items = new List<BattleItemData>();
        }

        RemoveInvalidItems();
    }

    public void RemoveInvalidItems()
    {
        if (items == null)
        {
            items = new List<BattleItemData>();
            return;
        }

        items.RemoveAll(item => item == null || !item.IsValid());

        if (items.Count > MaxSlots)
        {
            items.RemoveRange(MaxSlots, items.Count - MaxSlots);
        }
    }

    public bool TryAddItem(BattleItemData item)
    {
        if (item == null || !item.IsValid()) return false;
        if (items == null)
        {
            items = new List<BattleItemData>();
        }

        if (items.Count >= MaxSlots) return false;

        items.Add(item);
        return true;
    }

    public BattleItemData GetItemAt(int index)
    {
        if (items == null) return null;
        if (index < 0 || index >= items.Count) return null;
        return items[index];
    }

    public BattleItemData RemoveAt(int index)
    {
        if (items == null) return null;
        if (index < 0 || index >= items.Count) return null;

        BattleItemData item = items[index];
        items.RemoveAt(index);
        return item;
    }
}
