// =============================================================
// SceneTransitionManager.cs
// リッチなシーン遷移マネージャー — 全シーン共通 DontDestroyOnLoad
//
// トランジションパターン:
//   1. CircleIris    — ゼルダ的な円形ワイプ（中心から閉じる/開く）
//   2. DiamondIris   — ひし形ワイプ
//   3. HorizontalWipe — 横方向のカーテン
//   4. FadeBlack     — 従来の黒フェード（フォールバック）
//
// 使い方:
//   SceneTransitionManager.Instance.LoadScene("Battle", TransitionType.CircleIris);
//   SceneTransitionManager.Instance.LoadScene("Home");  // デフォルト: CircleIris
//
// セットアップ:
//   最初のシーン（Title等）に空 GameObject を置いて AddComponent するだけ。
//   Canvas / Image / Material は全て動的生成。
// =============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TransitionType
{
    CircleIris,
    DiamondIris,
    HorizontalWipe,
    FadeBlack
}

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance => instance;

    [Header("トランジション設定")]
    [SerializeField] private TransitionType defaultTransition = TransitionType.CircleIris;
    [SerializeField] private float closeTransitionDuration = 0.6f;
    [SerializeField] private float openTransitionDuration = 0.5f;
    [SerializeField] private float blackHoldDuration = 0.2f;
    [SerializeField] private Color transitionColor = Color.black;

    [Header("ロゴ表示")]
    [Tooltip("暗転中に表示するロゴ画像。未設定なら表示しない。")]
    [SerializeField] private Sprite logoSprite;
    [Tooltip("ロゴの表示サイズ（ピクセル）")]
    [SerializeField] private Vector2 logoSize = new Vector2(360f, 180f);
    [Tooltip("画面左寄りに配置するオフセット（Anchor 0.3, 0.5 基準）")]
    [SerializeField] private Vector2 logoAnchoredPosition = new Vector2(0f, 0f);
    [Tooltip("ロゴの Anchor X（0=左端, 0.5=中央）")]
    [SerializeField, Range(0f, 1f)] private float logoAnchorX = 0.30f;
    [SerializeField] private float logoFadeInDuration = 0.25f;
    [SerializeField] private float logoHoldDuration = 0.6f;
    [SerializeField] private float logoFadeOutDuration = 0.2f;

    [Header("Tips表示")]
    [Tooltip("ランダムで表示するTipsテキスト。空なら表示しない。")]
    [SerializeField, TextArea(1, 3)] private string[] tips =
    {
        "敵をタップすると敵の情報が確認できる",
        "ボスを倒すと武器を落とすことがある",
        "武器アイコンを長押しすると性能が確認できる",
        "銃ゲージは弾薬パネルだけでなく攻撃パネルでも少し溜まる",
        "ショットガンは攻撃直前の敵に撃つと効果的",
        "装備する武器によってパネルの取得上限が変わる",
        "大剣は最大5リンクまで取得できる",
    };
    [SerializeField] private float tipsFontSize = 26f;
    [SerializeField] private Color tipsColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color tipsLabelColor = new Color(0.55f, 0.75f, 0.65f, 1f);
    [SerializeField] private string tipsLabel = "TIPS";
    [Tooltip("Tips の表示位置 Y（0=下端, 0.5=中央, 1=上端）")]
    [SerializeField, Range(0f, 1f)] private float tipsAnchorY = 0.45f;

    [Header("Tips タイミング")]
    [SerializeField] private float tipsFadeInDuration = 0.3f;
    [SerializeField] private float tipsHoldDuration = 2.5f;
    [SerializeField] private float tipsFadeOutDuration = 0.25f;

    [Header("Tips フォント")]
    [Tooltip("Tips に使う TMP フォント。未設定ならデフォルトフォント。")]
    [SerializeField] private TMPro.TMP_FontAsset tipsFont;
    [Tooltip("TIPS ラベルに使う TMP フォント。未設定なら tipsFont と同じ。")]
    [SerializeField] private TMPro.TMP_FontAsset tipsLabelFont;

    // 内部
    private Canvas transitionCanvas;
    private RawImage transitionImage;
    private Material transitionMaterial;
    private bool isTransitioning;
    private RenderTexture maskRT;

    // ロゴ
    private Image logoImage;
    private CanvasGroup logoCanvasGroup;

    // Tips
    private TMPro.TMP_Text tipsLabelText;
    private TMPro.TMP_Text tipsBodyText;
    private CanvasGroup tipsCanvasGroup;
    private bool showTipsOnNextTransition;

    // シェーダーソース（ランタイム生成）
    private const string ShaderSource = @"
