using UnityEngine;

[CreateAssetMenu(menuName = "Game/Gun Definition", fileName = "GunDefinition")]
public class GunDefinition : ScriptableObject
{
    public GunType gunType;
    public string gunName;

    [Header("使用条件")]
    public int gaugeCost = 3;
    public bool useAllGauge = false;
    public int minGaugeToFire = 1;

    [Header("威力")]
    public int shotCount = 1;
    public int damagePerShot = 1;

    [Header("テンポ")]
    public float shotInterval = 0.08f;
    public float finishDelay = 0.24f;

    public GunData ToGunData()
    {
        return new GunData
        {
            gunType = gunType,
            gunName = gunName,
            gaugeCost = gaugeCost,
            shotCount = shotCount,
            damagePerShot = damagePerShot,
            useAllGauge = useAllGauge,
            minGaugeToFire = minGaugeToFire
        };
    }
}