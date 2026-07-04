using System.Collections;
using UnityEngine;

public class TVClickImagePlayer : MonoBehaviour
{
    [Header("TV Screen")]
    [SerializeField] private Renderer screenRenderer;

    [Tooltip("电视屏幕材质在 Renderer 的第几个位置。通常是 0。")]
    [SerializeField] private int materialIndex = 0;

    [Header("Images")]
    [Tooltip("按顺序拖入 5 张电视画面。")]
    [SerializeField] private Texture2D[] images;

    [Tooltip("每张图片显示多久。")]
    [SerializeField] private float imageDuration = 1.5f;

    [Header("Material Properties")]
    [Tooltip("URP Lit / Unlit 通常是 _BaseMap；旧 Standard Shader 是 _MainTex。")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    [Tooltip("URP Lit / Unlit 通常是 _BaseColor；旧 Standard Shader 是 _Color。")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Optional")]
    [Tooltip("打开电视时播放的声音。可留空。")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip tvTurnOnSound;

    [Tooltip("关闭电视时播放的声音。可留空。")]
    [SerializeField] private AudioClip tvTurnOffSound;

    private MaterialPropertyBlock propertyBlock;
    private int texturePropertyID;
    private int colorPropertyID;

    private int currentImageIndex;
    private bool isPlaying;
    private bool isShowingFinalImage;
    private Coroutine playRoutine;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        texturePropertyID = Shader.PropertyToID(
            texturePropertyName
        );

        colorPropertyID = Shader.PropertyToID(
            colorPropertyName
        );
    }

    private void Start()
    {
        if (screenRenderer == null)
        {
            screenRenderer = GetComponent<Renderer>();
        }

        if (screenRenderer == null)
        {
            Debug.LogWarning(
                "TVClickImagePlayer: Screen Renderer is missing."
            );

            enabled = false;
            return;
        }

        SetScreenBlack();
    }

    // 把这个方法接到 InteractableGeneral 的 On Primary Interact。
    public void HandleTVClick()
    {
        // 正在播放时，不接受新的点击。
        if (isPlaying)
        {
            return;
        }

        // 第五张停住时，点击后关闭电视。
        if (isShowingFinalImage)
        {
            TurnOffTV();
            return;
        }

        // 黑屏状态下，点击后开始播放。
        StartTVSequence();
    }

    public void StartTVSequence()
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning(
                "TVClickImagePlayer: Add at least one image."
            );

            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        currentImageIndex = 0;
        isPlaying = true;
        isShowingFinalImage = false;

        PlaySound(tvTurnOnSound);

        playRoutine = StartCoroutine(
            PlayImageSequence()
        );
    }

    public void TurnOffTV()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        isPlaying = false;
        isShowingFinalImage = false;
        currentImageIndex = 0;

        SetScreenBlack();

        PlaySound(tvTurnOffSound);
    }

    private IEnumerator PlayImageSequence()
    {
        for (int i = 0; i < images.Length; i++)
        {
            currentImageIndex = i;

            ShowImage(images[i]);

            // 最后一张不等待、不自动关闭。
            if (i == images.Length - 1)
            {
                break;
            }

            yield return new WaitForSeconds(imageDuration);
        }

        isPlaying = false;
        isShowingFinalImage = true;
        playRoutine = null;
    }

    private void ShowImage(Texture2D image)
    {
        if (image == null)
        {
            return;
        }

        screenRenderer.GetPropertyBlock(
            propertyBlock,
            materialIndex
        );

        propertyBlock.SetTexture(
            texturePropertyID,
            image
        );

        propertyBlock.SetColor(
            colorPropertyID,
            Color.white
        );

        screenRenderer.SetPropertyBlock(
            propertyBlock,
            materialIndex
        );
    }

    private void SetScreenBlack()
    {
        screenRenderer.GetPropertyBlock(
            propertyBlock,
            materialIndex
        );

        propertyBlock.SetColor(
            colorPropertyID,
            Color.black
        );

        screenRenderer.SetPropertyBlock(
            propertyBlock,
            materialIndex
        );
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}