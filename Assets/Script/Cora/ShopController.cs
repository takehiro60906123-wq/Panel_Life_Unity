using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// XRg[[
// iEwEύXǗB
//
// PanelBattleManager QƂA
// EncounterFlowController XɓƂ
// OpenShop() ĂԁB
// ============================================
public class ShopController : MonoBehaviour
{
    [Header("Xݒ")]
    [Tooltip("xɒ񂷂鏤i")]
    [SerializeField] private int offeringCount = 3;

    [Header("Ag")]
    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private BattleInventoryController battleInventoryController;
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private ShopUIController shopUIController;
    [SerializeField] private BattleItemIconDatabase battleItemIconDatabase;

    [Header("次帯に寄せる銃候補")]
    [SerializeField, Min(1)] private int projectedBattleLookAhead = 4;
    [SerializeField] private bool guaranteeOneFutureBiasedGunSlot = true;

    private StageFlowController stageFlowController;

    private List<ShopItemData> catalog;
    private List<ShopItemData> currentOfferings = new List<ShopItemData>();

    // RCQƗpR[obNiPanelBattleManager ݒj
    private Func<int> getCoins;
    private Action<int> addCoins;

    // XIR[obN
    private Action onShopClosed;

    [SerializeField] private GunDefinition pistolDefinition;
    [SerializeField] private GunDefinition machineGunDefinition;
    [SerializeField] private GunDefinition shotgunDefinition;
    [SerializeField] private GunDefinition rifleDefinition;

