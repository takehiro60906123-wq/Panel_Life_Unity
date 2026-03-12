using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// 商店コントローラー
// 品揃え生成・購入処理・装備変更を管理する。
//
// PanelBattleManager から参照し、
// EncounterFlowController が商店部屋に入ったときに
// OpenShop() を呼ぶ。
// ============================================
public class ShopController : MonoBehaviour
{
    [Header("商店設定")]
    [Tooltip("一度に陳列する商品数")]
    [SerializeField] private int offeringCount = 3;

    [Header("連携")]
    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private BattleInventoryController battleInventoryController;
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private ShopUIController shopUIController;

    private List<ShopItemData> catalog;
    private List<ShopItemData> currentOfferings = new List<ShopItemData>();

    // コイン参照用コールバック（PanelBattleManager から設定）
    private Func<int> getCoins;
    private Action<int> addCoins;

    // 商店終了時コールバック
    private Action onShopClosed;

    private void Awake()
    {
        catalog = ShopItemData.CreateDefaultCatalog();
    }

    // ============================================
    // 初期化（PanelBattleManager / Bootstrapper から呼ぶ）
    // ============================================
    public void Initialize(
        PlayerCombatController playerCombatController,
        BattleInventoryController battleInventoryController,
        BattleUnit playerUnit,
        BattleUIController battleUIController,
        Func<int> getCoins,
        Action<int> addCoins)
    {
        this.playerCombatController = playerCombatController;
        this.battleInventoryController = battleInventoryController;
        this.playerUnit = playerUnit;
        this.battleUIController = battleUIController;
        this.getCoins = getCoins;
        this.addCoins = addCoins;
    }

    // ============================================
    // 商店を開く
    // ============================================
    public void OpenShop(Action onClosed)
    {
        onShopClosed = onClosed;
        GenerateOfferings();

        if (shopUIController != null)
        {
            shopUIController.ShowShop(currentOfferings, this);
        }
    }

    // ============================================
    // 商店を閉じる（UIの「立ち去る」ボタンから呼ばれる）
    // ============================================
    public void CloseShop()
    {
        if (shopUIController != null)
        {
            shopUIController.HideShop();
        }

        onShopClosed?.Invoke();
        onShopClosed = null;
    }

    // ============================================
    // 品揃えをランダム生成
    // ============================================
    private void GenerateOfferings()
    {
        currentOfferings.Clear();

        if (catalog == null || catalog.Count == 0) return;

        // カタログをシャッフルして先頭から取る
        List<ShopItemData> shuffled = new List<ShopItemData>(catalog);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            ShopItemData temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        // 購入可能な候補をフィルタ（既に持っている武器/銃は除外）
        foreach (ShopItemData item in shuffled)
        {
            if (currentOfferings.Count >= offeringCount) break;
            if (ShouldExcludeFromOffering(item)) continue;

            currentOfferings.Add(item);
        }
    }

    // ============================================
    // 既に装備中の武器/銃は陳列しない
    // ============================================
    private bool ShouldExcludeFromOffering(ShopItemData item)
    {
        if (item == null) return true;
        if (playerCombatController == null) return false;

        if (item.category == ShopItemCategory.Weapon)
        {
            WeaponData current = playerCombatController.loadout?.meleeWeapon;
            if (current != null && current.weaponType == item.weaponType)
            {
                return true;
            }
        }

        if (item.category == ShopItemCategory.Gun)
        {
            GunData current = playerCombatController.GetGunData();
            if (current != null && current.gunType == item.gunType)
            {
                return true;
            }
        }

        // HP全快なら回復系を除外
        if (item.category == ShopItemCategory.HealHP)
        {
            if (playerUnit != null && playerUnit.CurrentHP >= playerUnit.maxHP)
            {
                return true;
            }
        }

        // インベントリ満杯なら消耗品を除外
        if (item.category == ShopItemCategory.Consumable)
        {
            if (battleInventoryController != null && battleInventoryController.FreeSlots <= 0)
            {
                return true;
            }
        }

        return false;
    }

