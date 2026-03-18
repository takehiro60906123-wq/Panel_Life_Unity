using UnityEngine;

[CreateAssetMenu(menuName = "Game/Gun Definition", fileName = "GunDefinition")]
public class GunDefinition : ScriptableObject
{
    public GunType gunType;
    public string gunName;

    [Header("gp")]
    public int gaugeCost = 3;
    public bool useAllGauge = false;
    public int minGaugeToFire = 1;

    [Header("З")]
    public int shotCount = 1;
    public int damagePerShot = 1;
    [Tooltip("プレイヤー攻撃力の一部を銃ダメージへ加算する係数。0以下なら銃種ごとの既定値を使用")]
    public float scalingRate = 0f;

    [Header("e|")]
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
            scalingRate = scalingRate,
            useAllGauge = useAllGauge,
            minGaugeToFire = minGaugeToFire,
            shotInterval = shotInterval,
            finishDelay = finishDelay
        };
    }
}