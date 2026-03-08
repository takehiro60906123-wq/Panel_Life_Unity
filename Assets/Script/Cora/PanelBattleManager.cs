using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum PanelType { Sword, Magic, Coin, Heal, LvUp, Chick, Diamond, None }

public enum EncounterType
{
    Enemy,
    Empty,
    Treasure
}

[System.Serializable]
public class PanelSetting
{
    public string label;
    public PanelType type;
    public Sprite panelImage;
    public int weight;
}

public class PanelBattleManager : MonoBehaviour
{
    [Header("UIの設定")]
    public GameObject panelPrefab;
    public Transform boardParent;
    public List<PanelSetting> panelSettings;

    [Header("UIコントローラー")]
    public BattleUIController battleUIController;

    [Header("霞コントローラー")]
    public DungeonMistController dungeonMistController;

    [Header("敵演出コントローラー")]
    public EnemyPresentationController enemyPresentationController;

    [Header("移動演出コントローラー")]
    public RoomTravelController roomTravelController;

    [Header("盤面コントローラー")]
    public PanelBoardController panelBoardController;

    [Header("バトルエフェクトコントローラー")]
    public BattleEffectController battleEffectController;

    [Header("ステージ進行コントローラー")]
    public StageFlowController stageFlowController;

    [Header("ターン進行コントローラー")]
    public BattleTurnController battleTurnController;

    [Header("パネル行動コントローラー")]
    public PanelActionController panelActionController;

    [Header("戦闘進行コントローラー")]
    public EncounterFlowController encounterFlowController;

    [Header("ダメージ解決コントローラー")]
    public BattleDamageResolver battleDamageResolver;

    [Header("初期化コントローラー")]
    public BattleBootstrapper battleBootstrapper;

    [Header("イベントハブ")]
    public BattleEventHub battleEventHub;

    [Header("バトルユニット連携")]
    public BattleUnit playerUnit;

    [Header("状態コンテキスト")]
    [SerializeField] private BattleContext battleContext;

    public BattleContext Context => battleContext;

