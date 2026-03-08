using UnityEngine;

[System.Serializable]
public class PlayerCombatLoadout
{
    public WeaponData meleeWeapon;
    public GunData gun;
    public int currentGunGauge;
    public int maxGunGauge = 10;

    public int MaxLink => meleeWeapon != null ? meleeWeapon.maxLink : 3;
    public int BaseAttack => meleeWeapon != null ? meleeWeapon.baseAttack : 1;

    public bool CanUseGun()
    {
        return gun != null && currentGunGauge >= gun.gaugeCost;
    }

    public void AddGunGauge(int value)
    {
        currentGunGauge = Mathf.Clamp(currentGunGauge + value, 0, maxGunGauge);
    }

    public bool ConsumeGunGauge()
    {
        if (!CanUseGun()) return false;
        currentGunGauge -= gun.gaugeCost;
        return true;
    }
}