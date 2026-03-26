// =============================================================
// BossIntroController.cs — シネマティック演出強化版
//
// 追加演出:
//   ・レターボックス（上下黒帯）でシネマスコープ演出
//   ・カメラズームイン → ボスのアップ
//   ・シルエット → 閃光リビール
//   ・ヒットストップ的な「溜め」
//   ・カメラ引き → レターボックス退場 → 戦闘開始
//
// 既存との互換:
//   ScreenShakeController の LateUpdate は、シェイク中でなければ
//   cameraBaseLocalPos を毎フレーム更新する仕様。
//   ズーム中はシェイクが走らないため競合しない。
//   ズーム完了後に位置を正しく戻せば安全。
//
// セットアップ:
//   1. Canvas 直下に「LetterboxTop」「LetterboxBottom」を Image で作成
//      - Color: 黒 (0,0,0,1)
//      - RaycastTarget: OFF
//      - 高さ: 画面の 12〜15% 程度（Anchor で上端/下端に貼る）
//      - 初期状態で画面外に配置（Top は上、Bottom は下に退避）
//   2. BossIntroController の Inspector で参照をセット
//   3. 既存の darkOverlay, bannerRoot 等はそのまま使用
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
    [Tooltip("画面全体を覆う Image。暗転用に専用の黒 Image を用意する。初期は透明。")]
    [SerializeField] private Image darkOverlay;

    [Header("盤面参照")]
    [SerializeField] private PanelBoardController panelBoardController;
    [SerializeField] private DungeonMistController dungeonMistController;

    // ─────────────────────────────────────────
    // レターボックス（上下黒帯）
    // ─────────────────────────────────────────
    [Header("レターボックス")]
    [Tooltip("画面上端の黒帯 RectTransform。Anchor を上端に設定し、初期は画面外（上方向）に退避させておく。")]
    [SerializeField] private RectTransform letterboxTop;
    [Tooltip("画面下端の黒帯 RectTransform。Anchor を下端に設定し、初期は画面外（下方向）に退避させておく。")]
    [SerializeField] private RectTransform letterboxBottom;
    [SerializeField] private float letterboxBarHeight = 120f;
    [SerializeField] private float letterboxEnterDuration = 0.30f;
    [SerializeField] private float letterboxExitDuration = 0.25f;
    [SerializeField] private Ease letterboxEnterEase = Ease.OutCubic;
    [SerializeField] private Ease letterboxExitEase = Ease.InCubic;

    // ─────────────────────────────────────────
    // シネマティックカメラ
    // ─────────────────────────────────────────
    [Header("カメラズーム")]
    [Tooltip("Orthographic: ズームイン時の orthographicSize。小さいほどアップ。")]
    [SerializeField] private float zoomInSize = 2.2f;
    [Tooltip("Perspective: ズームイン時の fieldOfView。小さいほどアップ。")]
    [SerializeField] private float zoomInFov = 25f;
    [Tooltip("スプライト中心からのオフセット（ワールド座標）。bounds.center 基準なので大きな値は不要。")]
    [SerializeField] private Vector2 zoomFocusOffset = new Vector2(0f, 0.15f);
    [SerializeField] private float zoomInDuration = 0.50f;
    [SerializeField] private float zoomInDelay = 0.10f;
    [SerializeField] private Ease zoomInEase = Ease.InOutCubic;
    [SerializeField] private float closeUpHoldDuration = 0.60f;
    [SerializeField] private float zoomOutDuration = 0.40f;
    [SerializeField] private Ease zoomOutEase = Ease.InOutQuad;

    // ─────────────────────────────────────────
    // シルエット → 閃光リビール
    // ─────────────────────────────────────────
    [Header("シルエットリビール")]
    [SerializeField] private Color silhouetteColor = new Color(0.08f, 0.06f, 0.12f, 1f);
    [SerializeField] private float silhouetteHoldDuration = 0.35f;
    [SerializeField] private float revealFlashAlpha = 0.55f;
    [SerializeField] private Color revealFlashColor = new Color(1f, 0.97f, 0.85f);
    [SerializeField] private float revealFlashDuration = 0.12f;
    [SerializeField] private float revealColorDuration = 0.18f;

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

    [Header("自動生成")]
    [Tooltip("true にすると、未設定の UI を PlayBossIntro 時に自動生成する。\nInspector で個別に設定済みのものはそのまま使う。")]
    [SerializeField] private bool autoGenerateUI = true;

    // ─────────────────────────────────────────
    // 内部状態
    // ─────────────────────────────────────────
    private Vector2 bannerBaseAnchoredPos;
    private bool hasCachedBannerPos;
    private float bgmOriginalVolume = 1f;
    private BattleSfxController battleSfxController;

    // カメラ復帰用
    private Camera cachedCamera;
    private float originalOrthoSize;
    private float originalFov;
    private Vector3 originalCameraPos;
    private bool isCameraOrtho;

    // レターボックス復帰用
    private Vector2 letterboxTopBasePos;
    private Vector2 letterboxBottomBasePos;
    private bool hasCachedLetterboxPos;

    // 自動生成済みフラグ
    private bool uiGenerated;

    // 盤面フェード用
    private CanvasGroup boardCanvasGroupRef;
    private float boardOriginalAlpha = 1f;

    private void Awake()
    {
        Debug.Log($"[BossIntro] Awake: autoGenerateUI={autoGenerateUI}, " +
                  $"darkOverlay={darkOverlay != null}, bannerRoot={bannerRoot != null}");

        CacheBannerPosition();
        CacheLetterboxPositions();
        HideBannerImmediate();
        HideLetterboxImmediate();

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

    private void CacheLetterboxPositions()
    {
        if (hasCachedLetterboxPos) return;

        if (letterboxTop != null)
        {
            letterboxTopBasePos = letterboxTop.anchoredPosition;
        }

        if (letterboxBottom != null)
        {
            letterboxBottomBasePos = letterboxBottom.anchoredPosition;
        }

        hasCachedLetterboxPos = true;
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

    private void HideLetterboxImmediate()
    {
        if (letterboxTop != null)
        {
            letterboxTop.anchoredPosition = letterboxTopBasePos + new Vector2(0f, letterboxBarHeight);
            letterboxTop.gameObject.SetActive(false);
        }

        if (letterboxBottom != null)
        {
            letterboxBottom.anchoredPosition = letterboxBottomBasePos + new Vector2(0f, -letterboxBarHeight);
            letterboxBottom.gameObject.SetActive(false);
        }
    }

    // =============================================================
    // 公開 API
    // =============================================================

    public IEnumerator PlayBossIntro(BattleUnit bossUnit, string bossName = null, string subtitle = null)
    {
        Debug.Log($"[BossIntro] PlayBossIntro 開始: bossUnit={bossUnit?.name}, enemyType={bossUnit?.enemyType}");

        if (bossUnit == null) yield break;

        // UI が未設定なら動的生成
        EnsureUIGenerated();

        Debug.Log($"[BossIntro] UI状態: darkOverlay={darkOverlay != null}, letterboxTop={letterboxTop != null}, " +
                  $"bannerRoot={bannerRoot != null}, bannerText={bossNameText != null}");

        // ─── 準備 ───
        SetBossVisibility(bossUnit, false);
        SetupBannerText(bossName, subtitle, bossUnit);
        CacheCamera();

        // =============================================
        // フェーズ 1: 前触れ — 不穏な空気
        // =============================================
        yield return new WaitForSeconds(forebodingDelay);

        PlaySe(bossRumbleSe);
        DuckBgm();

        if (dungeonMistController != null)
        {
            dungeonMistController.ApplyBattleState(true, false);
        }

        // レターボックス登場（地鳴りと同時）
        PlayLetterboxEnter();
        PlayForebodingShake();

        yield return new WaitForSeconds(forebodingShakeDuration);

        // =============================================
        // フェーズ 2: 暗転
        // =============================================
        yield return PlayDarkenRoutine();
        yield return new WaitForSeconds(darkenHoldDuration);

        // =============================================
        // フェーズ 3: ボス実体化 — シルエットで登場
        // =============================================
        yield return new WaitForSeconds(bossRevealDelay);

        Transform bossTransform = bossUnit.transform;
        Vector3 bossOriginalPos = bossTransform.position;

        // ボスをシルエット（黒塗り）で小さく表示
        bossTransform.localScale = Vector3.one * bossStartScale;
        SetBossVisibility(bossUnit, true);
        SetBossAlpha(bossUnit, 1f);
        TintBossColor(bossUnit, silhouetteColor);
        bossUnit.SetUIActive(false);

        // スケールアップ（シルエットのまま等倍へ）
        bossTransform.DOScale(Vector3.one, bossScaleUpDuration).SetEase(bossScaleUpEase);
        yield return new WaitForSeconds(bossScaleUpDuration);

        // 着地インパクト — スクワッシュ（横に潰れて縦に縮む）
        PlayLandingImpact();
        bossTransform.DOScale(new Vector3(1.15f, 0.85f, 1f), 0.06f)
            .SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(0.06f);
        // ストレッチ（縦に戻る）
        bossTransform.DOScale(Vector3.one, 0.10f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.12f);

        // シルエットを少し見せる間
        yield return new WaitForSeconds(silhouetteHoldDuration);

        // =============================================
        // フェーズ 4: 威圧ズーム — ボス自体がドカンと迫る
        // =============================================

        // ① ボスを一瞬でかくして手前に迫らせる
        float intimidateScale = 1.8f;
        float intimidateForwardX = -0.4f; // プレイヤー側に少し飛び出す
        bossTransform.DOScale(Vector3.one * intimidateScale, 0.12f).SetEase(Ease.OutCubic);
        bossTransform.DOMoveX(bossOriginalPos.x + intimidateForwardX, 0.12f).SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(0.12f);

        // ② ヒットストップ的な「溜め」— 一瞬止まる
        ScreenShakeController shakeCtrl = ScreenShakeController.Instance;
        if (shakeCtrl != null) shakeCtrl.HitStop(0.08f);
        yield return new WaitForSeconds(0.10f);

        // ③ 閃光 → シルエット解除（拡大状態で色が戻る＝一番インパクトある瞬間）
        yield return PlayRevealFlash(bossUnit);

        // ④ 拡大状態でちょっと保持（「こいつがボスか」と認識させる間）
        yield return new WaitForSeconds(closeUpHoldDuration);

        // ⑤ 元のサイズ・位置に戻す
        bossTransform.DOScale(Vector3.one, 0.25f).SetEase(Ease.InOutQuad);
        bossTransform.DOMove(bossOriginalPos, 0.25f).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(0.28f);

        // =============================================
        // フェーズ 5: バナー表示
        // =============================================
        yield return PlayBannerRoutine();

        // =============================================
        // フェーズ 6: 戦闘準備
        // =============================================

        // HP バー表示
        bossUnit.SetUIActive(true);

        // 着地で軽くバウンド
        bossTransform.DOPunchScale(
            new Vector3(0.08f, -0.12f, 0f),
            0.14f, 4, 0.7f);

        // レターボックス退場
        PlayLetterboxExit();

        // 暗転解除
        yield return PlayLightenRoutine();

        RestoreBgm();

        yield return new WaitForSeconds(unlockDelay);
    }

    public static bool IsBossUnit(BattleUnit unit)
    {
        if (unit == null) return false;
        return unit.enemyType == EnemyType.Boss;
    }

    // =============================================================
    // レターボックス演出
    // =============================================================

    private void PlayLetterboxEnter()
    {
        if (letterboxTop != null)
        {
            letterboxTop.gameObject.SetActive(true);
            letterboxTop.DOKill();
            letterboxTop.DOAnchorPos(letterboxTopBasePos, letterboxEnterDuration)
                .SetEase(letterboxEnterEase);
        }

        if (letterboxBottom != null)
        {
            letterboxBottom.gameObject.SetActive(true);
            letterboxBottom.DOKill();
            letterboxBottom.DOAnchorPos(letterboxBottomBasePos, letterboxEnterDuration)
                .SetEase(letterboxEnterEase);
        }
    }

    private void PlayLetterboxExit()
    {
        if (letterboxTop != null)
        {
            letterboxTop.DOKill();
            letterboxTop.DOAnchorPos(
                    letterboxTopBasePos + new Vector2(0f, letterboxBarHeight),
                    letterboxExitDuration)
                .SetEase(letterboxExitEase)
                .OnComplete(() =>
                {
                    if (letterboxTop != null) letterboxTop.gameObject.SetActive(false);
                });
        }

        if (letterboxBottom != null)
        {
            letterboxBottom.DOKill();
            letterboxBottom.DOAnchorPos(
                    letterboxBottomBasePos + new Vector2(0f, -letterboxBarHeight),
                    letterboxExitDuration)
                .SetEase(letterboxExitEase)
                .OnComplete(() =>
                {
                    if (letterboxBottom != null) letterboxBottom.gameObject.SetActive(false);
                });
        }
    }

    // =============================================================
    // カメラズーム演出
    // =============================================================

    private void CacheCamera()
    {
        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }

        if (cachedCamera != null)
        {
            isCameraOrtho = cachedCamera.orthographic;
            originalOrthoSize = cachedCamera.orthographicSize;
            originalFov = cachedCamera.fieldOfView;
            originalCameraPos = cachedCamera.transform.position;
        }

        // 盤面の CanvasGroup を探してキャッシュ
        if (boardCanvasGroupRef == null && panelBoardController != null)
        {
            // PanelBoardController の親 Canvas 内の boardParent を探す
            Transform boardParent = panelBoardController.transform.parent;
            if (boardParent != null)
            {
                boardCanvasGroupRef = boardParent.GetComponent<CanvasGroup>();
            }
            // 見つからなければ PanelBoardController 自体から
            if (boardCanvasGroupRef == null)
            {
                boardCanvasGroupRef = panelBoardController.GetComponentInParent<CanvasGroup>();
            }
        }
    }

    private IEnumerator PlayCameraZoomIn(Transform focusTarget)
    {
        if (cachedCamera == null || focusTarget == null) yield break;

        Transform camTransform = cachedCamera.transform;

        // スプライトの見た目の中心を取得
        Vector3 focusCenter = focusTarget.position;
        SpriteRenderer sr = focusTarget.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            focusCenter = sr.bounds.center;
        }

        Debug.Log($"[BossIntro ZOOM] focusCenter={focusCenter}, camera.pos={camTransform.position}, " +
                  $"ortho={isCameraOrtho}, fov={cachedCamera.fieldOfView}");

        // 盤面をフェードアウト（ボスが見えるように）
        FadeBoardOut();

        if (isCameraOrtho)
        {
            // Orthographic: XY移動 + orthographicSize 縮小
            Vector3 targetPos = new Vector3(
                focusCenter.x + zoomFocusOffset.x,
                focusCenter.y + zoomFocusOffset.y,
                camTransform.position.z
            );

            Sequence seq = DOTween.Sequence();
            seq.Join(camTransform.DOMove(targetPos, zoomInDuration).SetEase(zoomInEase));
            seq.Join(DOTween.To(
                () => cachedCamera.orthographicSize,
                x => cachedCamera.orthographicSize = x,
                zoomInSize, zoomInDuration).SetEase(zoomInEase));
            yield return seq.WaitForCompletion();
        }
        else
        {
            // Perspective: XY移動 + FOV 縮小でズーム
            Vector3 targetPos = new Vector3(
                focusCenter.x + zoomFocusOffset.x,
                focusCenter.y + zoomFocusOffset.y,
                camTransform.position.z
            );

            Sequence seq = DOTween.Sequence();
            seq.Join(camTransform.DOMove(targetPos, zoomInDuration).SetEase(zoomInEase));
            seq.Join(DOTween.To(
                () => cachedCamera.fieldOfView,
                x => cachedCamera.fieldOfView = x,
                zoomInFov, zoomInDuration).SetEase(zoomInEase));
            yield return seq.WaitForCompletion();
        }
    }

    private IEnumerator PlayCameraZoomOut()
    {
        if (cachedCamera == null) yield break;

        Transform camTransform = cachedCamera.transform;

        Sequence seq = DOTween.Sequence();
        seq.Join(camTransform.DOMove(originalCameraPos, zoomOutDuration).SetEase(zoomOutEase));

        if (isCameraOrtho)
        {
            seq.Join(DOTween.To(
                () => cachedCamera.orthographicSize,
                x => cachedCamera.orthographicSize = x,
                originalOrthoSize, zoomOutDuration).SetEase(zoomOutEase));
        }
        else
        {
            seq.Join(DOTween.To(
                () => cachedCamera.fieldOfView,
                x => cachedCamera.fieldOfView = x,
                originalFov, zoomOutDuration).SetEase(zoomOutEase));
        }

        yield return seq.WaitForCompletion();

        // 盤面を復帰
        FadeBoardIn();
    }

    // =============================================================
    // 盤面フェード（ズーム中にボスを隠さないため）
    // =============================================================

    private void FadeBoardOut()
    {
        if (boardCanvasGroupRef != null)
        {
            boardOriginalAlpha = boardCanvasGroupRef.alpha;
            boardCanvasGroupRef.DOKill();
            boardCanvasGroupRef.DOFade(0f, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    private void FadeBoardIn()
    {
        if (boardCanvasGroupRef != null)
        {
            boardCanvasGroupRef.DOKill();
            boardCanvasGroupRef.DOFade(boardOriginalAlpha, 0.25f).SetEase(Ease.OutQuad);
        }
    }

    // =============================================================
    // シルエット → 閃光リビール
    // =============================================================

    private void TintBossColor(BattleUnit unit, Color tint)
    {
        if (unit == null) return;

        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(unit);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            sr.color = tint;
        }
    }

    private IEnumerator PlayRevealFlash(BattleUnit bossUnit)
    {
        // 画面全体に白い閃光
        ScreenShakeController shakeController = ScreenShakeController.Instance;
        if (shakeController != null)
        {
            shakeController.Flash(revealFlashColor, revealFlashDuration, revealFlashAlpha);
            shakeController.HitStop(0.06f);
        }

        PlaySe(bossAppearSe);

        // シルエットから本来の色へ復帰
        SpriteRenderer[] renderers = TintHelper.GetTintableRenderers(bossUnit);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;

            sr.DOKill();
            sr.DOColor(Color.white, revealColorDuration).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(Mathf.Max(revealFlashDuration, revealColorDuration));
    }

    // =============================================================
    // 既存フェーズ（前触れ・暗転・バナー・着地・回復）
    // =============================================================

    private void PlayForebodingShake()
    {
        ScreenShakeController shakeController = ScreenShakeController.Instance;
        if (shakeController != null)
        {
            shakeController.Shake(forebodingShakeIntensity, forebodingShakeDuration, forebodingShakeVibrato);
            return;
        }

        if (panelBoardController != null)
        {
            panelBoardController.PlayImpactShake(forebodingShakeIntensity / 3f, forebodingShakeDuration);
        }
    }

    private void PlayLandingImpact()
    {
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
            0.12f, 6, 0.8f);

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
            BattleUnitView view = bossUnit.GetComponent<BattleUnitView>();
            if (view != null)
            {
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

        SpriteRenderer[] renderers = TintHelper.GetAllRenderers(unit);
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

        SpriteRenderer[] renderers = TintHelper.GetAllRenderers(unit);
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

    // =============================================================
    // UI 動的生成
    // =============================================================

    private void EnsureUIGenerated()
    {
        if (uiGenerated || !autoGenerateUI) return;
        uiGenerated = true;

        Debug.Log($"[BossIntro] EnsureUIGenerated: autoGenerateUI={autoGenerateUI}");

        Canvas targetCanvas = FindOverlayCanvas();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[BossIntro] Canvas が見つかりません。UI を自動生成できません。");
            return;
        }

        Debug.Log($"[BossIntro] Canvas 発見: {targetCanvas.gameObject.name} (sortOrder={targetCanvas.sortingOrder})");

        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();

        // --- DarkOverlay ---
        if (darkOverlay == null)
        {
            darkOverlay = CreateFullScreenImage(canvasRect, "BossIntroDarkOverlay", Color.black);
            darkOverlay.raycastTarget = false;
            Color c = darkOverlay.color;
            c.a = 0f;
            darkOverlay.color = c;
            darkOverlay.gameObject.SetActive(false);
        }

        // --- LetterboxTop ---
        if (letterboxTop == null)
        {
            GameObject topObj = CreateUIObject(canvasRect, "BossIntroLetterboxTop");
            letterboxTop = topObj.GetComponent<RectTransform>();
            Image topImg = topObj.AddComponent<Image>();
            topImg.color = Color.black;
            topImg.raycastTarget = false;

            // Anchor: top-stretch (左上～右上)
            letterboxTop.anchorMin = new Vector2(0f, 1f);
            letterboxTop.anchorMax = new Vector2(1f, 1f);
            letterboxTop.pivot = new Vector2(0.5f, 1f);
            letterboxTop.sizeDelta = new Vector2(0f, letterboxBarHeight);
            // 初期位置: 画面外（上方向に退避）
            letterboxTop.anchoredPosition = new Vector2(0f, letterboxBarHeight);

            letterboxTopBasePos = Vector2.zero; // 表示位置は (0, 0)
            topObj.SetActive(false);
        }

        // --- LetterboxBottom ---
        if (letterboxBottom == null)
        {
            GameObject bottomObj = CreateUIObject(canvasRect, "BossIntroLetterboxBottom");
            letterboxBottom = bottomObj.GetComponent<RectTransform>();
            Image bottomImg = bottomObj.AddComponent<Image>();
            bottomImg.color = Color.black;
            bottomImg.raycastTarget = false;

            // Anchor: bottom-stretch (左下～右下)
            letterboxBottom.anchorMin = new Vector2(0f, 0f);
            letterboxBottom.anchorMax = new Vector2(1f, 0f);
            letterboxBottom.pivot = new Vector2(0.5f, 0f);
            letterboxBottom.sizeDelta = new Vector2(0f, letterboxBarHeight);
            // 初期位置: 画面外（下方向に退避）
            letterboxBottom.anchoredPosition = new Vector2(0f, -letterboxBarHeight);

            letterboxBottomBasePos = Vector2.zero;
            bottomObj.SetActive(false);
        }

        // --- BannerRoot + テキスト ---
        if (bannerRoot == null)
        {
            GameObject bannerObj = CreateUIObject(canvasRect, "BossIntroBannerRoot");
            bannerRoot = bannerObj.GetComponent<RectTransform>();

            // 画面中央やや上
            bannerRoot.anchorMin = new Vector2(0.5f, 0.55f);
            bannerRoot.anchorMax = new Vector2(0.5f, 0.55f);
            bannerRoot.pivot = new Vector2(0.5f, 0.5f);
            bannerRoot.sizeDelta = new Vector2(600f, 120f);
            bannerRoot.anchoredPosition = Vector2.zero;

            bannerCanvasGroup = bannerObj.AddComponent<CanvasGroup>();
            bannerCanvasGroup.alpha = 0f;

            // 半透明背景バー
            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0f, 0f, 0f, 0.6f);
            bannerBg.raycastTarget = false;

            // ボス名テキスト
            GameObject nameObj = CreateUIObject(bannerRoot, "BossNameText");
            bossNameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
            bossNameText.text = "BOSS";
            bossNameText.fontSize = 42;
            bossNameText.fontStyle = TMPro.FontStyles.Bold;
            bossNameText.color = Color.white;
            bossNameText.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.35f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(16f, 0f);
            nameRect.offsetMax = new Vector2(-16f, -8f);

            // サブタイトルテキスト
            GameObject subObj = CreateUIObject(bannerRoot, "BossSubtitleText");
            bossSubtitleText = subObj.AddComponent<TMPro.TextMeshProUGUI>();
            bossSubtitleText.text = "";
            bossSubtitleText.fontSize = 20;
            bossSubtitleText.color = new Color(0.85f, 0.8f, 0.7f, 1f);
            bossSubtitleText.alignment = TMPro.TextAlignmentOptions.Center;
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.38f);
            subRect.offsetMin = new Vector2(16f, 4f);
            subRect.offsetMax = new Vector2(-16f, 0f);
            subObj.SetActive(false);

            bannerObj.SetActive(false);
        }

        // キャッシュ更新
        hasCachedBannerPos = false;
        hasCachedLetterboxPos = true;
        CacheBannerPosition();

        Debug.Log("[BossIntro] UI を動的生成しました");
    }

    private Canvas FindOverlayCanvas()
    {
        // BattleOverlayCanvas を名前で探す（あれば最適）
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas bestCandidate = null;
        int highestOrder = int.MinValue;

        foreach (Canvas c in canvases)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            // 名前に Overlay が含まれるものを優先
            if (c.gameObject.name.Contains("Overlay") && c.sortingOrder > highestOrder)
            {
                bestCandidate = c;
                highestOrder = c.sortingOrder;
            }
        }

        // Overlay Canvas が無ければ、最も Sort Order が高い Canvas を使う
        if (bestCandidate == null)
        {
            highestOrder = int.MinValue;
            foreach (Canvas c in canvases)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                if (c.sortingOrder > highestOrder)
                {
                    bestCandidate = c;
                    highestOrder = c.sortingOrder;
                }
            }
        }

        return bestCandidate;
    }

    private GameObject CreateUIObject(Transform parent, string objectName)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private Image CreateFullScreenImage(RectTransform parent, string objectName, Color color)
    {
        GameObject obj = CreateUIObject(parent, objectName);
        RectTransform rect = obj.GetComponent<RectTransform>();

        // Stretch-Stretch: 画面全体を覆う
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        return img;
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
