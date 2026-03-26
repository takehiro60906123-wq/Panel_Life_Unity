// =============================================================
// LiquidBoardBackground.cs
// 盤面の背景に液状シェーダーを自動適用する
//
// 使い方:
//   PanelBattleManager（boardParent がある GameObject）にアタッチ。
//   LiquidBoard.shader を Assets/Shaders/ に配置しておく。
//   あとは自動で盤面の後ろに液状背景が生成される。
// =============================================================
using UnityEngine;
using UnityEngine.UI;

public class LiquidBoardBackground : MonoBehaviour
{
    [Header("シェーダー設定")]
    [Tooltip("LiquidBoard シェーダーの Material。未設定なら自動で探す。")]
    [SerializeField] private Material liquidMaterial;

    [Header("サイズ調整")]
    [Tooltip("盤面より少し大きくする余白（ピクセル）")]
    [SerializeField] private float padding = 16f;

    [Tooltip("盤面からのオフセット")]
    [SerializeField] private Vector2 positionOffset = Vector2.zero;

    [Header("参照（自動取得可）")]
    [SerializeField] private PanelBoardController boardController;

    [Header("波紋")]
    [Tooltip("タップ時に波紋を出すか")]
    [SerializeField] private bool enableTapRipple = true;

    private RawImage liquidImage;
    private bool initialized;

    // 波紋管理（最大4つ同時）
    private const int MaxRipples = 4;
    private Vector4[] ripples = new Vector4[MaxRipples];
    private int nextRippleIndex;
    private RectTransform boardRectTransform;
    private Camera uiCamera;

