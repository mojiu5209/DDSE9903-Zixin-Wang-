using UnityEngine;

public class TVImagePlayer : MonoBehaviour
{
    [Header("TV Screen")]
    [Tooltip("拖入电视屏幕的 Renderer。通常是屏幕黑色平面所在物体。")]
    [SerializeField] private Renderer screenRenderer;

    [Tooltip("电视屏幕材质在 Renderer 的第几个位置。通常填 0。")]
    [SerializeField] private int materialIndex = 0;

    [Header("Images")]
    [Tooltip("按播放顺序拖入图片。")]
    [SerializeField] private Texture2D[] images;

    [Tooltip("每张图片显示多久。")]
    [SerializeField] private float imageDuration = 1.5f;

    [Tooltip("播放到最后一张后，是否重新从第一张开始。")]
    [SerializeField] private bool loop = true;

    [Tooltip("勾选后，每次随机播放下一张。")]
    [SerializeField] private bool randomOrder = false;

    [Header("Material Texture Property")]
    [Tooltip("URP Lit / Unlit 通常使用 _BaseMap。旧 Standard Shader 使用 _MainTex。")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    private MaterialPropertyBlock propertyBlock;
    private int currentImageIndex;
    private float timer;
    private int texturePropertyID;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        texturePropertyID = Shader.PropertyToID(texturePropertyName);
    }

    private void Start()
    {
        if (screenRenderer == null)
        {
            screenRenderer = GetComponent<Renderer>();
        }

        if (screenRenderer == null || images == null || images.Length == 0)
        {
            Debug.LogWarning(
                "TVImagePlayer: Assign a Screen Renderer and at least one image."
            );
            enabled = false;
            return;
        }

        currentImageIndex = 0;
        ShowImage(currentImageIndex);
    }

    private void Update()
    {
        if (images == null || images.Length <= 1)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= imageDuration)
        {
            timer = 0f;
            PlayNextImage();
        }
    }

    private void PlayNextImage()
    {
        if (randomOrder)
        {
            int nextIndex = currentImageIndex;

            while (nextIndex == currentImageIndex && images.Length > 1)
            {
                nextIndex = Random.Range(0, images.Length);
            }

            currentImageIndex = nextIndex;
        }
        else
        {
            currentImageIndex++;

            if (currentImageIndex >= images.Length)
            {
                if (loop)
                {
                    currentImageIndex = 0;
                }
                else
                {
                    currentImageIndex = images.Length - 1;
                    enabled = false;
                }
            }
        }

        ShowImage(currentImageIndex);
    }

    private void ShowImage(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= images.Length)
        {
            return;
        }

        screenRenderer.GetPropertyBlock(
            propertyBlock,
            materialIndex
        );

        propertyBlock.SetTexture(
            texturePropertyID,
            images[imageIndex]
        );

        screenRenderer.SetPropertyBlock(
            propertyBlock,
            materialIndex
        );
    }

    public void RestartTV()
    {
        currentImageIndex = 0;
        timer = 0f;
        enabled = true;

        ShowImage(currentImageIndex);
    }
}