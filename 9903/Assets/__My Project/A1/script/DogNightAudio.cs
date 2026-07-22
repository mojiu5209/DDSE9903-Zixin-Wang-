using System.Collections;
using UnityEngine;

public class DogNightAudio : MonoBehaviour
{
    [Header("Dog Night Audio")]
    [SerializeField] private AudioSource dogAudioSource;
    [SerializeField] private AudioClip dogNightClip;

    [Tooltip("进入狗狗视角后等待多久再播放。")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("是否循环播放。")]
    [SerializeField] private bool loopAudio = false;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (dogAudioSource != null)
        {
            dogAudioSource.playOnAwake = false;
            dogAudioSource.loop = false;
            dogAudioSource.spatialBlend = 0f;
        }
    }

    public void StartDogAudio()
    {
        Debug.Log("DogNightAudio: StartDogAudio called.");

        if (dogAudioSource == null)
        {
            Debug.LogWarning(
                "DogNightAudio: Dog Audio Source has not been assigned."
            );
            return;
        }

        if (dogNightClip == null)
        {
            Debug.LogWarning(
                "DogNightAudio: Dog Night Clip has not been assigned."
            );
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        dogAudioSource.Stop();

        yield return new WaitForSeconds(startDelay);

        dogAudioSource.clip = dogNightClip;
        dogAudioSource.volume = volume;
        dogAudioSource.loop = loopAudio;
        dogAudioSource.spatialBlend = 0f;
        dogAudioSource.Play();

        Debug.Log("DogNightAudio: Voice is playing.");
    }

    public void StopDogAudio()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (dogAudioSource != null)
        {
            dogAudioSource.Stop();
        }

        Debug.Log("DogNightAudio: Voice stopped.");
    }
}