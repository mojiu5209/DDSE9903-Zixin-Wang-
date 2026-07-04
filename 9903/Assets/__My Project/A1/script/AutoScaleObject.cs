using UnityEngine;

public class AutoScaleObject : MonoBehaviour
{
    [Header("Scale")]
    [Tooltip("真正要放大的物体。留空则放大当前物体。")]
    [SerializeField] private Transform targetToScale;

    [Tooltip("1.1 = 放大 10%。")]
    [SerializeField] private float scaleMultiplier = 1.1f;

    [Tooltip("放大速度。")]
    [SerializeField] private float scaleSpeed = 5f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        if (targetToScale == null)
        {
            targetToScale = transform;
        }

        originalScale = targetToScale.localScale;
        targetScale = originalScale * scaleMultiplier;
    }

    private void LateUpdate()
    {
        if (targetToScale == null)
        {
            return;
        }

        targetToScale.localScale = Vector3.Lerp(
            targetToScale.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }
}