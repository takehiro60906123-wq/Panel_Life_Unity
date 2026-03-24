// =============================================================
// BossIntroController.cs
// ボス登場カットイン演出
//
// 配置:
//   PanelBattleManager と同じ GameObject、または子オブジェクトにアタッチ。
//   Inspector で参照を設定する。
//
// 使い方:
//   ボス戦開始時（敵がボスタイプと判定された直後）に呼ぶ。
//   既存の EnemyPresentationController.PlayEntranceAnimation() の代わりに、
//   こちらの PlayBossIntro() を使う。
//
//   yield return StartCoroutine(bossIntroController.PlayBossIntro(bossUnit));
//
// 演出フロー:
//   1. 盤面ロック＋霧が濃くなる
//   2. BGM ダッキング（音が引く）
//   3. 画面シェイク（地鳴り）
//   4. 画面暗転
//   5. ボス名バナー スライドイン
//   6. ボスシルエット登場 → 全体表示
//   7. バナー退出
//   8. 霧が晴れる＋盤面アンロック
//
// 設計方針:
//   StageIntroController と同じコルーチンベースの構造。
//   既存の ScreenShakeController があれば連携する（なくても動く）。
//   DungeonMistController があれば霧演出も入る（なくても動く）。
// =============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BossIntroController : MonoBehaviour
{
    // ─────────────────────────────────────────
    // 参照（Inspector で設定）
    // ─────────────────────────────────────────
    [Header("バナー UI")]
    [Tooltip("ボス名を表示するバナーの親 RectTransform。初期状態で非表示にしておく。")]
    [SerializeField] private RectTransform bannerRoot;
    [SerializeField] private CanvasGroup bannerCanvasGroup;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text bossSubtitleText;

    [Header("暗転オーバーレイ")]
    [Tooltip("画面全体を覆う Image。ScreenShakeController の flashOverlay とは別に、\n暗転用に専用の黒 Image を用意する。初期は透明。")]
    [SerializeField] private Image darkOverlay;

    [Header("盤面参照")]
    [SerializeField] private PanelBoardController panelBoardController;
    [SerializeField] private DungeonMistController dungeonMistController;

    // ─────────────────────────────────────────
    // フェーズ 1: 前触れ（地鳴り）
    // ─────────────────────────────────────────
    [Header("前触れ")]
    [SerializeField] private float forebodingDelay = 0.3f;
    [SerializeField] private float forebodingShakeDuration = 0.45f;
    [SerializeField] private float forebodingShakeIntensity = 0.06f;
    [SerializeField] private int forebodingShakeVibrato = 10;

    // ─────────────────────────────────────────
    // フェーズ 2: 暗転
    // ─────────────────────────────────────────
    [Header("暗転")]
    [SerializeField] private float darkenDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float darkenAlpha = 0.72f;
    [SerializeField] private float darkenHoldDuration = 0.15f;

    // ─────────────────────────────────────────
    // フェーズ 3: バナー
    // ─────────────────────────────────────────
    [Header("バナー演出")]
    [SerializeField] private float bannerSlideInOffsetX = 600f;
    [SerializeField] private float bannerSlideInDuration = 0.22f;
    [SerializeField] private Ease bannerSlideInEase = Ease.OutCubic;
    [SerializeField] private float bannerSettlePunchScale = 0.08f;
    [SerializeField] private float bannerHoldDuration = 0.9f;
    [SerializeField] private float bannerSlideOutDuration = 0.18f;
    [SerializeField] private Ease bannerSlideOutEase = Ease.InCubic;

    // ─────────────────────────────────────────
    // フェーズ 4: ボス登場
    // ─────────────────────────────────────────
    [Header("ボス登場")]
    [SerializeField] private float bossRevealDelay = 0.10f;
    [SerializeField] private float bossScaleUpDuration = 0.22f;
    [SerializeField] private float bossStartScale = 0.4f;
    [SerializeField] private Ease bossScaleUpEase = Ease.OutBack;
    [SerializeField] private float bossLandingShakeIntensity = 0.10f;
    [SerializeField] private float bossLandingShakeDuration = 0.12f;

    // ─────────────────────────────────────────
    // フェーズ 5: 回復
    // ─────────────────────────────────────────
    [Header("回復")]
    [SerializeField] private float lightenDuration = 0.35f;
    [SerializeField] private float unlockDelay = 0.15f;

    // ─────────────────────────────────────────
    // BGM ダッキング
    // ─────────────────────────────────────────
    [Header("BGM ダッキング（任意）")]
    [Tooltip("BGM の AudioSource。設定すると前触れ時にボリュームを下げる。")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private float bgmDuckVolume = 0.15f;
    [SerializeField] private float bgmDuckDuration = 0.3f;
    [SerializeField] private float bgmRestoreDuration = 0.5f;

    // ─────────────────────────────────────────
    // SE
    // ─────────────────────────────────────────
    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip bossRumbleSe;
    [SerializeField] private AudioClip bossAppearSe;

    // ─────────────────────────────────────────
    // 内部状態
    // ─────────────────────────────────────────
    private Vector2 bannerBaseAnchoredPos;
    private bool hasCachedBannerPos;
    private float bgmOriginalVolume = 1f;
    private BattleSfxController battleSfxController;

    private void Awake()
    {
        CacheBannerPosition();
        HideBannerImmediate();

        if (panelBoardController == null)
        {
            panelBoardController = GetComponent<PanelBoardController>();
        }

        if (dungeonMistController == null)
        {
            dungeonMistController = GetComponent<DungeonMistController>();
        }

        battleSfxController = GetComponent<BattleSfxController>();
        if (battleSfxController == null)
        {
            battleSfxController = FindObjectOfType<BattleSfxController>();
        }
    }

    private void CacheBannerPosition()
    {
        if (bannerRoot != null && !hasCachedBannerPos)
        {
            bannerBaseAnchoredPos = bannerRoot.anchoredPosition;
            hasCachedBannerPos = true;
        }
    }

    private void HideBannerImmediate()
    {
        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 0f;
        }

        if (bannerRoot != null)
        {
            bannerRoot.gameObject.SetActive(false);
        }
    }

    // =============================================================
    // 公開 API
    // =============================================================

    /// <summary>
    /// ボスイントロ演出を実行する。
    /// コルーチンとして yield return で呼ぶ。
    /// bossUnit は既にシーン上に配置済み（非表示状態）である前提。
    /// </summary>
    public IEnumerator PlayBossIntro(BattleUnit bossUnit, string bossName = null, string subtitle = null)
    {
        if (bossUnit == null) yield break;

        // ─── 準備 ───
        SetBossVisibility(bossUnit, false);
        SetupBannerText(bossName, subtitle, bossUnit);

        // ─── フェーズ 1: 前触れ ───
        yield return new WaitForSeconds(forebodingDelay);

        PlaySe(bossRumbleSe);
        DuckBgm();

        // 霧を濃くする
        if (dungeonMistController != null)
        {
            dungeonMistController.ApplyBattleState(true, false);
        }

        // 地鳴りシェイク
        PlayForebodingShake();

        yield return new WaitForSeconds(forebodingShakeDuration);

        // ─── フェーズ 2: 暗転 ───
        yield return PlayDarkenRoutine();

        yield return new WaitForSeconds(darkenHoldDuration);

        // ─── フェーズ 3: バナー ───
        yield return PlayBannerRoutine();

        // ─── フェーズ 4: ボス登場 ───
        yield return new WaitForSeconds(bossRevealDelay);

        PlaySe(bossAppearSe);
        yield return PlayBossRevealRoutine(bossUnit);

        // ─── フェーズ 5: 回復 ───
        yield return PlayLightenRoutine();

        RestoreBgm();

        yield return new WaitForSeconds(unlockDelay);
    }

    /// <summary>
    /// ボスかどうかの簡易判定。
    /// 呼び出し側で使う用。
    /// </summary>
    public static bool IsBossUnit(BattleUnit unit)
    {
        if (unit == null) return false;
        return unit.enemyType == EnemyType.Boss;
    }

    // =============================================================
    // フェーズ実装
    // =============================================================

    private void PlayForebodingShake()
    {
        // ScreenShakeController があればそちらを使う
        ScreenShakeController shakeController = ScreenShakeController.Instance;
        if (shakeController != null)
        {
            shakeController.Shake(forebodingShakeIntensity, forebodingShakeDuration, forebodingShakeVibrato);
            return;
        }

        // フォールバック: 盤面シェイク
        if (panelBoardController != null)
        {
            panelBoardController.PlayImpactShake(forebodingShakeIntensity / 3f, forebodingShakeDuration);
        }
    }

    private IEnumerator PlayDarkenRoutine()
    {
        if (darkOverlay == null) yield break;

        darkOverlay.gameObject.SetActive(true);
        darkOverlay.raycastTarget = false;

        Color c = darkOverlay.color;
        c.a = 0f;
        darkOverlay.color = c;

        darkOverlay.DOKill();
        darkOverlay.DOFade(darkenAlpha, darkenDuration).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(darkenDuration);
    }

    private IEnumerator PlayBannerRoutine()
    {
        if (bannerRoot == null) yield break;

        CacheBannerPosition();

        // スライドイン準備
        bannerRoot.gameObject.SetActive(true);

        Vector2 startPos = bannerBaseAnchoredPos + new Vector2(bannerSlideInOffsetX, 0f);
        bannerRoot.anchoredPosition = startPos;
        bannerRoot.localScale = Vector3.one;

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.alpha = 1f;
        }

        // スライドイン
        bannerRoot.DOAnchorPos(bannerBaseAnchoredPos, bannerSlideInDuration)
            .SetEase(bannerSlideInEase);

        yield return new WaitForSeconds(bannerSlideInDuration);

        // 着地パンチ
        bannerRoot.DOPunchScale(
            new Vector3(bannerSettlePunchScale, bannerSettlePunchScale, 0f),
            0.12f,
            6,
            0.8f);

        // ホールド
        yield return new WaitForSeconds(bannerHoldDuration);

        // スライドアウト
        Vector2 exitPos = bannerBaseAnchoredPos + new Vector2(-bannerSlideInOffsetX, 0f);
        bannerRoot.DOAnchorPos(exitPos, bannerSlideOutDuration)
            .SetEase(bannerSlideOutEase);

        if (bannerCanvasGroup != null)
        {
            bannerCanvasGroup.DOFade(0f, bannerSlideOutDuration * 0.8f);
        }

        yield return new WaitForSeconds(bannerSlideOutDuration);

        bannerRoot.gameObject.SetActive(false);
    }

    private IEnumerator PlayBossRevealRoutine(BattleUnit bossUnit)
    {
        if (bossUnit == null) yield break;

        Transform bossTransform = bossUnit.transform;

        // 小さい状態で表示開始
        bossTransform.localScale = Vector3.one * bossStartScale;
        SetBossVisibility(bossUnit, true);
        SetBossAlpha(bossUnit, 1f);

        // スケールアップ
        bossTransform.DOScale(Vector3.one, bossScaleUpDuration)
            .SetEase(bossScaleUpEase);

        yield return new WaitForSeconds(bossScaleUpDuration);

        // 着地インパクト
        ScreenShakeController shakeController = ScreenShakeController.Instance;
        if (shakeController != null)
        {
            shakeController.Shake(bossLandingShakeIntensity, bossLandingShakeDuration, 20);
            shakeController.Flash(new Color(1f, 0.95f, 0.8f), 0.10f, 0.25f);
        }
        else if (panelBoardController != null)
        {
            panelBoardController.PlayImpactShake(1.5f, bossLandingShakeDuration);
        }

        // ボスの EnemyTweenPresenter で軽い着地演出
        EnemyTweenPresenter tweenPresenter = bossUnit.GetComponent<EnemyTweenPresenter>();
        if (tweenPresenter == null)
        {
            tweenPresenter = bossUnit.GetComponentInChildren<EnemyTweenPresenter>();
        }

        if (tweenPresenter != null)
        {
            tweenPresenter.EnsureSetup();
            // 着地で少し潰れて戻る
            bossTransform.DOPunchScale(
                new Vector3(0.08f, -0.12f, 0f),
                0.14f,
                4,
                0.7f);
        }

        yield return new WaitForSeconds(0.18f);
    }

    private IEnumerator PlayLightenRoutine()
    {
        if (darkOverlay == null) yield break;

        darkOverlay.DOKill();
        darkOverlay.DOFade(0f, lightenDuration).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(lightenDuration);

        darkOverlay.gameObject.SetActive(false);
    }

    // =============================================================
    // ユーティリティ
    // =============================================================

    private void SetupBannerText(string bossName, string subtitle, BattleUnit bossUnit)
    {
        string resolvedName = bossName;

        if (string.IsNullOrEmpty(resolvedName) && bossUnit != null)
        {
            // BattleUnit に名前があればそれを使う
            BattleUnitView view = bossUnit.GetComponent<BattleUnitView>();
            if (view != null)
            {
                // unitName があるか試す（なければ "BOSS" をフォールバック）
                resolvedName = bossUnit.name;
            }
        }

        if (string.IsNullOrEmpty(resolvedName))
        {
            resolvedName = "BOSS";
        }

        if (bossNameText != null)
        {
            bossNameText.text = resolvedName;
        }

        if (bossSubtitleText != null)
        {
            bossSubtitleText.text = subtitle ?? "";
            bossSubtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }
    }

    private void SetBossVisibility(BattleUnit unit, bool visible)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            sr.enabled = visible;
        }

        unit.SetUIActive(visible);
    }

    private void SetBossAlpha(BattleUnit unit, float alpha)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = unit.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private void DuckBgm()
    {
        if (bgmSource == null) return;

        bgmOriginalVolume = bgmSource.volume;
        bgmSource.DOKill();
        bgmSource.DOFade(bgmDuckVolume, bgmDuckDuration);
    }

    private void RestoreBgm()
    {
        if (bgmSource == null) return;

        bgmSource.DOKill();
        bgmSource.DOFade(bgmOriginalVolume, bgmRestoreDuration);
    }

    private void PlaySe(AudioClip clip)
    {
        if (seSource != null && clip != null)
        {
            seSource.PlayOneShot(clip);
            return;
        }

        if (clip == bossAppearSe && battleSfxController != null)
        {
            battleSfxController.PlayBossAppear();
        }
    }
}
