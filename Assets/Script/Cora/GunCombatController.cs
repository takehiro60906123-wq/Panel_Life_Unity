using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class GunCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private BattleDamageResolver battleDamageResolver;
    [SerializeField] private BattleEventHub battleEventHub;
    [SerializeField] private BattleUIController battleUIController;
    [SerializeField] private PanelBoardController panelBoardController;
    [SerializeField] private BattleUnit playerUnit;

    [Header("Effect Prefabs")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject hitEffectPrefab;

    [Header("Gun Rules")]
    [SerializeField] private int shotgunDangerBonusDamage = 2;
    [SerializeField] private int shotgunDelayChance = 30;

    [Header("Gun Scaling")]
    [SerializeField] private float pistolScalingRate = 0.15f;
    [SerializeField] private float machineGunScalingRate = 0.10f;
    [SerializeField] private float shotgunScalingRate = 0.20f;
    [SerializeField] private float rifleScalingRate = 0.50f;

    [Header("Shotgun Corrosion")]
    [SerializeField] private bool shotgunAppliesCorrosion = true;
    [SerializeField, Range(0, 100)] private int shotgunCorrosionChance = 100;
    [SerializeField] private int shotgunCorrosionTurns = 1;
    [SerializeField] private int shotgunCorrosionPotency = 1;

    [Header("Precision Bonus")]
    [SerializeField] private int pistolDangerBonusDamage = 1;
    [SerializeField] private int rifleDangerBonusDamage = 2;
    [SerializeField] private int pistolDangerDelayAmount = 0;

    [Header("Machine Gun Burst Bonus")]
    [SerializeField] private bool machineGunUsesBurstBonus = true;
    [SerializeField, Min(2)] private int machineGunBonusEveryNthShot = 3;
    [SerializeField] private int machineGunBurstBonusDamage = 1;

    [Header("Shot Timings")]
    [SerializeField] private float defaultShotInterval = 0.08f;
    [SerializeField] private float defaultFinishDelay = 0.24f;
    [SerializeField] private float shotgunIntervalOverride = 0.02f;

    [Header("Muzzle Scale")]
    [SerializeField] private float pistolMuzzleScale = 1.00f;
    [SerializeField] private float machineGunMuzzleScale = 0.82f;
    [SerializeField] private float shotgunMuzzleScale = 1.35f;
    [SerializeField] private float rifleMuzzleScale = 1.12f;

    [Header("Hit Scale")]
    [SerializeField] private float pistolHitScale = 1.00f;
    [SerializeField] private float machineGunHitScale = 0.78f;
    [SerializeField] private float shotgunHitScale = 1.18f;
    [SerializeField] private float rifleHitScale = 1.36f;

    [Header("Scatter")]
    [SerializeField] private float machineGunHitScatter = 0.16f;
    [SerializeField] private float shotgunHitScatter = 0.28f;

    [Header("Shotgun Visual")]
    [SerializeField] private int shotgunPelletVisualCount = 7;
    [SerializeField] private float shotgunSpreadX = 0.60f;
    [SerializeField] private float shotgunSpreadY = 0.22f;
    [SerializeField] private float shotgunPelletHitScale = 0.48f;
    [SerializeField] private float shotgunCenterHitScale = 1.15f;
    [SerializeField] private float shotgunPelletTracerWidth = 0.035f;
    [SerializeField] private float shotgunPelletTracerDuration = 0.05f;
    [SerializeField] private Color shotgunTracerColor = new Color(1f, 0.95f, 0.82f, 0.85f);

    [Header("Rifle Visual")]
    [SerializeField] private Color rifleTracerColor = new Color(1f, 0.96f, 0.84f, 0.95f);
    [SerializeField] private float rifleTracerWidth = 0.08f;
    [SerializeField] private float rifleTracerDuration = 0.06f;

    [Header("Shell Eject Timing")]
    [SerializeField] private float pistolShellDelay = 0.02f;
    [SerializeField] private float machineGunShellDelay = 0.015f;
    [SerializeField] private float shotgunShellDelay = 0.04f;
    [SerializeField] private float rifleShellDelay = 0.025f;

    private Func<BattleUnit> getEnemyUnit;
    private Func<bool> getIsPlayerTurn;
    private Func<int, int> applyPlayerDamageModifiers;
    private Func<IEnumerator> endPlayerTurnRoutineFactory;
    private Material gunTracerMaterial;
    private BattleSfxController battleSfxController;

    public void Initialize(
        PlayerCombatController playerCombatController,
        BattleDamageResolver battleDamageResolver,
        BattleEventHub battleEventHub,
        BattleUIController battleUIController,
        PanelBoardController panelBoardController,
        BattleUnit playerUnit,
        GameObject muzzleFlashPrefab,
        GameObject hitEffectPrefab,
        Func<BattleUnit> getEnemyUnit,
        Func<bool> getIsPlayerTurn,
        Func<int, int> applyPlayerDamageModifiers,
        Func<IEnumerator> endPlayerTurnRoutineFactory)
    {
        this.playerCombatController = playerCombatController;
        this.battleDamageResolver = battleDamageResolver;
        this.battleEventHub = battleEventHub;
        this.battleUIController = battleUIController;
        this.panelBoardController = panelBoardController;
        this.playerUnit = playerUnit;
        this.muzzleFlashPrefab = muzzleFlashPrefab;
        this.hitEffectPrefab = hitEffectPrefab;
        this.getEnemyUnit = getEnemyUnit;
        this.getIsPlayerTurn = getIsPlayerTurn;
        this.applyPlayerDamageModifiers = applyPlayerDamageModifiers;
        this.endPlayerTurnRoutineFactory = endPlayerTurnRoutineFactory;
        battleSfxController = FindObjectOfType<BattleSfxController>();
    }

    public void FireEquippedGun()
    {
        GunData gun = playerCombatController != null ? playerCombatController.GetGunData() : null;
        if (gun == null) return;

        Fire(gun.gunType);
    }

    public void Fire(GunType requiredGunType)
    {
        if (!TryPrepareGunAction(out GunData gun, out BattleUnit target, requiredGunType)) return;
        if (playerCombatController == null) return;
        if (!playerCombatController.TryConsumeEquippedGunForShotCount(out int shotCount)) return;

        StartCoroutine(FireRoutine(gun, target, shotCount));
    }

    private bool TryPrepareGunAction(out GunData gun, out BattleUnit target, GunType requiredGunType)
    {
        gun = null;
        target = null;

        if (getIsPlayerTurn != null && !getIsPlayerTurn()) return false;
        if (playerCombatController == null) return false;

        gun = playerCombatController.GetGunData();
        if (gun == null) return false;
        if (requiredGunType != GunType.None && gun.gunType != requiredGunType) return false;

        target = getEnemyUnit != null ? getEnemyUnit() : null;
        if (target == null) return false;
        if (target.IsDead()) return false;

        return true;
    }

    private IEnumerator FireRoutine(GunData gun, BattleUnit target, int shotCount)
    {
        if (gun == null || target == null) yield break;

        if (gun.gunType == GunType.Shotgun && panelBoardController != null)
        {
            panelBoardController.PlayImpactShake(1.15f, 0.10f);
        }

        string logMessage = gun.useAllGauge
            ? $"{gun.gunName}発射: {shotCount}連射"
            : $"{gun.gunName}発射";

        yield return StartCoroutine(
            ExecuteGunRoutine(
                gun,
                target,
                shotCount,
                ResolveShotInterval(gun),
                logMessage,
                ResolveFinishDelay(gun)));
    }

    private float ResolveShotInterval(GunData gun)
    {
        if (gun == null) return defaultShotInterval;

        float configured = gun.shotInterval > 0f ? gun.shotInterval : defaultShotInterval;
        if (gun.gunType == GunType.Shotgun && shotgunIntervalOverride > 0f)
        {
            return shotgunIntervalOverride;
        }

        return configured;
    }

    private float ResolveFinishDelay(GunData gun)
    {
        if (gun == null) return defaultFinishDelay;
        return gun.finishDelay > 0f ? gun.finishDelay : defaultFinishDelay;
    }

    private IEnumerator ExecuteGunRoutine(
        GunData gun,
        BattleUnit target,
        int shotCount,
        float interval,
        string logMessage,
        float finishDelay)
    {
        if (gun == null || target == null || target.IsDead()) yield break;

        PlayerAnimationPresenter playerAnim = GetPlayerAnimationPresenter();
        if (playerAnim != null)
        {
            playerAnim.PlayRunShoot();
        }

        bool precisionTriggered = HasDangerPrecisionBonus(gun, target);
        int damagePerShot = ResolveGunHitDamage(gun, target);
        bool queuedSuccessfulHitStatusEffect = QueueGunStatusEffectOnNextSuccessfulHit(gun, target);

        if (shotCount <= 1)
        {
            int resolvedDamage = ResolveShotDamageForIndex(gun, damagePerShot, 0);
            ExecuteGunHit(gun, target, resolvedDamage, 0, precisionTriggered);
        }
        else
        {
            yield return StartCoroutine(
                ExecuteRepeatedGunHitsRoutine(gun, target, shotCount, damagePerShot, interval, precisionTriggered));
        }

        if (queuedSuccessfulHitStatusEffect && battleDamageResolver != null)
        {
            battleDamageResolver.ClearQueuedSuccessfulEnemyHitStatusEffect();
        }

        ApplyGunAfterEffects(gun, target, precisionTriggered);

        yield return StartCoroutine(FinishGunActionRoutine(logMessage, finishDelay));
    }

    private IEnumerator ExecuteRepeatedGunHitsRoutine(
        GunData gun,
        BattleUnit target,
        int shotCount,
        int damagePerShot,
        float interval,
        bool precisionTriggered)
    {
        int safeShotCount = Mathf.Max(1, shotCount);
        float safeInterval = Mathf.Max(0.01f, interval);

        for (int i = 0; i < safeShotCount; i++)
        {
            if (target == null || target.IsDead())
            {
                yield break;
            }

            int resolvedDamage = ResolveShotDamageForIndex(gun, damagePerShot, i);
            ExecuteGunHit(gun, target, resolvedDamage, i, precisionTriggered && i == 0);

            if (i < safeShotCount - 1)
            {
                yield return new WaitForSeconds(safeInterval);
            }
        }
    }

    private void ExecuteGunHit(GunData gun, BattleUnit target, int damage, int shotIndex, bool showPrecisionText)
    {
        if (gun == null || target == null || target.IsDead()) return;

        if (showPrecisionText)
        {
            battleEventHub?.RaiseDamageTextRequested("PRECISION", target.transform.position + Vector3.up * 1.55f, ResolvePrecisionTextColor(gun));
        }

        PlayGunShotVisual(gun, target);

        if (ShouldEjectShell(gun, shotIndex) && battleUIController != null)
        {
            battleUIController.PlayGunShellEject(gun.gunType, ResolveShellDelay(gun));
        }

        if (battleDamageResolver != null)
        {
            battleDamageResolver.SetNextDamageIsGun(true);
            battleDamageResolver.SetNextDamageUseHeavyReaction(gun.gunType == GunType.Shotgun);
        }

        battleSfxController?.PlayGunFire(gun.gunType);
        battleEventHub?.RaiseEnemyDamageRequested(damage);
    }

    private bool ShouldEjectShell(GunData gun, int shotIndex)
    {
        if (gun == null) return false;

        switch (gun.gunType)
        {
            case GunType.MachineGun:
                return shotIndex % 2 == 0;

            case GunType.Shotgun:
                return shotIndex == 0;

            case GunType.Rifle:
            case GunType.Pistol:
            default:
                return true;
        }
    }

    private float ResolveShellDelay(GunData gun)
    {
        if (gun == null) return pistolShellDelay;

        switch (gun.gunType)
        {
            case GunType.MachineGun:
                return machineGunShellDelay;
            case GunType.Shotgun:
                return shotgunShellDelay;
            case GunType.Rifle:
                return rifleShellDelay;
            case GunType.Pistol:
            default:
                return pistolShellDelay;
        }
    }

    private int ResolveGunHitDamage(GunData gun, BattleUnit target)
    {
        if (gun == null) return 0;

        int damage = Mathf.Max(0, gun.damagePerShot);
        damage += ResolveGunScalingBonus(gun);

        if (gun.gunType == GunType.Shotgun && target != null && target.IsDangerEnemy())
        {
            damage += shotgunDangerBonusDamage;
        }

        if (gun.gunType == GunType.Pistol && HasDangerPrecisionBonus(gun, target))
        {
            damage += Mathf.Max(0, pistolDangerBonusDamage);
        }

        if (gun.gunType == GunType.Rifle && HasDangerPrecisionBonus(gun, target))
        {
            damage += Mathf.Max(0, rifleDangerBonusDamage);
        }

        if (applyPlayerDamageModifiers != null)
        {
            damage = applyPlayerDamageModifiers(damage);
        }

        return damage;
    }

    private int ResolveGunScalingBonus(GunData gun)
    {
        if (gun == null || playerCombatController == null) return 0;

        float scalingRate = gun.scalingRate > 0f
            ? gun.scalingRate
            : ResolveDefaultScalingRate(gun.gunType);

        if (scalingRate <= 0f) return 0;

        int playerAttack = Mathf.Max(0, playerCombatController.GetMeleeAttack());
        return Mathf.Max(0, Mathf.FloorToInt(playerAttack * scalingRate));
    }

    private float ResolveDefaultScalingRate(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                return Mathf.Max(0f, pistolScalingRate);
            case GunType.MachineGun:
                return Mathf.Max(0f, machineGunScalingRate);
            case GunType.Shotgun:
                return Mathf.Max(0f, shotgunScalingRate);
            case GunType.Rifle:
                return Mathf.Max(0f, rifleScalingRate);
            default:
                return 0f;
        }
    }

    private bool HasDangerPrecisionBonus(GunData gun, BattleUnit target)
    {
        if (gun == null || target == null) return false;
        if (!target.IsDangerEnemy()) return false;

        return gun.gunType == GunType.Pistol || gun.gunType == GunType.Rifle;
    }

    private Color ResolvePrecisionTextColor(GunData gun)
    {
        switch (gun != null ? gun.gunType : GunType.None)
        {
            case GunType.Rifle:
                return new Color(1f, 0.88f, 0.35f, 1f);
            case GunType.Pistol:
                return new Color(0.55f, 0.92f, 1f, 1f);
            default:
                return Color.white;
        }
    }

    private int ResolveShotDamageForIndex(GunData gun, int baseDamage, int shotIndex)
    {
        if (gun == null)
        {
            return Mathf.Max(0, baseDamage);
        }

        int damage = Mathf.Max(0, baseDamage);

        if (gun.gunType == GunType.MachineGun && IsMachineGunBurstBonusShot(shotIndex))
        {
            damage += Mathf.Max(0, machineGunBurstBonusDamage);
        }

        return damage;
    }

    private bool IsMachineGunBurstBonusShot(int shotIndex)
    {
        if (!machineGunUsesBurstBonus) return false;
        if (machineGunBurstBonusDamage <= 0) return false;

        int everyNth = Mathf.Max(2, machineGunBonusEveryNthShot);
        int oneBasedIndex = shotIndex + 1;
        return oneBasedIndex % everyNth == 0;
    }

    private bool QueueGunStatusEffectOnNextSuccessfulHit(GunData gun, BattleUnit target)
    {
        if (gun == null) return false;
        if (battleDamageResolver == null) return false;

        switch (gun.gunType)
        {
            case GunType.Shotgun:
                return QueueShotgunCorrosionOnNextSuccessfulHit();

            default:
                return false;
        }
    }

    private bool QueueShotgunCorrosionOnNextSuccessfulHit()
    {
        if (!shotgunAppliesCorrosion) return false;
        if (shotgunCorrosionTurns <= 0) return false;
        if (battleDamageResolver == null) return false;

        battleDamageResolver.QueueNextSuccessfulEnemyHitStatusEffect(
            StatusEffectType.Corrosion,
            shotgunCorrosionTurns,
            removeOnDamage: false,
            potency: shotgunCorrosionPotency,
            chancePercent: shotgunCorrosionChance);
        return true;
    }

    private void ApplyGunAfterEffects(GunData gun, BattleUnit target, bool precisionTriggered)
    {
        if (gun == null || target == null || target.IsDead()) return;

        if (gun.gunType == GunType.Shotgun)
        {
            TryDelayEnemyTurnByShotgun(target);
        }
    }

    private void TryDelayEnemyTurnByShotgun(BattleUnit target)
    {
        if (target == null || target.IsDead()) return;
        if (UnityEngine.Random.Range(0, 100) >= shotgunDelayChance) return;

        target.DelayCooldown(1);
        battleEventHub?.RaiseDamageTextRequested("STAGGER", target.transform.position + Vector3.up * 1.5f, Color.yellow);
    }

    private void TryDelayEnemyTurnByPistol(BattleUnit target)
    {
        if (target == null || target.IsDead()) return;
        if (pistolDangerDelayAmount <= 0) return;

        target.DelayCooldown(pistolDangerDelayAmount);
        battleEventHub?.RaiseDamageTextRequested("PIN", target.transform.position + Vector3.up * 1.45f, new Color(0.55f, 0.92f, 1f, 1f));
    }

    private IEnumerator FinishGunActionRoutine(string logMessage, float waitSeconds)
    {
        if (!string.IsNullOrEmpty(logMessage))
        {
            Debug.Log(logMessage);
        }

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
        }

        yield return new WaitForSeconds(waitSeconds);

        PlayerAnimationPresenter playerAnim = GetPlayerAnimationPresenter();
        if (playerAnim != null)
        {
            playerAnim.PlayIdle();
        }

        if (endPlayerTurnRoutineFactory != null)
        {
            IEnumerator routine = endPlayerTurnRoutineFactory();
            if (routine != null)
            {
                StartCoroutine(routine);
            }
        }
    }

    private PlayerAnimationPresenter GetPlayerAnimationPresenter()
    {
        if (playerUnit == null) return null;
        return playerUnit.GetComponent<PlayerAnimationPresenter>();
    }

    private void PlayGunShotVisual(GunData gun, BattleUnit target)
    {
        if (gun == null || target == null) return;

        switch (gun.gunType)
        {
            case GunType.MachineGun:
                PlayMachineGunShotVisual(target);
                break;

            case GunType.Shotgun:
                PlayShotgunShotVisual(target);
                break;

            case GunType.Rifle:
                PlayRifleShotVisual(target);
                break;

            case GunType.Pistol:
            default:
                PlayPistolShotVisual(target);
                break;
        }
    }

    private void PlayPistolShotVisual(BattleUnit target)
    {
        Vector3 muzzlePos = GetGunMuzzleWorldPos(new Vector3(0.60f, 0.35f, 0f));
        Vector3 hitPos = target.transform.position + new Vector3(0f, 0.50f, 0f);

        SpawnScaledMuzzleFlash(muzzlePos, pistolMuzzleScale, 0.18f);
        SpawnScaledHitEffect(hitPos, pistolHitScale, 0.22f);
    }

    private void PlayMachineGunShotVisual(BattleUnit target)
    {
        Vector3 muzzlePos = GetGunMuzzleWorldPos(new Vector3(0.58f, 0.34f, 0f));
        Vector3 hitPos = target.transform.position
            + new Vector3(
                UnityEngine.Random.Range(-machineGunHitScatter, machineGunHitScatter),
                0.46f + UnityEngine.Random.Range(-0.08f, 0.08f),
                0f);

        SpawnScaledMuzzleFlash(muzzlePos, machineGunMuzzleScale, 0.12f);
        SpawnScaledHitEffect(hitPos, machineGunHitScale, 0.16f);
    }

    private void PlayShotgunShotVisual(BattleUnit target)
    {
        Vector3 muzzlePos = GetGunMuzzleWorldPos(new Vector3(0.64f, 0.36f, 0f));
        Vector3 centerHitPos = target.transform.position + new Vector3(0f, 0.52f, 0f);

        SpawnScaledMuzzleFlash(muzzlePos, shotgunMuzzleScale, 0.16f);
        SpawnScaledHitEffect(centerHitPos, shotgunCenterHitScale, 0.22f);

        for (int i = 0; i < shotgunPelletVisualCount; i++)
        {
            float t = shotgunPelletVisualCount <= 1 ? 0.5f : (float)i / (shotgunPelletVisualCount - 1);

            float offsetX = Mathf.Lerp(-shotgunSpreadX, shotgunSpreadX, t);
            float offsetY = UnityEngine.Random.Range(-shotgunSpreadY, shotgunSpreadY);

            offsetX += UnityEngine.Random.Range(-0.06f, 0.06f);
            offsetX += UnityEngine.Random.Range(-shotgunHitScatter * 0.15f, shotgunHitScatter * 0.15f);

            Vector3 pelletHitPos = centerHitPos + new Vector3(offsetX, offsetY, 0f);

            SpawnShotgunPelletTracer(muzzlePos, pelletHitPos);
            SpawnScaledHitEffect(
                pelletHitPos,
                shotgunPelletHitScale * UnityEngine.Random.Range(0.9f, 1.1f),
                0.14f);
        }
    }

    private void PlayRifleShotVisual(BattleUnit target)
    {
        Vector3 muzzlePos = GetGunMuzzleWorldPos(new Vector3(0.72f, 0.38f, 0f));
        Vector3 hitPos = target.transform.position + new Vector3(0f, 0.56f, 0f);

        SpawnScaledMuzzleFlash(muzzlePos, rifleMuzzleScale, 0.14f);
        SpawnRifleTracer(muzzlePos, hitPos);
        SpawnScaledHitEffect(hitPos, rifleHitScale, 0.20f);
    }

    private Vector3 GetGunMuzzleWorldPos(Vector3 offset)
    {
        if (playerUnit == null) return offset;
        return playerUnit.transform.position + offset;
    }

    private void SpawnScaledMuzzleFlash(Vector3 worldPos, float uniformScale, float returnDelay)
    {
        if (muzzleFlashPrefab == null) return;
        SpawnScaledEffect(muzzleFlashPrefab, worldPos, uniformScale, returnDelay);
    }

    private void SpawnScaledHitEffect(Vector3 worldPos, float uniformScale, float returnDelay)
    {
        if (hitEffectPrefab == null) return;
        SpawnScaledEffect(hitEffectPrefab, worldPos, uniformScale, returnDelay);
    }

    private void SpawnScaledEffect(GameObject prefab, Vector3 worldPos, float uniformScale, float returnDelay)
    {
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity);
        instance.transform.localScale = Vector3.one * uniformScale;
        Destroy(instance, returnDelay);
    }

    private Material GetOrCreateTracerMaterial()
    {
        if (gunTracerMaterial != null)
        {
            return gunTracerMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return null;
        }

        gunTracerMaterial = new Material(shader);
        return gunTracerMaterial;
    }

    private void SpawnShotgunPelletTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObj = new GameObject("ShotgunPelletTracer");
        LineRenderer lr = tracerObj.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = shotgunPelletTracerWidth;
        lr.endWidth = shotgunPelletTracerWidth * 0.45f;
        lr.numCapVertices = 2;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingOrder = 28;

        Material tracerMaterial = GetOrCreateTracerMaterial();
        if (tracerMaterial != null)
        {
            lr.material = tracerMaterial;
        }

        Color startColor = shotgunTracerColor;
        Color endColor = shotgunTracerColor;
        endColor.a = 0f;

        lr.startColor = startColor;
        lr.endColor = endColor;

        Destroy(tracerObj, shotgunPelletTracerDuration);
    }

    private void SpawnRifleTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObj = new GameObject("RifleTracer");
        LineRenderer lr = tracerObj.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = rifleTracerWidth;
        lr.endWidth = rifleTracerWidth * 0.55f;
        lr.numCapVertices = 2;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingOrder = 27;

        Material tracerMaterial = GetOrCreateTracerMaterial();
        if (tracerMaterial != null)
        {
            lr.material = tracerMaterial;
        }

        Color startColor = rifleTracerColor;
        Color endColor = rifleTracerColor;
        endColor.a = 0f;

        lr.startColor = startColor;
        lr.endColor = endColor;

        Destroy(tracerObj, rifleTracerDuration);
    }
}
