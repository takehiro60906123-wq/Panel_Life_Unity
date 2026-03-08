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

    private readonly Queue<BattleUnit> upcomingEnemies = new Queue<BattleUnit>();
    private int spawnedEnemyCount = 0;

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
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError("enemyPrefabs が未設定です。敵プレハブを1体以上登録してください。");
            enemyUnit = null;
            SetBoardInteractable(false);
            return;
        }

        if (battlePosition == null)
        {
            Debug.LogError("battlePosition が未設定です。");
            enemyUnit = null;
            SetBoardInteractable(false);
            return;
        }

        if (maxFloors <= 0)
        {
            Debug.LogWarning("maxFloors が 0 以下です。");
            enemyUnit = null;
            SetBoardInteractable(false);
            return;
        }

        int initialSpawnCount = Mathf.Min(maxVisibleEnemies, maxFloors);
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnNextEnemy();
        }

        if (upcomingEnemies.Count > 0)
        {
            enemyUnit = upcomingEnemies.Dequeue();
            ActivateEnemyAsCurrent(enemyUnit);
            SetEncounter(EncounterType.Enemy, 0);
            SetDungeonMist(true);
        }
        else
        {
            enemyUnit = null;
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
            if (playerUnit != null && playerUnit.animator != null) playerUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(EndPlayerTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit != null && playerUnit.animator != null) playerUnit.animator.Play("ATTACK", 0, 0f);
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
            if (playerUnit != null && playerUnit.animator != null) playerUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(EndPlayerTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            if (playerUnit != null && playerUnit.animator != null) playerUnit.animator.Play("ATTACK", 0, 0f);
            yield return new WaitForSeconds(0.08f);
            SpawnMagicBullet();
            yield return new WaitForSeconds(0.12f);
        }

        StartCoroutine(EndPlayerTurn());
    }

    IEnumerator EndPlayerTurn()
    {
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
            Debug.Log("ステージクリア！リザルトへ");
            SetBoardInteractable(false);
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

                if (hitEffectPrefab != null)
                {
                    GameObject hit = GetPooledObject(hitEffectPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
                    StartCoroutine(ReturnPooledObjectAfterDelay(hitEffectPrefab, hit, 0.7f));
                }

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

        if (hitEffectPrefab != null)
        {
            GameObject hit = GetPooledObject(hitEffectPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
            StartCoroutine(ReturnPooledObjectAfterDelay(hitEffectPrefab, hit, 0.7f));
        }

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
                if (levelUpEffectPrefab != null)
                {
                    GameObject effect = GetPooledObject(levelUpEffectPrefab, playerUnit.transform.position, Quaternion.identity);
                    StartCoroutine(ReturnPooledObjectAfterDelay(levelUpEffectPrefab, effect, 1.2f));
                }

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

        if (upcomingEnemies.Count == 0 && spawnedEnemyCount >= maxFloors)
        {
            SetEncounter(EncounterType.Enemy, 0);
            isEnemySpawning = false;
            SetBoardInteractable(false);
            Debug.Log("ステージクリア！リザルトへ");
            yield break;
        }

        bool forceEnemy = (prevEncounter == EncounterType.Empty || prevEncounter == EncounterType.Treasure);
        int roll = forceEnemy ? 0 : Random.Range(0, 100);

        if (forceEnemy || roll < 70)
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
        else if (roll < 90)
        {
            SetEncounter(EncounterType.Empty, 3);
            yield return StartCoroutine(EnterSafeRoomRoutine("平和な部屋だ", Color.white));
        }
        else
        {
            SetEncounter(EncounterType.Treasure, 1);
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
        if (upcomingEnemies.Count > 0)
        {
            nextEnemy = upcomingEnemies.Dequeue();
        }

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

    public void AdvanceEmptyTurn()
    {
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

    void SpawnMagicBullet()
    {
        if (magicBulletPrefab == null || enemyUnit == null) return;

        Vector3 start = playerUnit.transform.position + new Vector3(0.5f, 0.5f, 0);
        Vector3 target = enemyUnit.transform.position + Vector3.up * 0.5f;

        GameObject bullet = GetPooledObject(magicBulletPrefab, start, Quaternion.identity);
        if (bullet == null) return;

        bullet.transform.DOMove(target, 0.15f).SetEase(Ease.Linear).OnComplete(() =>
        {
            ReturnPooledObject(magicBulletPrefab, bullet);

            if (!isEnemySpawning)
            {
                DamageEnemy(1);
            }
        });
    }

    void SpawnEnergyOrb(Vector3 startPos, Vector3 target, float duration, float delay)
    {
        if (energyOrbPrefab == null) return;

        GameObject orb = GetPooledObject(energyOrbPrefab, startPos, Quaternion.identity);
        if (orb == null) return;

        orb.transform.DOMove(target, duration)
            .SetDelay(delay)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                ReturnPooledObject(energyOrbPrefab, orb);

                if (absorbEffectPrefab != null)
                {
                    GameObject absorb = GetPooledObject(absorbEffectPrefab, target, Quaternion.identity);
                    if (absorb != null)
                    {
                        StartCoroutine(ReturnPooledObjectAfterDelay(absorbEffectPrefab, absorb, 0.8f));
                    }
                }
            });
    }

    IEnumerator SpawnExpTextWithDelay(int exp, Vector3 spawnPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnDamageText($"+{exp} EXP", spawnPos, Color.green);
    }

    IEnumerator SpawnLevelUpTextWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnDamageText("LEVEL UP!", playerUnit.transform.position + Vector3.up * 1.5f, Color.yellow);
    }

    void SpawnDamageText(string text, Vector3 position, Color color)
    {
        if (damageTextPrefab == null) return;

        GameObject textObj = GetPooledObject(damageTextPrefab, position, Quaternion.identity);
        if (textObj == null) return;

        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>(true);
        if (tmp == null)
        {
            ReturnPooledObject(damageTextPrefab, textObj);
            return;
        }

        textObj.transform.position = position;
        textObj.transform.localScale = Vector3.one;

        Color baseColor = color;
        baseColor.a = 1f;

        tmp.text = text;
        tmp.color = baseColor;
        tmp.alpha = 1f;

        float randomX = Random.Range(-0.5f, 0.5f);
        Vector3 targetPos = new Vector3(position.x + randomX, position.y + 1.5f, position.z);

        Sequence seq = DOTween.Sequence();
        seq.Join(textObj.transform.DOMove(targetPos, 0.8f).SetEase(Ease.OutCirc));
        seq.Insert(0.2f, tmp.DOFade(0f, 0.8f));
        seq.OnComplete(() =>
        {
            ReturnPooledObject(damageTextPrefab, textObj);
        });
    }

    void SpawnNextEnemy()
    {
        if (spawnedEnemyCount >= maxFloors) return;
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;
        if (battlePosition == null) return;

        BattleUnit prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Vector3 spawnPos = battlePosition.position + (waitOffset * spawnedEnemyCount);

        BattleUnit newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        newEnemy.transform.localScale = Vector3.one * 0.8f;
        SetEnemyVisible(newEnemy, false);

        upcomingEnemies.Enqueue(newEnemy);
        spawnedEnemyCount++;
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
        if (enemyPresentationController != null)
        {
            enemyPresentationController.HideAllUpcomingEnemies(upcomingEnemies);
            return;
        }

        foreach (BattleUnit enemy in upcomingEnemies)
        {
            if (enemy == null) continue;
            SetEnemyVisible(enemy, false);
        }
    }

    void ShiftUpcomingEnemies(float deltaX, float duration)
    {
        if (enemyPresentationController != null)
        {
            enemyPresentationController.ShiftUpcomingEnemies(upcomingEnemies, deltaX, duration);
            return;
        }

        foreach (BattleUnit enemy in upcomingEnemies)
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

    GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (effectPoolManager == null)
        {
            return Instantiate(prefab, position, rotation);
        }

        return effectPoolManager.GetPooledObject(prefab, position, rotation);
    }

    void ReturnPooledObject(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;

        if (effectPoolManager == null)
        {
            Destroy(obj);
            return;
        }

        effectPoolManager.ReturnPooledObject(prefab, obj);
    }

    IEnumerator ReturnPooledObjectAfterDelay(GameObject prefab, GameObject obj, float delay)
    {
        if (obj == null) yield break;

        if (effectPoolManager == null)
        {
            yield return new WaitForSeconds(delay);
            Destroy(obj);
            yield break;
        }

        yield return effectPoolManager.ReturnPooledObjectAfterDelay(prefab, obj, delay);
    }
}