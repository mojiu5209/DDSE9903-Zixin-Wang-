using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 1.5f;

    private Vector3 startLocalPosition;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float offsetY =
            Mathf.Sin(Time.time * floatSpeed) *
            floatHeight;

        transform.localPosition =
            startLocalPosition +
            new Vector3(0f, offsetY, 0f);
    }
}