    private static readonly string[] RipplePropertyNames =
    {
        "_Ripple0", "_Ripple1", "_Ripple2", "_Ripple3"
    };

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!enableTapRipple || liquidImage == null || liquidImage.material == null) return;
        if (boardRectTransform == null) return;

        // タップ検出
        bool tapped = Input.GetMouseButtonDown(0);
        if (!tapped && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tapped = true;
        }

        if (tapped)
        {
            Vector2 screenPos = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                screenPos = Input.GetTouch(0).position;
            }

            // スクリーン座標を盤面の UV (0-1) に変換
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    boardRectTransform, screenPos, uiCamera, out localPoint))
            {
                Rect boardRect = boardRectTransform.rect;

                // localPoint は RectTransform のピボット基準なので、左下原点に変換
                float u = (localPoint.x - boardRect.x) / boardRect.width;
                float v = (localPoint.y - boardRect.y) / boardRect.height;

                // 盤面の範囲内かチェック（少し余裕を持たせる）
                if (u >= -0.05f && u <= 1.05f && v >= -0.05f && v <= 1.05f)
                {
                    SpawnRipple(new Vector2(u, v), 1.0f);
                }
            }
        }

        // シェーダーにデータを送る
        Material mat = liquidImage.material;
        for (int i = 0; i < MaxRipples; i++)
        {
            mat.SetVector(RipplePropertyNames[i], ripples[i]);
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        // ── boardParent を探す ──
        Transform boardParent = FindBoardParent();
        if (boardParent == null)
        {
            Debug.LogWarning("[LiquidBoard] boardParent が見つかりません。");
            return;
        }

        // ── マテリアル準備 ──
        Material mat = ResolveMaterial();
        if (mat == null)
        {
            Debug.LogWarning("[LiquidBoard] LiquidBoard シェーダーが見つかりません。Assets/Shaders/ に配置してください。");
            return;
        }

        // ── 背景 Image 生成 ──
        CreateLiquidBackground(boardParent, mat);

        // ── 波紋用: 盤面の RectTransform と UIカメラをキャッシュ ──
        boardRectTransform = boardParent.GetComponent<RectTransform>();
        Canvas parentCanvas = boardParent.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = parentCanvas.worldCamera;
        }

        Debug.Log("[LiquidBoard] 液状背景を生成しました");
    }

    private Transform FindBoardParent()
    {
        // PanelBoardController から boardParent を探す
        if (boardController == null)
        {
            boardController = GetComponent<PanelBoardController>();
        }
        if (boardController == null)
        {
            boardController = FindObjectOfType<PanelBoardController>();
        }

        if (boardController == null) return null;

        // PanelBattleManager 経由で boardParent を取得
        PanelBattleManager manager = GetComponent<PanelBattleManager>();
        if (manager == null) manager = FindObjectOfType<PanelBattleManager>();
        if (manager != null && manager.boardParent != null)
        {
            return manager.boardParent;
        }

        // フォールバック: PanelBoardController の親
        return boardController.transform.parent != null
            ? boardController.transform.parent
            : boardController.transform;
    }

    private Material ResolveMaterial()
    {
        // Inspector で設定済みならそれを使う（インスタンス化して返す）
        if (liquidMaterial != null)
        {
            return new Material(liquidMaterial);
        }

        // シェーダーを名前で探す
        Shader shader = Shader.Find("UI/LiquidBoard");
        if (shader == null) return null;

        return new Material(shader);
    }

    private void CreateLiquidBackground(Transform boardParent, Material mat)
    {
        // 盤面の後ろに RawImage を作る
        GameObject bgObj = new GameObject("LiquidBoardBG", typeof(RectTransform));
        bgObj.transform.SetParent(boardParent, false);

        // 盤面の一番後ろに配置
        bgObj.transform.SetSiblingIndex(0);

        // GridLayoutGroup の影響を受けないようにする
        LayoutElement layoutElement = bgObj.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();

        // 盤面全体を覆うサイズ
        // Stretch で盤面サイズに追従 + padding
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(-padding, -padding) + positionOffset;
        bgRect.offsetMax = new Vector2(padding, padding) + positionOffset;

        // RawImage にマテリアルを適用
        liquidImage = bgObj.AddComponent<RawImage>();
        liquidImage.material = mat;
        liquidImage.raycastTarget = false;
        liquidImage.color = Color.white;

        // 白い 4x4 テクスチャ（RawImage の描画に必要）
        Texture2D whiteTex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        whiteTex.SetPixels(pixels);
        whiteTex.Apply();
        liquidImage.texture = whiteTex;
    }

    // =============================================================
    // 波紋
    // =============================================================

    private void SpawnRipple(Vector2 uvPosition, float strength)
    {
        // ラウンドロビンで波紋スロットを使い回す
        ripples[nextRippleIndex] = new Vector4(
            uvPosition.x,
            uvPosition.y,
            Time.timeSinceLevelLoad, // 開始時刻
            strength);
        nextRippleIndex = (nextRippleIndex + 1) % MaxRipples;
    }

    /// <summary>
    /// 外部から波紋を発生させる。
    /// uvPosition は盤面上の UV 座標 (0,0)=左下, (1,1)=右上。
    /// </summary>
    public void PlayRipple(Vector2 uvPosition, float strength = 1.0f)
    {
        SpawnRipple(uvPosition, strength);
    }

    /// <summary>
    /// 盤面中央に波紋を発生させる（オーバーリンクなどの全体演出用）。
    /// </summary>
    public void PlayCenterRipple(float strength = 1.5f)
    {
        SpawnRipple(new Vector2(0.5f, 0.5f), strength);
    }

    /// <summary>
    /// 外部から色を変更する（ボス戦時に赤くするなど）
    /// </summary>
    public void SetDeepColor(Color color)
    {
        if (liquidImage != null && liquidImage.material != null)
        {
            liquidImage.material.SetColor("_ColorDeep", color);
        }
    }

    public void SetMidColor(Color color)
    {
        if (liquidImage != null && liquidImage.material != null)
        {
            liquidImage.material.SetColor("_ColorMid", color);
        }
    }

    public void SetSurfaceColor(Color color)
    {
        if (liquidImage != null && liquidImage.material != null)
        {
            liquidImage.material.SetColor("_ColorSurface", color);
        }
    }

    /// <summary>
    /// ボス戦モード（赤みがかった不穏な液状）
    /// </summary>
    public void SetBossMode(bool isBoss)
    {
        if (liquidImage == null || liquidImage.material == null) return;

        Material mat = liquidImage.material;

        if (isBoss)
        {
            mat.SetColor("_ColorDeep", new Color(0.12f, 0.04f, 0.06f, 0.85f));
            mat.SetColor("_ColorMid", new Color(0.22f, 0.06f, 0.08f, 0.75f));
            mat.SetColor("_ColorSurface", new Color(0.30f, 0.10f, 0.10f, 0.65f));
            mat.SetColor("_ColorHighlight", new Color(0.55f, 0.18f, 0.15f, 0.30f));
            mat.SetColor("_EdgeGlowColor", new Color(0.45f, 0.12f, 0.10f, 0.45f));
            mat.SetFloat("_WaveSpeed", 0.5f);
        }
        else
        {
            mat.SetColor("_ColorDeep", new Color(0.04f, 0.08f, 0.12f, 0.85f));
            mat.SetColor("_ColorMid", new Color(0.06f, 0.18f, 0.16f, 0.75f));
            mat.SetColor("_ColorSurface", new Color(0.12f, 0.28f, 0.22f, 0.65f));
            mat.SetColor("_ColorHighlight", new Color(0.25f, 0.55f, 0.40f, 0.30f));
            mat.SetColor("_EdgeGlowColor", new Color(0.15f, 0.40f, 0.30f, 0.40f));
            mat.SetFloat("_WaveSpeed", 0.35f);
        }
    }

    private void OnDestroy()
    {
        // インスタンス化したマテリアルを解放
        if (liquidImage != null && liquidImage.material != null)
        {
            Destroy(liquidImage.material);
        }
    }
}
