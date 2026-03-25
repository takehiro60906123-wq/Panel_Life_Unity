// =============================================================
// BattleCinematicEnhancer.cs
// 演出強化コントローラー — ゼルダ的な重厚演出を既存フローに上乗せする
//
// 配置:
//   PanelBattleManager と同じ GameObject にアタッチ。
//   BattleEventHub のイベントにフックするため、既存コードの改造は最小限。
//
// 3つの強化ポイント:
//   1. 敵撃破 — トドメ演出（スローモーション + 白閃光 + 衝撃波）
//   2. ステージクリア — 勝利の溜め + 盤面収束 + テキスト演出
//   3. ゲームオーバー — 崩壊感 + 暗転 + 沈黙
//
// ステージ開始は StageIntroController 側を強化するため、ここでは扱わない。
// =============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BattleCinematicEnhancer : MonoBehaviour
{
    // ─────────────────────────────────────────
    // 参照
    // ─────────────────────────────────────────
    [Header("参照（自動取得可）")]
    [SerializeField] private PanelBattleManager battleManager;
    [SerializeField] private BattleEventHub battleEventHub;

    // ─────────────────────────────────────────
    // 敵撃破演出
    // ─────────────────────────────────────────
    [Header("敵撃破 — トドメ演出")]
    [Tooltip("撃破時のスローモーション時間")]
    [SerializeField] private float defeatSlowMoDuration = 0.25f;
    [SerializeField] private float defeatSlowMoTimeScale = 0.15f;
    [SerializeField] private Color defeatFlashColor = new Color(1f, 0.98f, 0.9f, 1f);
    [SerializeField] private float defeatFlashAlpha = 0.4f;
    [SerializeField] private float defeatFlashFade = 0.18f;
    [SerializeField] private float defeatShakeIntensity = 0.08f;
    [SerializeField] private float defeatShakeDuration = 0.12f;
    [Tooltip("ボス撃破時は更に強い演出にする")]
    [SerializeField] private float bossDefeatSlowMoDuration = 0.50f;
    [SerializeField] private float bossDefeatSlowMoTimeScale = 0.05f;
    [SerializeField] private float bossDefeatShakeIntensity = 0.16f;
    [SerializeField] private float bossDefeatShakeDuration = 0.20f;
    [SerializeField] private Color bossDefeatFlashColor = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField] private float bossDefeatFlashAlpha = 0.6f;

    // ─────────────────────────────────────────
    // ステージクリア演出
    // ─────────────────────────────────────────
    [Header("ステージクリア")]
    [SerializeField] private float clearSlowMoDuration = 0.6f;
    [SerializeField] private float clearSlowMoTimeScale = 0.3f;
    [SerializeField] private Color clearFlashColor = new Color(1f, 0.98f, 0.85f, 1f);
    [SerializeField] private float clearFlashAlpha = 0.3f;

    // ─────────────────────────────────────────
    // ゲームオーバー演出
    // ─────────────────────────────────────────
    [Header("ゲームオーバー")]
    [SerializeField] private float gameOverSlowMoDuration = 0.8f;
    [SerializeField] private float gameOverSlowMoTimeScale = 0.1f;
    [SerializeField] private Color gameOverFlashColor = new Color(0.8f, 0.15f, 0.1f, 1f);
    [SerializeField] private float gameOverFlashAlpha = 0.3f;
    [SerializeField] private float gameOverShakeIntensity = 0.12f;
    [SerializeField] private float gameOverShakeDuration = 0.3f;

    // ─────────────────────────────────────────
    // 内部
    // ─────────────────────────────────────────
    private bool subscribed;
    private Coroutine slowMoCoroutine;

    private void Awake()
    {
        if (battleManager == null)
            battleManager = GetComponent<PanelBattleManager>();

        if (battleEventHub == null && battleManager != null)
            battleEventHub = battleManager.battleEventHub;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        // スローモーション中に無効化された場合の安全策
        if (Time.timeScale < 0.5f)
            Time.timeScale = 1f;
    }

    private void Subscribe()
    {
        if (subscribed || battleEventHub == null) return;

        battleEventHub.EnemyDefeated += OnEnemyDefeated;
        battleEventHub.StageClearRequested += OnStageClear;
        battleEventHub.PlayerDefeatedRequested += OnPlayerDefeated;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || battleEventHub == null) return;

        battleEventHub.EnemyDefeated -= OnEnemyDefeated;
        battleEventHub.StageClearRequested -= OnStageClear;
        battleEventHub.PlayerDefeatedRequested -= OnPlayerDefeated;
        subscribed = false;
    }

    // =============================================================
    // 1. 敵撃破 — トドメ演出
    // =============================================================
    //
    // イメージ:
    //   ゼルダでモンスターを倒した瞬間、一瞬だけ世界が止まって
    //   白い閃光が走り、衝撃が広がる。
    //   ボスの場合は更に長い溜め + 強い閃光。
    //
    // 実装:
    //   EnemyDefeated イベントは TakeDamage → IsDead 判定後に発火する。
    //   死亡アニメーションと並行して上乗せ演出を走らせる。

    private void OnEnemyDefeated(BattleUnit defeatedEnemy)
    {
        if (defeatedEnemy == null) return;

        bool isBoss = defeatedEnemy.enemyType == EnemyType.Boss;
        ScreenShakeController shakeCtrl = ScreenShakeController.Instance;

        if (isBoss)
        {
            // ── ボス撃破: 長い溜め + 強烈な閃光 ──
            if (shakeCtrl != null)
            {
                shakeCtrl.Flash(bossDefeatFlashColor, defeatFlashFade * 1.5f, bossDefeatFlashAlpha);
                shakeCtrl.Shake(bossDefeatShakeIntensity, bossDefeatShakeDuration, 24);
            }

            // 敵スプライトを一瞬拡大してから消える（威圧ズームの再利用）
            Transform enemyTransform = defeatedEnemy.transform;
            enemyTransform.DOKill();
            Sequence bossDeathSeq = DOTween.Sequence();
            bossDeathSeq.Append(
                enemyTransform.DOScale(Vector3.one * 1.3f, 0.08f).SetEase(Ease.OutQuad));
            bossDeathSeq.Append(
                enemyTransform.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad));

            PlaySlowMo(bossDefeatSlowMoDuration, bossDefeatSlowMoTimeScale);
        }
        else
        {
            // ── 通常敵撃破: 短い溜め + 軽い閃光 ──
            if (shakeCtrl != null)
            {
                shakeCtrl.Flash(defeatFlashColor, defeatFlashFade, defeatFlashAlpha);
                shakeCtrl.Shake(defeatShakeIntensity, defeatShakeDuration, 16);
            }

            PlaySlowMo(defeatSlowMoDuration, defeatSlowMoTimeScale);
        }
    }

    // =============================================================
    // 2. ステージクリア
    // =============================================================
    //
    // イメージ:
    //   ゼルダの祠クリア時。一瞬の静寂 → 光が広がる → テキスト。
    //   既存の ReturnToSceneWithResultRoutine の前に「溜め」を入れる。

    private void OnStageClear()
    {
        ScreenShakeController shakeCtrl = ScreenShakeController.Instance;

        // 光の閃光
        if (shakeCtrl != null)
        {
            shakeCtrl.Flash(clearFlashColor, 0.4f, clearFlashAlpha);
        }

        // スローモーション
        PlaySlowMo(clearSlowMoDuration, clearSlowMoTimeScale);

        // プレイヤーに勝利感を出す軽いパンチ
        if (battleManager != null && battleManager.playerUnit != null)
        {
            Transform playerTransform = battleManager.playerUnit.transform;
            playerTransform.DOKill();

            Sequence victorySeq = DOTween.Sequence();
            // 一瞬縮んで
            victorySeq.Append(
                playerTransform.DOScale(new Vector3(0.95f, 1.08f, 1f), 0.1f)
                    .SetEase(Ease.OutQuad));
            // ぐっと伸びる（ガッツポーズ的な）
            victorySeq.Append(
                playerTransform.DOScale(new Vector3(1.05f, 0.96f, 1f), 0.08f)
                    .SetEase(Ease.OutQuad));
            // 元に戻る
            victorySeq.Append(
                playerTransform.DOScale(Vector3.one, 0.12f)
                    .SetEase(Ease.OutBack));
        }
    }

    // =============================================================
    // 3. ゲームオーバー
    // =============================================================
    //
    // イメージ:
    //   ゼルダで倒された時。世界が急激にスローになり、
    //   色が抜けていき、沈黙が訪れる。
    //   既存の OnPlayerDefeated の前に「崩壊」の溜めを入れる。

    private void OnPlayerDefeated()
    {
        ScreenShakeController shakeCtrl = ScreenShakeController.Instance;

        // 赤い閃光
        if (shakeCtrl != null)
        {
            shakeCtrl.Flash(gameOverFlashColor, 0.5f, gameOverFlashAlpha);
            shakeCtrl.Shake(gameOverShakeIntensity, gameOverShakeDuration, 20);
        }

        // 長いスローモーション（崩壊感）
        PlaySlowMo(gameOverSlowMoDuration, gameOverSlowMoTimeScale);

        // プレイヤーがよろめく演出
        if (battleManager != null && battleManager.playerUnit != null)
        {
            Transform playerTransform = battleManager.playerUnit.transform;
            playerTransform.DOKill();

            Sequence defeatSeq = DOTween.Sequence();
            defeatSeq.SetUpdate(true); // スローモーション中でも動く

            // のけぞり
            defeatSeq.Append(
                playerTransform.DOLocalMoveX(
                    playerTransform.localPosition.x - 0.15f, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true));

            // がくっと沈む
            defeatSeq.Append(
                playerTransform.DOScale(new Vector3(1.05f, 0.92f, 1f), 0.2f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true));
        }
    }

    // =============================================================
    // スローモーション共通処理
    // =============================================================

    private void PlaySlowMo(float duration, float timeScale)
    {
        if (slowMoCoroutine != null)
        {
            StopCoroutine(slowMoCoroutine);
            Time.timeScale = 1f;
        }

        slowMoCoroutine = StartCoroutine(SlowMoRoutine(duration, timeScale));
    }

    private IEnumerator SlowMoRoutine(float duration, float targetTimeScale)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = targetTimeScale;

        // realtime で待つ（Time.timeScale に影響されない）
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // じわっと戻す（急に戻ると不自然）
        float restoreDuration = 0.15f;
        float restoreElapsed = 0f;
        while (restoreElapsed < restoreDuration)
        {
            restoreElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(restoreElapsed / restoreDuration);
            Time.timeScale = Mathf.Lerp(targetTimeScale, 1f, t * t); // ease-in で加速感
            yield return null;
        }

        Time.timeScale = 1f;
        slowMoCoroutine = null;
    }
}
