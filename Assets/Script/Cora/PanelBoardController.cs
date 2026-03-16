using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PanelBoardController : MonoBehaviour
{
    [Header("盤面レイアウト設定")]
    public float cellSize = 150f;

    [Header("アイテム付きパネル演出")]
    [SerializeField] private Color normalPanelTint = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color attachedItemPanelTint = new Color(1f, 0.82f, 0.15f, 1f);

    [Header("パネル消去演出")]
    [SerializeField] private float tapFlashDuration = 0.04f;
    [SerializeField] private float tapScaleAmount = 1.15f;
    [SerializeField] private float chainStaggerDelay = 0.04f;
    [SerializeField] private float panelShrinkDuration = 0.15f;
    [SerializeField] private float boardShakeIntensity = 3f;
    [SerializeField] private int bigChainThreshold = 4;
    [SerializeField] private int maxChainThreshold = 5;
    [SerializeField] private GameObject panelBurstPrefab;
    [SerializeField] private GameObject swordPanelClearEffectPrefab;
    [SerializeField] private GameObject ammoPanelClearEffectPrefab;
    [SerializeField] private GameObject coinPanelClearEffectPrefab;
    [SerializeField] private GameObject healPanelClearEffectPrefab;
    [SerializeField] private GameObject levelUpPanelClearEffectPrefab;
    [SerializeField] private float chainPreviewScale = 1.10f;
    [SerializeField] private float chainPreviewJumpY = 10f;
    [SerializeField] private float chainPreviewFlashAlpha = 0.28f;
    [SerializeField] private float chainPreviewStagger = 0.015f;
[Header("剣パネル演出")]
[SerializeField] private Color swordAccentColor = new Color(1f, 0.82f, 0.35f, 1f);

[SerializeField] private float swordTapScaleX = 1.16f;
[SerializeField] private float swordTapScaleY = 0.92f;
[SerializeField] private float swordTapRotateZ = -10f;

[SerializeField] private float swordPreviewScaleX = 1.18f;
[SerializeField] private float swordPreviewScaleY = 0.90f;
[SerializeField] private float swordPreviewShiftX = 8f;

[SerializeField] private float swordPreClearScaleX = 1.20f;
[SerializeField] private float swordPreClearScaleY = 0.86f;
[SerializeField] private float swordPreClearShiftX = 14f;
[SerializeField] private float swordPreClearRotateZ = 14f;
[SerializeField] private float swordPreClearDuration = 0.05f;
[SerializeField] private float swordPreClearFlashAlpha = 0.45f;
    private static Sprite cachedWhiteSprite;
    private GameObject panelPrefab;
    private Transform boardParent;
    private List<PanelSetting> panelSettings;
    private Action<int, int> onPanelClicked;

    private int rows;
    private int cols;

    private PanelType[,] gridData;
    private GameObject[,] panelObjects;
    private BattleItemData[,] attachedItems;

    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private BattleUIController battleUIController;

    private void Awake()
    {
        if (battleUIController == null)
        {
            battleUIController = FindObjectOfType<BattleUIController>();
        }

        if (playerCombatController == null)
        {
            playerCombatController = FindObjectOfType<PlayerCombatController>();
        }
    }

    public bool Initialize(
        GameObject panelPrefabValue,
        Transform boardParentValue,
        List<PanelSetting> panelSettingsValue,
        int rowsValue,
        int colsValue,
        Action<int, int> clickCallback)
    {
        panelPrefab = panelPrefabValue;
        boardParent = boardParentValue;
        panelSettings = panelSettingsValue;
        rows = rowsValue;
        cols = colsValue;
        onPanelClicked = clickCallback;

        if (panelPrefab == null)
        {
            Debug.LogError("PanelBoardController: panelPrefab が未設定です。");
            return false;
        }

        if (boardParent == null)
        {
            Debug.LogError("PanelBoardController: boardParent が未設定です。");
            return false;
        }

        if (rows <= 0 || cols <= 0)
        {
            Debug.LogError("PanelBoardController: rows または cols が不正です。");
            return false;
        }

        gridData = new PanelType[rows, cols];
        panelObjects = new GameObject[rows, cols];
        attachedItems = new BattleItemData[rows, cols];

        return true;
    }

    public void GenerateBoard()
    {
        if (gridData == null || panelObjects == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject newPanel = Instantiate(panelPrefab, boardParent);
                newPanel.name = $"Panel_{r}_{c}";
                SetPanelBackgroundTransparent(newPanel);

                PanelType randomType = GetRandomPanelType();
                gridData[r, c] = randomType;
                panelObjects[r, c] = newPanel;
                attachedItems[r, c] = null;

                UpdatePanelVisual(r, c);
                RefreshPanelHighlightVisual(r, c);

                int row = r;
                int col = c;

                Button btn = newPanel.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        onPanelClicked?.Invoke(row, col);
                    });
                }
            }
        }
    }

    public PanelType GetPanelType(int row, int col)
    {
        if (!IsInRange(row, col)) return PanelType.None;
        return gridData[row, col];
    }

    public int GetPanelCount(PanelType targetType)
    {
        if (gridData == null) return 0;

        int count = 0;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == targetType)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public void ClearAllAttachedItems()
    {
        if (attachedItems == null || panelObjects == null) return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                attachedItems[r, c] = null;
                RefreshPanelHighlightVisual(r, c);
            }
        }
    }

    public bool TrySpawnAttachedItemPanel(float spawnChance)
    {
        if (gridData == null || attachedItems == null) return false;
        if (UnityEngine.Random.value > Mathf.Clamp01(spawnChance)) return false;

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == PanelType.Sword
                    || gridData[r, c] == PanelType.Heal
                    || gridData[r, c] == PanelType.LvUp)
                {
                    candidates.Add(new Vector2Int(r, c));
                }
            }
        }

        if (candidates.Count == 0) return false;

        Vector2Int selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        BattleItemData item = CreateAttachedItemForPanelType(gridData[selected.x, selected.y]);
        if (item == null) return false;

        attachedItems[selected.x, selected.y] = item;
        RefreshPanelHighlightVisual(selected.x, selected.y);

        Debug.Log($"[PanelItem] Spawned at ({selected.x},{selected.y}) panel={gridData[selected.x, selected.y]} item={item.itemName}");

        return true;
    }

    public List<CollectedPanelItemInfo> ConsumeAttachedItems(List<Vector2Int> positions)
    {
        List<CollectedPanelItemInfo> result = new List<CollectedPanelItemInfo>();
        if (positions == null || positions.Count == 0) return result;
        if (attachedItems == null) return result;

        HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>(positions);
        foreach (Vector2Int pos in uniquePositions)
        {
            if (!IsInRange(pos.x, pos.y)) continue;

            BattleItemData item = attachedItems[pos.x, pos.y];
            if (item == null) continue;

            result.Add(new CollectedPanelItemInfo
            {
                item = item,
                worldPosition = GetPanelWorldPosition(pos.x, pos.y)
            });

            attachedItems[pos.x, pos.y] = null;
            RefreshPanelHighlightVisual(pos.x, pos.y);
        }

        return result;
    }

    public int CollectAllPanelsOfType(PanelType targetType)
    {
        if (gridData == null || panelObjects == null) return 0;

        List<Vector2Int> positions = new List<Vector2Int>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == targetType)
                {
                    positions.Add(new Vector2Int(r, c));
                }
            }
        }

        if (positions.Count == 0)
        {
            return 0;
        }

        foreach (Vector2Int pos in positions)
        {
            gridData[pos.x, pos.y] = PanelType.None;
            attachedItems[pos.x, pos.y] = null;
            RefreshPanelHighlightVisual(pos.x, pos.y);

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj == null) continue;

            Transform iconTransform = panelObj.transform.Find("IconImage");
            if (iconTransform != null)
            {
                iconTransform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        Image img = iconTransform.GetComponent<Image>();
                        if (img != null) img.sprite = null;
                        iconTransform.localScale = Vector3.one;
                    });
            }
        }

        DOVirtual.DelayedCall(0.25f, () =>
        {
            if (this != null)
            {
                DropAndFillPanels();
            }
        });

        return positions.Count;
    }

    public List<Vector2Int> FindChain(int startRow, int startCol, PanelType targetType)
    {
        List<Vector2Int> chain = new List<Vector2Int>();
        if (!IsInRange(startRow, startCol)) return chain;

        int maxLink = GetCurrentMaxLink();

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int startNode = new Vector2Int(startRow, startCol);
        queue.Enqueue(startNode);
        visited.Add(startNode);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            if (chain.Count >= maxLink)
            {
                break;
            }

            Vector2Int current = queue.Dequeue();
            chain.Add(current);

            foreach (Vector2Int dir in directions)
            {
                int nr = current.x + dir.x;
                int nc = current.y + dir.y;

                if (!IsInRange(nr, nc)) continue;

                Vector2Int nextNode = new Vector2Int(nr, nc);
                if (!visited.Contains(nextNode) && gridData[nr, nc] == targetType)
                {
                    visited.Add(nextNode);
                    queue.Enqueue(nextNode);
                }
            }
        }

        return chain;
    }

    public List<Vector2Int> GetAdjacentLevelPanels(List<Vector2Int> attackChain)
    {
        HashSet<Vector2Int> levelPanels = new HashSet<Vector2Int>();

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int pos in attackChain)
        {
            foreach (Vector2Int dir in directions)
            {
                int nr = pos.x + dir.x;
                int nc = pos.y + dir.y;

                if (!IsInRange(nr, nc)) continue;

                if (gridData[nr, nc] == PanelType.LvUp)
                {
                    levelPanels.Add(new Vector2Int(nr, nc));
                }
            }
        }

        return new List<Vector2Int>(levelPanels);
    }

    public void ClearChainPanels(List<Vector2Int> chain)
    {
        if (chain == null || chain.Count == 0) return;

        foreach (Vector2Int pos in chain)
        {
            if (!IsInRange(pos.x, pos.y)) continue;

            gridData[pos.x, pos.y] = PanelType.None;
            attachedItems[pos.x, pos.y] = null;
            RefreshPanelHighlightVisual(pos.x, pos.y);

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj == null) continue;

            Transform iconTransform = panelObj.transform.Find("IconImage");
            if (iconTransform != null)
            {
                iconTransform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() =>
                    {
                        Image img = iconTransform.GetComponent<Image>();
                        if (img != null) img.sprite = null;
                        iconTransform.localScale = Vector3.one;
                    });
            }
        }
    }

    public void DropAndFillPanels()
    {
        for (int c = 0; c < cols; c++)
        {
            int writeRow = rows - 1;

            for (int r = rows - 1; r >= 0; r--)
            {
                if (gridData[r, c] != PanelType.None)
                {
                    if (r != writeRow)
                    {
                        gridData[writeRow, c] = gridData[r, c];
                        gridData[r, c] = PanelType.None;

                        attachedItems[writeRow, c] = attachedItems[r, c];
                        attachedItems[r, c] = null;

                        Transform writeIcon = panelObjects[writeRow, c].transform.Find("IconImage");
                        Transform readIcon = panelObjects[r, c].transform.Find("IconImage");

                        if (writeIcon != null && readIcon != null)
                        {
                            Image writeImg = writeIcon.GetComponent<Image>();
                            Image readImg = readIcon.GetComponent<Image>();

                            if (writeImg != null && readImg != null)
                            {
                                writeIcon.DOKill();
                                readIcon.DOKill();
                                writeImg.DOKill();
                                readImg.DOKill();

                                writeImg.sprite = readImg.sprite;
                                writeImg.color = Color.white;
                                writeIcon.localScale = Vector3.one;
                                writeIcon.localRotation = Quaternion.identity;

                                readImg.sprite = null;
                                readImg.color = Color.white;
                                readIcon.localScale = Vector3.one;
                                readIcon.localPosition = Vector3.zero;
                                readIcon.localRotation = Quaternion.identity;

                                int dropDistance = writeRow - r;
                                writeIcon.localPosition = new Vector3(0, dropDistance * cellSize, 0);
                                writeIcon.DOLocalMoveY(0, 0.4f).SetEase(Ease.OutBounce);
                            }
                        }

                        RefreshPanelHighlightVisual(writeRow, c);
                        RefreshPanelHighlightVisual(r, c);
                    }

                    writeRow--;
                }
            }

            for (int r = writeRow; r >= 0; r--)
            {
                PanelType newType = GetRandomPanelType();
                gridData[r, c] = newType;
                attachedItems[r, c] = null;

                Transform iconTransform = panelObjects[r, c].transform.Find("IconImage");
                if (iconTransform != null)
                {
                    iconTransform.DOKill();

                    Image img = iconTransform.GetComponent<Image>();
                    if (img != null)
                    {
                        img.DOKill();
                        img.sprite = GetSpriteForType(newType);
                        img.color = Color.white;
                    }

                    iconTransform.localScale = Vector3.one;
                    iconTransform.localRotation = Quaternion.identity;
                    iconTransform.localPosition = new Vector3(0, (r + 1) * cellSize, 0);

                    int dropOrder = writeRow - r;
                    iconTransform.DOLocalMoveY(0, 0.4f)
                        .SetDelay(0.08f * dropOrder)
                        .SetEase(Ease.OutBounce);
                }

                RefreshPanelHighlightVisual(r, c);
            }
        }
    }

    public Vector3 GetPanelWorldPosition(int row, int col)
    {
        if (!IsInRange(row, col)) return Vector3.zero;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return Vector3.zero;

        Transform uiTransform = panelObj.transform;
        Canvas canvas = boardParent.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            return uiTransform.position;
        }

        RectTransform rect = uiTransform as RectTransform;
        if (rect == null)
        {
            return uiTransform.position;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return uiTransform.position;
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);
        Vector3 world = new Vector3(screenPoint.x, screenPoint.y, Mathf.Abs(mainCam.transform.position.z));
        return mainCam.ScreenToWorldPoint(world);
    }

    private void UpdatePanelVisual(int row, int col)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon != null)
        {
            icon.DOKill();

            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.DOKill();
                img.sprite = GetSpriteForType(gridData[row, col]);
                img.color = Color.white;
            }

            icon.localScale = Vector3.one;
            icon.localPosition = Vector3.zero;
            icon.localRotation = Quaternion.identity;
        }

        SetPanelBackgroundTransparent(panelObj);
        RefreshPanelHighlightVisual(row, col);
    }

    private void RefreshPanelHighlightVisual(int row, int col)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        bool hasItem = attachedItems != null && attachedItems[row, col] != null;

        // 親パネルは常に完全透明
        SetPanelBackgroundTransparent(panelObj);

        // アイテム付き専用オーバーレイ
        Image itemFx = GetOrCreateItemPanelFx(panelObj.transform);
        if (itemFx != null)
        {
            itemFx.color = attachedItemPanelTint;
            itemFx.enabled = hasItem;
            itemFx.gameObject.SetActive(hasItem);
        }

        Transform badge = panelObj.transform.Find("ItemBadgeImage");
        if (badge == null)
        {
            badge = panelObj.transform.Find("ItemBadge");
        }

        if (badge != null)
        {
            badge.gameObject.SetActive(hasItem);
        }
    }

    private Image GetOrCreateItemPanelFx(Transform panelTransform)
    {
        if (panelTransform == null) return null;

        Transform fx = panelTransform.Find("ItemPanelFx");
        if (fx == null)
        {
            GameObject go = new GameObject("ItemPanelFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fx = go.transform;
            fx.SetParent(panelTransform, false);
            fx.SetSiblingIndex(0); // IconImage より後ろ

            RectTransform rt = fx as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Image img = fx.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetWhiteSprite();
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.enabled = false;
            }
        }

        return fx.GetComponent<Image>();
    }

    private Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;

        Texture2D tex = Texture2D.whiteTexture;
        cachedWhiteSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        return cachedWhiteSprite;
    }

    private BattleItemData CreateAttachedItemForPanelType(PanelType panelType)
    {
        int roll = UnityEngine.Random.Range(0, 100);

        switch (panelType)
        {
            case PanelType.Sword:
                return BattleItemData.CreatePreset(roll < 65
                    ? BattleItemType.AttackOil
                    : BattleItemType.ShockCanister);

            case PanelType.Heal:
                return BattleItemData.CreatePreset(roll < 75
                    ? BattleItemType.FieldBandage
                    : BattleItemType.ActivationCell);

            case PanelType.LvUp:
                return BattleItemData.CreatePreset(roll < 55
                    ? BattleItemType.AttackOil
                    : BattleItemType.ActivationCell);
        }

        return null;
    }

    private PanelType GetRandomPanelType()
    {
        int totalWeight = 0;

        if (panelSettings != null)
        {
            foreach (var setting in panelSettings)
            {
                totalWeight += setting.weight;
            }
        }

        if (totalWeight <= 0) return PanelType.Sword;

        int randomVal = UnityEngine.Random.Range(0, totalWeight);

        foreach (var setting in panelSettings)
        {
            randomVal -= setting.weight;
            if (randomVal < 0) return setting.type;
        }

        return PanelType.Sword;
    }

    private Sprite GetSpriteForType(PanelType type)
    {
        if (panelSettings != null)
        {
            foreach (var setting in panelSettings)
            {
                if (setting.type == type) return setting.panelImage;
            }
        }

        return null;
    }

    private bool IsInRange(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }

    private int GetCurrentMaxLink()
    {
        if (playerCombatController == null)
            return 3;

        return playerCombatController.GetMaxLink();
    }

    /// <summary>
    /// 盤面のランダムな位置にパネルを強制配置する（演出つき）。
    /// PanelCorrupt（盤面汚染）攻撃で使用。
    /// </summary>
    public int ForceSetRandomPanels(PanelType type, int count)
    {
        if (gridData == null || panelObjects == null) return 0;

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (gridData[r, c] == type) continue;
                candidates.Add(new Vector2Int(r, c));
            }
        }

        if (candidates.Count == 0) return 0;

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector2Int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        int placed = 0;
        for (int i = 0; i < Mathf.Min(count, candidates.Count); i++)
        {
            Vector2Int pos = candidates[i];
            gridData[pos.x, pos.y] = type;

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj != null)
            {
                Transform icon = panelObj.transform.Find("IconImage");
                if (icon != null)
                {
                    Image img = icon.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = GetSpriteForType(type);
                    }

                    icon.localScale = Vector3.one * 0.1f;
                    icon.localPosition = Vector3.zero;
                    icon.localRotation = Quaternion.identity;

                    SetPanelBackgroundTransparent(panelObj);

                    icon.DOScale(Vector3.one, 0.35f)
                        .SetDelay(0.15f * i)
                        .SetEase(Ease.OutBack);
                }
            }

            RefreshPanelHighlightVisual(pos.x, pos.y);
            placed++;
        }

        return placed;
    }

    private void SetPanelBackgroundTransparent(GameObject panelObj)
    {
        if (panelObj == null) return;

        Image bg = panelObj.GetComponent<Image>();
        if (bg != null)
        {
            Color c = bg.color;
            c.a = 0f;
            bg.color = c;
        }
    }

    public void PlayItemPanelCollectEffect(int row, int col)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon == null) return;

        Image itemFx = GetOrCreateItemPanelFx(panelObj.transform);

        if (itemFx != null)
        {
            itemFx.color = new Color(1f, 0.85f, 0.3f, 0f);

            itemFx
                .DOFade(0.9f, 0.12f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    itemFx.DOFade(0f, 0.25f);
                });
        }

        icon
            .DOScale(1.35f, 0.12f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                icon.DOScale(1f, 0.1f);
            });

        icon
            .DOLocalMoveY(icon.localPosition.y + 22f, 0.15f)
            .SetLoops(2, LoopType.Yoyo);
    }


    private void ResetIconTransform(Transform icon)
    {
        if (icon == null) return;

        icon.DOKill();
        icon.localScale = Vector3.one;
        icon.localPosition = Vector3.zero;
        icon.localRotation = Quaternion.identity;
    }

    // ============================================
    // タップ瞬間フィードバック
    // ============================================

    public void PlayTapFeedback(int row, int col, PanelType panelType)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon != null)
        {
            ResetIconTransform(icon);

            if (panelType == PanelType.Sword)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(icon.DOScale(new Vector3(swordTapScaleX, swordTapScaleY, 1f), 0.05f).SetEase(Ease.OutQuad));
                seq.Join(icon.DOLocalMoveX(6f, 0.05f).SetEase(Ease.OutQuad));
                seq.Append(icon.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutBack));
                seq.Join(icon.DOLocalMoveX(0f, 0.08f).SetEase(Ease.InOutQuad));
            }
            else
            {
                icon.DOScale(tapScaleAmount, 0.06f).SetEase(Ease.OutQuad);
            }
        }

        Image flash = GetOrCreateTapFlashFx(panelObj.transform);
        if (flash != null)
        {
            flash.DOKill();
            flash.enabled = true;
            flash.gameObject.SetActive(true);

            Color flashColor = panelType == PanelType.Sword
                ? new Color(swordAccentColor.r, swordAccentColor.g, swordAccentColor.b, 0f)
                : new Color(1f, 1f, 1f, 0f);

            flash.color = flashColor;

            float targetAlpha = panelType == PanelType.Sword ? 0.55f : 0.7f;

            flash.DOFade(targetAlpha, tapFlashDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    flash.DOFade(0f, 0.06f).OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.enabled = false;
                        }
                    });
                });
        }
    }

    public void PlayChainPreviewFeedback(List<Vector2Int> chain, PanelType panelType)
    {
        if (chain == null || chain.Count == 0) return;

        Color accent = GetChainPreviewColor(panelType);

        for (int i = 0; i < chain.Count; i++)
        {
            Vector2Int pos = chain[i];
            float delay = i * chainPreviewStagger;

            DOVirtual.DelayedCall(delay, () =>
            {
                if (this == null) return;
                PlaySingleChainPreview(pos.x, pos.y, panelType, accent);
            });
        }
    }

    private Color GetChainPreviewColor(PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.Sword:
                return new Color(1f, 0.82f, 0.35f, 1f);

            case PanelType.Ammo:
                return new Color(0.45f, 0.9f, 1f, 1f);

            case PanelType.Heal:
                return new Color(0.45f, 1f, 0.55f, 1f);

            case PanelType.Coin:
                return new Color(1f, 0.9f, 0.25f, 1f);

            case PanelType.LvUp:
                return new Color(0.75f, 0.55f, 1f, 1f);

            default:
                return Color.white;
        }
    }

    private void PlaySingleChainPreview(int row, int col, PanelType panelType, Color flashColor)
    {
        if (!IsInRange(row, col)) return;

        GameObject panelObj = panelObjects[row, col];
        if (panelObj == null) return;

        Transform icon = panelObj.transform.Find("IconImage");
        if (icon != null)
        {
            ResetIconTransform(icon);

            Vector3 baseScale = Vector3.one;
            Vector3 basePos = Vector3.zero;

            if (panelType == PanelType.Sword)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(icon.DOScale(new Vector3(swordPreviewScaleX, swordPreviewScaleY, 1f), 0.05f).SetEase(Ease.OutQuad));
                seq.Join(icon.DOLocalMove(basePos + new Vector3(swordPreviewShiftX, chainPreviewJumpY, 0f), 0.05f).SetEase(Ease.OutQuad));
                seq.Append(icon.DOScale(baseScale, 0.08f).SetEase(Ease.OutBack));
                seq.Join(icon.DOLocalMove(basePos, 0.08f).SetEase(Ease.InOutQuad));
            }
            else
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(icon.DOScale(chainPreviewScale, 0.05f).SetEase(Ease.OutQuad));
                seq.Join(icon.DOLocalMoveY(basePos.y + chainPreviewJumpY, 0.05f).SetEase(Ease.OutQuad));
                seq.Append(icon.DOScale(baseScale, 0.08f).SetEase(Ease.OutBack));
                seq.Join(icon.DOLocalMoveY(basePos.y, 0.08f).SetEase(Ease.InOutQuad));
            }
        }

        Image flash = GetOrCreateTapFlashFx(panelObj.transform);
        if (flash != null)
        {
            flash.DOKill();
            flash.enabled = true;
            flash.gameObject.SetActive(true);
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);

            float alpha = panelType == PanelType.Sword
                ? Mathf.Max(chainPreviewFlashAlpha, 0.35f)
                : chainPreviewFlashAlpha;

            flash.DOFade(alpha, 0.05f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    flash.DOFade(0f, 0.08f).OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.enabled = false;
                        }
                    });
                });
        }
    }

    private void PlaySwordPreClearFeedback(Transform iconTransform, Transform panelTransform)
    {
        if (iconTransform == null) return;

        ResetIconTransform(iconTransform);

        Vector3 baseScale = Vector3.one;
        Vector3 basePos = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(iconTransform.DOScale(new Vector3(swordPreClearScaleX, swordPreClearScaleY, 1f), swordPreClearDuration).SetEase(Ease.OutQuad));
        seq.Join(iconTransform.DOLocalMove(basePos + new Vector3(swordPreClearShiftX, 0f, 0f), swordPreClearDuration).SetEase(Ease.OutQuad));
        seq.Append(iconTransform.DOScale(baseScale, 0.04f).SetEase(Ease.InQuad));
        seq.Join(iconTransform.DOLocalMove(basePos, 0.04f).SetEase(Ease.InQuad));

        Image flash = GetOrCreateTapFlashFx(panelTransform);
        if (flash != null)
        {
            flash.DOKill();
            flash.enabled = true;
            flash.gameObject.SetActive(true);
            flash.color = new Color(swordAccentColor.r, swordAccentColor.g, swordAccentColor.b, 0f);

            flash.DOFade(swordPreClearFlashAlpha, swordPreClearDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    flash.DOFade(0f, 0.05f).OnComplete(() =>
                    {
                        if (flash != null)
                        {
                            flash.enabled = false;
                        }
                    });
                });
        }
    }
    public void PlayImpactShake(float multiplier = 1f, float duration = 0.10f)
    {
        if (boardParent == null) return;

        boardParent.DOKill();
        boardParent.DOShakePosition(
            duration,
            boardShakeIntensity * multiplier,
            20,
            90f,
            false,
            true
        );
    }

    private Image GetOrCreateTapFlashFx(Transform panelTransform)
    {
        if (panelTransform == null) return null;

        Transform fx = panelTransform.Find("TapFlashFx");
        if (fx == null)
        {
            GameObject go = new GameObject("TapFlashFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fx = go.transform;
            fx.SetParent(panelTransform, false);
            fx.SetAsLastSibling();

            RectTransform rt = fx as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            Image img = fx.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetWhiteSprite();
                img.type = Image.Type.Simple;
                img.raycastTarget = false;
                img.enabled = false;
                img.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        return fx.GetComponent<Image>();
    }

    private GameObject ResolvePanelClearEffectPrefab(PanelType panelType)
    {
        switch (panelType)
        {
            case PanelType.Sword:
                return swordPanelClearEffectPrefab != null ? swordPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.Ammo:
                return ammoPanelClearEffectPrefab != null ? ammoPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.Coin:
                return coinPanelClearEffectPrefab != null ? coinPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.Heal:
                return healPanelClearEffectPrefab != null ? healPanelClearEffectPrefab : panelBurstPrefab;
            case PanelType.LvUp:
                return levelUpPanelClearEffectPrefab != null ? levelUpPanelClearEffectPrefab : panelBurstPrefab;
            default:
                return panelBurstPrefab;
        }
    }

    // ============================================
    // 波紋ディレイ付き消去（コルーチン版）
    // ClearChainPanels の演出強化版。落下補充を内包する。
    // 既存の ClearChainPanels はそのまま残す（一括消去用）。
    // ============================================

    public IEnumerator ClearChainPanelsAnimated(List<Vector2Int> chain, PanelType sourceType, Action onComplete = null)
    {
        if (chain == null || chain.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        int chainSize = chain.Count;

        for (int i = 0; i < chainSize; i++)
        {
            Vector2Int pos = chain[i];
            if (!IsInRange(pos.x, pos.y)) continue;

            GameObject panelObj = panelObjects[pos.x, pos.y];
            if (panelObj == null) continue;

            Transform iconTransform = panelObj.transform.Find("IconImage");
            if (iconTransform == null) continue;

            Image img = iconTransform.GetComponent<Image>();

            if (sourceType == PanelType.Sword)
            {
                PlaySwordPreClearFeedback(iconTransform, panelObj.transform);
                yield return new WaitForSeconds(0.02f);
            }

            gridData[pos.x, pos.y] = PanelType.None;
            attachedItems[pos.x, pos.y] = null;
            RefreshPanelHighlightVisual(pos.x, pos.y);

            if (img != null)
            {
                img.DOColor(Color.white, 0.04f).SetEase(Ease.OutQuad);
            }

            iconTransform.DOScale(Vector3.zero, panelShrinkDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (img != null)
                    {
                        img.sprite = null;
                        img.color = Color.white;
                    }
                    iconTransform.localScale = Vector3.one;
                    iconTransform.localPosition = Vector3.zero;
                    iconTransform.localRotation = Quaternion.identity;
                });

            if (img != null)
            {
                img.DOFade(0f, panelShrinkDuration * 0.8f);
            }

            GameObject clearEffectPrefab = ResolvePanelClearEffectPrefab(sourceType);
            if (clearEffectPrefab != null)
            {
                Vector3 burstPos = GetPanelWorldPosition(pos.x, pos.y);
                GameObject burst = Instantiate(clearEffectPrefab, burstPos, Quaternion.identity);
                Destroy(burst, 1.0f);
            }

            if (i < chainSize - 1)
            {
                yield return new WaitForSeconds(chainStaggerDelay);
            }
        }

        yield return new WaitForSeconds(panelShrinkDuration);

        if (chainSize >= bigChainThreshold && boardParent != null)
        {
            float intensity = chainSize >= maxChainThreshold
                ? boardShakeIntensity * 1.5f
                : boardShakeIntensity;
            boardParent.DOShakePosition(0.15f, intensity, 20, 90f, false, true);
        }

        DropAndFillPanels();

        onComplete?.Invoke();
    }
}