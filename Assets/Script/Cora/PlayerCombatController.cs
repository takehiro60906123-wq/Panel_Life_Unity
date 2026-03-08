using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Loadout")]
    public PlayerCombatLoadout loadout;

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
                weaponName = "‘fŽè",
                maxLink = 3,
                baseAttack = 1
            };
        }

        if (loadout.gun == null)
        {
            loadout.gun = new GunData
            {
                gunType = GunType.Pistol,
                gunName = "ƒsƒXƒgƒ‹",
                gaugeCost = 3,
                shotCount = 2,
                damagePerShot = 1,
                useAllGauge = false,
                minGaugeToFire = 3
            };
        }
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
            return "‘fŽè";

        return loadout.meleeWeapon.weaponName;
    }

    public string GetGunName()
    {
        if (loadout == null || loadout.gun == null)
            return "‚È‚µ";

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
            weaponName = "Œ•",
            maxLink = 4,
            baseAttack = 1
        };
    }

    public void EquipGreatSword()
    {
        loadout.meleeWeapon = new WeaponData
        {
            weaponType = WeaponType.GreatSword,
            weaponName = "‘åŒ•",
            maxLink = 5,
            baseAttack = 1
        };
    }
}