    private GunDefinition GetGunDefinition(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol: return pistolDefinition;
            case GunType.MachineGun: return machineGunDefinition;
            case GunType.Shotgun: return shotgunDefinition;
            case GunType.Rifle: return rifleDefinition;
            default: return null;
        }
    }

    private void Awake()
    {
        catalog = ShopItemData.CreateDefaultCatalog();
    }

    // ============================================
    // iPanelBattleManager / Bootstrapper Ăԁj
    // ============================================
    public void Initialize(
        PlayerCombatController playerCombatController,
        BattleInventoryController battleInventoryController,
        BattleUnit playerUnit,
        BattleUIController battleUIController,
        BattleItemIconDatabase battleItemIconDatabase,
        Func<int> getCoins,
        Action<int> addCoins)
    {
        this.playerCombatController = playerCombatController;
        this.battleInventoryController = battleInventoryController;
        this.playerUnit = playerUnit;
        this.battleUIController = battleUIController;
        this.battleItemIconDatabase = battleItemIconDatabase;
        this.getCoins = getCoins;
        this.addCoins = addCoins;
    }

    public void SetStageFlowController(StageFlowController controller)
    {
        stageFlowController = controller;
    }

    // ============================================
    // XJ
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
    // XiUÍuv{^Ă΂j
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
    // i_
    // ============================================
    private void GenerateOfferings()
    {
        currentOfferings.Clear();

        if (catalog == null || catalog.Count == 0) return;

        List<ShopItemData> eligible = BuildEligibleOfferings();
        if (eligible.Count == 0)
        {
            return;
        }

        ShopItemData guidedGun = null;
        if (guaranteeOneFutureBiasedGunSlot)
        {
            guidedGun = TryPickFutureBiasedGunOffering(eligible);
            if (guidedGun != null)
            {
                currentOfferings.Add(guidedGun);
                eligible.Remove(guidedGun);
            }
        }

        ShuffleItems(eligible);
        for (int i = 0; i < eligible.Count && currentOfferings.Count < offeringCount; i++)
        {
            currentOfferings.Add(eligible[i]);
        }

        ShuffleItems(currentOfferings);
    }

    private List<ShopItemData> BuildEligibleOfferings()
    {
        List<ShopItemData> eligible = new List<ShopItemData>();
        if (catalog == null)
        {
            return eligible;
        }

        List<ShopItemData> shuffled = new List<ShopItemData>(catalog);
        ShuffleItems(shuffled);

        for (int i = 0; i < shuffled.Count; i++)
        {
            ShopItemData item = shuffled[i];
            if (ShouldExcludeFromOffering(item)) continue;
            eligible.Add(item);
        }

        return eligible;
    }

    private ShopItemData TryPickFutureBiasedGunOffering(List<ShopItemData> eligible)
    {
        if (eligible == null || eligible.Count == 0) return null;
        if (stageFlowController == null) return null;

        Dictionary<EnemyType, float> projectedEnemyTypes = stageFlowController.GetProjectedEnemyTypeWeights(projectedBattleLookAhead);
        if (projectedEnemyTypes == null || projectedEnemyTypes.Count == 0)
        {
            return null;
        }

        List<ShopItemData> gunCandidates = new List<ShopItemData>();
        List<float> weights = new List<float>();

        for (int i = 0; i < eligible.Count; i++)
        {
            ShopItemData item = eligible[i];
            if (item == null || item.category != ShopItemCategory.Gun)
            {
                continue;
            }

            float affinity = GetProjectedGunAffinityScore(item.gunType, projectedEnemyTypes);
            float weight = 1f + Mathf.Max(0f, affinity);
            gunCandidates.Add(item);
            weights.Add(weight);
        }

        if (gunCandidates.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i];
        }

        if (totalWeight <= 0f)
        {
            return gunCandidates[UnityEngine.Random.Range(0, gunCandidates.Count)];
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < gunCandidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                return gunCandidates[i];
            }
        }

        return gunCandidates[gunCandidates.Count - 1];
    }

    private float GetProjectedGunAffinityScore(GunType gunType, Dictionary<EnemyType, float> projectedEnemyTypes)
    {
        float score = 0f;
        foreach (KeyValuePair<EnemyType, float> pair in projectedEnemyTypes)
        {
            score += pair.Value * GetGunAffinityAgainstEnemyType(gunType, pair.Key);
        }

        return score;
    }

    private float GetGunAffinityAgainstEnemyType(GunType gunType, EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Armored:
                switch (gunType)
                {
                    case GunType.Rifle: return 3.0f;
                    case GunType.Shotgun: return 2.2f;
                    case GunType.MachineGun: return 0.6f;
                    case GunType.Pistol: return 0.8f;
                }
                break;

            case EnemyType.Rushing:
                switch (gunType)
                {
                    case GunType.Shotgun: return 3.0f;
                    case GunType.Pistol: return 2.2f;
                    case GunType.MachineGun: return 1.2f;
                    case GunType.Rifle: return 1.0f;
                }
                break;

            case EnemyType.Ranged:
                switch (gunType)
                {
                    case GunType.Rifle: return 2.4f;
                    case GunType.Pistol: return 2.0f;
                    case GunType.MachineGun: return 1.0f;
                    case GunType.Shotgun: return 0.8f;
                }
                break;

            case EnemyType.Floating:
                switch (gunType)
                {
                    case GunType.Rifle: return 3.0f;
                    case GunType.Pistol: return 1.6f;
                    case GunType.MachineGun: return 1.0f;
                    case GunType.Shotgun: return 0.7f;
                }
                break;

            case EnemyType.Normal:
            default:
                switch (gunType)
                {
                    case GunType.MachineGun: return 1.4f;
                    case GunType.Pistol: return 1.2f;
                    case GunType.Shotgun: return 1.0f;
                    case GunType.Rifle: return 1.0f;
                }
                break;
        }

        return 1f;
    }

    private static void ShuffleItems<T>(List<T> items)
    {
        if (items == null) return;

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = items[i];
            items[i] = items[j];
            items[j] = temp;
        }
    }

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

        // HPSȂ񕜌nO
        if (item.category == ShopItemCategory.HealHP)
        {
            if (playerUnit != null && playerUnit.CurrentHP >= playerUnit.maxHP)
            {
                return true;
            }
        }

        // CxgtȂՕiO
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
    // w\ǂ̔
    // ============================================
    public bool CanPurchase(int offeringIndex)
    {
        if (offeringIndex < 0 || offeringIndex >= currentOfferings.Count) return false;

        ShopItemData item = currentOfferings[offeringIndex];
        if (item == null) return false;

        int coins = getCoins != null ? getCoins() : 0;
        if (coins < item.cost) return false;

        // ՕȉꍇACxgɋ󂫂邩
        if (item.category == ShopItemCategory.Consumable)
        {
            if (battleInventoryController == null || battleInventoryController.FreeSlots <= 0)
            {
                return false;
            }
        }

        // HP񕜂̏ꍇASłȂ
        if (item.category == ShopItemCategory.HealHP)
        {
            if (playerUnit == null) return false;
            if (playerUnit.CurrentHP >= playerUnit.maxHP) return false;
        }

        return true;
    }

    // ============================================
    // ws
    // ============================================
    public bool TryPurchase(int offeringIndex)
    {
        if (!CanPurchase(offeringIndex)) return false;

        ShopItemData item = currentOfferings[offeringIndex];

        // RC
        addCoins?.Invoke(-item.cost);

        // ʓKp
        ApplyPurchase(item);

        // wς݂̏i񂩂珜iSOLD OUTj
        currentOfferings[offeringIndex] = null;

        // UI XV
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
    // wʂ̓Kp
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

        GunDefinition def = GetGunDefinition(item.gunType);
        if (def != null)
        {
            playerCombatController.loadout.gun = def.ToGunData();
        }
        else
        {
            playerCombatController.loadout.gun = CreateGunData(item.gunType);
        }
    }

    private void ApplyConsumablePurchase(ShopItemData item)
    {
        if (battleInventoryController == null) return;

        BattleItemData battleItem = battleItemIconDatabase != null
            ? battleItemIconDatabase.CreatePreset(item.consumableType)
            : BattleItemData.CreatePreset(item.consumableType);

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
    // GunData ipn[hR[hj
    //  ScriptableObject / GunDataCatalog Ɉڍs
    // ============================================
    private GunData CreateGunData(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                return new GunData
                {
                    gunType = GunType.Pistol,
                    gunName = "sXg",
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
                    gunName = "}VK",
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
                    gunName = "VbgK",
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
                    gunName = "Ct",
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
    // OANZX
    // ============================================
    public IReadOnlyList<ShopItemData> CurrentOfferings => currentOfferings;
    public int GetCurrentCoins() => getCoins != null ? getCoins() : 0;
}