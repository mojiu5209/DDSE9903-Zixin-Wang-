using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClickablePhotoAlbum : MonoBehaviour
{
    [Header("Click Detection")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Collider albumClickCollider;
    [SerializeField] private float maxClickDistance = 3f;
    [SerializeField] private LayerMask clickLayers = ~0;

    [Header("World Highlight")]
    [SerializeField] private GameObject albumHighlight;
    [SerializeField] private float highlightPulseSpeed = 2f;
    [SerializeField] private float highlightPulseAmount = 0.03f;

    [Header("Physical Album Animation")]
    [SerializeField] private Animator albumAnimator;
    [SerializeField] private string openTriggerName = "Open";

    [Tooltip("第 5 张照片之后再次点击时触发。")]
    [SerializeField] private string finishTriggerName = "Finish";

    [SerializeField] private float openAnimationDelay = 0.3f;

    [Tooltip("相册翻到背面并合上后，等待多久恢复控制。")]
    [SerializeField] private float finishAnimationDuration = 2.5f;

    [Header("Album UI")]
    [SerializeField] private GameObject albumPanel;
    [SerializeField] private Image pageImage;
    [SerializeField] private Sprite[] photoPages;
    [SerializeField] private GameObject pageClickHint;

    [Header("Player Controls")]
    [Tooltip("拖入 EZPZ 上控制移动和鼠标视角的脚本。")]
    [SerializeField] private MonoBehaviour[] controlsToDisable;

    private bool albumIsOpen;
    private bool isOpening;
    private bool isClosing;

    private int currentPageIndex;

    private Vector3 highlightBaseScale;
    private bool[] controlsWereEnabled;

    private void Start()
    {
        if (albumPanel != null)
        {
            albumPanel.SetActive(false);
        }

        if (pageClickHint != null)
        {
            pageClickHint.SetActive(false);
        }

        if (albumHighlight != null)
        {
            highlightBaseScale = albumHighlight.transform.localScale;
            SetHighlight(true);
        }

        if (photoPages == null || photoPages.Length != 5)
        {
            Debug.LogWarning(
                "Album should contain exactly 5 photo pages."
            );
        }
    }

    private void Update()
    {
        if (isOpening || isClosing)
        {
            return;
        }

        // 相册未打开：准星对准相册并左键点击
        if (!albumIsOpen)
        {
            PulseHighlight();

            if (Input.GetMouseButtonDown(0))
            {
                TryOpenAlbum();
            }

            return;
        }

        // 相册打开后：每次点击翻一页
        if (Input.GetMouseButtonDown(0))
        {
            HandleAlbumClick();
        }
    }

    private void TryOpenAlbum()
    {
        if (playerCamera == null || albumClickCollider == null)
        {
            Debug.LogWarning(
                "Please assign Player Camera and Album Click Collider."
            );
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        bool didHit = Physics.Raycast(
            ray,
            out hit,
            maxClickDistance,
            clickLayers,
            QueryTriggerInteraction.Collide
        );

        if (!didHit)
        {
            return;
        }

        bool clickedAlbum =
            hit.collider == albumClickCollider ||
            hit.collider.transform.IsChildOf(transform);

        if (clickedAlbum)
        {
            StartCoroutine(OpenAlbumRoutine());
        }
    }

    private IEnumerator OpenAlbumRoutine()
    {
        isOpening = true;
        albumIsOpen = true;
        currentPageIndex = 0;

        SetHighlight(false);
        DisablePlayerControls();

        if (albumAnimator != null &&
            !string.IsNullOrEmpty(openTriggerName))
        {
            albumAnimator.SetTrigger(openTriggerName);
        }

        yield return new WaitForSeconds(openAnimationDelay);

        if (albumPanel != null)
        {
            albumPanel.SetActive(true);
        }

        if (pageClickHint != null)
        {
            pageClickHint.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowCurrentPage();

        isOpening = false;
    }

    private void HandleAlbumClick()
    {
        if (photoPages == null || photoPages.Length == 0)
        {
            return;
        }

        // 前四张：点击进入下一张
        if (currentPageIndex < photoPages.Length - 1)
        {
            currentPageIndex++;
            ShowCurrentPage();
            return;
        }

        // 第五张已显示：再次点击才执行翻到背面并合上
        StartCoroutine(FinishAlbumRoutine());
    }

    private void ShowCurrentPage()
    {
        if (pageImage == null ||
            currentPageIndex < 0 ||
            currentPageIndex >= photoPages.Length)
        {
            return;
        }

        pageImage.sprite = photoPages[currentPageIndex];

        // 到最后一页时，提示文字改为关闭提示
        if (pageClickHint != null)
        {
            pageClickHint.SetActive(true);
        }
    }

    private IEnumerator FinishAlbumRoutine()
    {
        isClosing = true;

        if (pageClickHint != null)
        {
            pageClickHint.SetActive(false);
        }

        // 先隐藏 UI，才能看到现实中的相册翻回背面、合上
        if (albumPanel != null)
        {
            albumPanel.SetActive(false);
        }

        if (albumAnimator != null &&
            !string.IsNullOrEmpty(finishTriggerName))
        {
            albumAnimator.SetTrigger(finishTriggerName);
        }

        // 等待 3D 相册翻页 + 合上的动画完成
        yield return new WaitForSeconds(finishAnimationDuration);

        currentPageIndex = 0;
        albumIsOpen = false;
        isClosing = false;

        RestorePlayerControls();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetHighlight(true);
    }

    private void DisablePlayerControls()
    {
        if (controlsToDisable == null)
        {
            return;
        }

        controlsWereEnabled = new bool[
            controlsToDisable.Length
        ];

        for (int i = 0; i < controlsToDisable.Length; i++)
        {
            if (controlsToDisable[i] == null)
            {
                continue;
            }

            controlsWereEnabled[i] =
                controlsToDisable[i].enabled;

            controlsToDisable[i].enabled = false;
        }
    }

    private void RestorePlayerControls()
    {
        if (controlsToDisable == null ||
            controlsWereEnabled == null)
        {
            return;
        }

        for (int i = 0; i < controlsToDisable.Length; i++)
        {
            if (controlsToDisable[i] == null)
            {
                continue;
            }

            controlsToDisable[i].enabled =
                controlsWereEnabled[i];
        }
    }

    private void SetHighlight(bool visible)
    {
        if (albumHighlight == null)
        {
            return;
        }

        albumHighlight.SetActive(visible);

        if (visible)
        {
            albumHighlight.transform.localScale =
                highlightBaseScale;
        }
    }

    private void PulseHighlight()
    {
        if (albumHighlight == null ||
            !albumHighlight.activeSelf)
        {
            return;
        }

        float pulse =
            1f +
            Mathf.Sin(Time.time * highlightPulseSpeed) *
            highlightPulseAmount;

        albumHighlight.transform.localScale =
            highlightBaseScale * pulse;
    }
}