    // ============================================
    // 購入可能かどうかの判定
    // ============================================
    public bool CanPurchase(int offeringIndex)
    {
        if (offeringIndex < 0 || offeringIndex >= currentOfferings.Count) return false;

        ShopItemData item = currentOfferings[offeringIndex];
        if (item == null) return false;

        int coins = getCoins != null ? getCoins() : 0;
        if (coins < item.cost) return false;

        // 消耗品の場合、インベントリに空きがあるか
        if (item.category == ShopItemCategory.Consumable)
        {
            if (battleInventoryController == null || battleInventoryController.FreeSlots <= 0)
            {
                return false;
            }
        }

        // HP回復の場合、全快でないか
        if (item.category == ShopItemCategory.HealHP)
        {
            if (playerUnit == null) return false;
            if (playerUnit.CurrentHP >= playerUnit.maxHP) return false;
        }

        return true;
    }

    // ============================================
    // 購入実行
    // ============================================
    public bool TryPurchase(int offeringIndex)
    {
        if (!CanPurchase(offeringIndex)) return false;

        ShopItemData item = currentOfferings[offeringIndex];

        // コイン消費
        addCoins?.Invoke(-item.cost);

        // 効果適用
        ApplyPurchase(item);

        // 購入済みの商品を陳列から除去（SOLD OUT）
        currentOfferings[offeringIndex] = null;

        // UI 更新
        if (shopUIController != null)
        {
            shopUIController.RefreshShop(currentOfferings, this);
        }

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
            battleUIController.RefreshInventoryUI();
        }

        return true;
    }

    // ============================================
    // 購入効果の適用
    // ============================================
    private void ApplyPurchase(ShopItemData item)
    {
        if (item == null) return;

        switch (item.category)
        {
            case ShopItemCategory.Weapon:
                ApplyWeaponPurchase(item);
                break;

            case ShopItemCategory.Gun:
                ApplyGunPurchase(item);
                break;

            case ShopItemCategory.Consumable:
                ApplyConsumablePurchase(item);
                break;

            case ShopItemCategory.HealHP:
                ApplyHealPurchase(item);
                break;
        }
    }

    private void ApplyWeaponPurchase(ShopItemData item)
    {
        if (playerCombatController == null) return;

        switch (item.weaponType)
        {
            case WeaponType.Sword:
                playerCombatController.EquipSword();
                break;
            case WeaponType.GreatSword:
                playerCombatController.EquipGreatSword();
                break;
        }
    }

    private void ApplyGunPurchase(ShopItemData item)
    {
        if (playerCombatController == null) return;
        if (playerCombatController.loadout == null) return;

        playerCombatController.loadout.gun = CreateGunData(item.gunType);
    }

    private void ApplyConsumablePurchase(ShopItemData item)
    {
        if (battleInventoryController == null) return;

        BattleItemData battleItem = BattleItemData.CreatePreset(item.consumableType);
        if (battleItem != null)
        {
            battleInventoryController.TryAddItem(battleItem);
        }
    }

    private void ApplyHealPurchase(ShopItemData item)
    {
        if (playerUnit == null) return;

        int heal = item.healAmount;
        if (heal >= 999)
        {
            heal = playerUnit.maxHP - playerUnit.CurrentHP;
        }

        playerUnit.Heal(heal);
    }

    // ============================================
    // GunData 生成（試作用ハードコード）
    // 将来は ScriptableObject / GunDataCatalog に移行
    // ============================================
    private GunData CreateGunData(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                return new GunData
                {
                    gunType = GunType.Pistol,
                    gunName = "ピストル",
                    gaugeCost = 3,
                    shotCount = 2,
                    damagePerShot = 2,
                    useAllGauge = false,
                    minGaugeToFire = 3
                };

            case GunType.MachineGun:
                return new GunData
                {
                    gunType = GunType.MachineGun,
                    gunName = "マシンガン",
                    gaugeCost = 0,
                    shotCount = 1,
                    damagePerShot = 1,
                    useAllGauge = true,
                    minGaugeToFire = 3
                };

            case GunType.Shotgun:
                return new GunData
                {
                    gunType = GunType.Shotgun,
                    gunName = "ショットガン",
                    gaugeCost = 5,
                    shotCount = 3,
                    damagePerShot = 2,
                    useAllGauge = false,
                    minGaugeToFire = 5
                };

            case GunType.Rifle:
                return new GunData
                {
                    gunType = GunType.Rifle,
                    gunName = "ライフル",
                    gaugeCost = 4,
                    shotCount = 1,
                    damagePerShot = 6,
                    useAllGauge = false,
                    minGaugeToFire = 4
                };
        }

        return null;
    }

    // ============================================
    // 外部アクセス
    // ============================================
    public IReadOnlyList<ShopItemData> CurrentOfferings => currentOfferings;
    public int GetCurrentCoins() => getCoins != null ? getCoins() : 0;
}