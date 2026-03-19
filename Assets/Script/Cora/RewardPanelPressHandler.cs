using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class RewardPanelPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float longPressSeconds = 0.45f;

    private int row;
    private int col;
    private Action<int, int> onLongPressStart;
    private Action<int, int> onLongPressEnd;
    private Coroutine longPressRoutine;
    private bool longPressTriggered;
    private bool detailVisible;

    public void Configure(int row, int col, Action<int, int> onLongPressStart, Action<int, int> onLongPressEnd)
    {
        this.row = row;
        this.col = col;
        this.onLongPressStart = onLongPressStart;
        this.onLongPressEnd = onLongPressEnd;
        enabled = onLongPressStart != null || onLongPressEnd != null;
    }

    public bool ConsumeSuppressClick()
    {
        bool result = longPressTriggered;
        longPressTriggered = false;
        return result;
    }

    public void Clear()
    {
        CancelLongPress(false);
        onLongPressStart = null;
        onLongPressEnd = null;
        longPressTriggered = false;
        detailVisible = false;
        enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (onLongPressStart == null && onLongPressEnd == null) return;

        longPressTriggered = false;
        detailVisible = false;

        if (longPressRoutine != null)
        {
            StopCoroutine(longPressRoutine);
        }

        longPressRoutine = StartCoroutine(LongPressRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CancelLongPress(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CancelLongPress(true);
    }

    private IEnumerator LongPressRoutine()
    {
        yield return new WaitForSeconds(longPressSeconds);
        longPressTriggered = true;
        detailVisible = true;
        onLongPressStart?.Invoke(row, col);
        longPressRoutine = null;
    }

    private void CancelLongPress(bool notifyEnd)
    {
        if (longPressRoutine != null)
        {
            StopCoroutine(longPressRoutine);
            longPressRoutine = null;
        }

        if (detailVisible)
        {
            if (notifyEnd)
            {
                onLongPressEnd?.Invoke(row, col);
            }

            detailVisible = false;
        }
    }
}
