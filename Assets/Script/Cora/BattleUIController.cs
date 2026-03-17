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

    private RectTransform activeDragVisualRect;

    [Header("銃UI演出")]
    [SerializeField] private float fullGaugePulseScale = 1.06f;
    [SerializeField] private float fullGaugePulseDuration = 0.45f;
    [SerializeField] private float readyPopScale = 1.14f;
    [SerializeField] private float firePressScale = 0.9f;
    [SerializeField] private float fireReturnDuration = 0.14f;
    [SerializeField] private Color readyButtonColor = new Color(1f, 0.95f, 0.7f, 1f);
    [SerializeField] private float ammoLoadSlotPunchScale = 0.18f;
    [SerializeField] private float ammoLoadSlotPunchDuration = 0.24f;
    [SerializeField] private float ammoLoadSlotStepDelay = 0.035f;
    [SerializeField] private float ammoLoadTextPunchScale = 0.18f;
    [SerializeField] private float expBarPulseScale = 1.10f;
    [SerializeField] private float expBarPulseDuration = 0.22f;


    [Header("薬きょう演出")]
    [SerializeField] private Sprite gunShellSprite;
    [SerializeField] private RectTransform gunShellEffectRoot;
    [SerializeField] private Vector2 gunShellAnchorOffset = new Vector2(-22f, 26f);
    [SerializeField] private Vector2 pistolShellTravel = new Vector2(-78f, 68f);
    [SerializeField] private Vector2 machineGunShellTravel = new Vector2(-66f, 56f);
    [SerializeField] private Vector2 shotgunShellTravel = new Vector2(-86f, 74f);
    [SerializeField] private Vector2 rifleShellTravel = new Vector2(-92f, 82f);
    [SerializeField] private float pistolShellDuration = 0.40f;
    [SerializeField] private float machineGunShellDuration = 0.32f;
    [SerializeField] private float shotgunShellDuration = 0.46f;
    [SerializeField] private float rifleShellDuration = 0.44f;
    [SerializeField] private float gunShellBaseDelay = 0.03f;
    [SerializeField] private float gunShellScale = 0.42f;
    [SerializeField] private float gunShellFadeStartRatio = 0.48f;
    [SerializeField] private Color gunShellTint = new Color(1f, 0.96f, 0.82f, 0.96f);

    private Tween ammoCountPulseTween;
    private Tween pistolButtonPulseTween;

    private int lastGunGauge = -1;
    private bool lastCanUseGun = false;
    private Color cachedButtonBaseColor = Color.white;
    private bool hasCachedButtonBaseColor = false;

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
    private Canvas resolvedDragVisualCanvas;

    [Header("階層UI")]
    public TMP_Text floorText;

    [SerializeField] private Slider playerExpBar;

    private Tween gunGaugePulseTween;
    private void Start()
    {
        if (pistolButton != null)
        {
            pistolButton.onClick.RemoveAllListeners();
            pistolButton.onClick.AddListener(OnClickPistol);

            if (pistolButton.targetGraphic != null)
            {
                cachedButtonBaseColor = pistolButton.targetGraphic.color;
                hasCachedButtonBaseColor = true;
            }
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

        bool canUse = playerCombatController.CanUseCurrentGun();

        if (pistolButton != null)
        {
            pistolButton.interactable = canUse;
        }

        UpdateGunReadyPulse(current, max);
        PlayGunReadyAvailableFeedbackIfNeeded(canUse);
        PlayGunGaugeDeltaFeedbackIfNeeded(current);

        lastCanUseGun = canUse;
        lastGunGauge = current;
    }

    private void PlayGunGaugeDeltaFeedbackIfNeeded(int current)
    {
        if (lastGunGauge < 0) return;
        if (current <= lastGunGauge) return;

        if (ammoCountText != null)
        {
            ammoCountText.transform.DOKill();
            ammoCountText.transform.localScale = Vector3.one;
            ammoCountText.transform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.2f, 8, 0.85f);
        }

        if (gunGaugeText != null)
        {
            gunGaugeText.transform.DOKill();
            gunGaugeText.transform.localScale = Vector3.one;
            gunGaugeText.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.18f, 8, 0.85f);
        }
    }

    private void PlayGunReadyAvailableFeedbackIfNeeded(bool canUse)
    {
        if (lastGunGauge < 0) return;
        if (lastCanUseGun || !canUse) return;

        if (pistolButton != null)
        {
            RectTransform buttonRect = pistolButton.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.DOKill();
                buttonRect.localScale = Vector3.one;
                buttonRect.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.25f, 8, 0.85f);
            }

            if (pistolButton.targetGraphic != null)
            {
                Color baseColor = hasCachedButtonBaseColor ? cachedButtonBaseColor : pistolButton.targetGraphic.color;
                pistolButton.targetGraphic.DOKill();
                pistolButton.targetGraphic.color = readyButtonColor;
                pistolButton.targetGraphic.DOColor(baseColor, 0.22f);
            }
        }
    }

    private void UpdateGunReadyPulse(int current, int max)
    {
        bool isFull = current >= max;

        if (isFull)
        {
            if (gunGaugeText != null && (gunGaugePulseTween == null || !gunGaugePulseTween.IsActive()))
            {
                gunGaugeText.transform.localScale = Vector3.one;
                gunGaugePulseTween = gunGaugeText.transform
                    .DOScale(Vector3.one * fullGaugePulseScale, fullGaugePulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }

            if (ammoCountText != null && (ammoCountPulseTween == null || !ammoCountPulseTween.IsActive()))
            {
                ammoCountText.transform.localScale = Vector3.one;
                ammoCountPulseTween = ammoCountText.transform
                    .DOScale(Vector3.one * fullGaugePulseScale, fullGaugePulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            if (gunGaugePulseTween != null)
            {
                gunGaugePulseTween.Kill();
                gunGaugePulseTween = null;
            }

            if (ammoCountPulseTween != null)
            {
                ammoCountPulseTween.Kill();
                ammoCountPulseTween = null;
            }

            if (gunGaugeText != null)
            {
                gunGaugeText.transform.localScale = Vector3.one;
            }

            if (ammoCountText != null)
            {
                ammoCountText.transform.localScale = Vector3.one;
            }
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
        if (panelBattleManager.GunCombatController == null) return;
        if (playerCombatController == null) return;

        GunData gun = playerCombatController.GetGunData();
        if (gun == null) return;

        PlayGunFireFeedback();
        panelBattleManager.GunCombatController.FireEquippedGun();
    }
    private void PlayGunFireFeedback()
    {
        if (pistolButton != null)
        {
            RectTransform buttonRect = pistolButton.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.DOKill();
                buttonRect.localScale = Vector3.one;

                Sequence seq = DOTween.Sequence();
                seq.Append(buttonRect.DOScale(Vector3.one * firePressScale, 0.05f).SetEase(Ease.OutQuad));
                seq.Append(buttonRect.DOScale(Vector3.one, fireReturnDuration).SetEase(Ease.OutBack));
                pistolButtonPulseTween = seq;
            }
        }

        if (gunGaugeText != null)
        {
            gunGaugeText.transform.DOKill();
            gunGaugeText.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.16f, 8, 0.85f);
        }

        if (ammoCountText != null)
        {
            ammoCountText.transform.DOKill();
            ammoCountText.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.18f, 8, 0.85f);
        }
    }




    public void PlayGunShellEject(GunType gunType, float extraDelay = 0f)
    {
        if (!isActiveAndEnabled) return;

        RectTransform buttonRect = pistolButton != null ? pistolButton.transform as RectTransform : null;
        if (buttonRect == null) return;

        RectTransform effectRoot = ResolveGunShellEffectRoot(buttonRect);
        if (effectRoot == null) return;

        Sprite shellSprite = ResolveGunShellSprite();
        if (shellSprite == null) return;

        float delay = Mathf.Max(0f, gunShellBaseDelay + extraDelay);
        DOVirtual.DelayedCall(delay, () =>
        {
            if (this == null || !isActiveAndEnabled) return;
            SpawnGunShell(shellSprite, buttonRect, effectRoot, gunType);
        });
    }

    private RectTransform ResolveGunShellEffectRoot(RectTransform buttonRect)
    {
        if (gunShellEffectRoot != null)
        {
            return gunShellEffectRoot;
        }

        Canvas buttonCanvas = buttonRect != null ? buttonRect.GetComponentInParent<Canvas>() : null;
        if (buttonCanvas != null)
        {
            return buttonCanvas.transform as RectTransform;
        }

        return transform as RectTransform;
    }

    private Sprite ResolveGunShellSprite()
    {
        if (gunShellSprite != null)
        {
            return gunShellSprite;
        }

        if (ammoImages != null)
        {
            for (int i = 0; i < ammoImages.Length; i++)
            {
                if (ammoImages[i] != null && ammoImages[i].sprite != null)
                {
                    return ammoImages[i].sprite;
                }
            }
        }

        return null;
    }

    private void SpawnGunShell(Sprite shellSprite, RectTransform buttonRect, RectTransform effectRoot, GunType gunType)
    {
        if (shellSprite == null || buttonRect == null || effectRoot == null) return;

        Canvas rootCanvas = effectRoot.GetComponentInParent<Canvas>();
        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        Vector3 worldAnchor = buttonRect.TransformPoint((Vector3)gunShellAnchorOffset);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldAnchor);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPoint, uiCamera, out Vector2 localPoint))
        {
            return;
        }

        GameObject shellObj = new GameObject("GunShellFx", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        shellObj.transform.SetParent(effectRoot, false);
        shellObj.transform.SetAsLastSibling();

        RectTransform shellRect = shellObj.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = shellObj.GetComponent<CanvasGroup>();
        Image shellImage = shellObj.GetComponent<Image>();

        shellImage.sprite = shellSprite;
        shellImage.preserveAspect = true;
        shellImage.raycastTarget = false;
        shellImage.color = gunShellTint;
        canvasGroup.alpha = 1f;

        Vector2 size = shellSprite.rect.size * gunShellScale;
        shellRect.sizeDelta = size;
        shellRect.anchoredPosition = localPoint;
        shellRect.localScale = Vector3.one;
        shellRect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-18f, 18f));

        Vector2 travel = ResolveShellTravel(gunType);
        float duration = ResolveShellDuration(gunType);

        float sideVariance = UnityEngine.Random.Range(-10f, 10f);
        Vector2 end = localPoint + travel + new Vector2(sideVariance, UnityEngine.Random.Range(-10f, 6f));
        Vector2 control = localPoint + new Vector2(travel.x * 0.42f, Mathf.Max(28f, travel.y * 0.92f));

        DOTween.To(() => 0f, t =>
        {
            if (shellRect == null) return;
            shellRect.anchoredPosition = EvaluateQuadratic(localPoint, control, end, t);
        }, 1f, duration).SetEase(Ease.OutQuad);

        float rotateAmount = UnityEngine.Random.Range(180f, 300f);
        shellRect.DOLocalRotate(new Vector3(0f, 0f, rotateAmount), duration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic);

        Sequence scaleSeq = DOTween.Sequence();
        scaleSeq.Append(shellRect.DOScale(1.06f, duration * 0.18f).SetEase(Ease.OutQuad));
        scaleSeq.Append(shellRect.DOScale(0.88f, duration * 0.82f).SetEase(Ease.InQuad));

        float fadeStart = Mathf.Clamp01(gunShellFadeStartRatio) * duration;
        canvasGroup.DOFade(0f, Mathf.Max(0.08f, duration - fadeStart)).SetDelay(fadeStart).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (shellObj != null)
                {
                    Destroy(shellObj);
                }
            });
    }

    private Vector2 ResolveShellTravel(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.MachineGun:
                return machineGunShellTravel;
            case GunType.Shotgun:
                return shotgunShellTravel;
            case GunType.Rifle:
                return rifleShellTravel;
            case GunType.Pistol:
            default:
                return pistolShellTravel;
        }
    }

    private float ResolveShellDuration(GunType gunType)
    {
        switch (gunType)
        {
            case GunType.MachineGun:
                return machineGunShellDuration;
            case GunType.Shotgun:
                return shotgunShellDuration;
            case GunType.Rifle:
                return rifleShellDuration;
            case GunType.Pistol:
            default:
                return pistolShellDuration;
        }
    }

    private Vector2 EvaluateQuadratic(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float u = 1f - t;
        return (u * u * start) + (2f * u * t * control) + (t * t * end);
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

    // ============================================
    // アイテムGET飛行オーブの到着先座標を返す
    // ============================================

    public Vector3 GetInventorySlotWorldPosition(int slotIndex)
    {
        if (itemSlotButtons == null) return Vector3.zero;
        if (slotIndex < 0 || slotIndex >= itemSlotButtons.Length) return Vector3.zero;
        if (itemSlotButtons[slotIndex] == null) return Vector3.zero;

        RectTransform slotRect = itemSlotButtons[slotIndex].GetComponent<RectTransform>();
        if (slotRect == null) return Vector3.zero;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera mainCam = Camera.main;
        if (canvas == null || mainCam == null) return slotRect.position;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, slotRect.position);
        Vector3 world = new Vector3(screenPoint.x, screenPoint.y,
            Mathf.Abs(mainCam.transform.position.z));
        return mainCam.ScreenToWorldPoint(world);
    }

    // ============================================
    // 飛行オーブ到着時のスロットパルス演出
    // ============================================

    public void PlayInventorySlotReceivePulse(int slotIndex)
    {
        if (itemSlotButtons == null) return;
        if (slotIndex < 0 || slotIndex >= itemSlotButtons.Length) return;
        if (itemSlotButtons[slotIndex] == null) return;

        RectTransform slotRect = itemSlotButtons[slotIndex].GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.DOKill();
            slotRect.localScale = Vector3.one;
            slotRect.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.2f, 10, 0.8f);
        }

        Image highlightImage = null;
        if (itemSlotIcons != null && slotIndex >= 0 && slotIndex < itemSlotIcons.Length)
        {
            highlightImage = itemSlotIcons[slotIndex];
        }

        if (highlightImage == null && itemSlotButtons[slotIndex] != null)
        {
            highlightImage = itemSlotButtons[slotIndex].GetComponent<Image>();
        }

        if (highlightImage != null)
        {
            Color baseColor = highlightImage.color;
            Color flashColor = new Color(1f, 0.95f, 0.5f, baseColor.a);
            highlightImage.DOKill();
            highlightImage.color = flashColor;
            highlightImage.DOColor(baseColor, 0.3f);
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

    private Vector3 ConvertUIRectToWorldPosition(RectTransform rect)
    {
        if (rect == null) return Vector3.zero;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera mainCam = Camera.main;
        if (canvas == null || mainCam == null) return rect.position;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);
        Vector3 world = new Vector3(screenPoint.x, screenPoint.y,
            Mathf.Abs(mainCam.transform.position.z));
        return mainCam.ScreenToWorldPoint(world);
    }

    public Vector3 GetGunGaugeWorldPosition()
    {
        RectTransform targetRect = null;

        if (ammoCountText != null)
        {
            targetRect = ammoCountText.rectTransform;
        }
        else if (gunGaugeText != null)
        {
            targetRect = gunGaugeText.rectTransform;
        }
        else if (pistolButton != null)
        {
            targetRect = pistolButton.transform as RectTransform;
        }

        return ConvertUIRectToWorldPosition(targetRect);
    }

    public Vector3 GetCoinWorldPosition()
    {
        if (coinText == null) return Vector3.zero;
        return ConvertUIRectToWorldPosition(coinText.rectTransform);
    }

    public void PlayGunGaugeReceivePulse()
    {
        if (gunGaugeText != null)
        {
            gunGaugeText.transform.DOKill();
            gunGaugeText.transform.localScale = Vector3.one;
            gunGaugeText.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.2f, 8, 0.85f);
        }

        if (ammoCountText != null)
        {
            ammoCountText.transform.DOKill();
            ammoCountText.transform.localScale = Vector3.one;
            ammoCountText.transform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.22f, 8, 0.85f);
        }

        if (ammoImages != null)
        {
            foreach (Image ammoImage in ammoImages)
            {
                if (ammoImage == null) continue;
                ammoImage.transform.DOKill();
                ammoImage.transform.localScale = Vector3.one;
            }

            for (int i = 0; i < ammoImages.Length; i++)
            {
                Image ammoImage = ammoImages[i];
                if (ammoImage == null) continue;
                ammoImage.transform
                    .DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.18f, 6, 0.85f)
                    .SetDelay(i * 0.01f);
            }
        }
    }

    public void PlayGunGaugeLoadSequence(int previousGauge, int currentGauge, int maxGauge)
    {
        int displayMax = ammoImages != null && ammoImages.Length > 0
            ? Mathf.Min(ammoImages.Length, Mathf.Max(1, maxGauge))
            : Mathf.Max(1, maxGauge);

        int clampedPrev = Mathf.Clamp(previousGauge, 0, displayMax);
        int clampedCurrent = Mathf.Clamp(currentGauge, 0, displayMax);

        if (gunGaugeText != null)
        {
            gunGaugeText.transform.DOKill();
            gunGaugeText.transform.localScale = Vector3.one;
            gunGaugeText.transform.DOPunchScale(new Vector3(ammoLoadTextPunchScale * 0.75f, ammoLoadTextPunchScale * 0.75f, 0f), 0.18f, 8, 0.85f);
        }

        if (ammoCountText != null)
        {
            ammoCountText.transform.DOKill();
            ammoCountText.transform.localScale = Vector3.one;
            ammoCountText.transform.DOPunchScale(new Vector3(ammoLoadTextPunchScale, ammoLoadTextPunchScale, 0f), 0.2f, 8, 0.85f);
        }

        if (ammoImages == null || ammoImages.Length == 0)
        {
            return;
        }

        foreach (Image ammoImage in ammoImages)
        {
            if (ammoImage == null) continue;
            ammoImage.transform.DOKill();
            ammoImage.transform.localScale = Vector3.one;
        }

        if (clampedCurrent <= clampedPrev)
        {
            PlayGunGaugeReceivePulse();
            return;
        }

        int step = 0;
        for (int logicalGauge = clampedPrev + 1; logicalGauge <= clampedCurrent; logicalGauge++)
        {
            int imageIndex = ammoImages.Length - logicalGauge;
            if (imageIndex < 0 || imageIndex >= ammoImages.Length) continue;

            Image ammoImage = ammoImages[imageIndex];
            if (ammoImage == null) continue;

            RectTransform rect = ammoImage.rectTransform;
            float delay = ammoLoadSlotStepDelay * step;
            step++;

            Color litColor = Color.white;
            Color flashColor = new Color(1f, 0.98f, 0.82f, 1f);
            ammoImage.color = flashColor;

            Sequence seq = DOTween.Sequence();
            seq.SetLink(ammoImage.gameObject, LinkBehaviour.KillOnDestroy);
            seq.SetDelay(delay);
            seq.Append(rect.DOScale(1f + ammoLoadSlotPunchScale, ammoLoadSlotPunchDuration * 0.45f).SetEase(Ease.OutBack));
            seq.Join(DOVirtual.DelayedCall(0.01f, () =>
            {
                if (ammoImage != null)
                {
                    ammoImage.color = flashColor;
                }
            }));
            seq.Append(rect.DOScale(1f, ammoLoadSlotPunchDuration * 0.55f).SetEase(Ease.InOutQuad));
            seq.Join(ammoImage.DOColor(litColor, ammoLoadSlotPunchDuration * 0.55f).SetEase(Ease.OutQuad));
        }
    }

    public void PlayExpBarReceivePulse(bool isLevelUp)
    {
        if (playerExpBar == null) return;

        Transform t = playerExpBar.transform;
        t.DOKill();
        t.localScale = Vector3.one;

        float scale = isLevelUp ? expBarPulseScale + 0.04f : expBarPulseScale;
        float duration = isLevelUp ? expBarPulseDuration + 0.05f : expBarPulseDuration;
        t.DOPunchScale(new Vector3(scale - 1f, scale - 1f, 0f), duration, 8, 0.85f);
    }

    public void PlayCoinReceivePulse()
    {
        if (coinText == null) return;

        coinText.transform.DOKill();
        coinText.transform.localScale = Vector3.one;
        coinText.transform.DOPunchScale(new Vector3(0.14f, 0.14f, 0f), 0.22f, 8, 0.85f);
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
                case EncounterType.Shop:
                    encounterLabelText.text = "商店";
                    break;
                default:
                    encounterLabelText.text = "戦闘";
                    break;
            }
        }

        if (encounterStepText != null)
        {
            if (encounterType == EncounterType.Empty
                || encounterType == EncounterType.Treasure
                || encounterType == EncounterType.Shop)
            {
                encounterStepText.text = $"あと {remainingSteps} ターン";
            }
            else
            {
                encounterStepText.text = "";
            }
        }
    }

    public void SetFloorText(int currentFloor, int totalFloors)
    {
        if (floorText == null) return;
        floorText.text = $"{currentFloor}/{totalFloors}階";
    }

    public void SetPlayerExpBar(float normalized)
    {
        if (playerExpBar == null)
        {
            Debug.LogWarning("[BattleUIController] playerExpBar is null");
            return;
        }

        playerExpBar.value = Mathf.Clamp01(normalized);
        Debug.Log($"[BattleUIController] playerExpBar.value={playerExpBar.value}");
    }

    private void KillGunUiTweens()
    {
        if (gunGaugePulseTween != null)
        {
            gunGaugePulseTween.Kill();
            gunGaugePulseTween = null;
        }

        if (ammoCountPulseTween != null)
        {
            ammoCountPulseTween.Kill();
            ammoCountPulseTween = null;
        }

        if (pistolButtonPulseTween != null)
        {
            pistolButtonPulseTween.Kill();
            pistolButtonPulseTween = null;
        }

        if (gunGaugeText != null) gunGaugeText.transform.DOKill();
        if (ammoCountText != null) ammoCountText.transform.DOKill();

        if (pistolButton != null)
        {
            pistolButton.transform.DOKill();

            if (pistolButton.targetGraphic != null)
            {
                pistolButton.targetGraphic.DOKill();
            }
        }
    }
    private void ResetGunUiVisualState()
    {
        if (gunGaugeText != null)
        {
            gunGaugeText.transform.localScale = Vector3.one;
        }

        if (ammoCountText != null)
        {
            ammoCountText.transform.localScale = Vector3.one;
        }

        if (pistolButton != null)
        {
            pistolButton.transform.localScale = Vector3.one;

            if (pistolButton.targetGraphic != null && hasCachedButtonBaseColor)
            {
                pistolButton.targetGraphic.color = cachedButtonBaseColor;
            }
        }
    }

    private void OnDisable()
    {
        KillGunUiTweens();
        ResetGunUiVisualState();
    }
}