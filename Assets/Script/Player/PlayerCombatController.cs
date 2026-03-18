using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Loadout")]
    public PlayerCombatLoadout loadout;

    [Header("ߐڃx␳")]
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
                weaponName = "f",
                maxLink = 3,
                baseAttack = 1
            };
        }

        // Default Gun Definition ΍ŗDŎg
        if (defaultGunDefinition != null)
        {
            loadout.gun = defaultGunDefinition.ToGunData();
        }
        else if (loadout.gun == null)
        {
            // ی: `ݒȂVbgK𒼎w
            loadout.gun = new GunData
            {
                gunType = GunType.Shotgun,
                gunName = "VbgK",
                gaugeCost = 5,
                shotCount = 3,
                damagePerShot = 1,
                useAllGauge = false,
                minGaugeToFire = 5,
                shotInterval = 0.02f,
                finishDelay = 0.24f
            };
        }

        // Q[Wő傪ݒȂŒ␳
        if (loadout.maxGunGauge <= 0)
        {
            loadout.maxGunGauge = 10;
        }

        // ݃Q[W͔͈͓ɕ␳
        loadout.currentGunGauge = Mathf.Clamp(loadout.currentGunGauge, 0, loadout.maxGunGauge);

        NormalizeEquippedGunData();
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

    public bool CanUseCurrentGun()
    {
        GunData gun = GetGunData();
        if (gun == null) return false;

        return gun.useAllGauge
            ? CanUseMachineGun()
            : CanUseGun();
    }

    public bool TryConsumeEquippedGunForShotCount(out int shotCount)
    {
        shotCount = 0;

        GunData gun = GetGunData();
        if (gun == null) return false;

        if (gun.useAllGauge)
        {
            if (!CanUseMachineGun()) return false;

            shotCount = ConsumeAllGunGauge();
            return shotCount > 0;
        }

        if (!ConsumeGunGauge()) return false;

        shotCount = Mathf.Max(1, gun.shotCount);
        return true;
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
            return "f";

        return loadout.meleeWeapon.weaponName;
    }

    public string GetGunName()
    {
        if (loadout == null || loadout.gun == null)
            return "Ȃ";

        return loadout.gun.gunName;
    }

    public GunData GetGunData()
    {
        if (loadout == null) return null;

        NormalizeEquippedGunData();
        return loadout.gun;
    }

    private void NormalizeEquippedGunData()
    {
        if (loadout == null || loadout.gun == null) return;

        GunData gun = loadout.gun;
        switch (gun.gunType)
        {
            case GunType.Pistol:
                gun.gunName = string.IsNullOrEmpty(gun.gunName) ? "ピストル" : gun.gunName;
                gun.gaugeCost = 2;
                gun.minGaugeToFire = 2;
                gun.shotCount = 2;
                gun.damagePerShot = 2;
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.15f;
                break;

            case GunType.Rifle:
                gun.gunName = string.IsNullOrEmpty(gun.gunName) ? "ライフル" : gun.gunName;
                gun.gaugeCost = 4;
                gun.minGaugeToFire = 4;
                gun.shotCount = 1;
                gun.damagePerShot = 5;
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.5f;
                break;

            case GunType.Shotgun:
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.2f;
                break;

            case GunType.MachineGun:
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.1f;
                break;
        }
    }

    public void EquipSword()
    {
        loadout.meleeWeapon = new WeaponData
        {
            weaponType = WeaponType.Sword,
            weaponName = "",
            maxLink = 4,
            baseAttack = 1
        };
    }

    public void EquipGreatSword()
    {
        loadout.meleeWeapon = new WeaponData
        {
            weaponType = WeaponType.GreatSword,
            weaponName = "匕",
            maxLink = 5,
            baseAttack = 1
        };
    }
}