    public BattleUnit enemyUnit
    {
        get => battleContext != null ? battleContext.CurrentEnemy : null;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.CurrentEnemy = value;
            }
        }
    }

    public EncounterType currentEncounter
    {
        get => battleContext != null ? battleContext.CurrentEncounter : EncounterType.Enemy;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.CurrentEncounter = value;
            }
        }
    }

    public int remainingSteps
    {
        get => battleContext != null ? battleContext.RemainingSteps : 0;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.RemainingSteps = value;
            }
        }
    }

    private int currentCoins
    {
        get => battleContext != null ? battleContext.CurrentCoins : 0;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.CurrentCoins = value;
            }
        }
    }

    private bool isPlayerTurn
    {
        get => battleContext != null && battleContext.IsPlayerTurn;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.IsPlayerTurn = value;
            }
        }
    }

    private bool isEnemySpawning
    {
        get => battleContext != null && battleContext.IsEnemySpawning;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.IsEnemySpawning = value;
            }
        }
    }

    private bool isEnemyDefeatedThisTurn
    {
        get => battleContext != null && battleContext.IsEnemyDefeatedThisTurn;
        set
        {
            EnsureBattleContext();
            if (battleContext != null)
            {
                battleContext.IsEnemyDefeatedThisTurn = value;
            }
        }
    }

    [Header("ダンジョン演出")]
    public Transform dungeonMistRoot;
    [Range(0f, 1f)] public float battleMistAlpha = 0.8f;
    public float mistFadeDuration = 0.35f;

    [Header("移動演出")]
    public float roomTravelDuration = 1.0f;
    public Ease roomTravelEase = Ease.Linear;
    public float enemyRevealDuration = 0.2f;

    [Header("バトル演出")]
    public GameObject damageTextPrefab;
    public GameObject hitEffectPrefab;
    public GameObject magicBulletPrefab;
    public GameObject energyOrbPrefab;
    public GameObject absorbEffectPrefab;
    public GameObject levelUpEffectPrefab;

    [Header("ステージ進行設定")]
    public Transform battlePosition;
    public Vector3 waitOffset = new Vector3(2.5f, 0, 0);
    public int maxFloors = 100;
    public int maxVisibleEnemies = 3;
    public List<BattleUnit> enemyPrefabs;

    private CanvasGroup boardCanvasGroup;
    private EffectPoolManager effectPoolManager;

    private const int rows = 6;
    private const int cols = 6;

    public int BoardRows => rows;
    public int BoardCols => cols;
    public bool IsEnemySpawning => isEnemySpawning;
    public bool IsEnemyDefeatedThisTurn => isEnemyDefeatedThisTurn;

    private bool isEventHubSubscribed;

    private void SubscribeBattleEvents()
    {
        if (battleEventHub == null || isEventHubSubscribed)
        {
            return;
        }

        battleEventHub.BoardInteractableRequested += HandleBoardInteractableRequested;
        battleEventHub.CoinsGained += HandleCoinsGained;
        battleEventHub.EnergyOrbRequested += HandleEnergyOrbRequested;
        battleEventHub.MagicBulletRequested += HandleMagicBulletRequested;
        battleEventHub.EncounterStateChanged += HandleEncounterStateChanged;
        battleEventHub.DungeonMistRequested += HandleDungeonMistRequested;
        battleEventHub.DamageTextRequested += HandleDamageTextRequested;
        battleEventHub.OneShotEffectRequested += HandleOneShotEffectRequested;
        battleEventHub.ExpTextRequested += HandleExpTextRequested;
        battleEventHub.LevelUpTextRequested += HandleLevelUpTextRequested;
        battleEventHub.StageClearRequested += HandleStageClearRequested;
        battleEventHub.PlayerDefeatedRequested += HandlePlayerDefeatedRequested;

        isEventHubSubscribed = true;
    }

    private void UnsubscribeBattleEvents()
    {
        if (battleEventHub == null || !isEventHubSubscribed)
        {
            return;
        }

        battleEventHub.BoardInteractableRequested -= HandleBoardInteractableRequested;
        battleEventHub.CoinsGained -= HandleCoinsGained;
        battleEventHub.EnergyOrbRequested -= HandleEnergyOrbRequested;
        battleEventHub.MagicBulletRequested -= HandleMagicBulletRequested;
        battleEventHub.EncounterStateChanged -= HandleEncounterStateChanged;
        battleEventHub.DungeonMistRequested -= HandleDungeonMistRequested;
        battleEventHub.DamageTextRequested -= HandleDamageTextRequested;
        battleEventHub.OneShotEffectRequested -= HandleOneShotEffectRequested;
        battleEventHub.ExpTextRequested -= HandleExpTextRequested;
        battleEventHub.LevelUpTextRequested -= HandleLevelUpTextRequested;
        battleEventHub.StageClearRequested -= HandleStageClearRequested;
        battleEventHub.PlayerDefeatedRequested -= HandlePlayerDefeatedRequested;

        isEventHubSubscribed = false;
    }

    private void HandleBoardInteractableRequested(bool isInteractable)
    {
        SetBoardInteractable(isInteractable);
    }

    private void HandleCoinsGained(int amount)
    {
        AddCoins(amount);
    }

    private void HandleEnergyOrbRequested(Vector3 startPos, Vector3 target, float duration, float delay)
    {
        SpawnEnergyOrb(startPos, target, duration, delay);
    }

    private void HandleMagicBulletRequested()
    {
        SpawnMagicBullet();
    }

    private void HandleEncounterStateChanged(EncounterType encounterType, int steps)
    {
        SetEncounterState(encounterType, steps);
    }

    private void HandleDungeonMistRequested(bool isBattle, bool immediate)
    {
        SetDungeonMist(isBattle, immediate);
    }

    private void HandleDamageTextRequested(string text, Vector3 position, Color color)
    {
        SpawnDamageText(text, position, color);
    }

    private void HandleOneShotEffectRequested(GameObject prefab, Vector3 position, float returnDelay)
    {
        SpawnOneShotEffect(prefab, position, returnDelay);
    }

    private void HandleExpTextRequested(int exp, Vector3 position, float delay)
    {
        StartCoroutine(SpawnExpTextWithDelay(exp, position, delay));
    }

    private void HandleLevelUpTextRequested(float delay)
    {
        StartCoroutine(SpawnLevelUpTextWithDelay(delay));
    }

    private void HandleStageClearRequested()
    {
        OnStageClear();
    }

    private void HandlePlayerDefeatedRequested()
    {
        OnPlayerDefeated();
    }


    private void EnsureBattleContext()
    {
        if (battleContext == null)
        {
            battleContext = GetComponent<BattleContext>();
        }

        if (battleContext == null)
        {
            battleContext = gameObject.AddComponent<BattleContext>();
        }
    }

    void Awake()
    {
        EnsureBattleContext();

        if (battleBootstrapper == null)
        {
            battleBootstrapper = GetComponent<BattleBootstrapper>();
        }

        if (battleBootstrapper == null)
        {
            battleBootstrapper = gameObject.AddComponent<BattleBootstrapper>();
        }

        battleBootstrapper.EnsureDependencies(this);
        SubscribeBattleEvents();
    }

    void Start()
    {
        DOTween.Init();

        if (battleBootstrapper == null)
        {
            Debug.LogError("BattleBootstrapper が取得できません。");
            enabled = false;
            return;
        }

        bool initialized = battleBootstrapper.Initialize(this);
        if (!initialized)
        {
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeBattleEvents();
    }

    public void SetEffectPoolManager(EffectPoolManager poolManager)
    {
        effectPoolManager = poolManager;
    }

    public EffectPoolManager GetEffectPoolManager()
    {
        return effectPoolManager;
    }

    public void SetBoardCanvasGroup(CanvasGroup canvasGroup)
    {
        boardCanvasGroup = canvasGroup;
    }

    public void SetEnemySpawning(bool value)
    {
        isEnemySpawning = value;
    }

    public void SetEnemyDefeatedThisTurn(bool value)
    {
        isEnemyDefeatedThisTurn = value;
    }

    public void SetEncounterState(EncounterType encounterType, int steps)
    {
        currentEncounter = encounterType;
        remainingSteps = steps;
        UpdateEncounterUI();
    }

    public void UpdateEncounterUI()
    {
        if (battleUIController != null)
        {
            battleUIController.SetEncounterInfo(currentEncounter, remainingSteps);
        }
    }

    public void SetDungeonMist(bool isBattle, bool immediate = false)
    {
        if (dungeonMistController != null)
        {
            dungeonMistController.ApplyBattleState(isBattle, immediate);
        }
    }

    public void UpdateCoinUI()
    {
        if (battleUIController != null)
        {
            battleUIController.SetCoinText(currentCoins);
        }
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateCoinUI();
    }

    public IEnumerator EndPlayerTurn()
    {
        if (encounterFlowController == null)
        {
            yield break;
        }

        yield return StartCoroutine(encounterFlowController.EndPlayerTurnRoutine());
    }

    public void AdvanceEmptyTurn()
    {
        if (encounterFlowController == null)
        {
            return;
        }

        encounterFlowController.AdvanceEmptyTurn();
    }

    public IEnumerator TravelForward()
    {
        if (roomTravelController != null)
        {
            yield return roomTravelController.TravelForward(playerUnit.transform, waitOffset);
            yield break;
        }

        if (playerUnit == null)
        {
            yield break;
        }

        playerUnit.transform
            .DOMove(playerUnit.transform.position + waitOffset, roomTravelDuration)
            .SetEase(roomTravelEase);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform
                .DOMove(mainCam.transform.position + waitOffset, roomTravelDuration)
                .SetEase(roomTravelEase);
        }

        yield return new WaitForSeconds(roomTravelDuration);
    }

    public void SpawnMagicBullet()
    {
        if (battleEffectController == null) return;
        if (magicBulletPrefab == null || enemyUnit == null || playerUnit == null) return;

        Vector3 start = playerUnit.transform.position + new Vector3(0.5f, 0.5f, 0);
        Vector3 target = enemyUnit.transform.position + Vector3.up * 0.5f;

        battleEffectController.SpawnMagicBullet(magicBulletPrefab, start, target, () =>
        {
            if (!isEnemySpawning)
            {
                battleEventHub?.RaiseEnemyDamageRequested(1);
            }
        });
    }

    public void SpawnEnergyOrb(Vector3 startPos, Vector3 target, float duration, float delay)
    {
        if (battleEffectController == null) return;

        battleEffectController.SpawnEnergyOrb(
            energyOrbPrefab,
            absorbEffectPrefab,
            startPos,
            target,
            duration,
            delay);
    }

    public IEnumerator SpawnExpTextWithDelay(int exp, Vector3 spawnPos, float delay)
    {
        if (battleEffectController == null)
        {
            yield return new WaitForSeconds(delay);
            yield break;
        }

        yield return battleEffectController.SpawnExpTextWithDelay(damageTextPrefab, exp, spawnPos, delay);
    }

    public IEnumerator SpawnLevelUpTextWithDelay(float delay)
    {
        if (battleEffectController == null || playerUnit == null)
        {
            yield return new WaitForSeconds(delay);
            yield break;
        }

        yield return battleEffectController.SpawnLevelUpTextWithDelay(damageTextPrefab, playerUnit.transform, delay);
    }

    public void SpawnDamageText(string text, Vector3 position, Color color)
    {
        if (battleEffectController == null) return;
        battleEffectController.SpawnDamageText(damageTextPrefab, text, position, color);
    }

    public void SpawnOneShotEffect(GameObject prefab, Vector3 position, float returnDelay)
    {
        if (battleEffectController == null || prefab == null) return;
        battleEffectController.SpawnOneShotEffect(prefab, position, Quaternion.identity, returnDelay);
    }

    public void SpawnNextEnemy()
    {
        if (stageFlowController == null) return;
        if (enemyUnit == null) return;

        stageFlowController.SpawnNextEnemyAfter(enemyUnit.transform.position);

        if (enemyPresentationController != null)
        {
            enemyPresentationController.RefreshUpcomingEnemyStandbyVisuals(stageFlowController.GetUpcomingEnemies());
        }
    }

    public void SetBoardInteractable(bool isInteractable)
    {
        isPlayerTurn = isInteractable;

        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.interactable = isInteractable;
            boardCanvasGroup.blocksRaycasts = isInteractable;
            boardCanvasGroup.DOFade(isInteractable ? 1.0f : 0.6f, 0.3f);
        }
    }

    public void OnStageClear()
    {
        Debug.Log("ステージクリア！リザルトへ");
        SetBoardInteractable(false);
    }

    public void OnPlayerDefeated()
    {
        Debug.Log("ゲームオーバー");
        SetBoardInteractable(false);
    }
}