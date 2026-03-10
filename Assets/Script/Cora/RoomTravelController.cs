using System.Collections;
using UnityEngine;
using DG.Tweening;

public class RoomTravelController : MonoBehaviour
{
    [Header("ړoݒ")]
    public float roomTravelDuration = 1.1f;
    public Ease roomTravelEase = Ease.OutCubic;
    [Range(0.6f, 0.98f)] public float leadRatio = 0.84f;
    public float dashZoomInSizeDelta = 0.18f;

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

        playerTransform.DOKill(false);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.DOKill(false);
        }

        Vector3 playerTarget = playerTransform.position + moveOffset;

        // 1本の Tween で滑らかに減速させる（2段階分割による速度不連続を解消）
        Tween playerTween = playerTransform
            .DOMove(playerTarget, roomTravelDuration)
            .SetEase(roomTravelEase);

        if (mainCam != null)
        {
            Vector3 camTarget = mainCam.transform.position + moveOffset;
            mainCam.transform
                .DOMove(camTarget, roomTravelDuration)
                .SetEase(roomTravelEase);
        }

        yield return playerTween.WaitForCompletion();
    }
}