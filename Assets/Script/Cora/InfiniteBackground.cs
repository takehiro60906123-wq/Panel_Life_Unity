using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("背景画像の横の長さ")]
    // ★計算済みのピッタリの数値を最初から入れておきます
    public float backgroundWidth = 25.728f;

    private Transform cameraTransform;

    void Start()
    {
        // メインカメラを取得
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // カメラがこの背景グループよりも「画像の横幅分」右に進んだら
        if (cameraTransform.position.x - transform.position.x >= backgroundWidth)
        {
            // この背景グループを、右側にワープさせて使い回す！
            // ※背景2枚でループさせるので、移動距離は backgroundWidth * 2 になります
            transform.position += new Vector3(backgroundWidth * 2, 0, 0);
        }
    }
}