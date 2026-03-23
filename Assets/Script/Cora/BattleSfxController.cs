using UnityEngine;

[DisallowMultipleComponent]
public class BattleSfxController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private BattleEventHub battleEventHub;

    [Header("Master Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    [Header("UI")]
    [SerializeField] private AudioClip uiDecide;
    [SerializeField] private AudioClip uiCancel;
    [SerializeField] private AudioClip uiGaugeError;

    [Header("Board")]
    [SerializeField] private AudioClip boardPanelTap;
    [SerializeField] private AudioClip boardPanelClear;

    [Header("Battle")]
    [SerializeField] private AudioClip battleTurnShift;
    [SerializeField] private AudioClip battleMeleeHit;
    [SerializeField] private AudioClip battleEnemyHit;
    [SerializeField] private AudioClip battleEnemyDown;
    [SerializeField] private AudioClip battlePlayerHit;

    [Header("Gun")]
    [SerializeField] private AudioClip gunPistolFire;
    [SerializeField] private AudioClip gunMgFire;
    [SerializeField] private AudioClip gunShotgunFire;
    [SerializeField] private AudioClip gunRifleFire;

    [Header("Reward / Result")]
    [SerializeField] private AudioClip rewardCoin;
    [SerializeField] private AudioClip rewardExp;
    [SerializeField] private AudioClip rewardLevelUp;
    [SerializeField] private AudioClip bossAppear;
    [SerializeField] private AudioClip resultVictory;
    [SerializeField] private AudioClip resultGameOver;

    private bool isSubscribed;

    private void Awake()
    {
        if (seSource == null)
        {
            seSource = GetComponent<AudioSource>();
        }

        if (battleEventHub == null)
        {
            battleEventHub = FindObjectOfType<BattleEventHub>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Reset()
    {
        seSource = GetComponent<AudioSource>();
        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
        }

        seSource.playOnAwake = false;
        seSource.loop = false;
        seSource.spatialBlend = 0f;

        battleEventHub = FindObjectOfType<BattleEventHub>();
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        if (battleEventHub == null)
        {
            battleEventHub = FindObjectOfType<BattleEventHub>();
        }

        if (battleEventHub == null)
        {
            return;
        }

        battleEventHub.CoinsGained += HandleCoinsGained;
        battleEventHub.ExpTextRequested += HandleExpTextRequested;
        battleEventHub.LevelUpTextRequested += HandleLevelUpTextRequested;
        battleEventHub.EnemyDefeated += HandleEnemyDefeated;
        battleEventHub.StageClearRequested += HandleStageClearRequested;
        battleEventHub.PlayerDefeatedRequested += HandlePlayerDefeatedRequested;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || battleEventHub == null)
        {
            return;
        }

        battleEventHub.CoinsGained -= HandleCoinsGained;
        battleEventHub.ExpTextRequested -= HandleExpTextRequested;
        battleEventHub.LevelUpTextRequested -= HandleLevelUpTextRequested;
        battleEventHub.EnemyDefeated -= HandleEnemyDefeated;
        battleEventHub.StageClearRequested -= HandleStageClearRequested;
        battleEventHub.PlayerDefeatedRequested -= HandlePlayerDefeatedRequested;

        isSubscribed = false;
    }

    private void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (seSource == null || clip == null)
        {
            return;
        }

        float finalVolume = Mathf.Clamp01(masterVolume * volumeScale);
        seSource.PlayOneShot(clip, finalVolume);
    }

    public void PlayUiDecide(float volumeScale = 1f) => Play(uiDecide, volumeScale);
    public void PlayUiCancel(float volumeScale = 1f) => Play(uiCancel, volumeScale);
    public void PlayUiGaugeError(float volumeScale = 1f) => Play(uiGaugeError, volumeScale);

    public void PlayPanelTap(float volumeScale = 1f) => Play(boardPanelTap, volumeScale);
    public void PlayPanelClear(float volumeScale = 1f) => Play(boardPanelClear, volumeScale);

    public void PlayTurnShift(float volumeScale = 1f) => Play(battleTurnShift, volumeScale);
    public void PlayMeleeHit(float volumeScale = 1f) => Play(battleMeleeHit, volumeScale);
    public void PlayEnemyHit(float volumeScale = 1f) => Play(battleEnemyHit, volumeScale);
    public void PlayEnemyDown(float volumeScale = 1f) => Play(battleEnemyDown, volumeScale);
    public void PlayPlayerHit(float volumeScale = 1f) => Play(battlePlayerHit, volumeScale);

    public void PlayGunFire(GunType gunType, float volumeScale = 1f)
    {
        switch (gunType)
        {
            case GunType.Pistol:
                Play(gunPistolFire, volumeScale);
                break;
            case GunType.MachineGun:
                Play(gunMgFire, volumeScale);
                break;
            case GunType.Shotgun:
                Play(gunShotgunFire, volumeScale);
                break;
            case GunType.Rifle:
                Play(gunRifleFire, volumeScale);
                break;
        }
    }

    public void PlayCoin(float volumeScale = 1f) => Play(rewardCoin, volumeScale);
    public void PlayExp(float volumeScale = 1f) => Play(rewardExp, volumeScale);
    public void PlayLevelUp(float volumeScale = 1f) => Play(rewardLevelUp, volumeScale);
    public void PlayBossAppear(float volumeScale = 1f) => Play(bossAppear, volumeScale);
    public void PlayVictory(float volumeScale = 1f) => Play(resultVictory, volumeScale);
    public void PlayGameOver(float volumeScale = 1f) => Play(resultGameOver, volumeScale);

    private void HandleCoinsGained(int amount)
    {
        if (amount > 0)
        {
            PlayCoin();
        }
    }

    private void HandleExpTextRequested(int exp, Vector3 worldPos, float delay)
    {
        if (exp > 0)
        {
            PlayExp();
        }
    }

    private void HandleLevelUpTextRequested(float delay)
    {
        PlayLevelUp();
    }

    private void HandleEnemyDefeated(BattleUnit defeatedEnemy)
    {
        PlayEnemyDown();
    }

    private void HandleStageClearRequested()
    {
        PlayVictory();
    }

    private void HandlePlayerDefeatedRequested()
    {
        PlayGameOver();
    }
}
