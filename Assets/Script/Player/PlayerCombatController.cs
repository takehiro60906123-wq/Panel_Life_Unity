using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Loadout")]
    public PlayerCombatLoadout loadout;

    [Header("近接レベル補正")]
    [SerializeField] private int levelAttackBonus = 0;

    [SerializeField] private GunDefinition defaultGunDefinition;

    private void Awake()
    {
        InitializeDefaultLoadout();
    }

    private void InitializeDefaultLoadout()
    {
        if (loadout == null)
        {
            loadout = new PlayerCombatLoadout();
        }

        if (loadout.meleeWeapon == null)
        {
            loadout.meleeWeapon = new WeaponData
            {
                weaponType = WeaponType.None,
                weaponName = "素手",
                maxLink = 3,
                baseAttack = 1
            };
        }

        // Default Gun Definition があれば最優先で使う
        if (defaultGunDefinition != null)
        {
            loadout.gun = defaultGunDefinition.ToGunData();
        }
        else if (loadout.gun == null)
        {
            // 保険: 定義が未設定ならショットガンを直指定
            loadout.gun = new GunData
            {
                gunType = GunType.Shotgun,
                gunName = "ショットガン",
                gaugeCost = 5,
                shotCount = 3,
                damagePerShot = 1,
                useAllGauge = false,
                minGaugeToFire = 5
            };
        }

        // ゲージ最大が未設定なら最低限補正
        if (loadout.maxGunGauge <= 0)
        {
            loadout.maxGunGauge = 10;
        }

        // 現在ゲージは範囲内に補正
        loadout.currentGunGauge = Mathf.Clamp(loadout.currentGunGauge, 0, loadout.maxGunGauge);
    }

    public int GetMaxLink()
    {
        if (loadout == null || loadout.meleeWeapon == null)
            return 3;

        return loadout.meleeWeapon.maxLink;
    }

    public int GetBaseAttack()
    {
        if (loadout == null || loadout.meleeWeapon == null)
            return 1;

        return loadout.meleeWeapon.baseAttack;
    }

    public int GetLevelAttackBonus()
    {
        return Mathf.Max(0, levelAttackBonus);
    }

    public int GetMeleeAttack()
    {
        return Mathf.Max(2, GetBaseAttack() + 1 + GetLevelAttackBonus());
    }

    public void AddLevelAttackBonus(int amount)
    {
        if (amount <= 0) return;
        levelAttackBonus += amount;
    }

    public int GetGunGauge()
    {
        if (loadout == null) return 0;
        return loadout.currentGunGauge;
    }

    public int GetGunGaugeMax()
    {
        if (loadout == null) return 10;
        return loadout.maxGunGauge;
    }

    public void AddGunGauge(int value)
    {
        if (loadout == null) return;
        loadout.AddGunGauge(value);
    }

    public bool CanUseGun()
    {
        if (loadout == null) return false;
        return loadout.CanUseGun();
    }

    public bool ConsumeGunGauge()
    {
        if (loadout == null) return false;
        return loadout.ConsumeGunGauge();
    }

    public bool CanUseMachineGun()
    {
        if (loadout == null || loadout.gun == null) return false;

        int minGauge = loadout.gun.minGaugeToFire > 0 ? loadout.gun.minGaugeToFire : 1;
        return loadout.currentGunGauge >= minGauge;
    }

    public int ConsumeAllGunGauge()
    {
        if (loadout == null) return 0;

        int consumed = loadout.currentGunGauge;
        loadout.currentGunGauge = 0;
        return consumed;
    }

    public string GetWeaponName()
    {
        if (loadout == null || loadout.meleeWeapon == null)
            return "素手";

        return loadout.meleeWeapon.weaponName;
    }

    public string GetGunName()
    {
        if (loadout == null || loadout.gun == null)
            return "なし";

        return loadout.gun.gunName;
    }

    public GunData GetGunData()
    {
        if (loadout == null) return null;
        return loadout.gun;
    }

    public void EquipSword()
    {
        loadout.meleeWeapon = new WeaponData
        {
            weaponType = WeaponType.Sword,
            weaponName = "剣",
            maxLink = 4,
            baseAttack = 1
        };
    }

    public void EquipGreatSword()
    {
        loadout.meleeWeapon = new WeaponData
        {
            weaponType = WeaponType.GreatSword,
            weaponName = "大剣",
            maxLink = 5,
            baseAttack = 1
        };
    }
}