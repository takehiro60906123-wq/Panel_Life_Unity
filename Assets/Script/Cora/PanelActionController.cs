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

        List<Vector2Int> chain = panelBoardController.FindChain(row, col, clickedType);

        // 本来のパネル数（攻撃回数・ゲージ加算に使う）
        int primaryCount = chain.Count;

        // 攻撃パネルの隣接にLvUpがあったら巻き添え
        if (clickedType == PanelType.Sword)
        {
            chain.AddRange(panelBoardController.GetAdjacentLevelPanels(chain));
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
        StartCoroutine(AnimatedClearThenAction(clickedType, chain, primaryCount));
    }

    private IEnumerator AnimatedClearThenAction(PanelType type, List<Vector2Int> chain, int primaryCount)
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

        yield return ExecutePanelAction(type, primaryCount);
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
            {
                BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
                if (enemyUnit != null && !enemyUnit.IsDead())
                {
                    return enemyUnit.transform.position + Vector3.up * 0.8f;
                }

                return playerUnit.transform.position + Vector3.up * 1.0f;
            }

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
                PlayPlayerEmpowerArrivalFeedback();
                break;

            case PanelType.Ammo:
                if (battleUIController != null)
                {
                    battleUIController.PlayGunGaugeReceivePulse();
                }
                break;

            case PanelType.Coin:
                if (battleUIController != null)
                {
                    battleUIController.PlayCoinReceivePulse();
                }
                break;

            case PanelType.Heal:
                PlayPlayerEmpowerArrivalFeedback();
                break;

            case PanelType.LvUp:
                PlayEnemyLevelUpArrivalFeedback();
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

    private void PlayPlayerEmpowerArrivalFeedback()
    {
        Transform visualRoot = ResolvePlayerMeleeVisualRoot();
        if (visualRoot == null) return;

        visualRoot.DOKill();

        Vector3 baseScale = visualRoot.localScale;
        Vector3 basePos = visualRoot.localPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOScale(baseScale * 1.08f, 0.08f).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOLocalMoveY(basePos.y + 0.05f, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(visualRoot.DOScale(baseScale, 0.10f).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOLocalMoveY(basePos.y, 0.10f).SetEase(Ease.InOutQuad));
        seq.OnComplete(() =>
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = baseScale;
                visualRoot.localPosition = basePos;
            }
        });
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

    private IEnumerator ExecutePanelAction(PanelType type, int chainCount)
    {
        switch (type)
        {
            case PanelType.Sword:
                yield return StartCoroutine(PlayMeleeAttack(chainCount));
                break;

            // ============================================
            // �� Magic �� �e��p�l���iAmmo�j
            // �U�����o�Ȃ��B������ʒm���ăQ�[�W���Z�B
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
                yield return StartCoroutine(PlayEnemyLevelUp(chainCount));
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

    private IEnumerator PlaySingleMeleeHitTween()
    {
        Transform visualRoot = ResolvePlayerMeleeVisualRoot();

        if (visualRoot == null)
        {
            yield return new WaitForSeconds(0.18f);
            yield break;
        }

        visualRoot.DOKill();

        Vector3 baseLocalPos = visualRoot.localPosition;
        Vector3 baseLocalScale = visualRoot.localScale;
        Quaternion baseLocalRotation = visualRoot.localRotation;

        Vector3 windupPos = baseLocalPos + new Vector3(-meleeAttackDirectionX * meleeWindupDistance, -meleeHopY * 0.35f, 0f);
        Vector3 strikePos = baseLocalPos + new Vector3(meleeAttackDirectionX * meleeLungeDistance, meleeHopY, 0f);

        float totalDuration = meleeWindupDuration + meleeLungeDuration + meleeRecoverDuration;
        float hitMoment = meleeWindupDuration + (meleeLungeDuration * 0.75f);

        Sequence seq = DOTween.Sequence();
        seq.Append(visualRoot.DOLocalMove(windupPos, meleeWindupDuration).SetEase(Ease.OutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale * 0.94f, meleeWindupDuration).SetEase(Ease.OutQuad));

        seq.Append(visualRoot.DOLocalMove(strikePos, meleeLungeDuration).SetEase(Ease.OutExpo));
        seq.Join(visualRoot.DOScale(baseLocalScale * meleeHitScale, meleeLungeDuration).SetEase(Ease.OutBack));
        seq.Join(
            visualRoot.DOPunchRotation(
                new Vector3(0f, 0f, -meleePunchRotation * Mathf.Sign(meleeAttackDirectionX)),
                meleeLungeDuration + 0.03f,
                4,
                0.8f));

        seq.Append(visualRoot.DOLocalMove(baseLocalPos, meleeRecoverDuration).SetEase(Ease.InOutQuad));
        seq.Join(visualRoot.DOScale(baseLocalScale, meleeRecoverDuration).SetEase(Ease.OutQuad));

        yield return new WaitForSeconds(hitMoment);

        battleEventHub?.RaiseEnemyDamageRequested(GetCurrentMeleeDamage());

        yield return new WaitForSeconds(Mathf.Max(0f, totalDuration - hitMoment));

        visualRoot.localPosition = baseLocalPos;
        visualRoot.localScale = baseLocalScale;
        visualRoot.localRotation = baseLocalRotation;
    }

    private IEnumerator PlayMeleeAttack(int count)
    {
        count = Mathf.Max(1, count);

        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        if (enemyUnit == null || enemyUnit.IsDead())
        {
            yield return StartCoroutine(PlaySingleMeleeHitTween());

            // ����Q�[�W: �U���p�l���������Ƃ�+1
            battleEventHub?.RaiseSwordBonusGaugeRequested();

            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;
            if (enemyUnit == null || enemyUnit.IsDead()) break;

            yield return StartCoroutine(PlaySingleMeleeHitTween());

            if (i < count - 1)
            {
                yield return new WaitForSeconds(meleeComboInterval);
            }
        }

        // ����Q�[�W: �U���p�l���������Ƃ�+1�i�q�b�g���ł͂Ȃ�1��̏����s���ɂ�1�j
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

        // �e����W�C�x���g �� PanelBattleManager ������ �~ ammoGaugePerPanel �����Z
        battleEventHub?.RaiseAmmoCollected(panelCount);

        // ���W�̎艞���p�ɒZ���ҋ@�i�G�l���M�[�I�[�u���o����s���Ă���̂ł����͍ŏ����j
        yield return new WaitForSeconds(0.15f);

        yield return StartCoroutine(FinishTurn());
    }

    // =============================================================
    // LvUp �p�l�� �� �G���x���A�b�v
    // =============================================================

    private IEnumerator PlayEnemyLevelUp(int chainCount)
    {
        BattleUnit enemyUnit = getEnemyUnit != null ? getEnemyUnit() : null;

        // �G�����Ȃ� or ���S�� �� ���ʂȂ��Ń^�[���I��
        if (enemyUnit == null || enemyUnit.IsDead())
        {
            yield return StartCoroutine(FinishTurn());
            yield break;
        }

        // �G���x���A�b�v�K�p
        BattleUnit.EnemyLevelUpResult result = enemyUnit.EnemyLevelUp(chainCount);

        // --- ���o ---
        Vector3 enemyPos = enemyUnit.transform.position;

        // �x���e�L�X�g�i�Ԍn�j
        battleEventHub?.RaiseDamageTextRequested(
            "LEVEL UP!",
            enemyPos + Vector3.up * 2.0f,
            new Color(1f, 0.3f, 0.3f));

        yield return new WaitForSeconds(0.35f);

        // �X�e�[�^�X�ω��e�L�X�g�i�I�����W�n�j
        string statText = $"HP+{result.hpGained}  EXP+{result.expBonusGained}";
        battleEventHub?.RaiseDamageTextRequested(
            statText,
            enemyPos + Vector3.up * 1.4f,
            new Color(1f, 0.7f, 0.3f));

        yield return new WaitForSeconds(0.45f);

        // �^�[�������ŏI��
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