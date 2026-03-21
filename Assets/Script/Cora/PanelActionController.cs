using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PanelActionController : MonoBehaviour
{
    private BattleEventHub battleEventHub;
    private PanelBoardController panelBoardController;
    private BattleUnit playerUnit;
    private Func<BattleUnit> getEnemyUnit;
    private Func<IEnumerator> endPlayerTurnRoutine;
    private Func<int, int> modifyPlayerDamage;
    private Action<List<CollectedPanelItemInfo>> onAttachedItemsCollected;

    private PlayerCombatController playerCombatController;
    private BattleUIController battleUIController;
    private BattleDamageResolver battleDamageResolver;
    private PanelBattleManager panelBattleManager;

    private bool isProcessing;
    [Header("�ߐ�DoTween���o")]
    [SerializeField] private string playerMeleeVisualRootName = "PlayerVisual";
    [SerializeField] private float meleeAttackDirectionX = 1f;
    [SerializeField] private float meleeWindupDistance = 0.08f;
    [SerializeField] private float meleeLungeDistance = 0.26f;
    [SerializeField] private float meleeHopY = 0.05f;
    [SerializeField] private float meleeWindupDuration = 0.05f;
    [SerializeField] private float meleeLungeDuration = 0.08f;
    [SerializeField] private float meleeRecoverDuration = 0.10f;
    [SerializeField] private float meleeHitScale = 1.10f;
    [SerializeField] private float meleePunchRotation = 10f;
    [SerializeField] private float meleeComboInterval = 0.03f;

    [SerializeField] private float panelPreviewHoldDelay = 0.03f;

    [Header("Overlink 打撃感強化")]
    [SerializeField] private float overlinkImpactShakeMultiplier = 1.45f;
    [SerializeField] private float overlinkImpactShakeDuration = 0.12f;
    [SerializeField] private float overlinkExtraLungeDistance = 0.08f;
    [SerializeField] private float overlinkExtraHitScale = 0.12f;
    [SerializeField] private float overlinkExtraPunchRotation = 7f;
    [SerializeField] private float overlinkHitPause = 0.035f;

    public void Initialize(
       BattleEventHub battleEventHub,
       PanelBoardController panelBoardController,
       BattleUnit playerUnit,
       PlayerCombatController playerCombatController,
       BattleUIController battleUIController,
       Func<BattleUnit> getEnemyUnit,
       Func<IEnumerator> endPlayerTurnRoutine,
       Func<int, int> modifyPlayerDamage,
       Action<List<CollectedPanelItemInfo>> onAttachedItemsCollected)
    {
        this.battleEventHub = battleEventHub;
        this.panelBoardController = panelBoardController;
        this.playerUnit = playerUnit;
        this.playerCombatController = playerCombatController;
        this.battleUIController = battleUIController;
        this.getEnemyUnit = getEnemyUnit;
        this.endPlayerTurnRoutine = endPlayerTurnRoutine;
        this.modifyPlayerDamage = modifyPlayerDamage;
        this.onAttachedItemsCollected = onAttachedItemsCollected;
        battleDamageResolver = FindObjectOfType<BattleDamageResolver>();
        panelBattleManager = FindObjectOfType<PanelBattleManager>();
        isProcessing = false;
    }

    private int GetCurrentMeleeDamage()
    {
        if (playerCombatController == null)
            return 1;

        int damage = Mathf.Max(1, playerCombatController.GetMeleeAttack());
        if (modifyPlayerDamage != null)
        {
            damage = Mathf.Max(1, modifyPlayerDamage.Invoke(damage));
        }

        return damage;
    }

    public void OnPanelClicked(int row, int col)
    {
        if (isProcessing) return;
        if (panelBoardController == null) return;

        PanelType clickedType = panelBoardController.GetPanelType(row, col);
        if (clickedType == PanelType.None) return;

        isProcessing = true;
        battleEventHub?.RaiseBoardInteractableRequested(false);

        PanelBoardController.ChainResult chainResult = panelBoardController.FindChain(row, col, clickedType);
        List<Vector2Int> chain = chainResult.selected != null
            ? new List<Vector2Int>(chainResult.selected)
            : new List<Vector2Int>();

        // 本来のパネル数（攻撃回数・ゲージ加算に使う）
        int primaryCount = chain.Count;

        List<Vector2Int> adjacentCorruptPanels = panelBoardController.CollectAdjacentCorruptPanels(chain, clickedType);
        if (adjacentCorruptPanels != null && adjacentCorruptPanels.Count > 0)
        {
            chain.AddRange(adjacentCorruptPanels);
        }

        int resonanceBonus = 0;
        int overflow = 0;
        int overlinkBonus = 0;

        if (clickedType == PanelType.Sword)
        {
            resonanceBonus = panelBoardController.GetResonanceBonusForSelection(clickedType, chain);
            int effectiveSwordHits = primaryCount + resonanceBonus;
            overflow = Mathf.Max(0, chainResult.totalConnected - effectiveSwordHits);
            overlinkBonus = overflow / 2;

            if (resonanceBonus > 0 || overflow > 0)
            {
                Debug.Log($"[SwordChainBonus] total={chainResult.totalConnected} consumed={primaryCount} resonance=+{resonanceBonus} overflow={overflow} overlink=+{overlinkBonus}");
                ShowSwordChainBonusFeedback(resonanceBonus, overflow, overlinkBonus);
            }
        }

        List<CollectedPanelItemInfo> collectedItems = panelBoardController.ConsumeAttachedItems(chain);
        if (collectedItems != null && collectedItems.Count > 0)
        {
            onAttachedItemsCollected?.Invoke(collectedItems);
        }

        // 起点パネル反応
        panelBoardController.PlayTapFeedback(row, col, clickedType);

        // 連結全体の一瞬プレビュー
        panelBoardController.PlayChainPreviewFeedback(chain, clickedType);

        // 波紋消去 → エネルギーオーブ → アクション実行
        StartCoroutine(AnimatedClearThenAction(clickedType, chain, primaryCount, overlinkBonus, resonanceBonus));
    }
    private void ShowSwordChainBonusFeedback(int resonanceBonus, int overflow, int overlinkBonus)
    {
        if (battleEventHub == null)
        {
            return;
        }

        string text = null;
        bool hasResonance = resonanceBonus > 0;
        bool hasOverlink = overflow > 0;

        if (hasResonance && hasOverlink)
        {
            text = $"RESONANCE +{resonanceBonus} / OVERLINK +{Mathf.Max(0, overlinkBonus)}";
        }
        else if (hasResonance)
        {
            text = $"RESONANCE +{resonanceBonus}";
        }
        else if (hasOverlink)
        {
            text = $"OVERLINK +{Mathf.Max(0, overlinkBonus)}";
        }

        if (!string.IsNullOrEmpty(text))
        {
            battleEventHub.RaiseDamageTextRequested(text, Vector3.zero, Color.white);
        }
    }

    private IEnumerator AnimatedClearThenAction(PanelType type, List<Vector2Int> chain, int primaryCount, int overlinkBonus = 0, int resonanceBonus = 0)
    {
        if (playerUnit == null || panelBoardController == null)
        {
            isProcessing = false;
            yield break;
        }

        if (panelPreviewHoldDelay > 0f)
        {
            yield return new WaitForSeconds(panelPreviewHoldDelay);
        }

        float orbStartTime = Time.time;
        float orbArrivalTime = 0f;

        Vector3 targetPos = ResolvePanelEnergyTarget(type);
        float flyDuration = ResolvePanelEnergyDuration(type);
        float delay = 0f;

        foreach (Vector2Int pos in chain)
        {
            Vector3 panelWorldPos = panelBoardController.GetPanelWorldPosition(pos.x, pos.y);
            battleEventHub?.RaiseEnergyOrbRequested(type, panelWorldPos, targetPos, flyDuration, delay);
            delay += 0.04f;
        }

        orbArrivalTime = flyDuration + delay + 0.2f;

        yield return panelBoardController.ClearChainPanelsAnimated(chain, type);

        float elapsed = Time.time - orbStartTime;
        float remaining = orbArrivalTime - elapsed;
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
        }

        PlayPanelEnergyArrivalFeedback(type);

        yield return ExecutePanelAction(type, primaryCount, overlinkBonus, resonanceBonus);
    }

    private Vector3 ResolvePanelEnergyTarget(PanelType type)
    {
        switch (type)
        {
            case PanelType.Sword:
                return playerUnit.transform.position + Vector3.up * 0.72f;

            case PanelType.Ammo:
                if (battleUIController != null)
                {
                    Vector3 pos = battleUIController.GetGunGaugeWorldPosition();
                    if (pos != Vector3.zero) return pos;
                }
                return playerUnit.transform.position + Vector3.right * 1.2f + Vector3.up * 0.8f;

            case PanelType.Coin:
                if (battleUIController != null)
                {
                    Vector3 pos = battleUIController.GetCoinWorldPosition();
                    if (pos != Vector3.zero) return pos;
                }
                return playerUnit.transform.position + Vector3.left * 1.2f + Vector3.up * 1.2f;

            case PanelType.Heal:
                return playerUnit.transform.position + Vector3.up * 0.7f;

            case PanelType.LvUp:
                return playerUnit.transform.position + Vector3.up * 1.05f;

            default:
                return playerUnit.transform.position + Vector3.up * 0.5f;
        }
    }

    private float ResolvePanelEnergyDuration(PanelType type)
    {
        switch (type)
        {
            case PanelType.Sword:
                return 0.42f;
            case PanelType.Ammo:
            case PanelType.Coin:
                return 0.52f;
            default:
                return 0.48f;
        }
    }

    private void PlayPanelEnergyArrivalFeedback(PanelType type)
    {
        switch (type)
        {
            case PanelType.Sword:
                // プレイヤー配下の Canvas を一切巻き込まないため、
                // 到着時のプレイヤー拡大演出は使わない。
                break;

            case PanelType.Ammo:
                // 実際の装填感はゲージ加算後に個別スロットで返す
                break;

            case PanelType.Coin:
                if (battleUIController != null)
                {
                    battleUIController.PlayCoinReceivePulse();
                }
                break;

            case PanelType.Heal:
                // プレイヤー配下の Canvas に触れず、盤面波紋だけ返す。
                panelBoardController?.PlayHealWaveFeedback();
                break;

            case PanelType.LvUp:
                // EXP到着時はプレイヤーUIを触らず、盤面側へ起動・強化感だけ返す。
                panelBoardController?.PlayExpChargeFeedback();
                break;
        }
    }

    private Transform ResolveUnitVisualRoot(BattleUnit unit)
    {
        if (unit == null) return null;

        Transform t = unit.transform.Find("VisualRoot");
        if (t != null) return t;

        t = unit.transform.Find("UnitRoot");
        if (t != null) return t;

        t = unit.transform.Find("SpriteRoot");
        if (t != null) return t;

        for (int i = 0; i < unit.transform.childCount; i++)
        {
            Transform child = unit.transform.GetChild(i);
            if (child.name.Contains("Canvas")) continue;

            if (child.GetComponentInChildren<SpriteRenderer>(true) != null ||
                child.GetComponentInChildren<Animator>(true) != null)
            {
                return child;
            }
        }

        return unit.transform;
    }


    private bool ContainsCanvasInDescendants(Transform root)
    {
        if (root == null) return false;

        Stack<Transform> stack = new Stack<Transform>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            if (current == null) continue;
            if (current != root && current.GetComponent<Canvas>() != null) return true;
            for (int i = current.childCount - 1; i >= 0; i--)
            {
                stack.Push(current.GetChild(i));
            }
        }

        return false;
    }

    private Transform ResolveSafePlayerArrivalFeedbackRoot()
    {
        Transform candidate = ResolvePlayerMeleeVisualRoot();
        if (candidate == null) return null;

        if (!ContainsCanvasInDescendants(candidate))
        {
            return candidate;
        }

        Stack<Transform> stack = new Stack<Transform>();
        for (int i = candidate.childCount - 1; i >= 0; i--)
        {
            Transform child = candidate.GetChild(i);
            if (child == null) continue;
            if (child.GetComponent<Canvas>() != null || child.name.Contains("Canvas")) continue;
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            if (current == null) continue;
            if (current.GetComponent<Canvas>() != null || current.name.Contains("Canvas")) continue;

            bool hasVisual = current.GetComponent<SpriteRenderer>() != null ||
                             current.GetComponent<Animator>() != null ||
                             current.GetComponentInChildren<SpriteRenderer>(true) != null ||
                             current.GetComponentInChildren<Animator>(true) != null;

            if (hasVisual && !ContainsCanvasInDescendants(current))
            {
                return current;
            }

            for (int i = current.childCount - 1; i >= 0; i--)
            {
                Transform child = current.GetChild(i);
                if (child == null) continue;
                if (child.GetComponent<Canvas>() != null || child.name.Contains("Canvas")) continue;
                stack.Push(child);
            }
        }

        return candidate;
    }

    private void PlaySimpleArrivalPulse(Transform target, float scaleMultiplier, float moveUp, float scaleOutDuration, float scaleBackDuration)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 baseScale = target.localScale;
        Vector3 basePos = target.localPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(target.DOScale(baseScale * scaleMultiplier, scaleOutDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DOLocalMoveY(basePos.y + moveUp, scaleOutDuration).SetEase(Ease.OutQuad));
        seq.Append(target.DOScale(baseScale, scaleBackDuration).SetEase(Ease.InOutQuad));
        seq.Join(target.DOLocalMoveY(basePos.y, scaleBackDuration).SetEase(Ease.InOutQuad));
        seq.OnComplete(() =>
        {
            if (target != null)
            {
                target.localScale = baseScale;
                target.localPosition = basePos;
            }
        });
        seq.OnKill(() =>
        {
            if (target != null)
            {
                target.localScale = baseScale;
                target.localPosition = basePos;
            }
        });
    }

    private void PlayPlayerEmpowerArrivalFeedback()
    {
        // no-op
    }

    private void PlayEnemyLevelUpArrivalFeedback()
    {
        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
        if (enemyUnit == null || enemyUnit.IsDead()) return;

        Transform visualRoot = ResolveUnitVisualRoot(enemyUnit);
        if (visualRoot == null) return;

        visualRoot.DOKill();

        Vector3 baseScale = visualRoot.localScale;
        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOScale(baseScale * 1.14f, 0.09f).SetEase(Ease.OutQuad));
        seq.Append(visualRoot.DOScale(baseScale, 0.11f).SetEase(Ease.InOutQuad));
        seq.OnComplete(() =>
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale;
            }
        });
    }

    private void PlayPlayerExpArrivalFeedback()
    {
        // no-op
    }

    private IEnumerator ExecutePanelAction(PanelType type, int chainCount, int overlinkBonus = 0, int resonanceBonus = 0)
    {
        switch (type)
        {
            case PanelType.Sword:
                yield return StartCoroutine(PlayMeleeAttack(chainCount + Mathf.Max(0, resonanceBonus), overlinkBonus));
                break;

            // ============================================
            // 旧 Magic → 弾薬パネル（Ammo）
            // 攻撃は行わず。収集を通知してゲージ加算。
            // ============================================
            case PanelType.Ammo:
                yield return StartCoroutine(PlayAmmoCollect(chainCount));
                break;

            case PanelType.Heal:
                if (playerUnit != null) playerUnit.Heal(chainCount);
                yield return StartCoroutine(FinishTurn());
                break;

            case PanelType.Coin:
                battleEventHub?.RaiseCoinsGained(chainCount * 10);
                yield return StartCoroutine(FinishTurn());
                break;

            case PanelType.LvUp:
                yield return StartCoroutine(PlayExpCollect(chainCount));
                break;

            case PanelType.Corrupt:
                yield return StartCoroutine(PlayCorruptPanelClear(chainCount));
                break;

            default:
                yield return StartCoroutine(FinishTurn());
                break;
        }
    }

    private Transform ResolvePlayerMeleeVisualRoot()
    {
        if (playerUnit == null) return null;

        Transform t = playerUnit.transform.Find(playerMeleeVisualRootName);
        if (t != null) return t;

        t = playerUnit.transform.Find("VisualRoot");
        if (t != null) return t;

        t = playerUnit.transform.Find("UnitRoot");
        if (t != null) return t;

        for (int i = 0; i < playerUnit.transform.childCount; i++)
        {
            Transform child = playerUnit.transform.GetChild(i);
            if (child.name.Contains("Canvas")) continue;

            if (child.GetComponentInChildren<SpriteRenderer>(true) != null ||
                child.GetComponentInChildren<Animator>(true) != null)
            {
                return child;
            }
        }

        return playerUnit.transform;
    }

    private IEnumerator PlaySingleMeleeHitTween(int damageToApply, bool isOverlinkImpact = false)
    {
        Transform visualRoot = ResolvePlayerMeleeVisualRoot();

        if (visualRoot == null)
        {
            yield return new WaitForSeconds(0.18f + (isOverlinkImpact ? overlinkHitPause : 0f));
            yield break;
        }

        visualRoot.DOKill();

        Vector3 baseLocalPos = visualRoot.localPosition;
        Vector3 baseLocalScale = visualRoot.localScale;
        Quaternion baseLocalRotation = visualRoot.localRotation;

        float lungeDistance = meleeLungeDistance + (isOverlinkImpact ? overlinkExtraLungeDistance : 0f);
        float hitScale = meleeHitScale + (isOverlinkImpact ? overlinkExtraHitScale : 0f);
        float punchRotation = meleePunchRotation + (isOverlinkImpact ? overlinkExtraPunchRotation : 0f);

        Vector3 windupPos = baseLocalPos + new Vector3(-meleeAttackDirectionX * meleeWindupDistance, -meleeHopY * 0.35f, 0f);
        Vector3 strikePos = baseLocalPos + new Vector3(meleeAttackDirectionX * lungeDistance, meleeHopY, 0f);

        float totalDuration = meleeWindupDuration + meleeLungeDuration + meleeRecoverDuration;
        float hitMoment = meleeWindupDuration + (meleeLungeDuration * 0.75f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(windupPos, meleeWindupDuration).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * 0.94f, meleeWindupDuration).SetEase(Ease.OutQuad));

        seq.Append(visualRoot.DOLocalMove(strikePos, meleeLungeDuration).SetEase(Ease.OutExpo));
        seq.Join(visualRoot.DOScale(baseLocalScale * hitScale, meleeLungeDuration).SetEase(Ease.OutBack));
        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, -punchRotation * Mathf.Sign(meleeAttackDirectionX)),
                meleeLungeDuration + 0.03f,
                4,
                0.8f));

        seq.Append(visualRoot.DOLocalMove(baseLocalPos, meleeRecoverDuration).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale, meleeRecoverDuration).SetEase(Ease.OutQuad));

        yield return new WaitForSeconds(hitMoment);

        if (isOverlinkImpact)
        {
            panelBoardController?.PlayImpactShake(overlinkImpactShakeMultiplier, overlinkImpactShakeDuration);
            if (battleDamageResolver != null)
            {
                battleDamageResolver.SetNextDamageUseHeavyReaction(true);
                battleDamageResolver.SetNextDamageUseOverlinkTextBoost(true);
            }
        }

        battleEventHub?.RaiseEnemyDamageRequested(damageToApply);

        if (isOverlinkImpact && overlinkHitPause > 0f)
        {
            yield return new WaitForSeconds(overlinkHitPause);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, totalDuration - hitMoment));

        visualRoot.localPosition = baseLocalPos;
        visualRoot.localScale = baseLocalScale;
        visualRoot.localRotation = baseLocalRotation;
    }

    private IEnumerator PlayMeleeAttack(int count, int overlinkBonus = 0)
    {
        count = Mathf.Max(1, count);
        int baseDamage = GetCurrentMeleeDamage();

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        if (enemyUnit == null || enemyUnit.IsDead())
        {
            // 敵不在でも空振りモーション1回
            yield return StartCoroutine(PlaySingleMeleeHitTween(baseDamage + overlinkBonus, overlinkBonus > 0));

            // 銃ゲージ: 攻撃パネルを消したことで+1
            battleEventHub?.RaiseSwordBonusGaugeRequested();

            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            // --- オーバーリンクボーナスは1ヒット目だけ ---
            int hitDamage = baseDamage;
            if (i == 0 && overlinkBonus > 0)
            {
                hitDamage += overlinkBonus;
            }

            yield return StartCoroutine(PlaySingleMeleeHitTween(hitDamage, i == 0 && overlinkBonus > 0));

            if (i < count - 1)
            {
                yield return new WaitForSeconds(meleeComboInterval);
            }
        }

        // 銃ゲージ: 攻撃パネルを消したことで+1（ヒット数ではなく1回の処理行為に対し1）
        battleEventHub?.RaiseSwordBonusGaugeRequested();

        BattleUnitView playerView = playerUnit != null ? playerUnit.GetComponent<BattleUnitView>() : null;
        if (playerView != null)
        {
            playerView.PlayIdle();
        }

        yield return StartCoroutine(FinishTurn());
    }

    // ============================================
    // �e��p�l�����W�i�� PlayMagicAttack ��u���j
    // �U���͍s�킸�A������ʒm���ăQ�[�W���Z�̂݁B
    // ============================================
    private IEnumerator PlayAmmoCollect(int panelCount)
    {
        if (panelCount <= 0)
        {
            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        int gaugeBefore = playerCombatController != null ? playerCombatController.GetGunGauge() : 0;

        // 弾薬収集イベント → PanelBattleManager 側でゲージ加算
        battleEventHub?.RaiseAmmoCollected(panelCount);

        int gaugeAfter = playerCombatController != null ? playerCombatController.GetGunGauge() : gaugeBefore;
        int gaugeMax = playerCombatController != null ? playerCombatController.GetGunGaugeMax() : gaugeAfter;

        if (battleUIController != null)
        {
            battleUIController.PlayGunGaugeLoadSequence(gaugeBefore, gaugeAfter, gaugeMax);
        }

        yield return new WaitForSeconds(0.18f);

        yield return StartCoroutine(FinishTurn());
    }

    // =============================================================
    // LvUp パネル → プレイヤーEXP獲得
    // =============================================================

    private IEnumerator PlayExpCollect(int chainCount)
    {
        int expAmount = Mathf.Max(0, chainCount);
        if (expAmount <= 0)
        {
            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        bool isLevelUp = false;
        if (playerUnit != null)
        {
            isLevelUp = playerUnit.AddExp(expAmount);
        }

        if (panelBattleManager == null)
        {
            panelBattleManager = FindObjectOfType<PanelBattleManager>();
        }

        panelBattleManager?.RefreshPlayerExpUI();
        battleUIController?.PlayExpBarReceivePulse(isLevelUp);

        Vector3 expTextPos = playerUnit != null
            ? playerUnit.transform.position + Vector3.up * 1.8f
            : Vector3.zero;

        battleEventHub?.RaiseExpTextRequested(expAmount, expTextPos, 0f);

        if (isLevelUp)
        {
            battleEventHub?.RaiseLevelUpTextRequested(0.05f);
        }

        yield return new WaitForSeconds(0.14f);
        yield return StartCoroutine(FinishTurn());
    }

    private IEnumerator PlayCorruptPanelClear(int chainCount)
    {
        int cleared = Mathf.Max(0, chainCount);

        Vector3 textPos = playerUnit != null
            ? playerUnit.transform.position + Vector3.up * 1.8f
            : Vector3.zero;

        string text = cleared > 1 ? $"腐敗除去 x{cleared}" : "腐敗除去";
        battleEventHub?.RaiseDamageTextRequested(text, textPos, new Color(0.62f, 0.95f, 0.72f, 1f));

        yield return new WaitForSeconds(0.12f);
        yield return StartCoroutine(FinishTurn());
    }

    // =============================================================

    private IEnumerator FinishTurn()
    {
        if (endPlayerTurnRoutine != null)
        {
            yield return StartCoroutine(endPlayerTurnRoutine());
        }

        isProcessing = false;
    }
}