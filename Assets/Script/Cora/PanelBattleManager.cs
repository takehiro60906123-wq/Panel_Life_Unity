using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public enum PanelType { Sword, Ammo, Coin, Heal, LvUp, Chick, Diamond, Monster, None }

public enum EncounterType
{
    Enemy,
    Empty,
    Treasure,
    Shop
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

    [Header("商店コントローラー")]
    public ShopController shopController;

    [Header("ダメージ解決コントローラー")]
    public BattleDamageResolver battleDamageResolver;

    [Header("銃戦闘コントローラー")]
    public GunCombatController gunCombatController;

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
    public PlayerCombatController PlayerCombatController => playerCombatController;
    public GunCombatController GunCombatController => gunCombatController;

    [Header("状態異常")]
    [SerializeField] private float paralysisSkipDelay = 0.6f;
    private bool paralysisSkipRunning;

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

    [Header("アイテム付きパネル設定")]
    [SerializeField, Range(0f, 1f)] private float panelItemSpawnChanceNormal = 0.35f;
    [SerializeField] private List<int> guaranteedPanelItemBattleNumbers = new List<int> { 10, 20, 25 };

    [Header("持続アイテム設定")]
    [SerializeField, Min(1)] private int attackOilDefaultDurationTurns = 3;
    [SerializeField, Range(0f, 1f)] private float attackOilDamageBonusRate = 0.25f;

    private int playerDamageBoostTurnsRemaining;
    private float playerDamageBoostRate;
    private bool skipNextPlayerBuffDecrement;

    private bool defeatSequenceRunning;
    [SerializeField] private float defeatSequenceStartDelay = 0.06f;
    [SerializeField] private float dropFeedbackHoldDelay = 0.18f;
    [SerializeField] private float defeatSequenceBeforeRewardDelay = 0.06f;
    [SerializeField] private float defeatSequenceAfterRewardDelay = 0.10f;

