using System.Collections;
using DG.Tweening;
using UnityEngine;

public class StageIntroController : MonoBehaviour
{
    [Header("有効化")]
    [SerializeField] private bool playOnGameStart = true;

    [Header("黒フェード")]
    [SerializeField] private CanvasGroup introBlackOverlay;
    [SerializeField] private float blackHoldDuration = 0.18f;
    [SerializeField] private float blackFadeDuration = 0.65f;

    [Header("プレイヤー入場")]
    [SerializeField] private Vector3 playerStartOffset = new Vector3(-3.2f, 0f, 0f);
    [SerializeField] private float playerEntranceDelay = 0.10f;
    [SerializeField] private float playerEntranceDuration = 0.90f;
    [SerializeField] private float playerStartAlpha = 0.35f;
    [SerializeField] private Ease playerEntranceEase = Ease.OutCubic;

    [Header("盤面")]
    [SerializeField] private float boardRevealDuration = 0.18f;
    [SerializeField] private float boardDropDelayAfterEnemy = 0.08f;

    [Header("敵登場")]
    [SerializeField] private float enemyEntranceDelay = 0.12f;
    [SerializeField] private float enemyEntranceSettleDuration = 0.26f;
    [SerializeField] private float unlockDelayAfterBoardDrop = 0.10f;

    public bool ShouldPlayOnGameStart => playOnGameStart;

    public void BeginGameStartIntro(PanelBattleManager manager)
    {
        if (manager == null)
        {
            return;
        }

        PrepareInitialVisualState(manager);
        StartCoroutine(PlayGameStartIntroRoutine(manager));
    }

    private void PrepareInitialVisualState(PanelBattleManager manager)
    {
        CanvasGroup boardCanvasGroup = EnsureBoardCanvasGroup(manager);
        if (boardCanvasGroup != null)
        {
            boardCanvasGroup.DOKill();
            boardCanvasGroup.alpha = 0f;
            boardCanvasGroup.interactable = false;
            boardCanvasGroup.blocksRaycasts = false;
        }

        if (introBlackOverlay != null)
        {
            introBlackOverlay.DOKill();
            introBlackOverlay.alpha = 1f;
            introBlackOverlay.interactable = false;
            introBlackOverlay.blocksRaycasts = false;
            introBlackOverlay.gameObject.SetActive(true);
        }

        manager.SetDungeonMist(false, true);

        PreparePlayer(manager.playerUnit);
        PrepareEnemy(manager.enemyUnit, manager.enemyPresentationController);

        if (manager.panelBoardController != null)
        {
            manager.panelBoardController.PrepareBoardForIntroDrop();
        }
    }

    private IEnumerator PlayGameStartIntroRoutine(PanelBattleManager manager)
    {
        if (manager == null)
        {
            yield break;
        }

        CanvasGroup boardCanvasGroup = EnsureBoardCanvasGroup(manager);
        BattleUnit playerUnit = manager.playerUnit;
        BattleUnit enemyUnit = manager.enemyUnit;
        EnemyPresentationController enemyPresentation = manager.enemyPresentationController;
        PlayerAnimationPresenter playerAnim = playerUnit != null
            ? playerUnit.GetComponent<PlayerAnimationPresenter>()
            : null;

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blackHoldDuration);
        }

        if (introBlackOverlay != null)
        {
            introBlackOverlay.DOFade(0f, blackFadeDuration).SetEase(Ease.OutQuad);
        }

        manager.SetDungeonMist(true, false);

        if (playerEntranceDelay > 0f)
        {
            yield return new WaitForSeconds(playerEntranceDelay);
        }

        if (playerAnim != null)
        {
            playerAnim.PlayRun();
        }

        yield return PlayPlayerEntrance(playerUnit);

        if (playerAnim != null)
        {
            playerAnim.PlayIdle();
        }

        if (enemyEntranceDelay > 0f)
        {
            yield return new WaitForSeconds(enemyEntranceDelay);
        }

        if (enemyPresentation != null && enemyUnit != null)
        {
            enemyPresentation.PlayCurrentEnemyEntrance(enemyUnit);
        }

        if (enemyEntranceSettleDuration > 0f)
        {
            yield return new WaitForSeconds(enemyEntranceSettleDuration);
        }

        if (boardDropDelayAfterEnemy > 0f)
        {
            yield return new WaitForSeconds(boardDropDelayAfterEnemy);
        }

        Tween boardRevealTween = null;
        if (boardCanvasGroup != null)
        {
            boardRevealTween = boardCanvasGroup
                .DOFade(1f, boardRevealDuration)
                .SetEase(Ease.OutQuad);
        }

        if (manager.panelBoardController != null)
        {
            yield return manager.panelBoardController.PlayIntroBoardDropRoutine();
        }

        if (boardRevealTween != null && boardRevealTween.IsActive())
        {
            yield return boardRevealTween.WaitForCompletion();
        }

        if (unlockDelayAfterBoardDrop > 0f)
        {
            yield return new WaitForSeconds(unlockDelayAfterBoardDrop);
        }

        manager.SetBoardInteractable(true);

        if (introBlackOverlay != null)
        {
            introBlackOverlay.gameObject.SetActive(false);
        }

        if (playerUnit != null)
        {
            playerUnit.SetUIActive(true);
        }
    }

    private IEnumerator PlayPlayerEntrance(BattleUnit playerUnit)
    {
        if (playerUnit == null)
        {
            yield break;
        }

        Transform root = playerUnit.transform;
        SpriteRenderer[] renderers = playerUnit.GetComponentsInChildren<SpriteRenderer>(true);
        Vector3 targetPos = root.position;
        Vector3 startPos = targetPos + playerStartOffset;

        root.DOKill(false);
        root.position = startPos;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            sr.DOKill(false);
            sr.enabled = true;

            Color c = sr.color;
            c.a = playerStartAlpha;
            sr.color = c;
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(root.DOMove(targetPos, playerEntranceDuration).SetEase(playerEntranceEase));

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;
            seq.Join(sr.DOFade(1f, playerEntranceDuration * 0.85f).SetEase(Ease.OutQuad));
        }

        yield return seq.WaitForCompletion();
    }

    private void PreparePlayer(BattleUnit playerUnit)
    {
        if (playerUnit == null)
        {
            return;
        }

        playerUnit.SetUIActive(false);

        SpriteRenderer[] renderers = playerUnit.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            sr.DOKill(false);
            sr.enabled = true;

            Color c = sr.color;
            c.a = playerStartAlpha;
            sr.color = c;
        }
    }

    private void PrepareEnemy(BattleUnit enemyUnit, EnemyPresentationController enemyPresentation)
    {
        if (enemyUnit == null || enemyPresentation == null)
        {
            return;
        }

        enemyPresentation.PrepareEnemyForDeferredEntrance(enemyUnit);
    }

    private CanvasGroup EnsureBoardCanvasGroup(PanelBattleManager manager)
    {
        if (manager == null || manager.boardParent == null)
        {
            return null;
        }

        CanvasGroup cg = manager.boardParent.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = manager.boardParent.gameObject.AddComponent<CanvasGroup>();
        }

        manager.SetBoardCanvasGroup(cg);
        return cg;
    }
}
