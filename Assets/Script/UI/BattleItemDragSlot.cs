using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleItemDragSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BattleUIController battleUIController;
    private int slotIndex;
    private CanvasGroup canvasGroup;
    private GraphicRaycaster graphicRaycaster;

    public void Setup(BattleUIController controller, int index)
    {
        battleUIController = controller;
        slotIndex = index;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (graphicRaycaster == null)
        {
            graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BattleItemData item = battleUIController != null ? battleUIController.GetInventoryItem(slotIndex) : null;
        if (item == null || item.useTarget != BattleItemUseTarget.Enemy)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.65f;
            canvasGroup.blocksRaycasts = false;
        }

        battleUIController?.BeginItemDragVisual(slotIndex, eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        battleUIController?.UpdateItemDragVisual(eventData.position, eventData.pressEventCamera);
        battleUIController?.UpdateItemDragHover(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        battleUIController?.ClearItemDragHover();
        battleUIController?.EndItemDragVisual();

        BattleItemData item = battleUIController != null ? battleUIController.GetInventoryItem(slotIndex) : null;
        if (item == null || item.useTarget != BattleItemUseTarget.Enemy)
        {
            return;
        }

        battleUIController.HandleItemDragEnd(slotIndex, eventData.position);
    }
}