Shader ""Hidden/SceneTransition""
{
    Properties
    {
        _Progress (""Progress"", Range(0,1)) = 0
        _Color (""Color"", Color) = (0,0,0,1)
        _Pattern (""Pattern"", Int) = 0
        _Aspect (""Aspect"", Float) = 1.777
        _Softness (""Softness"", Range(0,0.1)) = 0.02
    }
    SubShader
    {
        Tags { ""Queue""=""Overlay"" ""RenderType""=""Transparent"" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            float _Progress;
            float4 _Color;
            int _Pattern;
            float _Aspect;
            float _Softness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                float dist = 0;

                // Pattern 0: Circle Iris
                if (_Pattern == 0)
                {
                    float2 aspect = float2(_Aspect, 1.0);
                    dist = length((uv - center) * aspect);
                    dist = dist / (length(float2(0.5, 0.5) * aspect)); // normalize to 0-1
                }
                // Pattern 1: Diamond Iris
                else if (_Pattern == 1)
                {
                    float2 aspect = float2(_Aspect, 1.0);
                    float2 d = abs((uv - center) * aspect);
                    dist = (d.x + d.y);
                    dist = dist / (0.5 * _Aspect + 0.5);
                }
                // Pattern 2: Horizontal Wipe
                else if (_Pattern == 2)
                {
                    dist = uv.x;
                }
                // Pattern 3: Fade (全画面均一)
                else
                {
                    dist = 0.5;
                }

                float threshold = _Progress;

                // Pattern 3 の場合は alpha = progress
                if (_Pattern == 3)
                {
                    float4 col = _Color;
                    col.a = _Progress;
                    return col;
                }

                // dist < threshold ならマスク（黒）、境界にソフトエッジ
                float alpha = smoothstep(threshold - _Softness, threshold + _Softness, dist);
                alpha = 1.0 - alpha; // 内側が塗られる

                float4 col = _Color;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateTransitionUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (maskRT != null)
        {
            maskRT.Release();
            Destroy(maskRT);
        }

        if (transitionMaterial != null)
        {
            Destroy(transitionMaterial);
        }
    }

    // =============================================================
    // 公開 API
    // =============================================================

    /// <summary>
    /// リッチなトランジション付きでシーンを読み込む。
    /// </summary>
    public void LoadScene(string sceneName, TransitionType type = TransitionType.CircleIris)
    {
        if (isTransitioning) return;
        showTipsOnNextTransition = false;
        StartCoroutine(TransitionRoutine(sceneName, type));
    }

    /// <summary>
    /// Tips表示付きでシーンを読み込む。
    /// </summary>
    public void LoadSceneWithTips(string sceneName, TransitionType type = TransitionType.CircleIris)
    {
        if (isTransitioning) return;
        showTipsOnNextTransition = true;
        StartCoroutine(TransitionRoutine(sceneName, type));
    }

    /// <summary>
    /// デフォルトのトランジションでシーンを読み込む。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, defaultTransition);
    }

    /// <summary>
    /// トランジションの「開く」演出だけを再生する（シーン読み込み後の入場用）。
    /// 通常は自動で走るが、手動制御したい場合に使う。
    /// </summary>
    public void PlayOpenTransition(TransitionType type = TransitionType.CircleIris, Action onComplete = null)
    {
        StartCoroutine(OpenOnlyRoutine(type, onComplete));
    }

    /// <summary>
    /// 現在トランジション中かどうか。
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    // =============================================================
    // トランジション本体
    // =============================================================

    private IEnumerator TransitionRoutine(string sceneName, TransitionType type)
    {
        isTransitioning = true;

        // ── 閉じる演出 ──
        SetPattern(type);
        transitionImage.gameObject.SetActive(true);

        yield return AnimateProgress(0f, 1f, closeTransitionDuration);

        // ── 暗転中: ロゴ表示 ──
        yield return ShowLogoDuringBlack();

        // ── シーン読み込み ──
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        // シーンが完全に読み込まれるまで待つ
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 1フレーム待って新シーンの Awake/Start を通す
        yield return null;

        // ── 開く演出 ──
        yield return AnimateProgress(1f, 0f, openTransitionDuration);

        transitionImage.gameObject.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator OpenOnlyRoutine(TransitionType type, Action onComplete)
    {
        SetPattern(type);
        SetProgress(1f);
        transitionImage.gameObject.SetActive(true);

        yield return AnimateProgress(1f, 0f, openTransitionDuration);

        transitionImage.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator AnimateProgress(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, duration));

            // イージング: 閉じる時は加速、開く時は減速
            float eased;
            if (from < to)
            {
                // 閉じる: ease-in-out
                eased = t < 0.5f
                    ? 2f * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            }
            else
            {
                // 開く: ease-out (ぱっと開く感じ)
                eased = 1f - (1f - t) * (1f - t);
            }

            float progress = Mathf.Lerp(from, to, eased);
            SetProgress(progress);
            yield return null;
        }

        SetProgress(to);
    }

    // =============================================================
    // UI / マテリアル
    // =============================================================

    private void CreateTransitionUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("SceneTransitionCanvas");
        canvasObj.transform.SetParent(transform, false);

        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // 最前面

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        // RawImage（シェーダーで描画）
        GameObject imageObj = new GameObject("TransitionImage", typeof(RectTransform));
        imageObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        transitionImage = imageObj.AddComponent<RawImage>();
        transitionImage.raycastTarget = true; // トランジション中の入力をブロック

        // マテリアル
        Shader shader = CreateTransitionShader();
        if (shader != null)
        {
            transitionMaterial = new Material(shader);
            transitionMaterial.SetColor("_Color", transitionColor);
            transitionMaterial.SetFloat("_Progress", 0f);
            transitionMaterial.SetFloat("_Aspect", (float)Screen.width / Screen.height);
            transitionMaterial.SetFloat("_Softness", 0.02f);
            transitionImage.material = transitionMaterial;

            // 白いテクスチャを設定（RawImage が描画されるために必要）
            Texture2D whiteTex = new Texture2D(4, 4);
            Color[] pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            whiteTex.SetPixels(pixels);
            whiteTex.Apply();
            transitionImage.texture = whiteTex;
        }
        else
        {
            // シェーダーが使えない場合は単純な黒 Image でフォールバック
            Debug.LogWarning("[SceneTransition] カスタムシェーダーの生成に失敗。FadeBlack にフォールバック。");
            Image fallbackImage = imageObj.AddComponent<Image>();
            fallbackImage.color = transitionColor;
            fallbackImage.raycastTarget = true;
            Destroy(transitionImage);
            transitionImage = null;
        }

        imageObj.SetActive(false);

        // ── ロゴ Image ──
        CreateLogoUI(canvasObj.transform);

        // ── Tips テキスト ──
        CreateTipsUI(canvasObj.transform);
    }

    private void CreateLogoUI(Transform canvasRoot)
    {
        if (logoSprite == null) return;

        GameObject logoObj = new GameObject("TransitionLogo", typeof(RectTransform));
        logoObj.transform.SetParent(canvasRoot, false);

        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(logoAnchorX, 0.5f);
        logoRect.anchorMax = new Vector2(logoAnchorX, 0.5f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = logoSize;
        logoRect.anchoredPosition = logoAnchoredPosition;

        logoImage = logoObj.AddComponent<Image>();
        logoImage.sprite = logoSprite;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;

        logoCanvasGroup = logoObj.AddComponent<CanvasGroup>();
        logoCanvasGroup.alpha = 0f;
        logoCanvasGroup.interactable = false;
        logoCanvasGroup.blocksRaycasts = false;

        logoObj.SetActive(false);
    }

    private void CreateTipsUI(Transform canvasRoot)
    {
        if (tips == null || tips.Length == 0) return;

        // Tips 全体の親（CanvasGroup でまとめてフェード）
        GameObject tipsRoot = new GameObject("TransitionTips", typeof(RectTransform));
        tipsRoot.transform.SetParent(canvasRoot, false);

        RectTransform tipsRect = tipsRoot.GetComponent<RectTransform>();
        tipsRect.anchorMin = new Vector2(0.1f, tipsAnchorY);
        tipsRect.anchorMax = new Vector2(0.9f, tipsAnchorY);
        tipsRect.pivot = new Vector2(0.5f, 0.5f);
        tipsRect.sizeDelta = new Vector2(0f, 120f);
        tipsRect.anchoredPosition = Vector2.zero;

        tipsCanvasGroup = tipsRoot.AddComponent<CanvasGroup>();
        tipsCanvasGroup.alpha = 0f;
        tipsCanvasGroup.interactable = false;
        tipsCanvasGroup.blocksRaycasts = false;

        // 「TIPS」ラベル
        GameObject labelObj = new GameObject("TipsLabel", typeof(RectTransform));
        labelObj.transform.SetParent(tipsRoot.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.55f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        tipsLabelText = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        tipsLabelText.text = tipsLabel;
        tipsLabelText.fontSize = tipsFontSize * 0.8f;
        tipsLabelText.color = tipsLabelColor;
        tipsLabelText.alignment = TMPro.TextAlignmentOptions.Center;
        tipsLabelText.fontStyle = TMPro.FontStyles.Bold;
        tipsLabelText.raycastTarget = false;
        ApplyTipsFont(tipsLabelText, tipsLabelFont != null ? tipsLabelFont : tipsFont);

        // Tips 本文
        GameObject bodyObj = new GameObject("TipsBody", typeof(RectTransform));
        bodyObj.transform.SetParent(tipsRoot.transform, false);

        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 0.52f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        tipsBodyText = bodyObj.AddComponent<TMPro.TextMeshProUGUI>();
        tipsBodyText.text = "";
        tipsBodyText.fontSize = tipsFontSize;
        tipsBodyText.color = tipsColor;
        tipsBodyText.alignment = TMPro.TextAlignmentOptions.Center;
        tipsBodyText.enableWordWrapping = true;
        tipsBodyText.raycastTarget = false;
        ApplyTipsFont(tipsBodyText, tipsFont);

        tipsRoot.SetActive(false);
    }

    private void ApplyTipsFont(TMPro.TMP_Text text, TMPro.TMP_FontAsset font)
    {
        if (text == null || font == null) return;
        text.font = font;
    }

    // =============================================================
    // ロゴ＋Tips 表示（暗転中）
    // =============================================================

    private IEnumerator ShowLogoDuringBlack()
    {
        bool hasLogo = logoImage != null && logoSprite != null;
        bool hasTips = showTipsOnNextTransition && tipsBodyText != null && tips != null && tips.Length > 0;

        if (!hasLogo && !hasTips)
        {
            yield return new WaitForSecondsRealtime(blackHoldDuration);
            yield break;
        }

        // ── ロゴ準備 ──
        if (hasLogo)
        {
            logoImage.gameObject.SetActive(true);
            logoCanvasGroup.alpha = 0f;
        }

        // ── Tips 準備（ランダム選択） ──
        if (hasTips)
        {
            string selectedTip = tips[UnityEngine.Random.Range(0, tips.Length)];
            tipsBodyText.text = selectedTip;
            tipsCanvasGroup.alpha = 0f;
            tipsCanvasGroup.gameObject.SetActive(true);
        }

        // ── フェードイン ──
        float fadeInTime = hasTips
            ? Mathf.Max(logoFadeInDuration, tipsFadeInDuration)
            : logoFadeInDuration;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (hasLogo) logoCanvasGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, logoFadeInDuration));
            if (hasTips) tipsCanvasGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, tipsFadeInDuration));
            yield return null;
        }
        if (hasLogo) logoCanvasGroup.alpha = 1f;
        if (hasTips) tipsCanvasGroup.alpha = 1f;

        // ── 保持（Tips があれば Tips の時間を優先、なければロゴの時間） ──
        float holdTime = hasTips ? tipsHoldDuration : logoHoldDuration;
        yield return new WaitForSecondsRealtime(holdTime);

        // ── フェードアウト ──
        float fadeOutTime = hasTips
            ? Mathf.Max(logoFadeOutDuration, tipsFadeOutDuration)
            : logoFadeOutDuration;
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (hasLogo) logoCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.001f, logoFadeOutDuration));
            if (hasTips) tipsCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.001f, tipsFadeOutDuration));
            yield return null;
        }
        if (hasLogo)
        {
            logoCanvasGroup.alpha = 0f;
            logoImage.gameObject.SetActive(false);
        }
        if (hasTips)
        {
            tipsCanvasGroup.alpha = 0f;
            tipsCanvasGroup.gameObject.SetActive(false);
        }

        showTipsOnNextTransition = false;

        yield return new WaitForSecondsRealtime(0.1f);
    }

    private Shader CreateTransitionShader()
    {
        // ランタイムでシェーダーをコンパイル
        // Unity 2021+ では ShaderUtil が Editor 限定なので、
        // ビルド時は事前にシェーダーを Assets に入れる必要がある。
        // ここでは Shader.Find でフォールバック可能な形にする。

        // まず既存のシェーダーを探す
        Shader existing = Shader.Find("Hidden/SceneTransition");
        if (existing != null) return existing;

        // エディタではランタイムコンパイルを試みる
#if UNITY_EDITOR
        return UnityEditor.ShaderUtil.CreateShaderAsset(ShaderSource, false);
#else
        // ビルドではフォールバック
        return null;
#endif
    }

    private void SetPattern(TransitionType type)
    {
        if (transitionMaterial == null) return;

        int pattern;
        switch (type)
        {
            case TransitionType.CircleIris:    pattern = 0; break;
            case TransitionType.DiamondIris:   pattern = 1; break;
            case TransitionType.HorizontalWipe: pattern = 2; break;
            case TransitionType.FadeBlack:     pattern = 3; break;
            default:                           pattern = 0; break;
        }

        transitionMaterial.SetInt("_Pattern", pattern);
        transitionMaterial.SetFloat("_Aspect", (float)Screen.width / Screen.height);
    }

    private void SetProgress(float progress)
    {
        if (transitionMaterial != null)
        {
            transitionMaterial.SetFloat("_Progress", progress);
        }
        else if (transitionImage != null)
        {
            // フォールバック: alpha で制御
            Color c = transitionColor;
            c.a = progress;
            transitionImage.color = c;
        }
    }

    // =============================================================
    // 互換ヘルパー
    // =============================================================

    /// <summary>
    /// 既存の SceneManager.LoadScene を置き換える静的メソッド。
    /// Instance が無い場合は従来通り直接読み込む。
    /// </summary>
    public static void TransitionToScene(string sceneName, TransitionType type = TransitionType.CircleIris)
    {
        if (instance != null)
        {
            instance.LoadScene(sceneName, type);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Tips表示付きでシーンを読み込む静的メソッド。
    /// Home → Battle など、攻略情報を出したい遷移で使う。
    /// </summary>
    public static void TransitionToSceneWithTips(string sceneName, TransitionType type = TransitionType.CircleIris)
    {
        if (instance != null)
        {
            instance.LoadSceneWithTips(sceneName, type);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
