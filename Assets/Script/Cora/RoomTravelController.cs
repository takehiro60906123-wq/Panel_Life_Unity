using System.Collections;
using UnityEngine;
using DG.Tweening;

public class RoomTravelController : MonoBehaviour
{
    [Header("à⁄ìÆââèoê›íË")]
    public float roomTravelDuration = 1.0f;
    public Ease roomTravelEase = Ease.Linear;

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

        playerTransform
            .DOMove(playerTransform.position + moveOffset, roomTravelDuration)
            .SetEase(roomTravelEase);

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform
                .DOMove(mainCam.transform.position + moveOffset, roomTravelDuration)
                .SetEase(roomTravelEase);
        }

        yield return new WaitForSeconds(roomTravelDuration);
    }
}