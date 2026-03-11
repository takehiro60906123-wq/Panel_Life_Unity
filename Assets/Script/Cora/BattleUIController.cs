using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class BattleUIController : MonoBehaviour
{
    [Header("グローバルUI")]
    public TextMeshProUGUI coinText;

    [Header("探索UI")]
    public TextMeshProUGUI encounterLabelText;
    public TextMeshProUGUI encounterStepText;

    [Header("銃UI")]
    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private Button pistolButton;
    [SerializeField] private TMP_Text gunGaugeText;
    [SerializeField] private PanelBattleManager panelBattleManager;
    [SerializeField] private Image[] ammoImages;
    [SerializeField] private TMP_Text ammoCountText;

    [Header("アイテムUI")]
    [SerializeField] private Button[] itemSlotButtons;
    [SerializeField] private TMP_Text[] itemSlotTexts;
    [SerializeField] private Image[] itemSlotIcons;
    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_Text scrapText;
    [SerializeField] private Color disabledItemColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("アイテムドラッグ演出")]
    [SerializeField] private Canvas dragVisualCanvas;
    [SerializeField] private Vector2 dragVisualSize = new Vector2(72f, 72f);
    [SerializeField] private Vector2 dragVisualPointerOffset = new Vector2(0f, 84f);
    [SerializeField, Range(0.1f, 1f)] private float dragVisualAlpha = 0.9f;

    private Image activeDragVisual;
    private RectTransform activeDragVisualRect;
    private Canvas resolvedDragVisualCanvas;

    [Header("階層UI")]
    public TMP_Text floorText;

    private void Start()
    {
        if (pistolButton != null)
        {
            pistolButton.onClick.RemoveAllListeners();
            pistolButton.onClick.AddListener(OnClickPistol);
        }

        BindItemSlotButtons();
        RefreshGunUI();
        RefreshInventoryUI();
    }

    private void BindItemSlotButtons()
    {
        if (itemSlotButtons == null) return;

        for (int i = 0; i < itemSlotButtons.Length; i++)
        {
            Button button = itemSlotButtons[i];
            if (button == null) continue;

            int slotIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClickItemSlot(slotIndex));

            BattleItemDragSlot dragSlot = button.GetComponent<BattleItemDragSlot>();
            if (dragSlot == null)
            {
                dragSlot = button.gameObject.AddComponent<BattleItemDragSlot>();
            }

            dragSlot.Setup(this, slotIndex);
        }
    }

    public void RefreshGunUI()
    {
        if (playerCombatController == null) return;

        int current = playerCombatController.GetGunGauge();
        int max = playerCombatController.GetGunGaugeMax();

        if (gunGaugeText != null)
        {
            gunGaugeText.text = $"GUN {current}/{max}";
        }

        if (ammoCountText != null)
        {
            ammoCountText.text = $"{current}/{max}";
        }

        RefreshAmmoIcons(current, max);

        if (pistolButton != null)
        {
            GunData gun = playerCombatController.GetGunData();

            bool canUse = false;
            if (gun != null)
            {
                switch (gun.gunType)
                {
                    case GunType.MachineGun:
                        canUse = playerCombatController.CanUseMachineGun();
                        break;

                    case GunType.Pistol:
                    case GunType.Shotgun:
                    case GunType.Rifle:
                        canUse = playerCombatController.CanUseGun();
                        break;
                }
            }

            pistolButton.interactable = canUse;
        }
    }

    public void RefreshInventoryUI()
    {
        BattleInventoryController inventory = panelBattleManager != null
            ? panelBattleManager.GetBattleInventoryController()
            : null;

        if (inventoryCountText != null)
        {
            if (inventory != null)
            {
                inventoryCountText.text = $"{inventory.Count}/{inventory.MaxSlots}";
            }
            else
            {
                inventoryCountText.text = "0/0";
            }
        }

        if (scrapText != null)
        {
            int scrap = panelBattleManager != null ? panelBattleManager.GetCurrentScrap() : 0;
            scrapText.text = $"SCRAP {scrap}";
        }

        int slotCount = 0;
        if (itemSlotButtons != null) slotCount = Mathf.Max(slotCount, itemSlotButtons.Length);
        if (itemSlotTexts != null) slotCount = Mathf.Max(slotCount, itemSlotTexts.Length);
        if (itemSlotIcons != null) slotCount = Mathf.Max(slotCount, itemSlotIcons.Length);

        for (int i = 0; i < slotCount; i++)
        {
            BattleItemData item = inventory != null ? inventory.GetItemAt(i) : null;
            bool hasItem = item != null;
            bool canUseNow = hasItem && panelBattleManager != null && panelBattleManager.CanUseInventoryItemAt(i);

            bool keepButtonInteractive = hasItem;
            if (hasItem && item.useTarget == BattleItemUseTarget.Self)
            {
                keepButtonInteractive = canUseNow;
            }

            Color slotColor = Color.white;
            if (hasItem && !canUseNow)
            {
                slotColor = disabledItemColor;
            }

            if (itemSlotButtons != null && i < itemSlotButtons.Length && itemSlotButtons[i] != null)
            {
                itemSlotButtons[i].interactable = keepButtonInteractive;
            }

            if (itemSlotTexts != null && i < itemSlotTexts.Length && itemSlotTexts[i] != null)
            {
                itemSlotTexts[i].text = hasItem ? item.itemName : "---";
                itemSlotTexts[i].color = hasItem ? slotColor : disabledItemColor;
            }

            if (itemSlotIcons != null && i < itemSlotIcons.Length && itemSlotIcons[i] != null)
            {
                if (hasItem)
                {
                    if (item.icon != null)
                    {
                        itemSlotIcons[i].sprite = item.icon;
                    }

                    itemSlotIcons[i].enabled = itemSlotIcons[i].sprite != null;
                    itemSlotIcons[i].color = slotColor;
                }
                else
                {
                    itemSlotIcons[i].enabled = false;
                    itemSlotIcons[i].color = disabledItemColor;
                }
            }
        }
    }

    private void RefreshAmmoIcons(int current, int max)
    {
        if (ammoImages == null || ammoImages.Length == 0) return;

        int displayCount = Mathf.Min(ammoImages.Length, max);
        int filledCount = Mathf.Min(current, displayCount);

        for (int i = 0; i < ammoImages.Length; i++)
        {
            if (ammoImages[i] == null) continue;

            bool isFilled = i >= ammoImages.Length - filledCount;

            ammoImages[i].color = isFilled
                ? Color.white
                : new Color(1f, 1f, 1f, 0.2f);
        }
    }

    private void OnClickPistol()
    {
        if (panelBattleManager == null) return;
        if (playerCombatController == null) return;

        GunData gun = playerCombatController.GetGunData();
        if (gun == null) return;

        switch (gun.gunType)
        {
            case GunType.Pistol:
                panelBattleManager.FirePistol();
                break;

            case GunType.MachineGun:
                panelBattleManager.FireMachineGun();
                break;

            case GunType.Shotgun:
                panelBattleManager.FireShotgun();
                break;

            case GunType.Rifle:
                panelBattleManager.FireRifle();
                break;
        }
    }

    private void OnClickItemSlot(int slotIndex)
    {
        if (panelBattleManager == null) return;

        BattleItemData item = GetInventoryItem(slotIndex);
        if (item == null) return;
        if (item.useTarget == BattleItemUseTarget.Enemy) return;

        panelBattleManager.TryUseInventoryItem(slotIndex);
    }

    public BattleItemData GetInventoryItem(int slotIndex)
    {
        BattleInventoryController inventory = panelBattleManager != null
            ? panelBattleManager.GetBattleInventoryController()
            : null;

        return inventory != null ? inventory.GetItemAt(slotIndex) : null;
    }

    public bool CanUseInventoryItemAt(int slotIndex)
    {
        return panelBattleManager != null && panelBattleManager.CanUseInventoryItemAt(slotIndex);
    }

    public void HandleItemDragEnd(int slotIndex, Vector2 screenPosition)
    {
        if (panelBattleManager == null) return;

        BattleItemData item = GetInventoryItem(slotIndex);
        if (item == null) return;
        if (item.useTarget != BattleItemUseTarget.Enemy) return;

        panelBattleManager.TryUseInventoryItemByDrag(slotIndex, screenPosition);
    }

    public void UpdateItemDragHover(Vector2 screenPosition)
    {
        panelBattleManager?.SetEnemyDragHoverByScreenPosition(screenPosition);
    }

    public void ClearItemDragHover()
    {
        panelBattleManager?.ClearEnemyDragHoverVisual();
    }

    public void BeginItemDragVisual(int slotIndex, Vector2 screenPosition, Camera eventCamera = null)
    {
        BattleItemData item = GetInventoryItem(slotIndex);
        if (item == null) return;

        Canvas canvas = GetOrResolveDragVisualCanvas();
        if (canvas == null) return;

        EndItemDragVisual();

        GameObject go = new GameObject("ItemDragVisual", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        activeDragVisual = go.GetComponent<Image>();
        activeDragVisual.raycastTarget = false;
        activeDragVisual.preserveAspect = true;
        activeDragVisual.color = new Color(1f, 1f, 1f, dragVisualAlpha);
        activeDragVisual.sprite = ResolveDragVisualSprite(slotIndex, item);
        activeDragVisual.enabled = activeDragVisual.sprite != null;

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        activeDragVisualRect = go.GetComponent<RectTransform>();
        activeDragVisualRect.sizeDelta = ResolveDragVisualSize(slotIndex);

        UpdateItemDragVisual(screenPosition, eventCamera);
    }

    public void UpdateItemDragVisual(Vector2 screenPosition, Camera eventCamera = null)
    {
        if (activeDragVisualRect == null) return;

        Canvas canvas = GetOrResolveDragVisualCanvas();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 anchoredPosition;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out anchoredPosition))
        {
            activeDragVisualRect.anchoredPosition = anchoredPosition + dragVisualPointerOffset;
        }
    }

    public void EndItemDragVisual()
    {
        if (activeDragVisual != null)
        {
            Destroy(activeDragVisual.gameObject);
        }

        activeDragVisual = null;
        activeDragVisualRect = null;
    }

    public void PlayItemDropFeedback(int slotIndex, bool overflow = false)
    {
        if (inventoryCountText != null)
        {
            RectTransform countRect = inventoryCountText.rectTransform;
            if (countRect != null)
            {
                countRect.DOKill();
                countRect.localScale = Vector3.one;
                countRect.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.25f, 8, 0.9f);
            }
        }

        if (overflow) return;
        if (itemSlotButtons == null) return;
        if (slotIndex < 0 || slotIndex >= itemSlotButtons.Length) return;
        if (itemSlotButtons[slotIndex] == null) return;

        RectTransform slotRect = itemSlotButtons[slotIndex].GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.DOKill();
            slotRect.localScale = Vector3.one;
            slotRect.DOPunchScale(new Vector3(0.22f, 0.22f, 0f), 0.28f, 8, 0.9f);
        }

        Image highlightImage = null;

        if (itemSlotIcons != null && slotIndex >= 0 && slotIndex < itemSlotIcons.Length)
        {
            highlightImage = itemSlotIcons[slotIndex];
        }

        if (highlightImage == null)
        {
            highlightImage = itemSlotButtons[slotIndex].GetComponent<Image>();
        }

        if (highlightImage != null)
        {
            Color baseColor = highlightImage.color;
            Color flashColor = new Color(1f, 0.92f, 0.45f, baseColor.a);

            highlightImage.DOKill();
            highlightImage.color = flashColor;
            highlightImage.DOColor(baseColor, 0.22f);
        }
    }

    private Canvas GetOrResolveDragVisualCanvas()
    {
        if (resolvedDragVisualCanvas == null)
        {
            resolvedDragVisualCanvas = dragVisualCanvas != null
                ? dragVisualCanvas
                : GetComponentInParent<Canvas>();
        }

        return resolvedDragVisualCanvas;
    }

    private Sprite ResolveDragVisualSprite(int slotIndex, BattleItemData item)
    {
        if (item != null && item.icon != null)
        {
            return item.icon;
        }

        if (itemSlotIcons != null && slotIndex >= 0 && slotIndex < itemSlotIcons.Length && itemSlotIcons[slotIndex] != null)
        {
            Sprite iconSprite = itemSlotIcons[slotIndex].sprite;
            if (iconSprite != null) return iconSprite;
        }

        if (itemSlotButtons != null && slotIndex >= 0 && slotIndex < itemSlotButtons.Length && itemSlotButtons[slotIndex] != null)
        {
            Image buttonImage = itemSlotButtons[slotIndex].GetComponent<Image>();
            if (buttonImage != null && buttonImage.sprite != null)
            {
                return buttonImage.sprite;
            }

            if (itemSlotButtons[slotIndex].targetGraphic is Image targetImage && targetImage.sprite != null)
            {
                return targetImage.sprite;
            }
        }

        return null;
    }

    private Vector2 ResolveDragVisualSize(int slotIndex)
    {
        if (itemSlotIcons != null && slotIndex >= 0 && slotIndex < itemSlotIcons.Length && itemSlotIcons[slotIndex] != null)
        {
            RectTransform iconRect = itemSlotIcons[slotIndex].rectTransform;
            if (iconRect != null && iconRect.rect.size.sqrMagnitude > 0.01f)
            {
                return iconRect.rect.size;
            }
        }

        if (itemSlotButtons != null && slotIndex >= 0 && slotIndex < itemSlotButtons.Length && itemSlotButtons[slotIndex] != null)
        {
            RectTransform buttonRect = itemSlotButtons[slotIndex].GetComponent<RectTransform>();
            if (buttonRect != null && buttonRect.rect.size.sqrMagnitude > 0.01f)
            {
                return buttonRect.rect.size;
            }
        }

        return dragVisualSize;
    }

    public void SetCoinText(int coins)
    {
        if (coinText != null)
        {
            coinText.text = coins.ToString();
        }
    }

    public void SetEncounterInfo(EncounterType encounterType, int remainingSteps)
    {
        if (encounterLabelText != null)
        {
            switch (encounterType)
            {
                case EncounterType.Empty:
                    encounterLabelText.text = "平穏な部屋";
                    break;

                case EncounterType.Treasure:
                    encounterLabelText.text = "宝物の部屋";
                    break;

                default:
                    encounterLabelText.text = "";
                    break;
            }
        }

        if (encounterStepText != null)
        {
            if (encounterType == EncounterType.Empty || encounterType == EncounterType.Treasure)
            {
                encounterStepText.text = $"あと {remainingSteps} ターン";
            }
            else
            {
                encounterStepText.text = "";
            }
        }
    }

    public void SetFloorText(int currentBattle, int totalBattles)
    {
        if (floorText != null)
        {
            floorText.text = $"{currentBattle}F / {totalBattles}F";
        }
    }
}
