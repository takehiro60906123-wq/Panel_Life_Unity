using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// ============================================
// 商店UIコントローラー
//
// Inspector で以下を設定:
//   - shopPanel: 商店全体のルートパネル（Canvas 内の Panel）
//   - shopSlots[3]: 各商品スロットの設定
//   - leaveButton: 「立ち去る」ボタン
//   - playerCoinsText: 所持コイン表示
//
// 【Unity 側で必要な構成】
//   Canvas
//   └── ShopPanel (非表示で配置)
//       ├── Title ("商店" テキスト)
//       ├── CoinsText ("所持: 120G")
//       ├── Slot0
//       │   ├── NameText
//       │   ├── DescText
//       │   ├── CostText
//       │   └── BuyButton
//       ├── Slot1 (同上)
//       ├── Slot2 (同上)
//       └── LeaveButton ("立ち去る")
// ============================================
public class ShopUIController : MonoBehaviour
{
    [Header("商店パネル")]
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

    private void Start()
    {
        // 起動時は非表示
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(OnClickLeave);
        }
    }

    // ============================================
    // 商店を表示する
    // ============================================
    public void ShowShop(List<ShopItemData> offerings, ShopController controller)
    {
        shopController = controller;

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        // フェードイン
        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = 0f;
            shopCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        }

        RefreshShop(offerings, controller);
    }

    // ============================================
    // 商店を非表示にする
    // ============================================
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

    // ============================================
    // 表示を更新する（購入後に呼ばれる）
    // ============================================
    public void RefreshShop(List<ShopItemData> offerings, ShopController controller)
    {
        shopController = controller;

        // 所持コイン
        if (playerCoinsText != null && controller != null)
        {
            playerCoinsText.text = $"所持: {controller.GetCurrentCoins()}G";
        }

        // 各スロットを更新
        if (shopSlots == null) return;

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i] == null) continue;

            ShopItemData item = (offerings != null && i < offerings.Count) ? offerings[i] : null;
            bool canBuy = controller != null && controller.CanPurchase(i);

            shopSlots[i].SetItem(item, canBuy, i, OnClickBuy);
        }
    }

    // ============================================
    // 「購入」ボタン押下
    // ============================================
    private void OnClickBuy(int slotIndex)
    {
        if (shopController == null) return;

        bool success = shopController.TryPurchase(slotIndex);
        if (!success) return;

        // 購入成功のフィードバック
        if (shopSlots != null && slotIndex >= 0 && slotIndex < shopSlots.Length)
        {
            shopSlots[slotIndex].PlayPurchasedFeedback();
        }
    }

    // ============================================
    // 「立ち去る」ボタン押下
    // ============================================
    private void OnClickLeave()
    {
        if (shopController != null)
        {
            shopController.CloseShop();
        }
    }

    public bool IsShopOpen => shopPanel != null && shopPanel.activeSelf;
}

// ============================================
// 商品スロット1つ分のUI参照
// ============================================
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

        // SOLD OUT or 空スロット
        if (item == null)
        {
            if (nameText != null) nameText.text = "SOLD OUT";
            if (descriptionText != null) descriptionText.text = "";
            if (costText != null) costText.text = "";
            if (buyButton != null) buyButton.interactable = false;
            ApplyColor(soldOutColor);
            return;
        }

        // 商品名
        if (nameText != null)
        {
            string categoryTag = GetCategoryTag(item.category);
            nameText.text = $"{categoryTag} {item.itemName}";
        }

        // 説明
        if (descriptionText != null)
        {
            descriptionText.text = item.description;
        }

        // 価格
        if (costText != null)
        {
            costText.text = $"{item.cost}G";
        }

        // 購入ボタン
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