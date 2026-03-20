using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    public System.Action<WeaponType> OnMeleeWeaponEquipped;
    public System.Action<GunType> OnGunEquipped;

    [Header("Loadout")]
    public PlayerCombatLoadout loadout;

    [Header("レベル補正")]
    [SerializeField] private int levelAttackBonus = 0;

    [Header("初期銃（提出版はピストル固定。未設定でもピストルを装備）")]
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

        loadout.gun = BuildInitialGunData();

        if (loadout.maxGunGauge <= 0)
        {
            loadout.maxGunGauge = 10;
        }

        loadout.currentGunGauge = Mathf.Clamp(loadout.currentGunGauge, 0, loadout.maxGunGauge);
        NormalizeEquippedGunData();
    }

    private GunData BuildInitialGunData()
    {
        if (defaultGunDefinition != null && defaultGunDefinition.gunType == GunType.Pistol)
        {
            return defaultGunDefinition.ToGunData();
        }

        return CreatePrototypeGunData(GunType.Pistol);
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

        NormalizeEquippedGunData();
        return loadout.gun;
    }

    public void EquipGun(GunData gunData, bool refillGauge = false)
    {
        if (loadout == null)
        {
            loadout = new PlayerCombatLoadout();
        }

        loadout.gun = CloneGunData(gunData);
        NormalizeEquippedGunData();

        if (refillGauge)
        {
            loadout.currentGunGauge = loadout.maxGunGauge;
        }
        else
        {
            loadout.currentGunGauge = Mathf.Clamp(loadout.currentGunGauge, 0, loadout.maxGunGauge);
        }

        if (loadout.gun != null)
        {
            OnGunEquipped?.Invoke(loadout.gun.gunType);
        }
    }

    public void EquipGunType(GunType gunType, bool refillGauge = false)
    {
        EquipGun(CreatePrototypeGunData(gunType), refillGauge);
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
                if (gun.shotInterval <= 0f) gun.shotInterval = 0.08f;
                if (gun.finishDelay <= 0f) gun.finishDelay = 0.22f;
                break;

            case GunType.Rifle:
                gun.gunName = string.IsNullOrEmpty(gun.gunName) ? "ライフル" : gun.gunName;
                gun.gaugeCost = 4;
                gun.minGaugeToFire = 4;
                gun.shotCount = 1;
                gun.damagePerShot = 5;
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.5f;
                if (gun.shotInterval <= 0f) gun.shotInterval = 0.08f;
                if (gun.finishDelay <= 0f) gun.finishDelay = 0.24f;
                break;

            case GunType.Shotgun:
                gun.gunName = string.IsNullOrEmpty(gun.gunName) ? "ショットガン" : gun.gunName;
                gun.gaugeCost = 5;
                gun.minGaugeToFire = 5;
                gun.shotCount = 3;
                gun.damagePerShot = 1;
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.2f;
                if (gun.shotInterval <= 0f) gun.shotInterval = 0.05f;
                if (gun.finishDelay <= 0f) gun.finishDelay = 0.26f;
                break;

            case GunType.MachineGun:
                gun.gunName = string.IsNullOrEmpty(gun.gunName) ? "マシンガン" : gun.gunName;
                gun.gaugeCost = 0;
                gun.minGaugeToFire = 3;
                gun.shotCount = 1;
                gun.damagePerShot = 1;
                gun.useAllGauge = true;
                if (gun.scalingRate <= 0f) gun.scalingRate = 0.1f;
                if (gun.shotInterval <= 0f) gun.shotInterval = 0.04f;
                if (gun.finishDelay <= 0f) gun.finishDelay = 0.28f;
                break;
        }
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

        OnMeleeWeaponEquipped?.Invoke(loadout.meleeWeapon.weaponType);
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

        OnMeleeWeaponEquipped?.Invoke(loadout.meleeWeapon.weaponType);
    }

    public static GunData CreatePrototypeGunData(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                return new GunData
                {
                    gunType = GunType.Pistol,
                    gunName = "ピストル",
                    gaugeCost = 2,
                    shotCount = 2,
                    damagePerShot = 2,
                    scalingRate = 0.15f,
                    useAllGauge = false,
                    minGaugeToFire = 2,
                    shotInterval = 0.08f,
                    finishDelay = 0.22f,
                };
            case GunType.MachineGun:
                return new GunData
                {
                    gunType = GunType.MachineGun,
                    gunName = "マシンガン",
                    gaugeCost = 0,
                    shotCount = 1,
                    damagePerShot = 1,
                    scalingRate = 0.1f,
                    useAllGauge = true,
                    minGaugeToFire = 3,
                    shotInterval = 0.04f,
                    finishDelay = 0.28f,
                };
            case GunType.Shotgun:
                return new GunData
                {
                    gunType = GunType.Shotgun,
                    gunName = "ショットガン",
                    gaugeCost = 5,
                    shotCount = 3,
                    damagePerShot = 1,
                    scalingRate = 0.2f,
                    useAllGauge = false,
                    minGaugeToFire = 5,
                    shotInterval = 0.05f,
                    finishDelay = 0.26f,
                };
            case GunType.Rifle:
                return new GunData
                {
                    gunType = GunType.Rifle,
                    gunName = "ライフル",
                    gaugeCost = 4,
                    shotCount = 1,
                    damagePerShot = 5,
                    scalingRate = 0.5f,
                    useAllGauge = false,
                    minGaugeToFire = 4,
                    shotInterval = 0.08f,
                    finishDelay = 0.24f,
                };
            default:
                return null;
        }
    }

    private static GunData CloneGunData(GunData source)
    {
        if (source == null) return null;

        return new GunData
        {
            gunType = source.gunType,
            gunName = source.gunName,
            gaugeCost = source.gaugeCost,
            shotCount = source.shotCount,
            damagePerShot = source.damagePerShot,
            scalingRate = source.scalingRate,
            useAllGauge = source.useAllGauge,
            minGaugeToFire = source.minGaugeToFire,
            shotInterval = source.shotInterval,
            finishDelay = source.finishDelay,
        };
    }
}