    [SerializeField] private PlayerProgression playerProgression;


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
        new BattleItemDropEntry { itemType = BattleItemType.FieldBandage, weight = 30 },
        new BattleItemDropEntry { itemType = BattleItemType.ShockCanister, weight = 22 },
        new BattleItemDropEntry { itemType = BattleItemType.ActivationCell, weight = 22 },
        new BattleItemDropEntry { itemType = BattleItemType.MagneticCollectorCanister, weight = 14 },
        new BattleItemDropEntry { itemType = BattleItemType.AttackOil, weight = 12 }
    };
    }

    public void PrepareItemPanelForCurrentBattle()
    {
        if (panelBoardController == null)
        {
            return;
        }

        panelBoardController.ClearAllAttachedItems();

        if (currentEncounter != EncounterType.Enemy)
        {
            return;
        }

        int battleNumber = 0;
        if (stageFlowController != null)
        {
            battleNumber = enemyUnit != null
                ? stageFlowController.DefeatedEnemyCount + 1
                : stageFlowController.DefeatedEnemyCount;
        }

        if (battleNumber <= 0)
        {
            return;
        }

        bool guaranteed = guaranteedPanelItemBattleNumbers != null
            && guaranteedPanelItemBattleNumbers.Contains(battleNumber);

        float chance = guaranteed ? 1f : panelItemSpawnChanceNormal;
        panelBoardController.TrySpawnAttachedItemPanel(chance);
    }

    public void HandleCollectedPanelItems(List<CollectedPanelItemInfo> collectedItems)
    {
        if (collectedItems == null || collectedItems.Count == 0)
        {
            return;
        }

        for (int i = 0; i < collectedItems.Count; i++)
        {
            CollectedPanelItemInfo info = collectedItems[i];
            if (info == null || info.item == null)
            {
                continue;
            }

            int slotIndexBeforeAdd = battleInventoryController != null
                ? battleInventoryController.Count : -1;

            if (battleInventoryController != null && battleInventoryController.TryAddItem(info.item))
            {
                SpawnDamageText($"ITEM GET!\n{info.item.itemName}", info.worldPosition + Vector3.up * 1.2f, new Color(1f, 0.9f, 0.25f));

                // ★ 飛行オーブ演出（パネル位置 → インベントリスロット）
                Vector3 targetPos = Vector3.zero;
                if (battleUIController != null)
                {
                    targetPos = battleUIController.GetInventorySlotWorldPosition(slotIndexBeforeAdd);
                }

                int capturedSlotIndex = slotIndexBeforeAdd;

                if (battleEffectController != null && targetPos != Vector3.zero)
                {
                    battleEffectController.SpawnItemGetOrb(
                        energyOrbPrefab,
                        absorbEffectPrefab,
                        info.worldPosition + Vector3.up * 0.4f,
                        targetPos,
                        0.4f,
                        () =>
                        {
                            battleUIController?.PlayInventorySlotReceivePulse(capturedSlotIndex);
                            battleUIController?.RefreshInventoryUI();
                        });
                }
                else
                {
                    // フォールバック（飛行先が取れない場合は従来演出）
                    if (absorbEffectPrefab != null)
                    {
                        SpawnOneShotEffect(absorbEffectPrefab, info.worldPosition + Vector3.up * 0.4f, 0.55f);
                    }
                }
            }
            else
            {
                int addedScrap = Mathf.Max(0, scrapOnOverflow);
                currentScrap += addedScrap;
                SpawnDamageText($"ITEM LOST\nSCRAP +{addedScrap}", info.worldPosition + Vector3.up * 1.2f, new Color(0.85f, 0.85f, 0.85f));
            }
        }

        battleUIController?.RefreshInventoryUI();
    }

    public int ApplyPlayerDamageModifiers(int baseDamage)
    {
        if (baseDamage <= 0)
        {
            return 0;
        }

        float multiplier = 1f;
        if (playerDamageBoostTurnsRemaining > 0)
        {
            multiplier += Mathf.Max(0f, playerDamageBoostRate);
        }

        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
    }

    private void ApplyAttackOilBuff(BattleItemData item)
    {
        int duration = item != null && item.durationTurns > 0
            ? item.durationTurns
            : attackOilDefaultDurationTurns;

        float bonusRate = item != null && item.power > 0
            ? item.power / 100f
            : attackOilDamageBonusRate;

        playerDamageBoostTurnsRemaining = Mathf.Max(1, duration);
        playerDamageBoostRate = Mathf.Max(0f, bonusRate);
        skipNextPlayerBuffDecrement = true;
    }

    private void TickPlayerTimedBuffsOnTurnEnd()
    {
        if (playerDamageBoostTurnsRemaining <= 0)
        {
            return;
        }

        if (skipNextPlayerBuffDecrement)
        {
            skipNextPlayerBuffDecrement = false;
            return;
        }

        playerDamageBoostTurnsRemaining--;
        if (playerDamageBoostTurnsRemaining <= 0)
        {
            playerDamageBoostTurnsRemaining = 0;
            playerDamageBoostRate = 0f;
        }
    }

    /// <summary>
    /// 敵のパッシブ状態異常（腐食など）のターン消費。
    /// プレイヤーターン終了時に呼ぶ。
    /// 将来パッシブ効果が増えたらここに足す。
    /// </summary>
    private void TickEnemyPassiveEffectsOnPlayerTurnEnd()
    {
        BattleUnit currentEnemy = enemyUnit;
        if (currentEnemy == null) return;
        StatusEffectHolder holder = currentEnemy.StatusEffects;
        if (holder == null) return;

        holder.ConsumeEffectTurn(StatusEffectType.Corrosion);
    }


    /// <summary>
    /// プレイヤーの駆動遅延。
    /// プレイヤーターン終了時、敵の cooldown を追加で1進める。
    /// 1ターン効果でも次の敵攻撃テンポに確実に影響するよう、
    /// EncounterFlowController の敵ターン開始前に適用する。
    /// </summary>
    private void ApplyPlayerSlowPenaltyOnTurnEnd()
    {
        if (currentEncounter != EncounterType.Enemy) return;
        if (playerUnit == null || playerUnit.StatusEffects == null) return;
        if (!playerUnit.StatusEffects.HasEffect(StatusEffectType.Slow)) return;

        BattleUnit currentEnemy = enemyUnit;
        if (currentEnemy != null && !currentEnemy.IsDead())
        {
            int beforeCooldown = currentEnemy.currentCooldown;
            currentEnemy.TickCooldown();

            if (currentEnemy.currentCooldown < beforeCooldown)
            {
                Vector3 textPos = playerUnit.transform.position + Vector3.up * 1.5f;
                SpawnDamageText("駆動遅延！", textPos, new Color(0.45f, 0.9f, 1f));
            }
        }

        playerUnit.StatusEffects.ConsumeEffectTurn(StatusEffectType.Slow);
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

            case BattleItemType.AttackOil:
                return playerUnit != null && !playerUnit.IsDead();
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

            case BattleItemType.AttackOil:
                ApplyAttackOilBuff(item);
                if (playerUnit != null)
                {
                    Vector3 oilPos = playerUnit.transform.position + Vector3.up * 1.5f;
                    SpawnDamageText($"攻撃UP {playerDamageBoostTurnsRemaining}T", oilPos, new Color(1f, 0.7f, 0.2f));
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

        // 待たずに流す
        if (pendingDropFeedback.hasDrop)
        {
            StartCoroutine(PlayPendingDropFeedback());
        }

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


    // PanelBattleManager に追加
    public void UpdateFloorUI()
    {
        if (battleUIController == null || stageFlowController == null) return;

        int totalFloors = stageFlowController.TotalBattles;

        int currentFloor;
        if (enemyUnit != null)
        {
            currentFloor = Mathf.Clamp(stageFlowController.DefeatedEnemyCount + 1, 1, totalFloors);
        }
        else
        {
            currentFloor = Mathf.Clamp(stageFlowController.DefeatedEnemyCount, 1, totalFloors);
        }

        battleUIController.SetFloorText(currentFloor, totalFloors);
    }

    private bool TryGetValidEnemyTarget(out BattleUnit target)
    {
        target = enemyUnit;
        if (target == null) return false;
        if (target.IsDead()) return false;
        return true;
    }


    private PlayerAnimationPresenter GetPlayerAnimationPresenter()
    {
        if (playerUnit == null) return null;
        return playerUnit.GetComponent<PlayerAnimationPresenter>();
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

            UpdateFloorUI();
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
    public GameObject energyOrbPrefab;
    public GameObject absorbEffectPrefab;
    public GameObject levelUpEffectPrefab;

    [Header("パネルエネルギー色")]
    [SerializeField] private Color swordEnergyColor = new Color(0.52f, 0.92f, 1f, 1f);
    [SerializeField] private Color ammoEnergyColor = new Color(0.92f, 0.95f, 1f, 1f);
    [SerializeField] private Color coinEnergyColor = new Color(1f, 0.86f, 0.22f, 1f);
    [SerializeField] private Color healEnergyColor = new Color(0.52f, 1f, 0.52f, 1f);
    [SerializeField] private Color levelUpEnergyColor = new Color(0.76f, 0.42f, 1f, 1f);

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

    // ============================================
    // 弾薬パネルのゲージ供給量
    // ============================================
    [Header("弾薬パネル設定")]
    [Tooltip("弾薬パネル1個あたりのゲージ供給量（主食）")]
    [SerializeField] private int ammoGaugePerPanel = 2;

    [Tooltip("攻撃パネル消去時のゲージ供給量（おやつ）")]
    [SerializeField] private int swordGaugeBonusPerAction = 1;

    private void SubscribeBattleEvents()
    {
        if (battleEventHub == null || isEventHubSubscribed)
        {
            return;
        }

        battleEventHub.BoardInteractableRequested += HandleBoardInteractableRequested;
        battleEventHub.CoinsGained += HandleCoinsGained;
        battleEventHub.EnergyOrbRequested += HandleEnergyOrbRequested;
        battleEventHub.AmmoCollected += HandleAmmoCollected;
        battleEventHub.SwordBonusGaugeRequested += HandleSwordBonusGauge;
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
        battleEventHub.AmmoCollected -= HandleAmmoCollected;
        battleEventHub.SwordBonusGaugeRequested -= HandleSwordBonusGauge;
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

    private void HandleEnergyOrbRequested(PanelType panelType, Vector3 startPos, Vector3 target, float duration, float delay)
    {
        SpawnEnergyOrb(panelType, startPos, target, duration, delay);
    }

    // ============================================
    // 弾薬パネル収集 → 銃ゲージ加算（主食: +2/枚）
    // ============================================
    private void HandleAmmoCollected(int panelCount)
    {
        if (playerCombatController == null) return;
        if (panelCount <= 0) return;

        int gaugeGain = panelCount * ammoGaugePerPanel;
        playerCombatController.AddGunGauge(gaugeGain);

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
        }
    }

    // ============================================
    // 攻撃パネル消去 → 銃ゲージ微量加算（おやつ: +1/回）
    // ============================================
    private void HandleSwordBonusGauge()
    {
        if (playerCombatController == null) return;

        playerCombatController.AddGunGauge(swordGaugeBonusPerAction);

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
        }
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

        InitializeShop();

        if (battleUIController != null)
        {
            battleUIController.RefreshInventoryUI();
            battleUIController.RefreshGunUI();
        }

        UpdateFloorUI();
        RefreshPlayerExpUI();
    }

    private void OnDestroy()
    {
        UnsubscribeBattleEvents();
    }

    // ============================================
    // 商店の初期化
    // ============================================
    private void InitializeShop()
    {
        if (shopController == null) return;

        shopController.Initialize(
            playerCombatController,
            battleInventoryController,
            playerUnit,
            battleUIController,
            () => currentCoins,
            (amount) => AddCoins(amount));

        // EncounterFlowController に商店コントローラーを渡す
        if (encounterFlowController != null)
        {
            encounterFlowController.SetShopController(shopController);
        }
    }

    public int GetCurrentCoins()
    {
        return currentCoins;
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

        if (encounterType != EncounterType.Enemy)
        {
            panelBoardController?.ClearAllAttachedItems();
        }

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

        TickPlayerTimedBuffsOnTurnEnd();
        ApplyPlayerSlowPenaltyOnTurnEnd();
        TickEnemyPassiveEffectsOnPlayerTurnEnd();
        if (battleUIController != null)
        {
            battleUIController.RefreshInventoryUI();
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

    public void SpawnEnergyOrb(PanelType panelType, Vector3 startPos, Vector3 target, float duration, float delay)
    {
        if (battleEffectController == null) return;

        battleEffectController.SpawnEnergyOrb(
            energyOrbPrefab,
            absorbEffectPrefab,
            startPos,
            target,
            duration,
            delay,
            ResolvePanelEnergyColor(panelType));
    }

    private Color ResolvePanelEnergyColor(PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.Sword:
                return swordEnergyColor;
            case PanelType.Ammo:
                return ammoEnergyColor;
            case PanelType.Coin:
                return coinEnergyColor;
            case PanelType.Heal:
                return healEnergyColor;
            case PanelType.LvUp:
                return levelUpEnergyColor;
            default:
                return Color.white;
        }
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

    public void SpawnOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay)
    {
        if (battleEffectController == null || prefab == null) return;
        battleEffectController.SpawnOneShotEffect(prefab, position, rotation, returnDelay);
    }


    public void SpawnOneShotEffect(GameObject prefab, Vector3 position, Quaternion rotation, float returnDelay, Vector3 scale)
    {
        if (battleEffectController == null || prefab == null) return;
        battleEffectController.SpawnOneShotEffect(prefab, position, rotation, returnDelay, scale);
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

        UpdateFloorUI();
        PrepareItemPanelForCurrentBattle();
    }

    public void SetBoardInteractable(bool isInteractable)
    {
        // === 状態異常: プレイヤー金縛りチェック ===
        // 戦闘中にプレイヤーターンが始まろうとしたとき、
        // 金縛り中なら盤面を開放せずターンをスキップする。
        if (isInteractable
            && !paralysisSkipRunning
            && currentEncounter == EncounterType.Enemy
            && playerUnit != null
            && playerUnit.StatusEffects != null
            && playerUnit.StatusEffects.HasEffect(StatusEffectType.Paralysis))
        {
            StartCoroutine(HandlePlayerParalysisSkip());
            return;
        }

        isPlayerTurn = isInteractable;

        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.interactable = isInteractable;
            boardCanvasGroup.blocksRaycasts = isInteractable;
            boardCanvasGroup.DOFade(isInteractable ? 1.0f : 0.6f, 0.3f);
        }
    }

    /// <summary>
    /// プレイヤー金縛り時のターンスキップ処理。
    /// 盤面を開放せず、スキップ演出後に EndPlayerTurn() を呼ぶ。
    /// EndPlayerTurn() → 敵ターン → SetBoardInteractable(true) と巡回し、
    /// まだ金縛りが残っていれば再びここに来る。
    /// paralysisSkipRunning フラグで多重起動を防止する。
    /// </summary>
    private System.Collections.IEnumerator HandlePlayerParalysisSkip()
    {
        if (paralysisSkipRunning)
        {
            yield break;
        }

        paralysisSkipRunning = true;

        // 盤面ロック状態を維持
        isPlayerTurn = false;

        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.interactable = false;
            boardCanvasGroup.blocksRaycasts = false;
        }

        // 演出テキスト
        Vector3 textPos = playerUnit != null
            ? playerUnit.transform.position + Vector3.up * 1.5f
            : Vector3.up * 1.5f;
        SpawnDamageText("金縛り！", textPos, new Color(0.8f, 0.4f, 1f));

        yield return new WaitForSeconds(paralysisSkipDelay);

        // 行動スキップ後に残りターンを消費
        if (playerUnit != null && playerUnit.StatusEffects != null)
        {
            playerUnit.StatusEffects.ConsumeEffectTurn(StatusEffectType.Paralysis);
        }

        paralysisSkipRunning = false;

        // 通常のターン終了処理へ（敵ターンに遷移する）
        yield return StartCoroutine(EndPlayerTurn());
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

    public void RefreshPlayerExpUI()
    {
        if (battleUIController == null) return;
        if (playerUnit == null) return;

        PlayerProgression progression = playerUnit.GetComponent<PlayerProgression>();
        if (progression == null) return;

        float progress = progression.GetExpProgress01();
        battleUIController.SetPlayerExpBar(progress);

        Debug.Log($"[PanelBattleManager] ExpBar progress={progress}");
    }
}
