using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PanelBoardController : MonoBehaviour
{
    [Header("盤面演出設定")]
    public float cellSize = 150f;

    private GameObject panelPrefab;
    private Transform boardParent;
    private List<PanelSetting> panelSettings;
    private Action<int, int> onPanelClicked;

    private int rows;
    private int cols;

    private PanelType[,] gridData;
    private GameObject[,] panelObjects;

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

                PanelType randomType = GetRandomPanelType();
                gridData[r, c] = randomType;
                panelObjects[r, c] = newPanel;

                UpdatePanelVisual(r, c);

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

        if (playerCombatController != null)
        {
            playerCombatController.AddGunGauge(chain.Count);
            Debug.Log("GunGauge: " + playerCombatController.GetGunGauge());
        }

        if (battleUIController != null)
        {
            battleUIController.RefreshGunUI();
        }

        foreach (Vector2Int pos in chain)
        {
            if (!IsInRange(pos.x, pos.y)) continue;

            gridData[pos.x, pos.y] = PanelType.None;

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

                        Transform writeIcon = panelObjects[writeRow, c].transform.Find("IconImage");
                        Transform readIcon = panelObjects[r, c].transform.Find("IconImage");

                        if (writeIcon != null && readIcon != null)
                        {
                            Image writeImg = writeIcon.GetComponent<Image>();
                            Image readImg = readIcon.GetComponent<Image>();

                            if (writeImg != null && readImg != null)
                            {
                                writeImg.sprite = readImg.sprite;
                                writeIcon.localScale = Vector3.one;

                                readImg.sprite = null;

                                int dropDistance = writeRow - r;
                                writeIcon.localPosition = new Vector3(0, dropDistance * cellSize, 0);
                                writeIcon.DOLocalMoveY(0, 0.4f).SetEase(Ease.OutBounce);
                            }
                        }
                    }

                    writeRow--;
                }
            }

            for (int r = writeRow; r >= 0; r--)
            {
                PanelType newType = GetRandomPanelType();
                gridData[r, c] = newType;

                Transform iconTransform = panelObjects[r, c].transform.Find("IconImage");
                if (iconTransform != null)
                {
                    Image img = iconTransform.GetComponent<Image>();
                    if (img != null) img.sprite = GetSpriteForType(newType);

                    iconTransform.localScale = Vector3.one;
                    iconTransform.localPosition = new Vector3(0, (r + 1) * cellSize, 0);

                    int dropOrder = writeRow - r;
                    iconTransform.DOLocalMoveY(0, 0.4f)
                        .SetDelay(0.08f * dropOrder)
                        .SetEase(Ease.OutBounce);
                }
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
            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetSpriteForType(gridData[row, col]);
            }

            icon.localScale = Vector3.one;
        }
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
    /// 盤面のランダムな位置に指定タイプのパネルを強制配置する。
    /// PanelCorrupt（盤面汚染）攻撃で使用。
    /// </summary>
    public int ForceSetRandomPanels(PanelType type, int count)
    {
        if (gridData == null || panelObjects == null) return 0;

        // 空きではないマスの中からランダムに選ぶ（既存パネルを上書き）
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // 既に同じタイプなら除外
                if (gridData[r, c] == type) continue;
                candidates.Add(new Vector2Int(r, c));
            }
        }

        if (candidates.Count == 0) return 0;

        // シャッフルして先頭から count 個を汚染
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
            UpdatePanelVisual(pos.x, pos.y);
            placed++;
        }

        return placed;
    }
}