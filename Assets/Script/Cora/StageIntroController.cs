// =============================================================
// StageIntroController.cs — ゼルダ的演出強化版
//
// 追加演出:
//   ・暗闇の中の地鳴り（不穏な腐海の入口）
//   ・暗転から段階的に明ける（一気に明るくしない）
//   ・プレイヤー着地のスクワッシュ＆ストレッチ + 軽い衝撃
//   ・着地後の「間」（ゼルダ的な溜め）
//   ・霧がじわっと晴れる演出との連動
// =============================================================
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

    // ─── 演出強化フィールド ───

    [Header("前触れ（腐海の不穏さ）")]
    [SerializeField] private float forebodingDelay = 0.25f;
    [SerializeField] private float forebodingShakeIntensity = 0.04f;
    [SerializeField] private float forebodingShakeDuration = 0.35f;
    [SerializeField] private int forebodingShakeVibrato = 8;

    [Header("段階的フェード")]
    [Tooltip("完全な暗闘からまず薄暗い状態に遷移する")]
    [SerializeField] private float dimPhaseAlpha = 0.65f;
    [SerializeField] private float dimPhaseDuration = 0.35f;
    [SerializeField] private float dimPhaseHold = 0.15f;

    [Header("プレイヤー着地")]
    [SerializeField] private float landingSquashX = 1.12f;
    [SerializeField] private float landingSquashY = 0.88f;
    [SerializeField] private float landingSquashDuration = 0.06f;
    [SerializeField] private float landingRecoverDuration = 0.12f;
    [SerializeField] private float landingShakeIntensity = 0.03f;
    [SerializeField] private float postLandingPause = 0.30f;

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

        ScreenShakeController shakeCtrl = ScreenShakeController.Instance;

        // =============================================
        // フェーズ 1: 暗闇の中の前触れ
        // =============================================

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blackHoldDuration);
        }

        // 暗闇の中で地鳴り — 「何かの中に入っていく」不穏さ
        if (forebodingDelay > 0f)
        {
            yield return new WaitForSeconds(forebodingDelay);
        }

        if (shakeCtrl != null)
        {
            shakeCtrl.Shake(forebodingShakeIntensity, forebodingShakeDuration, forebodingShakeVibrato);
        }

        yield return new WaitForSeconds(forebodingShakeDuration * 0.5f);

        // =============================================
        // フェーズ 2: 段階的に明ける（一気に明るくしない）
        // =============================================

        if (introBlackOverlay != null)
        {
            // まず薄暗い状態まで
            introBlackOverlay.DOFade(dimPhaseAlpha, dimPhaseDuration).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(dimPhaseDuration);

            // 少し薄暗いまま保持（目が慣れる「間」）
            yield return new WaitForSeconds(dimPhaseHold);

            // 完全に明ける
            introBlackOverlay.DOFade(0f, blackFadeDuration).SetEase(Ease.OutQuad);
        }

        // 霧がじわっと晴れる
        manager.SetDungeonMist(true, false);

        // =============================================
        // フェーズ 3: プレイヤー入場
        // =============================================

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

        // ── 着地のスクワッシュ＆ストレッチ ──
        if (playerUnit != null)
        {
            Transform pt = playerUnit.transform;
            pt.DOKill();

            // 横に潰れる（着地の重さ）
            pt.DOScale(new Vector3(landingSquashX, landingSquashY, 1f), landingSquashDuration)
                .SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(landingSquashDuration);

            // 元に戻る（弾力）
            pt.DOScale(Vector3.one, landingRecoverDuration).SetEase(Ease.OutBack);

            // 軽い衝撃
            if (shakeCtrl != null)
            {
                shakeCtrl.Shake(landingShakeIntensity, 0.06f, 10);
            }
        }

        // ── ゼルダ的「間」— 着地後の静寂 ──
        yield return new WaitForSeconds(postLandingPause);

        // =============================================
        // フェーズ 4: 敵登場
        // =============================================

        if (enemyEntranceDelay > 0f)
        {
            yield return new WaitForSeconds(enemyEntranceDelay);
        }

        if (enemyUnit != null)
        {
            if (manager.bossIntroController != null && BossIntroController.IsBossUnit(enemyUnit))
            {
                yield return manager.StartCoroutine(manager.PlayEnemyEntranceRoutine(enemyUnit));
            }
            else if (enemyPresentation != null)
            {
                enemyPresentation.PlayCurrentEnemyEntrance(enemyUnit);
            }
        }

        if (enemyEntranceSettleDuration > 0f)
        {
            yield return new WaitForSeconds(enemyEntranceSettleDuration);
        }

        // =============================================
        // フェーズ 5: 盤面登場
        // =============================================

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

        // =============================================
        // 完了 — 盤面アンロック
        // =============================================

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

    // =============================================================
    // プレイヤー入場アニメーション
    // =============================================================

    private IEnumerator PlayPlayerEntrance(BattleUnit playerUnit)
    {
        if (playerUnit == null)
        {
            yield break;
        }

        Transform root = playerUnit.transform;
        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(playerUnit);

        // PreparePlayer で既に startPos に移動済みなので targetPos を逆算
        Vector3 startPos = root.position;
        Vector3 targetPos = startPos - playerStartOffset;

        root.DOKill(false);

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

    // =============================================================
    // 初期状態設定
    // =============================================================

    private void PreparePlayer(BattleUnit playerUnit)
    {
        if (playerUnit == null)
        {
            return;
        }

        playerUnit.SetUIActive(false);

        // 入場開始位置に移動（元の位置で一瞬映るのを防ぐ）
        Transform root = playerUnit.transform;
        root.position = root.position + playerStartOffset;

        // 完全に透明にする（影は除外）
        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(playerUnit);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            sr.DOKill(false);
            sr.enabled = true;

            Color c = sr.color;
            c.a = 0f;  // 完全に透明（入場演出で playerStartAlpha まで戻す）
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
