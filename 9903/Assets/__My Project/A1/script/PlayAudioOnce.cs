using UnityEngine;

public class PlayAudioOnce : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private bool hasPlayed;

    public void PlayOnce()
    {
        if (hasPlayed)
        {
            return;
        }

        if (audioSource == null || audioClip == null)
        {
            Debug.LogWarning(
                "PlayAudioOnce: Audio Source 或 Audio Clip 没有设置。"
            );
            return;
        }

        hasPlayed = true;

        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
    }

    // 测试或需要重播时使用
    public void ResetAudio()
    {
        hasPlayed = false;
    }
}