using System.Collections;
using UnityEngine;
using DG.Tweening;

public class RoomTravelController : MonoBehaviour
{
    [Header("ˆÚ“®‰‰oÝ’è")]
    public float roomTravelDuration = 0.72f;
    public Ease roomTravelEase = Ease.OutExpo;
    public float dashOvershootDistance = 0.65f;
    public float dashZoomInSizeDelta = 0.35f;

    public void Configure(float travelDuration, Ease travelEase)
    {
        roomTravelDuration = travelDuration;
        roomTravelEase = travelEase;
    }

    public IEnumerator TravelForward(Transform playerTransform, Vector3 moveOffset)
    {
        if (playerTransform == null)
        {
            yield break;
        }

        Vector3 moveDir = moveOffset.sqrMagnitude > 0.0001f
            ? moveOffset.normalized
            : Vector3.right;

        Vector3 playerStart = playerTransform.position;
        Vector3 playerMainTarget = playerStart + moveOffset;
        Vector3 playerOvershootTarget = playerMainTarget + moveDir * dashOvershootDistance;

        float dashDuration = roomTravelDuration * 0.78f;
        float settleDuration = roomTravelDuration - dashDuration;

        Sequence playerSeq = DOTween.Sequence();
        playerSeq.Append(
            playerTransform.DOMove(playerOvershootTarget, dashDuration).SetEase(roomTravelEase)
        );
        playerSeq.Append(
            playerTransform.DOMove(playerMainTarget, settleDuration).SetEase(Ease.OutCubic)
        );

        Camera mainCam = Camera.main;
        Sequence camSeq = null;

        if (mainCam != null)
        {
            Vector3 camStart = mainCam.transform.position;
            Vector3 camMainTarget = camStart + moveOffset;
            Vector3 camOvershootTarget = camMainTarget + moveDir * dashOvershootDistance;

            camSeq = DOTween.Sequence();
            camSeq.Append(
                mainCam.transform.DOMove(camOvershootTarget, dashDuration).SetEase(roomTravelEase)
            );
            camSeq.Append(
                mainCam.transform.DOMove(camMainTarget, settleDuration).SetEase(Ease.OutCubic)
            );

            if (mainCam.orthographic)
            {
                float startSize = mainCam.orthographicSize;
                camSeq.Join(
                    DOTween.Sequence()
                        .Append(DOTween.To(
                            () => mainCam.orthographicSize,
                            x => mainCam.orthographicSize = x,
                            startSize - dashZoomInSizeDelta,
                            dashDuration * 0.65f))
                        .Append(DOTween.To(
                            () => mainCam.orthographicSize,
                            x => mainCam.orthographicSize = x,
                            startSize,
                            roomTravelDuration - (dashDuration * 0.65f)))
                );
            }
        }

        yield return playerSeq.WaitForCompletion();
    }
}