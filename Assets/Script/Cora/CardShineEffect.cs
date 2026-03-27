// =============================================================
// CardShineEffect.cs
// カード上を光が走るエフェクト（TitleLogoShine のカード版）
//
// 使い方:
//   Image がある GameObject にアタッチするだけ。
//   Mask を自分に追加して光をクリップする。
//   Button があっても干渉しない。
//
// 修正内容:
//   - RectMask2D → Mask に変更（Image 形状でクリップ、TitleLogoShine と同方式）
//   - Setup をコルーチン化し、レイアウト確定後に光を生成
//   - cachedWidth が 0 のまま光が走る問題を解消
// =============================================================
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

[RequireComponent(typeof(Image))]
public class CardShineEffect : MonoBehaviour
{
    [Header("光の見た目")]
    [SerializeField, Range(0.05f, 0.5f)] private float shineWidthRatio = 0.2f;
    [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private float shineAngle = 20f;

    [Header("アニメーション")]
    [SerializeField] private float shineDuration = 0.5f;
    [SerializeField] private float loopInterval = 4f;
    [SerializeField] private float startDelay = 0.8f;
    [SerializeField] private bool autoStart = false;

    private RectTransform shineRect;
    private Tween shineTween;
    private bool isSetUp = false;
    private float cachedWidth;
    private float cachedHeight;

    // セットアップ完了後に実行するアクション
    private System.Action pendingAction;

    private void Start()
    {
        if (autoStart)
        {
            StartShineLoop();
        }
    }

    private void OnDestroy()
    {
        shineTween?.Kill();
        pendingAction = null;
    }

    // =============================================================
    // 公開
    // =============================================================

    public void SetStartDelay(float delay) { startDelay = delay; }

    public void PlayShine(float delay = 0f)
    {
        EnsureSetup(() => DoPlayShine(delay));
    }

    public void StartShineLoop()
    {
        EnsureSetup(() => DoStartShineLoop());
    }

    public void StopShine()
    {
        shineTween?.Kill();
        pendingAction = null;
    }

    // =============================================================
    // Setup をレイアウト確定後に実行
    // =============================================================

    private void EnsureSetup(System.Action onReady)
    {
        if (isSetUp)
        {
            // 既にセットアップ済みならそのまま実行
            onReady?.Invoke();
            return;
        }

        // まだセットアップしていない → フレーム末まで待ってから構築
        pendingAction = onReady;
        StartCoroutine(SetupAfterLayout());
    }

    private IEnumerator SetupAfterLayout()
    {
        // レイアウトが確定するまで待つ（最低1フレーム）
        yield return null;

        // 念のため Canvas を強制更新
        Canvas.ForceUpdateCanvases();

        // もう1フレーム待つ（ディール演出のスケールが反映されるように）
        yield return null;

        Setup();

        // 保留中のアクションを実行
        if (pendingAction != null)
        {
            var action = pendingAction;
            pendingAction = null;
            action.Invoke();
        }
    }

    // =============================================================
    // 実際のアニメーション処理
    // =============================================================

    private void DoPlayShine(float delay)
    {
        if (shineRect == null || cachedWidth < 1f) return;

        shineTween?.Kill();

        float sw = cachedWidth * shineWidthRatio;
        float sx = -(cachedWidth * 0.5f + sw);
        float ex = cachedWidth * 0.5f + sw;

        shineRect.anchoredPosition = new Vector2(sx, 0f);
        shineRect.gameObject.SetActive(true);

        shineTween = DOTween.Sequence()
            .AppendInterval(delay)
            .Append(shineRect.DOAnchorPosX(ex, shineDuration).SetEase(Ease.InOutQuad))
            .OnComplete(() =>
            {
                if (shineRect != null)
                    shineRect.anchoredPosition = new Vector2(sx, 0f);
            });
    }

    private void DoStartShineLoop()
    {
        if (shineRect == null || cachedWidth < 1f) return;

        shineTween?.Kill();

        float sw = cachedWidth * shineWidthRatio;
        float sx = -(cachedWidth * 0.5f + sw);
        float ex = cachedWidth * 0.5f + sw;

        shineRect.anchoredPosition = new Vector2(sx, 0f);
        shineRect.gameObject.SetActive(true);

        if (loopInterval <= 0f)
        {
            DoPlayShine(startDelay);
            return;
        }

        shineTween = DOTween.Sequence()
            .AppendInterval(startDelay)
            .AppendCallback(() =>
            {
                if (shineRect != null)
                    shineRect.anchoredPosition = new Vector2(sx, 0f);
            })
            .Append(shineRect.DOAnchorPosX(ex, shineDuration).SetEase(Ease.InOutQuad))
            .AppendInterval(loopInterval)
            .SetLoops(-1, LoopType.Restart);
    }

    // =============================================================
    // 構築
    // =============================================================

    private void Setup()
    {
        if (isSetUp) return;
        isSetUp = true;

        // --- Mask を追加（TitleLogoShine と同方式） ---
        // Image の形状でクリップするので、カード外に光が漏れない
        Mask existingMask = GetComponent<Mask>();
        if (existingMask == null)
        {
            Mask mask = gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true; // カード自体は表示したまま
        }

        // --- サイズ取得 ---
        RectTransform rt = GetComponent<RectTransform>();

        // LayoutRebuilder で強制的にレイアウトを確定
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        cachedWidth = rt.rect.width;
        cachedHeight = rt.rect.height;

        // rect が取れなかった場合は sizeDelta にフォールバック
        if (cachedWidth < 1f) cachedWidth = rt.sizeDelta.x;
        if (cachedHeight < 1f) cachedHeight = rt.sizeDelta.y;

        // それでもサイズが取れない場合はセットアップ中断
        if (cachedWidth < 1f || cachedHeight < 1f)
        {
            Debug.LogWarning($"[CardShineEffect] サイズが取得できません: w={cachedWidth} h={cachedHeight} on '{gameObject.name}'");
            return;
        }

        // --- 光を自分の直接の子として生成 ---
        float sw = cachedWidth * shineWidthRatio;
        float sh = cachedHeight * 2.5f; // 斜めにするので高さは余裕を持たせる（Mask でクリップされる）

        GameObject shineObj = new GameObject("CardShineStreak");
        shineObj.transform.SetParent(transform, false);
        shineObj.transform.SetAsLastSibling();

        shineRect = shineObj.AddComponent<RectTransform>();
        shineRect.sizeDelta = new Vector2(sw, sh);
        shineRect.localRotation = Quaternion.Euler(0f, 0f, shineAngle);

        float sx = -(cachedWidth * 0.5f + sw);
        shineRect.anchoredPosition = new Vector2(sx, 0f);

        // 初期状態は非表示（アニメーション開始時に表示）
        shineObj.SetActive(false);

        // メインの光
        MakeSlice(shineObj.transform, sw, sh, 0f, shineColor);
        // 中心ハイライト
        MakeSlice(shineObj.transform, sw * 0.35f, sh, 0f,
            new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * 1.6f));
        // 左右ソフトエッジ
        MakeSlice(shineObj.transform, sw * 0.7f, sh, -sw * 0.35f,
            new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * 0.25f));
        MakeSlice(shineObj.transform, sw * 0.7f, sh, sw * 0.35f,
            new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * 0.25f));
    }

    private void MakeSlice(Transform parent, float w, float h, float ox, Color c)
    {
        GameObject obj = new GameObject("S");
        obj.transform.SetParent(parent, false);

        RectTransform r = obj.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(w, h);
        r.anchoredPosition = new Vector2(ox, 0f);

        Image img = obj.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
    }
}
