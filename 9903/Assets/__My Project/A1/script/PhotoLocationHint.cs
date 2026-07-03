using TMPro;
using UnityEngine;

public class PhotoLocationHint : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerTransform;

    [Header("UI")]
    [Tooltip("拖入主 Canvas。不要新建第二个 Canvas。")]
    [SerializeField] private Canvas mainCanvas;

    [Tooltip("拖入 Canvas 下的 TextMeshProUGUI 文字。")]
    [SerializeField] private TextMeshProUGUI locationText;

    [Tooltip("在相框右上角建立一个空物体，再拖进来。")]
    [SerializeField] private Transform labelAnchor;

    [TextArea(2, 4)]
    [SerializeField]
    private string photoLocation =
        "This photo was taken at Centennial Park.";

    [Header("Display Settings")]
    [SerializeField] private float showDistance = 2f;

    [Tooltip("文字相对相框的屏幕偏移，X 是左右，Y 是上下。")]
    [SerializeField]
    private Vector2 screenOffset =
        new Vector2(80f, 35f);

    private Camera playerCamera;
    private RectTransform canvasRect;
    private RectTransform textRect;

    private void Start()
    {
        if (mainCanvas != null)
        {
            canvasRect = mainCanvas.GetComponent<RectTransform>();
        }

        if (locationText != null)
        {
            textRect = locationText.rectTransform;
            locationText.gameObject.SetActive(false);
        }

        if (Camera.main != null)
        {
            playerCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null ||
            mainCanvas == null ||
            locationText == null ||
            labelAnchor == null)
        {
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                return;
            }
        }

        float distance = Vector3.Distance(
            playerTransform.position,
            labelAnchor.position
        );

        if (distance > showDistance)
        {
            locationText.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPoint = playerCamera.WorldToScreenPoint(
            labelAnchor.position
        );

        // 相框在镜头后面时，不显示文字
        if (screenPoint.z <= 0f)
        {
            locationText.gameObject.SetActive(false);
            return;
        }

        locationText.gameObject.SetActive(true);
        locationText.text = photoLocation;

        Vector2 localPoint;
        Camera uiCamera = mainCanvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
            ? null
            : mainCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            uiCamera,
            out localPoint
        );

        textRect.anchoredPosition = localPoint + screenOffset;
    }
}