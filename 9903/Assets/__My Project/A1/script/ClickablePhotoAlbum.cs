using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private string finishTriggerName = "Finish";
    [SerializeField] private float openAnimationDelay = 0.3f;
    [SerializeField] private float finishAnimationDuration = 2.5f;
    [SerializeField] private float postCloseHoldTime = 1.5f;

    [Header("Album UI")]
    [SerializeField] private GameObject albumPanel;
    [SerializeField] private Image pageImage;
    [SerializeField] private Sprite[] photoPages;
    [SerializeField] private GameObject pageClickHint;

    [Header("Player Controls")]
    [SerializeField] private MonoBehaviour[] controlsToDisable;

    private bool albumIsOpen;
    private bool isOpening;
    private bool isClosing;
    private int currentPageIndex;

    private Vector3 highlightBaseScale;
    private bool[] controlsWereEnabled;

    private void Awake()
    {
        if (albumPanel != null)
        {
            albumPanel.SetActive(false);
        }

        if (pageClickHint != null)
        {
            pageClickHint.SetActive(false);
        }
    }

    private void Start()
    {
        if (albumHighlight != null)
        {
            highlightBaseScale =
                albumHighlight.transform.localScale;

            SetHighlight(true);
        }
    }

    private void Update()
    {
        if (isOpening || isClosing)
        {
            return;
        }

        if (!albumIsOpen)
        {
            PulseHighlight();

            if (LeftClickThisFrame())
            {
                TryOpenAlbum();
            }

            return;
        }

        if (LeftClickThisFrame())
        {
            HandleAlbumClick();
        }
    }

    private bool LeftClickThisFrame()
    {
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }

    private void TryOpenAlbum()
    {
        if (playerCamera == null ||
            albumClickCollider == null)
        {
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
            hit.collider.transform == transform ||
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
        if (photoPages == null ||
            photoPages.Length == 0)
        {
            return;
        }

        if (currentPageIndex < photoPages.Length - 1)
        {
            currentPageIndex++;
            ShowCurrentPage();
            return;
        }

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
    }

    private IEnumerator FinishAlbumRoutine()
    {
        isClosing = true;

        if (pageClickHint != null)
        {
            pageClickHint.SetActive(false);
        }

        if (albumPanel != null)
        {
            albumPanel.SetActive(false);
        }

        if (albumAnimator != null &&
            !string.IsNullOrEmpty(finishTriggerName))
        {
            albumAnimator.SetTrigger(finishTriggerName);
        }

        yield return new WaitForSeconds(
            finishAnimationDuration
        );

        yield return new WaitForSeconds(
            postCloseHoldTime
        );

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

        controlsWereEnabled =
            new bool[controlsToDisable.Length];

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