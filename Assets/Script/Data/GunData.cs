using UnityEngine;



public enum GunType
{
    None,
    Pistol,
    MachineGun
}

[System.Serializable]
public class GunData
{
    public GunType gunType;
    public string gunName;
    public int gaugeCost;
    public int shotCount;
    public int damagePerShot;
    public bool useAllGauge;
    public int minGaugeToFire;
}