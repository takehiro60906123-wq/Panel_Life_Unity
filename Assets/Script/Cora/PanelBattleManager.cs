using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum PanelType { Sword, Magic, Coin, Heal, LvUp, Chick, Diamond, Monster, None }

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

    [SerializeField] private PlayerCombatLoadout playerLoadout;
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("アイテム設定")]
    [SerializeField] private BattleInventoryController battleInventoryController;
    [SerializeField] private float itemUseDelay = 0.08f;
    [SerializeField, Range(0f, 1f)] private float enemyItemDropChance = 0.35f;
    [SerializeField] private List<BattleItemDropEntry> itemDropEntries = new List<BattleItemDropEntry>();
    [SerializeField] private int scrapOnOverflow = 1;
    [SerializeField] private int currentScrap;
    [SerializeField] private float enemyItemDragDropRadius = 140f;

    [SerializeField, Range(1f, 1.5f)] private float enemyDragHoverScale = 1.08f;
    [SerializeField] private Color enemyDragHoverColor = new Color(1f, 0.95f, 0.8f, 1f);

    private bool defeatSequenceRunning;
    [SerializeField] private float defeatSequenceStartDelay = 0.22f;
    [SerializeField] private float dropFeedbackHoldDelay = 0.55f;
    [SerializeField] private float defeatSequenceBeforeRewardDelay = 0.25f;
    [SerializeField] private float defeatSequenceAfterRewardDelay = 0.55f;

    private struct PendingDropFeedback
    {
        public bool hasDrop;
        public bool overflow;
        public int slotIndex;
        public int scrapAmount;
        public string itemName;
        public Vector3 worldPos;
    }

    private PendingDropFeedback pendingDropFeedback;

    private bool enemyDragHoverActive;
    private Vector3 enemyDragHoverOriginalScale = Vector3.one;
    private BattleUnit enemyDragHoverTarget;

    public BattleInventoryController GetBattleInventoryController()
    {
        return battleInventoryController;
    }

    private void EnsureInventoryController()
    {
        if (battleInventoryController == null)
        {
            battleInventoryController = GetComponent<BattleInventoryController>();
        }

        if (battleInventoryController == null)
        {
            battleInventoryController = gameObject.AddComponent<BattleInventoryController>();
        }
    }

    private void EnsureDefaultItemDrops()
    {
        if (itemDropEntries != null && itemDropEntries.Count > 0)
        {
            return;
        }

        itemDropEntries = new List<BattleItemDropEntry>
    {
        new BattleItemDropEntry { itemType = BattleItemType.FieldBandage, weight = 35 },
        new BattleItemDropEntry { itemType = BattleItemType.ShockCanister, weight = 25 },
        new BattleItemDropEntry { itemType = BattleItemType.ActivationCell, weight = 25 },
        new BattleItemDropEntry { itemType = BattleItemType.MagneticCollectorCanister, weight = 15 }
    };
    }

    public void TryUseInventoryItem(int slotIndex)
    {
        if (!isPlayerTurn) return;
        if (battleInventoryController == null) return;

        BattleItemData item = battleInventoryController.GetItemAt(slotIndex);
        if (item == null) return;
        if (item.useTarget == BattleItemUseTarget.Enemy) return;

        BattleUnit target = null;
        if (!CanUseBattleItem(item, target))
        {
            return;
        }

        StartCoroutine(UseInventoryItemRoutine(slotIndex, target));
    }

    public void TryUseInventoryItemByDrag(int slotIndex, Vector2 screenPosition)
    {
        if (!isPlayerTurn) return;
        if (battleInventoryController == null) return;

        BattleItemData item = battleInventoryController.GetItemAt(slotIndex);
        if (item == null) return;
        if (item.useTarget != BattleItemUseTarget.Enemy) return;

        if (!TryGetDraggedEnemyTarget(screenPosition, out BattleUnit target))
        {
            return;
        }

        if (!CanUseBattleItem(item, target))
        {
            return;
        }

        StartCoroutine(UseInventoryItemRoutine(slotIndex, target));
    }

    private bool CanUseBattleItem(BattleItemData item, BattleUnit target)
    {
        if (item == null) return false;

        switch (item.itemType)
        {
            case BattleItemType.FieldBandage:
                return playerUnit != null
                    && !playerUnit.IsDead()
                    && playerUnit.CurrentHP < playerUnit.maxHP;

            case BattleItemType.ActivationCell:
                return playerCombatController != null
                    && playerCombatController.GetGunGauge() < playerCombatController.GetGunGaugeMax();

            case BattleItemType.ShockCanister:
                return target != null && !target.IsDead();

            case BattleItemType.MagneticCollectorCanister:
                return panelBoardController != null
                    && panelBoardController.GetPanelCount(PanelType.Coin) > 0;
        }

        return false;
    }

    public bool CanUseInventoryItemAt(int slotIndex)
    {
        if (!isPlayerTurn) return false;
        if (battleInventoryController == null) return false;

        BattleItemData item = battleInventoryController.GetItemAt(slotIndex);
        if (item == null) return false;

        BattleUnit target = null;

        if (item.useTarget == BattleItemUseTarget.Enemy)
        {
            if (!TryGetValidEnemyTarget(out target))
            {
                return false;
            }
        }

        return CanUseBattleItem(item, target);
    }

    public int GetCurrentScrap()
    {
        return currentScrap;
    }

    private bool TryGetDraggedEnemyTarget(Vector2 screenPosition, out BattleUnit target)
    {
        target = null;

        if (!TryGetValidEnemyTarget(out BattleUnit enemy))
        {
            return false;
        }

        if (!IsScreenPositionNearEnemy(screenPosition, enemy))
        {
            return false;
        }

        target = enemy;
        return true;
    }

    public void SetEnemyDragHoverByScreenPosition(Vector2 screenPosition)
    {
        if (!TryGetValidEnemyTarget(out BattleUnit enemy))
        {
            ClearEnemyDragHoverVisual();
            return;
        }

        bool isHovering = IsScreenPositionNearEnemy(screenPosition, enemy);
        ApplyEnemyDragHoverVisual(enemy, isHovering);
    }

    public void ClearEnemyDragHoverVisual()
    {
        if (!enemyDragHoverActive) return;

        if (enemyDragHoverTarget != null)
        {
            enemyDragHoverTarget.transform.localScale = enemyDragHoverOriginalScale;

            if (enemyPresentationController != null)
            {
                enemyPresentationController.RestoreEnemyColors(enemyDragHoverTarget);
            }
        }

        enemyDragHoverActive = false;
        enemyDragHoverTarget = null;
    }

    private void ApplyEnemyDragHoverVisual(BattleUnit target, bool isHovering)
    {
        if (target == null || !isHovering)
        {
            ClearEnemyDragHoverVisual();
            return;
        }

        if (!enemyDragHoverActive || enemyDragHoverTarget != target)
        {
            ClearEnemyDragHoverVisual();
            enemyDragHoverTarget = target;
            enemyDragHoverOriginalScale = target.transform.localScale;
            enemyDragHoverActive = true;
        }

        target.transform.localScale = enemyDragHoverOriginalScale * Mathf.Max(1f, enemyDragHoverScale);

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            Color c = renderers[i].color;
            c.r = enemyDragHoverColor.r;
            c.g = enemyDragHoverColor.g;
            c.b = enemyDragHoverColor.b;
            c.a = enemyDragHoverColor.a;
            renderers[i].color = c;
        }
    }

    private bool IsScreenPositionNearEnemy(Vector2 screenPosition, BattleUnit enemy)
    {
        if (enemy == null || enemy.IsDead())
        {
            return false;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return false;
        }

        Vector3 enemyWorldPos = enemy.transform.position + Vector3.up * 0.75f;
        Renderer enemyRenderer = enemy.GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            enemyWorldPos = enemyRenderer.bounds.center;
        }

        Vector2 enemyScreenPos = RectTransformUtility.WorldToScreenPoint(mainCam, enemyWorldPos);
        float radius = Mathf.Max(32f, enemyItemDragDropRadius);

        return Vector2.Distance(screenPosition, enemyScreenPos) <= radius;
    }

    private float GetBattleItemResolveDelay(BattleItemData item)
    {
        if (item == null)
        {
            return itemUseDelay;
        }

        switch (item.itemType)
        {
            case BattleItemType.MagneticCollectorCanister:
                return Mathf.Max(itemUseDelay, 0.45f);
        }

        return itemUseDelay;
    }

    private IEnumerator UseInventoryItemRoutine(int slotIndex, BattleUnit target)
    {
        SetBoardInteractable(false);

        BattleItemData item = battleInventoryController.RemoveAt(slotIndex);
        if (item == null)
        {
            SetBoardInteractable(true);
            yield break;
        }

        ApplyBattleItem(item, target);

        if (battleUIController != null)
        {
            battleUIController.RefreshInventoryUI();
            battleUIController.RefreshGunUI();
        }

        yield return new WaitForSeconds(GetBattleItemResolveDelay(item));
        StartCoroutine(EndPlayerTurn());
    }

    private void ApplyBattleItem(BattleItemData item, BattleUnit target)
    {
        if (item == null) return;

        switch (item.itemType)
        {
            case BattleItemType.FieldBandage:
                if (playerUnit != null)
                {
                    playerUnit.Heal(item.power);
                    Vector3 healPos = playerUnit.transform.position + Vector3.up * 1.5f;
                    SpawnDamageText($"+{item.power}", healPos, Color.green);
                }
                break;

            case BattleItemType.ActivationCell:
                if (playerCombatController != null)
                {
                    playerCombatController.AddGunGauge(item.power);
                }

                if (playerUnit != null)
                {
                    Vector3 cellPos = playerUnit.transform.position + Vector3.up * 1.5f;
                    SpawnDamageText($"GUN +{item.power}", cellPos, Color.cyan);
                }
                break;

            case BattleItemType.ShockCanister:
                if (target != null && !target.IsDead())
                {
                    battleEventHub?.RaiseEnemyDamageRequested(item.power);
                }
                break;

            case BattleItemType.MagneticCollectorCanister:
                if (panelBoardController != null)
                {
                    int collectedCoinPanels = panelBoardController.CollectAllPanelsOfType(PanelType.Coin);
                    if (collectedCoinPanels > 0)
                    {
                        int gainedCoins = collectedCoinPanels * 10;
                        AddCoins(gainedCoins);

                        Vector3 coinPos = playerUnit != null
                            ? playerUnit.transform.position + Vector3.up * 1.5f
                            : transform.position + Vector3.up * 1.5f;

                        SpawnDamageText($"+{gainedCoins}G", coinPos, new Color(1f, 0.9f, 0.2f));
                    }
                }
                break;
        }
    }

    private void TryAwardEnemyDrop(BattleUnit defeatedEnemy)
    {
        if (battleInventoryController == null) return;
        if (defeatedEnemy == null) return;
        if (itemDropEntries == null || itemDropEntries.Count == 0) return;
        if (UnityEngine.Random.value > enemyItemDropChance) return;

        BattleItemType itemType = RollDroppedItemType();
        if (itemType == BattleItemType.None) return;

        BattleItemData item = BattleItemData.CreatePreset(itemType);
        if (item == null) return;

        pendingDropFeedback = new PendingDropFeedback
        {
            hasDrop = false,
            overflow = false,
            slotIndex = -1,
            scrapAmount = 0,
            itemName = item.itemName,
            worldPos = defeatedEnemy.transform.position
        };

        int slotIndexBeforeAdd = battleInventoryController.Count;

        if (battleInventoryController.TryAddItem(item))
        {
            pendingDropFeedback.hasDrop = true;
            pendingDropFeedback.slotIndex = slotIndexBeforeAdd;
        }
        else
        {
            int addedScrap = Mathf.Max(0, scrapOnOverflow);
            currentScrap += addedScrap;

            pendingDropFeedback.hasDrop = true;
            pendingDropFeedback.overflow = true;
            pendingDropFeedback.scrapAmount = addedScrap;
        }
    }

    private IEnumerator PlayPendingDropFeedback()
    {
        if (!pendingDropFeedback.hasDrop) yield break;

        Vector3 effectPos = pendingDropFeedback.worldPos + Vector3.up * 0.9f;
        Vector3 textPos = pendingDropFeedback.worldPos + Vector3.up * 1.8f;

        if (pendingDropFeedback.overflow)
        {
            if (hitEffectPrefab != null)
            {
                SpawnOneShotEffect(hitEffectPrefab, effectPos, 0.25f);
            }

            SpawnDamageText("ITEM LOST", textPos + Vector3.up * 0.35f, new Color(1f, 0.6f, 0.6f));
            SpawnDamageText($"SCRAP +{pendingDropFeedback.scrapAmount}", textPos, Color.gray);

            if (battleUIController != null)
            {
                battleUIController.RefreshInventoryUI();
                battleUIController.PlayItemDropFeedback(-1, true);
            }
        }
        else
        {
            if (absorbEffectPrefab != null)
            {
                SpawnOneShotEffect(absorbEffectPrefab, effectPos, 0.55f);
            }
            else if (hitEffectPrefab != null)
            {
                SpawnOneShotEffect(hitEffectPrefab, effectPos, 0.25f);
            }

            SpawnDamageText("ITEM GET!", textPos + Vector3.up * 0.35f, new Color(1f, 0.9f, 0.25f));
            SpawnDamageText(pendingDropFeedback.itemName, textPos, Color.cyan);

            if (battleUIController != null)
            {
                battleUIController.RefreshInventoryUI();
                battleUIController.PlayItemDropFeedback(pendingDropFeedback.slotIndex, false);
            }
        }

        pendingDropFeedback = default;
        yield return new WaitForSeconds(dropFeedbackHoldDelay);
    }

    public IEnumerator PlayEnemyDefeatSequence(BattleUnit defeatedEnemy, System.Action resolveAfterSequence)
    {
        if (defeatSequenceRunning)
        {
            resolveAfterSequence?.Invoke();
            yield break;
        }

        defeatSequenceRunning = true;

        yield return new WaitForSeconds(defeatSequenceStartDelay);

        yield return StartCoroutine(PlayPendingDropFeedback());

        yield return new WaitForSeconds(defeatSequenceBeforeRewardDelay);

        resolveAfterSequence?.Invoke();

        yield return new WaitForSeconds(defeatSequenceAfterRewardDelay);

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
            battleUIController.RefreshInventoryUI();
        }

        defeatSequenceRunning = false;
    }

    private BattleItemType RollDroppedItemType()
    {
        if (itemDropEntries == null || itemDropEntries.Count == 0)
        {
            return BattleItemType.None;
        }

        int totalWeight = 0;
        for (int i = 0; i < itemDropEntries.Count; i++)
        {
            BattleItemDropEntry entry = itemDropEntries[i];
            if (entry == null) continue;
            if (entry.itemType == BattleItemType.None) continue;
            totalWeight += Mathf.Max(0, entry.weight);
        }

        if (totalWeight <= 0)
        {
            return BattleItemType.None;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        for (int i = 0; i < itemDropEntries.Count; i++)
        {
            BattleItemDropEntry entry = itemDropEntries[i];
            if (entry == null) continue;
            if (entry.itemType == BattleItemType.None) continue;

            cumulative += Mathf.Max(0, entry.weight);
            if (roll < cumulative)
            {
                return entry.itemType;
            }
        }

        return BattleItemType.None;
    }

    public void FirePistol()
    {
        if (!TryPrepareGunAction(out GunData gun, out BattleUnit target)) return;

        bool consumed = playerCombatController.ConsumeGunGauge();
        if (!consumed) return;

        StartCoroutine(FirePistolRoutine(gun, target));
    }

    private IEnumerator FirePistolRoutine(GunData gun, BattleUnit target)
    {
        yield return StartCoroutine(
            ExecuteGunRoutine(gun, target, gun.shotCount, 0.08f, "ピストル発射", 0.24f));
    }

    private void SpawnPistolMuzzleFlash()
    {
        if (playerUnit == null) return;
        if (pistolMuzzleFlashPrefab == null) return;

        Vector3 spawnPos = playerUnit.transform.position + new Vector3(0.6f, 0.35f, 0f);
        SpawnOneShotEffect(pistolMuzzleFlashPrefab, spawnPos, 0.2f);
    }

    private void SpawnPistolHitEffect(BattleUnit target)
    {
        if (target == null) return;
        if (hitEffectPrefab == null) return;

        Vector3 hitPos = target.transform.position + new Vector3(0f, 0.5f, 0f);
        SpawnOneShotEffect(hitEffectPrefab, hitPos, 0.25f);
    }

    private bool TryGetValidEnemyTarget(out BattleUnit target)
    {
        target = enemyUnit;
        if (target == null) return false;
        if (target.IsDead()) return false;
        return true;
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

        StartCoroutine(EndPlayerTurn());
    }

    private int ResolveGunHitDamage(GunData gun, BattleUnit target)
    {
        if (gun == null) return 0;

        int damage = gun.damagePerShot;

        if (gun.gunType == GunType.Shotgun && target != null && target.IsDangerEnemy())
        {
            damage += shotgunDangerBonusDamage;
        }

        return damage;
    }

    private void ApplyGunAfterEffects(GunData gun, BattleUnit target)
    {
        if (gun == null) return;
        if (target == null) return;
        if (target.IsDead()) return;

        if (gun.gunType == GunType.Shotgun)
        {
            TryDelayEnemyTurnByShotgun(target);
        }
    }

    private PlayerAnimationPresenter GetPlayerAnimationPresenter()
    {
        if (playerUnit == null) return null;
        return playerUnit.GetComponent<PlayerAnimationPresenter>();
    }

    private IEnumerator ExecuteGunRoutine(
 GunData gun,
 BattleUnit target,
 int shotCount,
 float interval,
 string logMessage,
 float finishDelay)
    {
        if (gun == null) yield break;
        if (target == null) yield break;
        if (target.IsDead()) yield break;

        PlayerAnimationPresenter playerAnim = GetPlayerAnimationPresenter();
        if (playerAnim != null)
        {
            playerAnim.PlayRunShoot();
        }

        int damagePerShot = ResolveGunHitDamage(gun, target);

        if (shotCount <= 1)
        {
            ExecuteGunHit(gun, target, damagePerShot);
        }
        else
        {
            yield return StartCoroutine(
                ExecuteRepeatedGunHitsRoutine(gun, target, shotCount, damagePerShot, interval));
        }

        ApplyGunAfterEffects(gun, target);

        yield return StartCoroutine(FinishGunActionRoutine(logMessage, finishDelay));
    }

    private void ExecuteGunHit(GunData gun, BattleUnit target, int damage)
    {
        if (gun == null) return;
        if (target == null) return;
        if (target.IsDead()) return;

        SpawnPistolMuzzleFlash();
        battleEventHub?.RaiseEnemyDamageRequested(damage);
    }

    private IEnumerator ExecuteRepeatedGunHitsRoutine(GunData gun, BattleUnit target, int shotCount, int damagePerShot, float interval)
    {
        for (int i = 0; i < shotCount; i++)
        {
            if (enemyUnit == null) break;
            if (enemyUnit.IsDead()) break;

            ExecuteGunHit(gun, target, damagePerShot);

            if (i < shotCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

    private bool TryPrepareGunAction(out GunData gun, out BattleUnit target, GunType? requiredGunType = null)
    {
        gun = null;
        target = null;

        if (playerCombatController == null) return false;

        gun = playerCombatController.GetGunData();
        if (gun == null) return false;

        if (requiredGunType.HasValue && gun.gunType != requiredGunType.Value) return false;

        bool canUse = gun.gunType == GunType.MachineGun
            ? playerCombatController.CanUseMachineGun()
            : playerCombatController.CanUseGun();

        if (!canUse) return false;
        if (!TryGetValidEnemyTarget(out target)) return false;

        return true;
    }

    private void HandleEnemyDefeatedByGun(BattleUnit defeatedEnemy)
    {
        isEnemyDefeatedThisTurn = true;

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
        }

        // まずは既存の通常撃破導線に乗せるための最低限
        StartCoroutine(EndPlayerTurn());
    }

    public void FireMachineGun()
    {
        if (!TryPrepareGunAction(out GunData gun, out BattleUnit target, GunType.MachineGun)) return;
        if (!playerCombatController.CanUseMachineGun()) return;

        int shotCount = playerCombatController.ConsumeAllGunGauge();
        if (shotCount <= 0) return;

        StartCoroutine(FireMachineGunRoutine(gun, target, shotCount));
    }

    private IEnumerator FireMachineGunRoutine(GunData gun, BattleUnit target, int shotCount)
    {
        yield return StartCoroutine(
            ExecuteGunRoutine(gun, target, shotCount, 0.05f, $"マシンガン発射: {shotCount}連射", 0.08f));
    }


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
    public float roomTravelDuration = 0.58f;
    public Ease roomTravelEase = Ease.OutCubic;
    public float enemyRevealDuration = 0.2f;


    [Header("バトル演出")]
    public GameObject damageTextPrefab;
    public GameObject hitEffectPrefab;
    public GameObject pistolMuzzleFlashPrefab;
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
    public StageConfig stageConfig;

    private CanvasGroup boardCanvasGroup;
    private EffectPoolManager effectPoolManager;

    private const int rows = 6;
    private const int cols = 6;

    public int BoardRows => rows;
    public int BoardCols => cols;
    public bool IsEnemySpawning => isEnemySpawning;
    public bool IsEnemyDefeatedThisTurn => isEnemyDefeatedThisTurn;

    private bool isEventHubSubscribed;
    [Header("銃の追加設定")]
    [SerializeField] private float shotgunInterval = 0.06f;
    [SerializeField] private float rifleAfterDelay = 0.30f;
    [SerializeField] private int shotgunDangerBonusDamage = 1;
    [SerializeField] private int shotgunDelayChance = 30;

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
        battleEventHub.EnemyDefeated += HandleEnemyDefeatedForDrops;

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
        battleEventHub.EnemyDefeated -= HandleEnemyDefeatedForDrops;

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

    private void HandleEnemyDefeatedForDrops(BattleUnit defeatedEnemy)
    {
        TryAwardEnemyDrop(defeatedEnemy);
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
        EnsureInventoryController();
        EnsureDefaultItemDrops();

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
            return;
        }

        if (battleUIController != null)
        {
            battleUIController.RefreshInventoryUI();
            battleUIController.RefreshGunUI();
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
        PlayerAnimationPresenter playerAnim = GetPlayerAnimationPresenter();
        if (playerAnim != null)
        {
            playerAnim.PlayRun();
        }

        if (roomTravelController != null)
        {
            yield return roomTravelController.TravelForward(playerUnit.transform, waitOffset);

            if (playerAnim != null)
            {
                playerAnim.PlayIdle();
            }

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

        if (playerAnim != null)
        {
            playerAnim.PlayIdle();
        }
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

    public void FireShotgun()
    {
        if (!TryPrepareGunAction(out GunData gun, out BattleUnit target, GunType.Shotgun)) return;

        bool consumed = playerCombatController.ConsumeGunGauge();
        if (!consumed) return;

        StartCoroutine(FireShotgunRoutine(gun, target));
    }

    private IEnumerator FireShotgunRoutine(GunData gun, BattleUnit target)
    {
        yield return StartCoroutine(
            ExecuteGunRoutine(gun, target, gun.shotCount, shotgunInterval, "ショットガン発射", 0.24f));
    }


    public void FireRifle()
    {
        if (!TryPrepareGunAction(out GunData gun, out BattleUnit target, GunType.Rifle)) return;

        bool consumed = playerCombatController.ConsumeGunGauge();
        if (!consumed) return;

        StartCoroutine(FireRifleRoutine(gun, target));
    }

    private IEnumerator FireRifleRoutine(GunData gun, BattleUnit target)
    {
        yield return StartCoroutine(
            ExecuteGunRoutine(gun, target, 1, 0f, "ライフル発射", rifleAfterDelay));
    }

    private void TryDelayEnemyTurnByShotgun(BattleUnit target)
    {
        if (target == null) return;
        if (target.IsDead()) return;

        bool success = UnityEngine.Random.Range(0, 100) < shotgunDelayChance;
        if (!success) return;

        target.DelayCooldown(1);

        Vector3 pos = target.transform.position + Vector3.up * 1.5f;
        SpawnDamageText("STAGGER", pos, Color.yellow);
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