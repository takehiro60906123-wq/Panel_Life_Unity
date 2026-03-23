using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ShopUIController : MonoBehaviour
{
    [Header("ショップパネル")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private CanvasGroup shopCanvasGroup;

    [Header("所持コイン")]
    [SerializeField] private TMP_Text playerCoinsText;

    [Header("商品スロット")]
    [SerializeField] private ShopSlotUI[] shopSlots;

    [Header("立ち去るボタン")]
    [SerializeField] private Button leaveButton;

    [Header("演出設定")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    private ShopController shopController;
    private BattleSfxController battleSfxController;

    private void Start()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(OnClickLeave);
        }

        battleSfxController = FindObjectOfType<BattleSfxController>();
    }

    public void ShowShop(List<ShopItemData> offerings, ShopController controller)
    {
        shopController = controller;

        if (battleSfxController == null)
        {
            battleSfxController = FindObjectOfType<BattleSfxController>();
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = 0f;
            shopCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        }

        RefreshShop(offerings, controller);
    }

    public void HideShop()
    {
        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                if (shopPanel != null)
                {
                    shopPanel.SetActive(false);
                }
            });
        }
        else if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        shopController = null;
    }

    public void RefreshShop(List<ShopItemData> offerings, ShopController controller)
    {
        shopController = controller;

        if (playerCoinsText != null && controller != null)
        {
            playerCoinsText.text = $"所持: {controller.GetCurrentCoins()}G";
        }

        if (shopSlots == null) return;

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i] == null) continue;

            ShopItemData item = (offerings != null && i < offerings.Count) ? offerings[i] : null;
            bool canBuy = controller != null && controller.CanPurchase(i);

            shopSlots[i].SetItem(item, canBuy, i, OnClickBuy);
        }
    }

    private void OnClickBuy(int slotIndex)
    {
        if (shopController == null) return;

        bool success = shopController.TryPurchase(slotIndex);
        if (!success) return;

        battleSfxController?.PlayUiDecide();

        if (shopSlots != null && slotIndex >= 0 && slotIndex < shopSlots.Length)
        {
            shopSlots[slotIndex].PlayPurchasedFeedback();
        }
    }

    private void OnClickLeave()
    {
        battleSfxController?.PlayUiCancel();

        if (shopController != null)
        {
            shopController.CloseShop();
        }
    }

    public bool IsShopOpen => shopPanel != null && shopPanel.activeSelf;
}

[System.Serializable]
public class ShopSlotUI
{
    public GameObject slotRoot;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text costText;
    public Button buyButton;
    public Image iconImage;

    [Header("色設定")]
    public Color affordableColor = Color.white;
    public Color tooExpensiveColor = new Color(1f, 0.4f, 0.4f);
    public Color soldOutColor = new Color(0.5f, 0.5f, 0.5f);

    public void SetItem(ShopItemData item, bool canBuy, int index, System.Action<int> onBuy)
    {
        if (slotRoot == null) return;

        if (item == null)
        {
            if (nameText != null) nameText.text = "SOLD OUT";
            if (descriptionText != null) descriptionText.text = "";
            if (costText != null) costText.text = "";
            if (buyButton != null) buyButton.interactable = false;
            ApplyColor(soldOutColor);
            return;
        }

        if (nameText != null)
        {
            string categoryTag = GetCategoryTag(item.category);
            nameText.text = $"{categoryTag} {item.itemName}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = item.description;
        }

        if (costText != null)
        {
            costText.text = $"{item.cost}G";
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            int capturedIndex = index;
            buyButton.onClick.AddListener(() => onBuy?.Invoke(capturedIndex));
            buyButton.interactable = canBuy;
        }

        ApplyColor(canBuy ? affordableColor : tooExpensiveColor);
    }

    public void PlayPurchasedFeedback()
    {
        if (slotRoot == null) return;

        RectTransform rect = slotRoot.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.25f, 6, 0.8f);
        }

        if (nameText != null)
        {
            nameText.text = "SOLD OUT";
        }
        if (descriptionText != null)
        {
            descriptionText.text = "購入済み";
        }
        if (costText != null)
        {
            costText.text = "";
        }
        if (buyButton != null)
        {
            buyButton.interactable = false;
        }

        ApplyColor(soldOutColor);
    }

    private void ApplyColor(Color color)
    {
        color.a = 1f;

        if (nameText != null) nameText.color = color;
        if (descriptionText != null) descriptionText.color = color;
        if (costText != null) costText.color = color;
    }

    private string GetCategoryTag(ShopItemCategory category)
    {
        switch (category)
        {
            case ShopItemCategory.Weapon: return "[武器]";
            case ShopItemCategory.Gun: return "[銃]";
            case ShopItemCategory.Consumable: return "[消耗品]";
            case ShopItemCategory.HealHP: return "[回復]";
            default: return "";
        }
    }
}
