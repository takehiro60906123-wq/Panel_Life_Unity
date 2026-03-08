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

    [Header("バトルユニット連携")]
    public BattleUnit playerUnit;
    public BattleUnit enemyUnit;

    [Header("グローバル状態")]
    [SerializeField] private int currentCoins = 0;

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

    [Header("進行状況")]
    public EncounterType currentEncounter = EncounterType.Enemy;
    public int remainingSteps = 0;

    private bool isPlayerTurn = true;
    private bool isEnemySpawning = false;
    private bool isEnemyDefeatedThisTurn = false;

    private CanvasGroup boardCanvasGroup;
    private EffectPoolManager effectPoolManager;

    private const int rows = 6;
    private const int cols = 6;

    void Awake()
    {
        effectPoolManager = GetComponent<EffectPoolManager>();
        if (effectPoolManager == null)
        {
            effectPoolManager = gameObject.AddComponent<EffectPoolManager>();
        }

        if (battleUIController == null)
        {
            battleUIController = GetComponent<BattleUIController>();
        }

        if (battleUIController == null)
        {
            battleUIController = FindObjectOfType<BattleUIController>();
        }

        if (battleUIController == null)
        {
            Debug.LogWarning("BattleUIController が見つかりません。UI表示は更新されません。");
        }

        if (dungeonMistController == null)
        {
            dungeonMistController = GetComponent<DungeonMistController>();
        }

        if (dungeonMistController == null)
        {
            dungeonMistController = gameObject.AddComponent<DungeonMistController>();
        }

        if (enemyPresentationController == null)
        {
            enemyPresentationController = GetComponent<EnemyPresentationController>();
        }

        if (enemyPresentationController == null)
        {
            enemyPresentationController = gameObject.AddComponent<EnemyPresentationController>();
        }

        if (roomTravelController == null)
        {
            roomTravelController = GetComponent<RoomTravelController>();
        }

        if (roomTravelController == null)
        {
            roomTravelController = gameObject.AddComponent<RoomTravelController>();
        }

        if (panelBoardController == null)
        {
            panelBoardController = GetComponent<PanelBoardController>();
        }

        if (panelBoardController == null)
        {
            panelBoardController = gameObject.AddComponent<PanelBoardController>();
        }

        if (battleEffectController == null)
        {
            battleEffectController = GetComponent<BattleEffectController>();
        }

        if (battleEffectController == null)
        {
            battleEffectController = gameObject.AddComponent<BattleEffectController>();
        }

        if (stageFlowController == null)
        {
            stageFlowController = GetComponent<StageFlowController>();
        }

        if (stageFlowController == null)
        {
            stageFlowController = gameObject.AddComponent<StageFlowController>();
        }

        if (battleTurnController == null)
        {
            battleTurnController = GetComponent<BattleTurnController>();
        }

        if (battleTurnController == null)
        {
            battleTurnController = gameObject.AddComponent<BattleTurnController>();
        }
    }

    void Start()
    {
        DOTween.Init();

        if (boardParent == null)
        {
            Debug.LogError("boardParent が未設定です。");
            enabled = false;
            return;
        }

        if (panelPrefab == null)
        {
            Debug.LogError("panelPrefab が未設定です。");
            enabled = false;
            return;
        }

        boardCanvasGroup = boardParent.GetComponent<CanvasGroup>();
        if (boardCanvasGroup == null)
        {
            boardCanvasGroup = boardParent.gameObject.AddComponent<CanvasGroup>();
        }

        if (dungeonMistController != null)
        {
            dungeonMistController.Configure(dungeonMistRoot, battleMistAlpha, mistFadeDuration);
            dungeonMistController.ApplyBattleState(true, true);
        }

        if (enemyPresentationController != null)
        {
            enemyPresentationController.Configure(enemyRevealDuration, roomTravelEase);
        }

        if (roomTravelController != null)
        {
            roomTravelController.Configure(roomTravelDuration, roomTravelEase);
        }

        if (battleEffectController != null)
        {
            battleEffectController.Configure(effectPoolManager);
        }

        if (stageFlowController != null)
        {
            stageFlowController.Configure(
                battlePosition,
                waitOffset,
                maxFloors,
                maxVisibleEnemies,
                enemyPrefabs);
        }

        if (battleTurnController != null)
        {
            battleTurnController.Configure(0.5f, 0.2f, 0.5f, 0.25f);
        }

        if (panelBoardController == null)
        {
            Debug.LogError("PanelBoardController が取得できません。");
            enabled = false;
            return;
        }

        bool boardInitialized = panelBoardController.Initialize(
            panelPrefab,
            boardParent,
            panelSettings,
            rows,
            cols,
            OnPanelClicked);

        if (!boardInitialized)
        {
            enabled = false;
            return;
        }

        SetBoardInteractable(true);
        UpdateCoinUI();
        UpdateEncounterUI();
        panelBoardController.GenerateBoard();
        SetupStage();
    }

    void SetupStage()
    {
        if (stageFlowController == null)
        {
            Debug.LogError("StageFlowController が取得できません。");
            enemyUnit = null;
            SetBoardInteractable(false);
            return;
        }

        if (!stageFlowController.SetupInitialStage(out BattleUnit initialEnemy, out string errorMessage))
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                if (errorMessage.Contains("0 以下"))
                    Debug.LogWarning(errorMessage);
                else
                    Debug.LogError(errorMessage);
            }

            enemyUnit = null;
            SetBoardInteractable(false);
            return;
        }

        enemyUnit = initialEnemy;

        RefreshUpcomingEnemyStandbyVisuals();

        if (enemyUnit != null)
        {
            enemyUnit.transform.localScale = Vector3.one;
            ActivateEnemyAsCurrent(enemyUnit);
            SetEncounter(EncounterType.Enemy, 0);
            SetDungeonMist(true);
        }
        else
        {
            Debug.LogWarning("生成できる敵がいませんでした。");
            SetBoardInteractable(false);
        }
    }

    void SetEncounter(EncounterType encounterType, int steps = 0)
    {
        currentEncounter = encounterType;
        remainingSteps = steps;
        UpdateEncounterUI();
    }

    void UpdateEncounterUI()
    {
        if (battleUIController != null)
        {
            battleUIController.SetEncounterInfo(currentEncounter, remainingSteps);
        }
    }

    void SetDungeonMist(bool isBattle, bool immediate = false)
    {
        if (dungeonMistController != null)
        {
            dungeonMistController.ApplyBattleState(isBattle, immediate);
        }
    }

    void UpdateCoinUI()
    {
        if (battleUIController != null)
        {
            battleUIController.SetCoinText(currentCoins);
        }
    }

    void OnPanelClicked(int row, int col)
    {
        if (!isPlayerTurn) return;
        if (panelBoardController == null) return;

        PanelType clickedType = panelBoardController.GetPanelType(row, col);
        if (clickedType == PanelType.None) return;

        SetBoardInteractable(false);

        List<Vector2Int> chain = panelBoardController.FindChain(row, col, clickedType);

        if (clickedType == PanelType.Sword)
        {
            chain.AddRange(panelBoardController.GetAdjacentLevelPanels(chain));
        }

        StartCoroutine(CollectEnergyAndAttack(clickedType, chain));
        panelBoardController.ClearChainPanels(chain);

        DOVirtual.DelayedCall(0.25f, () =>
        {
            if (panelBoardController != null)
            {
                panelBoardController.DropAndFillPanels();
            }
        });
    }

    IEnumerator CollectEnergyAndAttack(PanelType type, List<Vector2Int> chain)
    {
        if (playerUnit == null) yield break;
        if (panelBoardController == null) yield break;

        Vector3 targetPos = playerUnit.transform.position + Vector3.up * 0.5f;

        float flyDuration = 0.5f;
        float delay = 0f;

        foreach (Vector2Int pos in chain)
        {
            Vector3 panelWorldPos = panelBoardController.GetPanelWorldPosition(pos.x, pos.y);
            SpawnEnergyOrb(panelWorldPos, targetPos, flyDuration, delay);
            delay += 0.04f;
        }

        yield return new WaitForSeconds(flyDuration + delay + 0.2f);
        ExecutePanelAction(type, chain.Count);
    }

    void ExecutePanelAction(PanelType type, int chainCount)
    {
        switch (type)
        {
            case PanelType.Sword:
                StartCoroutine(PlayMeleeAttack(chainCount));
                break;

            case PanelType.Magic:
                StartCoroutine(PlayMagicAttack(chainCount));
                break;

            case PanelType.Heal:
                if (playerUnit != null) playerUnit.Heal(chainCount * 5);
                StartCoroutine(EndPlayerTurn());
                break;

            case PanelType.Coin:
                currentCoins += chainCount * 10;
                UpdateCoinUI();
                StartCoroutine(EndPlayerTurn());
                break;

            default:
                StartCoroutine(EndPlayerTurn());
                break;
        }
    }

    IEnumerator PlayMeleeAttack(int count)
    {
        if (enemyUnit == null || enemyUnit.IsDead())
        {
            if (playerUnit != null && playerUnit.animator != null)
            {
                playerUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.4f);
            StartCoroutine(EndPlayerTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit != null && playerUnit.animator != null)
            {
                playerUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.12f);
            DamageEnemy(1);
            yield return new WaitForSeconds(0.08f);
        }

        StartCoroutine(EndPlayerTurn());
    }

    IEnumerator PlayMagicAttack(int count)
    {
        if (enemyUnit == null || enemyUnit.IsDead())
        {
            if (playerUnit != null && playerUnit.animator != null)
            {
                playerUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.4f);
            StartCoroutine(EndPlayerTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit != null && playerUnit.animator != null)
            {
                playerUnit.animator.Play("ATTACK", 0, 0f);
            }

            yield return new WaitForSeconds(0.08f);
            SpawnMagicBullet();
            yield return new WaitForSeconds(0.12f);
        }

        StartCoroutine(EndPlayerTurn());
    }

    IEnumerator EndPlayerTurn()
    {
        if (battleTurnController != null)
        {
            yield return battleTurnController.EndPlayerTurnRoutine(
     currentEncounter,
     () => isEnemySpawning,
     () => isEnemyDefeatedThisTurn,
     () => isEnemyDefeatedThisTurn = false,
     () => enemyUnit,
     () =>
     {
         Debug.Log("ステージクリア！リザルトへ");
         SetBoardInteractable(false);
     },
     EnemyTurnRoutine,
     AdvanceEmptyTurn,
     SetBoardInteractable);

            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        while (isEnemySpawning)
        {
            yield return null;
        }

        if (currentEncounter == EncounterType.Empty || currentEncounter == EncounterType.Treasure)
        {
            if (!isEnemyDefeatedThisTurn)
            {
                AdvanceEmptyTurn();
            }

            isEnemyDefeatedThisTurn = false;
            yield break;
        }

        if (enemyUnit == null)
        {
            isEnemySpawning = false;

            if (stageFlowController != null && stageFlowController.IsStageComplete())
            {
                SetBoardInteractable(false);
                Debug.Log("ステージクリア！リザルトへ");
            }
            else
            {
                Debug.LogError("次の敵取得に失敗しました。ステージは未完了です。");
                SetBoardInteractable(true);
            }

            yield break;
        }

        if (isEnemyDefeatedThisTurn)
        {
            isEnemyDefeatedThisTurn = false;
            yield break;
        }

        yield return StartCoroutine(EnemyTurnRoutine());
    }

    IEnumerator EnemyTurnRoutine()
    {
        if (battleTurnController != null)
        {
            yield return battleTurnController.EnemyTurnRoutine(
                enemyUnit,
                playerUnit,
                hitEffectPrefab,
                SpawnDamageText,
                SpawnOneShotEffect,
                () =>
                {
                    Debug.Log("ゲームオーバー");
                    SetBoardInteractable(false);
                },
                SetBoardInteractable);

            yield break;
        }

        if (enemyUnit == null)
        {
            SetBoardInteractable(true);
            yield break;
        }

        enemyUnit.currentCooldown--;
        enemyUnit.UpdateTurnUI();

        if (enemyUnit.currentCooldown <= 0)
        {
            if (enemyUnit.animator != null) enemyUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.2f);

            bool isEvasion = Random.Range(0, 100) < 15;
            bool isCritical = Random.Range(0, 100) < 10;

            Vector3 pos = playerUnit.transform.position;

            if (isEvasion)
            {
                SpawnDamageText("Miss", pos + Vector3.up * 1.5f, Color.gray);
            }
            else
            {
                int damage = isCritical ? 3 : 1;
                playerUnit.TakeDamage(damage);

                SpawnOneShotEffect(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);

                Color textColor = isCritical ? Color.yellow : Color.red;
                string textStr = isCritical ? $"CRITICAL!\n{damage}" : damage.ToString();
                SpawnDamageText(textStr, pos + Vector3.up * 1.5f, textColor);

                if (playerUnit.IsDead())
                {
                    Debug.Log("ゲームオーバー");
                    SetBoardInteractable(false);
                    yield break;
                }
            }

            enemyUnit.currentCooldown = enemyUnit.attackInterval;
            enemyUnit.UpdateTurnUI();

            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
        }

        SetBoardInteractable(true);
    }

    public void AdvanceEmptyTurn()
    {
        if (battleTurnController != null)
        {
            battleTurnController.AdvanceEmptyTurn(
                currentEncounter,
                ref remainingSteps,
                playerUnit,
                UpdateEncounterUI,
                SpawnDamageText,
                SetBoardInteractable,
                EnemyRespawnRoutine);

            return;
        }

        if (currentEncounter == EncounterType.Empty || currentEncounter == EncounterType.Treasure)
        {
            remainingSteps--;
            UpdateEncounterUI();

            if (remainingSteps > 0)
            {
                SpawnDamageText($"あと {remainingSteps} ターン", playerUnit.transform.position + Vector3.up * 1.5f, Color.white);
                SetBoardInteractable(true);
            }
            else
            {
                SpawnDamageText("次の部屋へ！", playerUnit.transform.position + Vector3.up * 1.5f, Color.cyan);
                StartCoroutine(EnemyRespawnRoutine());
            }
        }
    }

    void DamageEnemy(int baseDamage)
    {
        if (enemyUnit == null || enemyUnit.IsDead() || isEnemySpawning) return;

        bool isEvasion = Random.Range(0, 100) < 10;
        bool isCritical = Random.Range(0, 100) < 20;

        Vector3 pos = enemyUnit.transform.position;

        if (isEvasion)
        {
            SpawnDamageText("Miss", pos + Vector3.up * 1.5f, Color.gray);
            return;
        }

        int finalDamage = isCritical ? baseDamage * 2 : baseDamage;

        enemyUnit.TakeDamage(finalDamage);

        SpawnOneShotEffect(hitEffectPrefab, pos + Vector3.up * 0.5f, 0.7f);

        Color textColor = isCritical ? Color.yellow : Color.white;
        string textStr = isCritical ? $"CRITICAL!\n{finalDamage}" : finalDamage.ToString();
        SpawnDamageText(textStr, pos + Vector3.up * 1.5f, textColor);

        if (enemyUnit.IsDead())
        {
            isEnemyDefeatedThisTurn = true;

            Vector3 expTextPos = enemyUnit.transform.position + Vector3.up * 1.0f;
            StartCoroutine(SpawnExpTextWithDelay(enemyUnit.expYield, expTextPos, 0.2f));

            bool isLevelUp = playerUnit.AddExp(enemyUnit.expYield);
            if (isLevelUp)
            {
                SpawnOneShotEffect(levelUpEffectPrefab, playerUnit.transform.position, 1.2f);
                StartCoroutine(SpawnLevelUpTextWithDelay(0.45f));
            }

            StartCoroutine(EnemyRespawnRoutine());
        }
    }

    IEnumerator EnemyRespawnRoutine()
    {
        SetBoardInteractable(false);
        isEnemySpawning = true;

        EncounterType prevEncounter = currentEncounter;

        yield return new WaitForSeconds(0.9f);

        if (enemyUnit != null && enemyUnit.IsDead())
        {
            Destroy(enemyUnit.gameObject);
            enemyUnit = null;
        }

        if (stageFlowController == null)
        {
            isEnemySpawning = false;
            SetBoardInteractable(false);
            Debug.LogError("StageFlowController が取得できません。");
            yield break;
        }

        StageFlowController.NextEncounterPlan plan = stageFlowController.DecideNextEncounter(prevEncounter);

        if (plan.isStageClear)
        {
            SetEncounter(EncounterType.Enemy, 0);
            isEnemySpawning = false;
            SetBoardInteractable(false);
            Debug.Log("ステージクリア！リザルトへ");
            yield break;
        }

        if (plan.encounterType == EncounterType.Enemy)
        {
            SetEncounter(EncounterType.Enemy, 0);
            yield return StartCoroutine(MoveToNextEnemyRoutine());

            if (enemyUnit == null)
            {
                isEnemySpawning = false;
                SetBoardInteractable(false);
                Debug.Log("ステージクリア！リザルトへ");
                yield break;
            }
        }
        else if (plan.encounterType == EncounterType.Empty)
        {
            SetEncounter(EncounterType.Empty, plan.steps);
            yield return StartCoroutine(EnterSafeRoomRoutine("平和な部屋だ", Color.white));
        }
        else
        {
            SetEncounter(EncounterType.Treasure, plan.steps);
            yield return StartCoroutine(EnterSafeRoomRoutine("宝箱の部屋だ！", Color.yellow));
        }

        isEnemySpawning = false;
        SetBoardInteractable(true);
    }

    IEnumerator MoveToNextEnemyRoutine()
    {
        HideAllUpcomingEnemies();
        SetDungeonMist(true);

        BattleUnit nextEnemy = null;

        if (stageFlowController != null && enemyUnit != null)
        {
            nextEnemy = stageFlowController.TakeNextEnemyOrSpawn(enemyUnit.transform.position);
        }
        else if (stageFlowController != null)
        {
            nextEnemy = stageFlowController.TakeNextEnemyOrSpawn(battlePosition != null ? battlePosition.position : Vector3.zero);
        }

        RefreshUpcomingEnemyStandbyVisuals();

        if (nextEnemy != null)
        {
            RevealWaitingEnemy(nextEnemy);
            SpawnDamageText("敵の気配…", nextEnemy.transform.position + Vector3.up * 1.5f, Color.red);
            yield return new WaitForSeconds(enemyRevealDuration);
        }

        SpawnDamageText("先へ走る…", playerUnit.transform.position + Vector3.up * 1.5f, Color.white);

        SetMoveAnimation(playerUnit.animator, true);

        yield return StartCoroutine(TravelForward());

        SetMoveAnimation(playerUnit.animator, false);

        enemyUnit = nextEnemy;

        if (enemyUnit != null)
        {
            ActivateEnemyAsCurrent(enemyUnit);
            SpawnDamageText("敵に到達した！", enemyUnit.transform.position + Vector3.up * 1.5f, Color.red);
            SpawnNextEnemy();
        }
    }

    IEnumerator EnterSafeRoomRoutine(string popupText, Color popupColor)
    {
        HideAllUpcomingEnemies();
        SetDungeonMist(false);

        SpawnDamageText(popupText, playerUnit.transform.position + Vector3.up * 1.5f, popupColor);

        SetMoveAnimation(playerUnit.animator, true);
        ShiftUpcomingEnemies(waitOffset.x, roomTravelDuration);

        yield return StartCoroutine(TravelForward());

        SetMoveAnimation(playerUnit.animator, false);
    }

    IEnumerator TravelForward()
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

    void SpawnMagicBullet()
    {
        if (battleEffectController == null) return;
        if (magicBulletPrefab == null || enemyUnit == null || playerUnit == null) return;

        Vector3 start = playerUnit.transform.position + new Vector3(0.5f, 0.5f, 0);
        Vector3 target = enemyUnit.transform.position + Vector3.up * 0.5f;

        battleEffectController.SpawnMagicBullet(magicBulletPrefab, start, target, () =>
        {
            if (!isEnemySpawning)
            {
                DamageEnemy(1);
            }
        });
    }

    void SpawnEnergyOrb(Vector3 startPos, Vector3 target, float duration, float delay)
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

    IEnumerator SpawnExpTextWithDelay(int exp, Vector3 spawnPos, float delay)
    {
        if (battleEffectController == null)
        {
            yield return new WaitForSeconds(delay);
            yield break;
        }

        yield return battleEffectController.SpawnExpTextWithDelay(damageTextPrefab, exp, spawnPos, delay);
    }

    IEnumerator SpawnLevelUpTextWithDelay(float delay)
    {
        if (battleEffectController == null || playerUnit == null)
        {
            yield return new WaitForSeconds(delay);
            yield break;
        }

        yield return battleEffectController.SpawnLevelUpTextWithDelay(damageTextPrefab, playerUnit.transform, delay);
    }

    void SpawnDamageText(string text, Vector3 position, Color color)
    {
        if (battleEffectController == null) return;
        battleEffectController.SpawnDamageText(damageTextPrefab, text, position, color);
    }

    void SpawnOneShotEffect(GameObject prefab, Vector3 position, float returnDelay)
    {
        if (battleEffectController == null || prefab == null) return;
        battleEffectController.SpawnOneShotEffect(prefab, position, Quaternion.identity, returnDelay);
    }

    void SpawnNextEnemy()
    {
        if (stageFlowController == null) return;
        if (enemyUnit == null) return;

        stageFlowController.SpawnNextEnemyAfter(enemyUnit.transform.position);
        RefreshUpcomingEnemyStandbyVisuals();
    }

    void RefreshUpcomingEnemyStandbyVisuals()
    {
        if (stageFlowController == null) return;

        foreach (BattleUnit unit in stageFlowController.GetUpcomingEnemies())
        {
            if (unit == null) continue;

            unit.transform.localScale = Vector3.one * 0.8f;
            RestoreEnemyColors(unit);
            SetEnemyAlpha(unit, 1f);
            SetEnemyVisible(unit, false);
        }
    }

    void ActivateEnemyAsCurrent(BattleUnit unit)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.ActivateEnemyAsCurrent(unit);
            return;
        }

        if (unit == null) return;

        unit.transform.localScale = Vector3.one;
        SetEnemyVisible(unit, true);
        unit.SetUIActive(true);
        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 1f);
        unit.InitializeTurn();
    }

    void RestoreEnemyColors(BattleUnit unit)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.RestoreEnemyColors(unit);
            return;
        }

        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.r = 1f;
            c.g = 1f;
            c.b = 1f;
            sr.color = c;
        }
    }

    void SetEnemyVisible(BattleUnit unit, bool isVisible)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.SetEnemyVisible(unit, isVisible);
            return;
        }

        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            sr.enabled = isVisible;
        }

        unit.SetUIActive(isVisible);
    }

    void SetEnemyAlpha(BattleUnit unit, float alpha)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.SetEnemyAlpha(unit, alpha);
            return;
        }

        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    void RevealWaitingEnemy(BattleUnit unit)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.RevealWaitingEnemy(unit);
            return;
        }

        if (unit == null) return;

        SetEnemyVisible(unit, true);
        unit.SetUIActive(false);
        RestoreEnemyColors(unit);
        SetEnemyAlpha(unit, 0f);

        unit.transform.localScale = Vector3.one * 0.96f;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            sr.DOFade(1f, enemyRevealDuration);
        }

        unit.transform.DOScale(Vector3.one, enemyRevealDuration).SetEase(Ease.OutQuad);

        if (unit.animator != null)
        {
            unit.animator.Play("IDLE", 0, 0f);
        }
    }

    void HideAllUpcomingEnemies()
    {
        if (stageFlowController == null) return;

        if (enemyPresentationController != null)
        {
            enemyPresentationController.HideAllUpcomingEnemies(stageFlowController.GetUpcomingEnemies());
            return;
        }

        foreach (BattleUnit enemy in stageFlowController.GetUpcomingEnemies())
        {
            if (enemy == null) continue;
            SetEnemyVisible(enemy, false);
        }
    }

    void ShiftUpcomingEnemies(float deltaX, float duration)
    {
        if (stageFlowController == null) return;

        if (enemyPresentationController != null)
        {
            enemyPresentationController.ShiftUpcomingEnemies(stageFlowController.GetUpcomingEnemies(), deltaX, duration);
            return;
        }

        foreach (BattleUnit enemy in stageFlowController.GetUpcomingEnemies())
        {
            if (enemy == null) continue;
            enemy.transform.DOMoveX(enemy.transform.position.x + deltaX, duration).SetEase(roomTravelEase);
        }
    }

    void SetMoveAnimation(Animator animator, bool isMoving)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.SetMoveAnimation(animator, isMoving);
            return;
        }

        if (animator == null) return;

        bool hasMoveBool = false;
        foreach (var param in animator.parameters)
        {
            if (param.name == "1_Move" && param.type == AnimatorControllerParameterType.Bool)
            {
                hasMoveBool = true;
                break;
            }
        }

        if (hasMoveBool)
        {
            animator.SetBool("1_Move", isMoving);
        }
        else
        {
            if (isMoving) animator.Play("MOVE", 0, 0f);
            else animator.Play("IDLE", 0, 0f);
        }
    }

    void SetBoardInteractable(bool isInteractable)
    {
        isPlayerTurn = isInteractable;

        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.interactable = isInteractable;
            boardCanvasGroup.blocksRaycasts = isInteractable;
            boardCanvasGroup.DOFade(isInteractable ? 1.0f : 0.6f, 0.3f);
        }
    }
}