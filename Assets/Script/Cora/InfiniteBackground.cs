using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("Loop Width")]
    [SerializeField] private float backgroundWidth = 25.728f;

    private Transform cameraTransform;
    private Vector3 initialLocalPosition;
    private bool initialized;

    public float BackgroundWidth => backgroundWidth;

    private void Awake()
    {
        CacheInitialState();
    }

    private void OnEnable()
    {
        CacheInitialState();
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void CacheInitialState()
    {
        initialLocalPosition = transform.localPosition;
        initialized = true;
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                return;
            }
        }

        if (cameraTransform.position.x - transform.position.x >= backgroundWidth)
        {
            transform.localPosition += new Vector3(backgroundWidth * 2f, 0f, 0f);
        }
    }

    public void ResetToInitialPosition()
    {
        if (!initialized)
        {
            CacheInitialState();
        }

        transform.localPosition = initialLocalPosition;
    }
}
