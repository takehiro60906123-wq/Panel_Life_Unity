// =============================================================
// TitleLogoShine.cs
// タイトルロゴに光が走るエフェクト
//
// 使い方:
//   ロゴの Image がついた GameObject にこのスクリプトをアタッチ。
//   以上。Mask も光 Image も全部コードで生成する。
// =============================================================
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class TitleLogoShine : MonoBehaviour
{
    [Header("光の見た目")]
    [Tooltip("光の幅（ロゴ幅に対する割合）")]
    [SerializeField, Range(0.1f, 0.6f)] private float shineWidthRatio = 0.25f;

    [Tooltip("光の色")]
    [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.55f);

    [Tooltip("光の傾き（度）")]
    [SerializeField] private float shineAngle = 25f;

    [Header("アニメーション")]
    [Tooltip("光が横切る時間（秒）")]
    [SerializeField] private float shineDuration = 0.7f;

    [Tooltip("ループ間隔（秒）")]
    [SerializeField] private float loopInterval = 3.5f;

    [Tooltip("開始までの遅延（秒）")]
    [SerializeField] private float startDelay = 1.0f;

    private RectTransform shineRect;
    private Tween shineTween;

    private void Start()
    {
        SetupMask();
        CreateShineImage();
        StartShineLoop();
    }

    private void OnDestroy()
    {
        shineTween?.Kill();
    }

    // =============================================================
    // Mask 設定
    // =============================================================

    private void SetupMask()
    {
        // 既に Mask があればそのまま使う
        Mask existingMask = GetComponent<Mask>();
        if (existingMask != null) return;

        Mask mask = gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true; // ロゴ自体は表示したまま
    }

    // =============================================================
    // 光 Image 生成
    // =============================================================

    private void CreateShineImage()
    {
        RectTransform logoRect = GetComponent<RectTransform>();
        float logoWidth = logoRect.rect.width;
        float logoHeight = logoRect.rect.height;

        // 光の幅と高さ
        float shineWidth = logoWidth * shineWidthRatio;
        // 斜めにするので高さは余裕を持たせる
        float shineHeight = logoHeight * 2.5f;

        // 光用の GameObject を子として生成
        GameObject shineObj = new GameObject("ShineStreak");
        shineObj.transform.SetParent(transform, false);

        shineRect = shineObj.AddComponent<RectTransform>();
        shineRect.sizeDelta = new Vector2(shineWidth, shineHeight);
        shineRect.localRotation = Quaternion.Euler(0f, 0f, shineAngle);

        // 初期位置: ロゴの左外
        float startX = -(logoWidth * 0.5f + shineWidth);
        shineRect.anchoredPosition = new Vector2(startX, 0f);

        // 光の Image（グラデーション風に3枚重ね）
        // 中央が一番明るく、左右がフェードアウト
        CreateShineSlice(shineObj.transform, shineWidth, shineHeight, 0f, shineColor);
        CreateShineSlice(shineObj.transform, shineWidth * 0.4f, shineHeight, 0f,
            new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * 1.5f));

        // 左右のソフトエッジ
        CreateShineSlice(shineObj.transform, shineWidth * 0.8f, shineHeight,
            -shineWidth * 0.35f,
            new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * 0.3f));
        CreateShineSlice(shineObj.transform, shineWidth * 0.8f, shineHeight,
            shineWidth * 0.35f,
            new Color(shineColor.r, shineColor.g, shineColor.b, shineColor.a * 0.3f));
    }

    private void CreateShineSlice(Transform parent, float width, float height, float offsetX, Color color)
    {
        GameObject sliceObj = new GameObject("Slice", typeof(RectTransform), typeof(Image));
        sliceObj.transform.SetParent(parent, false);

        RectTransform rect = sliceObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(offsetX, 0f);

        Image img = sliceObj.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // =============================================================
    // アニメーション
    // =============================================================

    private void StartShineLoop()
    {
        RectTransform logoRect = GetComponent<RectTransform>();
        float logoWidth = logoRect.rect.width;
        float shineWidth = logoWidth * shineWidthRatio;

        // 移動範囲: 左外 → 右外
        float startX = -(logoWidth * 0.5f + shineWidth);
        float endX = logoWidth * 0.5f + shineWidth;

        // 初期位置
        shineRect.anchoredPosition = new Vector2(startX, 0f);

        // ループ Sequence
        shineTween = DOTween.Sequence()
            .AppendInterval(startDelay)
            .AppendCallback(() =>
            {
                // 開始位置にリセット
                shineRect.anchoredPosition = new Vector2(startX, 0f);
            })
            .Append(
                shineRect.DOAnchorPosX(endX, shineDuration)
                    .SetEase(Ease.InOutQuad))
            .AppendInterval(loopInterval)
            .SetLoops(-1, LoopType.Restart);
    }
}
