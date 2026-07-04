using UnityEngine;

public class LookGlow : MonoBehaviour
{
    [Header("Hover Scale")]
    [Tooltip("真正需要放大的模型。留空则放大当前物体。")]
    [SerializeField] private Transform targetToScale;

    [Tooltip("1.05 = 放大 5%。")]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;

    [Tooltip("放大与恢复速度。")]
    [SerializeField] private float scaleSpeed = 8f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        if (targetToScale == null)
        {
            targetToScale = transform;
        }

        originalScale = targetToScale.localScale;
        targetScale = originalScale;
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

    // 给 EZPZ 的 On Hover Enter 使用
    public void ShowGlow()
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    // 给 EZPZ 的 On Hover Exit 使用
    public void HideGlow()
    {
        targetScale = originalScale;
    }
}