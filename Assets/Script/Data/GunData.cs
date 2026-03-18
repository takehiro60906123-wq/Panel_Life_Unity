using UnityEngine;

public enum GunType
{
    None,
    Pistol,
    MachineGun,
    Shotgun,
    Rifle
}

[System.Serializable]
public class GunData
{
    public GunType gunType;
    public string gunName;
    public int gaugeCost;
    public int shotCount;
    public int damagePerShot;
    [Tooltip("プレイヤー攻撃力の一部を銃ダメージへ加算する係数。0以下なら銃種ごとの既定値を使用")]
    public float scalingRate;
    public bool useAllGauge;
    public int minGaugeToFire;
    public float shotInterval;
    public float finishDelay;
}