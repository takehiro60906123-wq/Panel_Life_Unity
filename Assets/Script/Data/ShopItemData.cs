using System.Collections.Generic;
using UnityEngine;

// ============================================
// 商店で売っている商品のカテゴリ
// ============================================
public enum ShopItemCategory
{
    Weapon,
    Gun,
    Consumable,
    HealHP
}

// ============================================
// 商店で売っている商品1つの定義
// ============================================
[System.Serializable]
public class ShopItemData
{
    public string itemName;
    public string description;
    public int cost;
    public ShopItemCategory category;

    // --- Weapon の場合 ---
    public WeaponType weaponType;

    // --- Gun の場合 ---
    public GunType gunType;

    // --- Consumable の場合 ---
    public BattleItemType consumableType;

    // --- HealHP の場合 ---
    public int healAmount;

    // ============================================
    // 既定カタログ（ハードコード・試作用）
    // 将来は ScriptableObject 化する想定
    // ============================================
    public static List<ShopItemData> CreateDefaultCatalog()
    {
        return new List<ShopItemData>
        {
            // --- 武器 ---
            new ShopItemData
            {
                itemName = "剣",
                description = "リンク上限4。バランス型。",
                cost = 30,
                category = ShopItemCategory.Weapon,
                weaponType = WeaponType.Sword
            },
            new ShopItemData
            {
                itemName = "大剣",
                description = "リンク上限5。高リンクの一振り。",
                cost = 60,
                category = ShopItemCategory.Weapon,
                weaponType = WeaponType.GreatSword
            },

            // --- 銃 ---
            new ShopItemData
            {
                itemName = "ピストル",
                description = "コスト3。2連射。安定の保険。",
                cost = 20,
                category = ShopItemCategory.Gun,
                gunType = GunType.Pistol
            },
            new ShopItemData
            {
                itemName = "マシンガン",
                description = "ゲージ全消費で連射。手数で押す。",
                cost = 50,
                category = ShopItemCategory.Gun,
                gunType = GunType.MachineGun
            },
            new ShopItemData
            {
                itemName = "ショットガン",
                description = "コスト5。迎撃・遅延に強い。",
                cost = 45,
                category = ShopItemCategory.Gun,
                gunType = GunType.Shotgun
            },
            new ShopItemData
            {
                itemName = "ライフル",
                description = "コスト4。単発高火力の狙撃。",
                cost = 50,
                category = ShopItemCategory.Gun,
                gunType = GunType.Rifle
            },

            // --- 消耗品 ---
            new ShopItemData
            {
                itemName = "野戦包帯",
                description = "HP を 6 回復する消耗品。",
                cost = 15,
                category = ShopItemCategory.Consumable,
                consumableType = BattleItemType.FieldBandage
            },
            new ShopItemData
            {
                itemName = "衝撃筒",
                description = "敵に 3 ダメージを与える消耗品。",
                cost = 20,
                category = ShopItemCategory.Consumable,
                consumableType = BattleItemType.ShockCanister
            },
            new ShopItemData
            {
                itemName = "起動セル",
                description = "銃ゲージを 3 回復する消耗品。",
                cost = 15,
                category = ShopItemCategory.Consumable,
                consumableType = BattleItemType.ActivationCell
            },

            // --- HP回復 ---
            new ShopItemData
            {
                itemName = "応急修理",
                description = "その場で HP を 8 回復。",
                cost = 12,
                category = ShopItemCategory.HealHP,
                healAmount = 8
            },
            new ShopItemData
            {
                itemName = "全身修復",
                description = "その場で HP を全回復。",
                cost = 30,
                category = ShopItemCategory.HealHP,
                healAmount = 999
            }
        };
    }
}