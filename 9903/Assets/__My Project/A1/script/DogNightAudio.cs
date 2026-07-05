using UnityEngine;

public class DogNightAudio : MonoBehaviour
{
    [Header("Dog Night Audio")]
    [Tooltip("拖入 Audio Source。建议挂在 DOG 根物体上。")]
    [SerializeField] private AudioSource dogAudioSource;

    [Tooltip("拖入狗狗夜晚的环境音、脚步声、喘气声或狗叫声。")]
    [SerializeField] private AudioClip dogNightClip;

    [Tooltip("开始狗狗视角后多久播放声音。")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("夜晚过程是否循环播放。")]
    [SerializeField] private bool loopAudio = true;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private float playTime;
    private bool waitingToPlay;

    private void Awake()
    {
        if (dogAudioSource != null)
        {
            dogAudioSource.playOnAwake = false;
            dogAudioSource.loop = loopAudio;
            dogAudioSource.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        if (!waitingToPlay)
        {
            return;
        }

        playTime += Time.deltaTime;

        if (playTime >= startDelay)
        {
            waitingToPlay = false;
            PlayDogAudioNow();
        }
    }

    public void StartDogAudio()
    {
        if (dogAudioSource == null || dogNightClip == null)
        {
            Debug.LogWarning(
                "DogNightAudio: Audio Source 或 Dog Night Clip 没有设置。"
            );
            return;
        }

        StopDogAudio();

        playTime = 0f;
        waitingToPlay = true;
    }

    public void StopDogAudio()
    {
        waitingToPlay = false;
        playTime = 0f;

        if (dogAudioSource != null)
        {
            dogAudioSource.Stop();
        }
    }

    private void PlayDogAudioNow()
    {
        dogAudioSource.clip = dogNightClip;
        dogAudioSource.volume = volume;
        dogAudioSource.loop = loopAudio;
        dogAudioSource.spatialBlend = 0f;
        dogAudioSource.Play();
    }
}