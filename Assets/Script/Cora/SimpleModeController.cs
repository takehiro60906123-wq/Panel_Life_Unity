// =============================================================
// SimpleModeController.cs
// シンプルモード：6×6 盤面を非表示にして手札カードで操作する機能。
//
// ■ 概要
//   - 盤面データはそのまま裏で保持
//   - 各パネル種の「最良チェーン」を手札カードとして表示
//   - カードタップ → 既存の PanelActionController.OnPanelClicked() を呼ぶ
//   - 既存の戦闘処理フローを一切変更しない
//
// ■ 使い方
//   PanelBattleManager と同じ GameObject、または同シーンに配置。
//   Inspector で各参照をセット。
//   トグルボタンで通常モード ⇔ シンプルモードを切り替え。
// =============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SimpleModeController : MonoBehaviour
{
    // =============================================================
    // Inspector 参照
    // =============================================================

    [Header("コア参照")]
    [SerializeField] private PanelBoardController panelBoardController;
    [SerializeField] private PanelActionController panelActionController;
    [SerializeField] private PanelBattleManager panelBattleManager;
    [SerializeField] private BattleEventHub battleEventHub;

    [Header("盤面の CanvasGroup (非表示切替用)")]
    [SerializeField] private CanvasGroup boardCanvasGroup;

    [Header("カード表示エリア")]
    [Tooltip("カードを並べる親 Transform (HorizontalLayoutGroup 推奨)")]
    [SerializeField] private RectTransform cardContainer;

    [Tooltip("シンプルモード全体の CanvasGroup")]
    [SerializeField] private CanvasGroup simpleModeCanvasGroup;

    [Header("トグルボタン")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleButtonText;

    [Header("カードプレハブ")]
    [Tooltip("カード1枚分のプレハブ。構成は下記参照。")]
    [SerializeField] private GameObject cardPrefab;

    [Header("扇形レイアウト")]
    [Tooltip("扇の広がり角度（全体）")]
    [SerializeField] private float fanTotalAngle = 25f;
    [Tooltip("カード間の間隔(px)")]
    [SerializeField] private float fanCardSpacing = 95f;
    [Tooltip("扇の回転軸（カード下端からの距離）")]
    [SerializeField] private float fanPivotDistance = 400f;
    [Tooltip("カード群の垂直位置オフセット")]
    [SerializeField] private float fanVerticalOffset = -20f;

    [Header("ディール演出")]
    [SerializeField] private float dealDuration = 0.35f;
    [SerializeField] private float dealStaggerDelay = 0.08f;
    [SerializeField] private float dealStartOffsetY = -200f;
    [SerializeField] private float dealOvershootY = -15f;

    [Header("タップ演出")]
    [SerializeField] private float tapFlyUpDistance = 60f;
    [SerializeField] private float tapFlyDuration = 0.18f;
    [SerializeField] private float tapScaleUp = 1.08f;

    [Header("モード切替演出")]
    [SerializeField] private float modeSwitchFadeDuration = 0.25f;

    [Header("最大リンク数表示")]
    [Tooltip("現在装備の最大リンク数を表示する TMP_Text")]
    [SerializeField] private TMP_Text maxLinkText;

    [Header("敵の緊急度演出")]
    [Tooltip("カードコンテナに赤パルスを出すための Image（カード背後に配置）")]
    [SerializeField] private Image dangerPulseImage;
    [SerializeField] private Color dangerPulseColor = new Color(1f, 0.15f, 0.1f, 0.25f);
    [SerializeField] private float dangerPulseDuration = 0.6f;

    [Header("アイテム付きバッジ色")]
    [SerializeField] private Color attachedItemBadgeColor = new Color(1f, 0.85f, 0.15f, 1f);

    // =============================================================
    // 内部状態
    // =============================================================

    private bool isSimpleMode = false;
    private bool isProcessingCard = false;
    private bool isShowingBoardForAction = false;
    private List<GameObject> activeCards = new List<GameObject>();
    private bool isSubscribed = false;
    private Tween dangerPulseTween;

    // =============================================================
    // 初期化
    // =============================================================

    private void Start()
    {
        if (panelBoardController == null)
            panelBoardController = FindObjectOfType<PanelBoardController>();
        if (panelActionController == null)
            panelActionController = FindObjectOfType<PanelActionController>();
        if (panelBattleManager == null)
            panelBattleManager = FindObjectOfType<PanelBattleManager>();
        if (battleEventHub == null)
            battleEventHub = FindObjectOfType<BattleEventHub>();

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(OnToggleClicked);
        }

        // 初期状態: 通常モード
        SetSimpleModeVisuals(false, true);
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        dangerPulseTween?.Kill();
    }

    /// <summary>
    /// シンプルモード中は毎フレーム盤面を隠し続ける。
    /// PanelBattleManager の DOFade が alpha を戻してしまうのを防ぐ。
    /// </summary>
    private void LateUpdate()
    {
        if (!isSimpleMode) return;
        if (isShowingBoardForAction) return; // 演出中は盤面表示を許可
        if (boardCanvasGroup == null) return;

        if (boardCanvasGroup.alpha > 0.01f)
        {
            boardCanvasGroup.DOKill();
            boardCanvasGroup.alpha = 0f;
            boardCanvasGroup.interactable = false;
            boardCanvasGroup.blocksRaycasts = false;
        }
    }

    // =============================================================
    // イベント購読
    // =============================================================

    private void SubscribeEvents()
    {
        if (isSubscribed || battleEventHub == null) return;

        // 盤面が操作可能になったタイミングでカードを再生成
        battleEventHub.BoardInteractableRequested += OnBoardInteractableChanged;
        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed || battleEventHub == null) return;

        battleEventHub.BoardInteractableRequested -= OnBoardInteractableChanged;
        isSubscribed = false;
    }

    /// <summary>
    /// 盤面の操作可能状態が変わったとき呼ばれる。
    /// プレイヤーターン開始時にカードを更新する。
    /// </summary>
    private void OnBoardInteractableChanged(bool interactable)
    {
        if (!isSimpleMode) return;

        if (interactable)
        {
            if (isShowingBoardForAction)
            {
                // 演出が終わった → 少し待ってからカード表示に戻す
                StartCoroutine(ReturnToSimpleModeAfterDelay());
            }
            else
            {
                // 通常のターン開始（演出なし）
                ForceHideBoard();
                ShowCardArea();
                RefreshCards();
                isProcessingCard = false;
            }
        }
        else
        {
            // ターン終了 → カード操作不可に
            SetCardsInteractable(false);
        }
    }

    [Header("演出→カード復帰")]
    [SerializeField] private float boardShowAfterActionDelay = 0.4f;

    private IEnumerator ReturnToSimpleModeAfterDelay()
    {
        // 演出結果をしばらく見せる
        yield return new WaitForSeconds(boardShowAfterActionDelay);

        // フラグを落とす → LateUpdate が盤面を即座に隠す
        isShowingBoardForAction = false;

        // 少し待ってからカード表示（盤面フェードと重ならないように）
        yield return new WaitForSeconds(0.1f);

        ShowCardArea();
        RefreshCards();
        isProcessingCard = false;
    }

    private void ShowCardArea()
    {
        if (simpleModeCanvasGroup != null)
        {
            simpleModeCanvasGroup.DOKill();
            simpleModeCanvasGroup.DOFade(1f, 0.2f);
            simpleModeCanvasGroup.interactable = true;
            simpleModeCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// シンプルモード中に盤面を強制的に隠す。
    /// PanelBattleManager.SetBoardInteractable() が alpha を戻してしまうので、
    /// その後に呼んで上書きする。
    /// </summary>
    private void ForceHideBoard()
    {
        if (boardCanvasGroup == null) return;

        boardCanvasGroup.DOKill();
        boardCanvasGroup.alpha = 0f;
        boardCanvasGroup.interactable = false;
        boardCanvasGroup.blocksRaycasts = false;
    }

    // =============================================================
    // トグル切替
    // =============================================================

    private void OnToggleClicked()
    {
        isSimpleMode = !isSimpleMode;
        SetSimpleModeVisuals(isSimpleMode, false);

        if (isSimpleMode)
        {
            RefreshCards();
        }
        else
        {
            ClearCards();
        }
    }

    public void SetSimpleMode(bool active)
    {
        if (isSimpleMode == active) return;
        isSimpleMode = active;
        SetSimpleModeVisuals(active, false);

        if (active) RefreshCards();
        else ClearCards();
    }

    public bool IsSimpleMode => isSimpleMode;

    // =============================================================
    // 表示切替
    // =============================================================

    private void SetSimpleModeVisuals(bool simple, bool instant)
    {
        float duration = instant ? 0f : modeSwitchFadeDuration;

        // 盤面を隠す / 表示する
        if (boardCanvasGroup != null)
        {
            if (instant)
            {
                boardCanvasGroup.alpha = simple ? 0f : 1f;
            }
            else
            {
                boardCanvasGroup.DOKill();
                boardCanvasGroup.DOFade(simple ? 0f : 1f, duration);
            }

            boardCanvasGroup.interactable = !simple;
            boardCanvasGroup.blocksRaycasts = !simple;
        }

        // カードエリアを表示 / 隠す
        if (simpleModeCanvasGroup != null)
        {
            if (instant)
            {
                simpleModeCanvasGroup.alpha = simple ? 1f : 0f;
            }
            else
            {
                simpleModeCanvasGroup.DOKill();
                simpleModeCanvasGroup.DOFade(simple ? 1f : 0f, duration);
            }

            simpleModeCanvasGroup.interactable = simple;
            simpleModeCanvasGroup.blocksRaycasts = simple;
            simpleModeCanvasGroup.gameObject.SetActive(true);
        }

        // ボタンテキスト
        if (toggleButtonText != null)
        {
            toggleButtonText.text = simple ? "盤面に戻す" : "シンプル";
        }
    }

    // =============================================================
    // カード生成
    // =============================================================

    /// <summary>
    /// 盤面をスキャンしてカードを生成・表示する。
    /// </summary>
    public void RefreshCards()
    {
        ClearCards();

        if (panelBoardController == null)
        {
            Debug.LogWarning("[SimpleMode] panelBoardController が null です");
            return;
        }
        if (cardPrefab == null)
        {
            Debug.LogWarning("[SimpleMode] cardPrefab が null です");
            return;
        }
        if (cardContainer == null)
        {
            Debug.LogWarning("[SimpleMode] cardContainer が null です");
            return;
        }

        // LayoutGroup がついていると扇形配置を上書きするので無効化
        var layoutGroup = cardContainer.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;
        var layoutGroupV = cardContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layoutGroupV != null) layoutGroupV.enabled = false;
        var layoutGroupG = cardContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (layoutGroupG != null) layoutGroupG.enabled = false;

        List<PanelBoardController.SimpleCardInfo> cardData =
            panelBoardController.ScanBoardForSimpleCards();

        Debug.Log($"[SimpleMode] ScanBoardForSimpleCards → {(cardData != null ? cardData.Count : 0)} 種類");

        if (cardData == null || cardData.Count == 0) return;

        int count = cardData.Count;

        for (int i = 0; i < count; i++)
        {
            PanelBoardController.SimpleCardInfo info = cardData[i];

            Debug.Log($"[SimpleMode] Card[{i}]: type={info.type} selected={info.selectedCount} total={info.totalOnBoard} origin=({info.origin.x},{info.origin.y}) icon={info.icon}");

            GameObject card = Instantiate(cardPrefab, cardContainer);
            card.name = $"SimpleCard_{info.type}";

            // --- カード内部 UI 設定 ---
            SetupCardUI(card, info);

            // --- 扇形レイアウト配置 ---
            RectTransform rt = card.GetComponent<RectTransform>();
            if (rt != null)
            {
                // LayoutGroup を無効化して手動配置
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);

                // 扇の角度計算: 中央を0として左右に広げる
                float t = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
                float angle = t * fanTotalAngle;
                float xOffset = t * fanCardSpacing * count;

                rt.anchoredPosition = new Vector2(xOffset, fanVerticalOffset);
                rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            // --- タップハンドラ ---
            int capturedRow = info.origin.x;
            int capturedCol = info.origin.y;
            PanelType capturedType = info.type;
            bool capturedIsReward = info.isReward;

            Button btn = EnsureButtonOnClickableTarget(card);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnCardTapped(card, capturedRow, capturedCol, capturedType, capturedIsReward));

            // 子の Image の raycastTarget を無効化して、
            // クリックが必ずルートの Button に届くようにする
            Image[] childImages = card.GetComponentsInChildren<Image>(true);
            foreach (Image img in childImages)
            {
                if (img.gameObject == card) continue; // ルートはスキップ
                img.raycastTarget = false;
            }

            // --- ディール演出 ---
            PlayDealAnimation(card, i, count);

            // --- シャイン演出 (ディール完了後に走る) ---
            float shineDelay = dealStaggerDelay * i + dealDuration + 0.1f;
            AttachAndPlayShine(card, shineDelay);

            activeCards.Add(card);
        }

        // --- 敵の緊急度パルス更新 ---
        UpdateDangerPulse();
    }

    /// <summary>
    /// カードプレハブの内部 UI を設定する。
    ///
    /// ■ カードプレハブに必要な構成:
    ///   - Button (ルート)
    ///   - Image "CardIcon"     … パネルアイコン
    ///   - TMP_Text "CardCount" … 取得枚数 (例: "×4")
    ///   - TMP_Text "CardLabel" … パネル種名 (例: "攻撃")
    ///   - TMP_Text "CardTotal" … 盤面上の総数 (例: "全8枚") [任意]
    /// </summary>
    private void SetupCardUI(GameObject card, PanelBoardController.SimpleCardInfo info)
    {
        // === まず全 TMP_Text を列挙して状況把握 ===
        TMP_Text[] allTexts = card.GetComponentsInChildren<TMP_Text>(true);
        Debug.Log($"[SimpleMode] SetupCardUI: card={card.name} type={info.type} isReward={info.isReward} sel={info.selectedCount} TMP数={allTexts.Length}");
        for (int t = 0; t < allTexts.Length; t++)
        {
            Debug.Log($"[SimpleMode]   TMP[{t}] name='{allTexts[t].gameObject.name}' text='{allTexts[t].text}'");
        }

        // === アイコン ===
        Transform iconTr = FindChildRecursive(card.transform, "CardIcon");
        if (iconTr != null)
        {
            Image iconImg = iconTr.GetComponent<Image>();
            if (iconImg != null && info.icon != null)
            {
                iconImg.sprite = info.icon;
                iconImg.enabled = true;
            }
        }

        // === リワードカードの場合 ===
        if (info.isReward)
        {
            // Count: "!" を表示（1枚取るだけなので枚数ではなく注目マーク）
            foreach (TMP_Text tmp in allTexts)
            {
                if (tmp.gameObject.name.Contains("Count"))
                {
                    tmp.text = "!";
                    break;
                }
            }

            // Label: リワードの短縮ラベルを表示
            foreach (TMP_Text tmp in allTexts)
            {
                if (tmp.gameObject.name.Contains("Label"))
                {
                    tmp.text = !string.IsNullOrEmpty(info.rewardLabel) ? info.rewardLabel : "報酬";
                    break;
                }
            }

            // Total: 非表示
            foreach (TMP_Text tmp in allTexts)
            {
                if (tmp.gameObject.name.Contains("Total"))
                {
                    tmp.text = "";
                    break;
                }
            }

            // Overlink: 非表示
            foreach (TMP_Text tmp in allTexts)
            {
                if (tmp.gameObject.name.Contains("Overlink"))
                {
                    tmp.gameObject.SetActive(false);
                    break;
                }
            }

            // テキスト変更を強制反映
            foreach (TMP_Text tmp in allTexts)
            {
                tmp.ForceMeshUpdate();
            }
            Canvas.ForceUpdateCanvases();
            return;
        }

        // === 以下、通常パネルカードの処理 ===

        // === 取得枚数: 名前検索 → フォールバック ===
        bool countDone = false;
        foreach (TMP_Text tmp in allTexts)
        {
            if (tmp.gameObject.name.Contains("Count"))
            {
                tmp.text = info.selectedCount.ToString();
                countDone = true;
                Debug.Log($"[SimpleMode]   Count → '{info.selectedCount}' on '{tmp.gameObject.name}'");
                break;
            }
        }
        if (!countDone)
        {
            // "99" などデフォルト数字のテキストを探す
            foreach (TMP_Text tmp in allTexts)
            {
                string trimmed = tmp.text.Trim();
                int val;
                if (int.TryParse(trimmed, out val) && !tmp.gameObject.name.Contains("Label"))
                {
                    tmp.text = info.selectedCount.ToString();
                    countDone = true;
                    Debug.Log($"[SimpleMode]   Count(数字フォールバック) → '{info.selectedCount}' on '{tmp.gameObject.name}'");
                    break;
                }
            }
        }

        // === パネル種名: 名前検索 → フォールバック ===
        bool labelDone = false;
        foreach (TMP_Text tmp in allTexts)
        {
            if (tmp.gameObject.name.Contains("Label"))
            {
                tmp.text = GetPanelTypeLabel(info.type);
                labelDone = true;
                Debug.Log($"[SimpleMode]   Label → '{GetPanelTypeLabel(info.type)}' on '{tmp.gameObject.name}'");
                break;
            }
        }

        // === 盤面上の総数 (任意) ===
        foreach (TMP_Text tmp in allTexts)
        {
            if (tmp.gameObject.name.Contains("Total"))
            {
                tmp.text = info.totalOnBoard > info.selectedCount
                    ? $"全{info.totalOnBoard}枚"
                    : "";
                break;
            }
        }

        // === オーバーリンク (任意) ===
        foreach (TMP_Text tmp in allTexts)
        {
            if (tmp.gameObject.name.Contains("Overlink"))
            {
                int overflow = info.totalConnected - info.selectedCount;
                if (overflow > 0 && info.type == PanelType.Sword)
                {
                    int overlinkBonus = overflow / 2;
                    tmp.text = overlinkBonus > 0 ? $"+{overlinkBonus}" : "";
                    tmp.gameObject.SetActive(overlinkBonus > 0);
                }
                else
                {
                    tmp.gameObject.SetActive(false);
                }
                break;
            }
        }

        // === テキスト変更を強制反映 ===
        foreach (TMP_Text tmp in allTexts)
        {
            tmp.ForceMeshUpdate();
        }
        Canvas.ForceUpdateCanvases();

        // === アイテム付きバッジ（チェーン内にアイテムがある場合） ===
        if (info.hasAttachedItem)
        {
            CreateAttachedItemBadge(card, info);
        }
    }

    // =============================================================
    // ② アイテム付きバッジ表示
    // =============================================================

    /// <summary>
    /// カード右上に小さなバッジを生成し、アイテムが付いていることを示す。
    /// アイコンがあればアイコン、なければ「★」テキスト + アイテム名。
    /// </summary>
    private void CreateAttachedItemBadge(GameObject card, PanelBoardController.SimpleCardInfo info)
    {
        if (card == null) return;

        // BackPanel を基準にする（なければルート）
        Transform parent = FindChildRecursive(card.transform, "BackPanel");
        if (parent == null) parent = card.transform;

        // バッジのルート
        GameObject badgeObj = new GameObject("ItemBadge");
        badgeObj.transform.SetParent(parent, false);

        RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(1f, 1f); // 右上基準
        badgeRt.anchorMax = new Vector2(1f, 1f);
        badgeRt.pivot = new Vector2(1f, 1f);
        badgeRt.sizeDelta = new Vector2(32f, 32f);
        badgeRt.anchoredPosition = new Vector2(-4f, -4f);

        // 背景丸（疑似円）
        Image badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.color = attachedItemBadgeColor;
        badgeBg.raycastTarget = false;

        // アイコンがあれば表示
        if (info.attachedItemIcon != null)
        {
            GameObject iconObj = new GameObject("BadgeIcon");
            iconObj.transform.SetParent(badgeObj.transform, false);

            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(3f, 3f);
            iconRt.offsetMax = new Vector2(-3f, -3f);

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = info.attachedItemIcon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            // アイコンなし → ★テキスト
            GameObject textObj = new GameObject("BadgeText");
            textObj.transform.SetParent(badgeObj.transform, false);

            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TMP_Text badgeText = textObj.AddComponent<TextMeshProUGUI>();
            badgeText.text = "★";
            badgeText.fontSize = 18f;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.white;
            badgeText.raycastTarget = false;
        }

        // アイテム名ラベル（バッジの下に小さく表示）
        if (!string.IsNullOrEmpty(info.attachedItemName))
        {
            GameObject nameObj = new GameObject("ItemNameLabel");
            nameObj.transform.SetParent(parent, false);

            RectTransform nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 1f);
            nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.sizeDelta = new Vector2(100f, 18f);
            nameRt.anchoredPosition = new Vector2(0f, 2f); // カード上端の少し上

            TMP_Text nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = info.attachedItemName;
            nameText.fontSize = 11f;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = attachedItemBadgeColor;
            nameText.raycastTarget = false;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        Debug.Log($"[SimpleMode] アイテムバッジ生成: {info.attachedItemName} on {card.name}");
    }

    // =============================================================
    // ③ 敵の緊急度パルス演出
    // =============================================================

    /// <summary>
    /// 敵の currentCooldown を確認して、危険なら赤パルスを表示する。
    /// RefreshCards のたびに呼ぶ。
    /// </summary>
    private void UpdateDangerPulse()
    {
        // パルス用 Image が未設定なら何もしない
        if (dangerPulseImage == null) return;

        // 敵の残りターン取得
        int enemyCooldown = GetEnemyCooldown();

        if (enemyCooldown <= 1 && enemyCooldown >= 0)
        {
            // 危険 → パルス開始
            StartDangerPulse();
        }
        else
        {
            // 安全 → パルス停止
            StopDangerPulse();
        }
    }

    private int GetEnemyCooldown()
    {
        if (panelBattleManager == null) return -1;

        BattleUnit enemy = panelBattleManager.enemyUnit;
        if (enemy == null || enemy.IsDead()) return -1;

        return enemy.currentCooldown;
    }

    private void StartDangerPulse()
    {
        if (dangerPulseImage == null) return;

        // 既にパルス中なら何もしない
        if (dangerPulseTween != null && dangerPulseTween.IsActive() && dangerPulseTween.IsPlaying())
            return;

        dangerPulseImage.gameObject.SetActive(true);
        dangerPulseImage.color = new Color(dangerPulseColor.r, dangerPulseColor.g, dangerPulseColor.b, 0f);

        dangerPulseTween = DOTween.Sequence()
            .Append(dangerPulseImage.DOFade(dangerPulseColor.a, dangerPulseDuration * 0.4f)
                .SetEase(Ease.OutQuad))
            .Append(dangerPulseImage.DOFade(0f, dangerPulseDuration * 0.6f)
                .SetEase(Ease.InQuad))
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopDangerPulse()
    {
        dangerPulseTween?.Kill();
        dangerPulseTween = null;

        if (dangerPulseImage != null)
        {
            dangerPulseImage.DOKill();
            dangerPulseImage.color = new Color(dangerPulseColor.r, dangerPulseColor.g, dangerPulseColor.b, 0f);
            dangerPulseImage.gameObject.SetActive(false);
        }
    }

    // =============================================================
    // 再帰的に子オブジェクトを名前で検索
    // =============================================================

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // まず直接の子を完全一致 → 前方一致で探す
        Transform direct = parent.Find(childName);
        if (direct != null) return direct;

        // 再帰検索（前方一致: "CardCount" で "CardCount Text (TMP)" もヒット）
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName || child.name.StartsWith(childName))
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    private string ListChildNames(Transform parent)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < parent.childCount; i++)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(parent.GetChild(i).name);
        }
        return sb.ToString();
    }

    // =============================================================
    // カードタップ処理
    // =============================================================

    /// <summary>
    /// カードタップ → 通常パネルは PanelActionController.OnPanelClicked()、
    /// リワードパネルは PanelBoardController.TryInvokeSpecialPanelClick() を呼ぶ。
    /// </summary>
    private void OnCardTapped(GameObject card, int row, int col, PanelType type, bool isReward = false)
    {
        Debug.Log($"[SimpleMode] OnCardTapped: type={type} row={row} col={col} isReward={isReward}");

        if (isProcessingCard) return;

        isProcessingCard = true;
        isShowingBoardForAction = true;
        SetCardsInteractable(false);

        // 他のカードをさっとフェードアウト
        foreach (GameObject other in activeCards)
        {
            if (other == null || other == card) continue;
            CanvasGroup otherCg = other.GetComponent<CanvasGroup>();
            if (otherCg == null) otherCg = other.AddComponent<CanvasGroup>();
            otherCg.DOFade(0f, 0.12f);
        }

        // タップしたカードをフライアップ → 盤面表示 → アクション
        PlayCardTapFlyUp(card, () =>
        {
            // カードエリアを隠す
            if (simpleModeCanvasGroup != null)
            {
                simpleModeCanvasGroup.DOKill();
                simpleModeCanvasGroup.alpha = 0f;
                simpleModeCanvasGroup.interactable = false;
                simpleModeCanvasGroup.blocksRaycasts = false;
            }

            // 盤面を表示（演出を見せる）
            if (boardCanvasGroup != null)
            {
                boardCanvasGroup.DOKill();
                boardCanvasGroup.alpha = 1f;
                boardCanvasGroup.interactable = false;
                boardCanvasGroup.blocksRaycasts = false;
            }

            if (isReward)
            {
                // リワードパネル → 特殊クリックハンドラ経由
                Debug.Log($"[SimpleMode] → 盤面表示 → TryInvokeSpecialPanelClick({row}, {col})");
                bool handled = false;
                if (panelBoardController != null)
                {
                    handled = panelBoardController.TryInvokeSpecialPanelClick(row, col);
                }

                if (!handled)
                {
                    Debug.LogWarning("[SimpleMode] リワードクリックが処理されませんでした");
                }

                // リワード取得はターンを消費しない → 少し待ってからカード表示に戻す
                StartCoroutine(ReturnToSimpleModeAfterReward());
            }
            else
            {
                // 通常パネル → 既存の処理
                if (panelActionController == null) return;
                Debug.Log($"[SimpleMode] → 盤面表示 → OnPanelClicked({row}, {col})");
                panelActionController.OnPanelClicked(row, col);
            }
        });
    }

    /// <summary>
    /// リワード取得後、盤面を少し見せてからカード表示に戻す。
    /// リワードはターンを消費しないので、すぐにカード操作に復帰する。
    /// </summary>
    private IEnumerator ReturnToSimpleModeAfterReward()
    {
        // リワード取得演出（テキスト等）を見せる
        yield return new WaitForSeconds(0.6f);

        // 盤面を隠す
        isShowingBoardForAction = false;
        yield return new WaitForSeconds(0.1f);

        ShowCardArea();
        RefreshCards();
        isProcessingCard = false;
    }

    // =============================================================
    // カード操作可否
    // =============================================================

    /// <summary>
    /// Button を必ずカードのルートに配置する。
    /// ルートに Image がなければ透明 Image を追加して、
    /// カード全体がタップ対象になるようにする。
    /// </summary>
    private Button EnsureButtonOnClickableTarget(GameObject card)
    {
        // ルートに Image がなければ透明な Image を追加
        // → カード全面が raycast 対象になる
        Image rootImg = card.GetComponent<Image>();
        if (rootImg == null)
        {
            rootImg = card.AddComponent<Image>();
            rootImg.color = new Color(0f, 0f, 0f, 0f); // 完全透明
        }
        rootImg.raycastTarget = true;

        // Button は必ずルートに配置
        Button btn = card.GetComponent<Button>();
        if (btn == null) btn = card.AddComponent<Button>();

        // Button の Transition を None にして見た目の変化を防ぐ
        btn.transition = Selectable.Transition.None;

        Debug.Log($"[SimpleMode] Button → ルート '{card.name}'");
        return btn;
    }

    private void SetCardsInteractable(bool interactable)
    {
        foreach (GameObject card in activeCards)
        {
            if (card == null) continue;
            Button btn = card.GetComponent<Button>();
            if (btn != null) btn.interactable = interactable;
        }
    }

    // =============================================================
    // カード破棄
    // =============================================================

    private void ClearCards()
    {
        foreach (GameObject card in activeCards)
        {
            if (card != null) Destroy(card);
        }

        activeCards.Clear();
        StopDangerPulse();
    }

    // =============================================================
    // 演出
    // =============================================================

    /// <summary>
    /// ディール演出: カードが下から飛んできて扇形の定位置に収まる。
    /// トランプを配る感覚。
    /// 演出中はタップ不可にし、完了後に有効化する。
    /// </summary>
    private void PlayDealAnimation(GameObject card, int index, int totalCards)
    {
        if (card == null) return;

        RectTransform rt = card.GetComponent<RectTransform>();
        if (rt == null) return;

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.AddComponent<CanvasGroup>();

        // 演出中はタップ不可（alpha 低い状態でタップが吸われるのを防ぐ）
        Button btn = card.GetComponent<Button>();
        if (btn != null) btn.interactable = false;

        // 最終位置・角度を保存
        Vector2 targetPos = rt.anchoredPosition;
        Quaternion targetRot = rt.localRotation;

        // 初期状態: 中央下から、回転なし、小さく透明
        rt.anchoredPosition = new Vector2(0f, dealStartOffsetY);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one * 0.5f;
        cg.alpha = 0f;

        float delay = index * dealStaggerDelay;

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);

        // フェードイン
        seq.Append(cg.DOFade(1f, dealDuration * 0.4f).SetEase(Ease.OutQuad));

        // 位置: オーバーシュート → 定位置
        seq.Join(rt.DOAnchorPos(
            targetPos + new Vector2(0f, dealOvershootY),
            dealDuration * 0.6f
        ).SetEase(Ease.OutQuad));

        seq.Append(rt.DOAnchorPos(targetPos, dealDuration * 0.4f).SetEase(Ease.OutBack));

        // 回転: 途中から扇の角度にスイング (sequence 内の絶対時間)
        seq.Insert(dealDuration * 0.3f,
            rt.DOLocalRotateQuaternion(targetRot, dealDuration * 0.5f)
            .SetEase(Ease.OutBack));

        // スケール: 最初からぽんっと出る
        seq.Insert(0f,
            rt.DOScale(Vector3.one, dealDuration * 0.7f)
            .SetEase(Ease.OutBack, 1.2f));

        // 演出完了 → タップ有効化
        seq.OnComplete(() =>
        {
            if (btn != null) btn.interactable = true;
        });
    }

    /// <summary>
    /// カードの BackPanel (Image付き子) に CardShineEffect を付けて光らせる。
    /// </summary>
    private void AttachAndPlayShine(GameObject card, float delay)
    {
        if (card == null) return;

        // BackPanel など Image 付きの子を探す（Mask のターゲットになる）
        Transform target = FindChildRecursive(card.transform, "BackPanel");
        if (target == null)
        {
            // BackPanel がなければルートに Image があるか確認
            Image rootImg = card.GetComponent<Image>();
            if (rootImg != null)
            {
                target = card.transform;
            }
            else
            {
                // Image 付きの最初の子
                Image[] imgs = card.GetComponentsInChildren<Image>(true);
                if (imgs.Length > 0) target = imgs[0].transform;
            }
        }

        if (target == null) return;

        CardShineEffect shine = target.GetComponent<CardShineEffect>();
        if (shine == null)
        {
            shine = target.gameObject.AddComponent<CardShineEffect>();
        }

        // カードごとにシャイン開始タイミングをずらす
        shine.SetStartDelay(delay);
        shine.StartShineLoop();
    }

    /// <summary>
    /// タップ演出: カードが上にフライして消え → 盤面表示 → アクション実行。
    /// </summary>
    private void PlayCardTapFlyUp(GameObject card, System.Action onComplete)
    {
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        RectTransform rt = card.GetComponent<RectTransform>();
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) cg = card.AddComponent<CanvasGroup>();

        if (rt == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2 basePos = rt.anchoredPosition;

        Sequence seq = DOTween.Sequence();

        // 少し大きくなって
        seq.Append(rt.DOScale(Vector3.one * tapScaleUp, tapFlyDuration * 0.3f)
            .SetEase(Ease.OutQuad));

        // 上にフライ + フェードアウト
        seq.Append(rt.DOAnchorPosY(basePos.y + tapFlyUpDistance, tapFlyDuration * 0.7f)
            .SetEase(Ease.InQuad));
        seq.Join(cg.DOFade(0f, tapFlyDuration * 0.6f)
            .SetEase(Ease.InQuad));
        seq.Join(rt.DOScale(Vector3.one * 0.85f, tapFlyDuration * 0.7f)
            .SetEase(Ease.InQuad));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    // =============================================================
    // パネル種 → 表示名
    // =============================================================

    private string GetPanelTypeLabel(PanelType type)
    {
        switch (type)
        {
            case PanelType.Sword:   return "攻撃";
            case PanelType.Ammo:    return "弾薬";
            case PanelType.Heal:    return "回復";
            case PanelType.Coin:    return "コイン";
            case PanelType.LvUp:    return "経験値";
            case PanelType.Corrupt: return "腐敗";
            default:                return type.ToString();
        }
    }
}
