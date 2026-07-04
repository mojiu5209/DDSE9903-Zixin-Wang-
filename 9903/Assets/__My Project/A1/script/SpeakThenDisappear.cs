using System.Collections;
using UnityEngine;

public class SpeakThenDisappear : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip speechAudio;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;

    [Tooltip("Animator Parameters 里的 Trigger 名字。")]
    [SerializeField] private string disappearTrigger = "Disappear";

    [SerializeField] private float disappearAnimationDuration = 2f;

    [Header("Disappear Target")]
    [SerializeField] private GameObject characterToHide;

    [SerializeField] private bool destroyAfterDisappear = false;

    [Header("Timing")]
    [Tooltip("开场后等待多久开始说话。")]
    [SerializeField] private float speechDelay = 2f;

    private void Start()
    {
        StartCoroutine(SpeakingRoutine());
    }

    private IEnumerator SpeakingRoutine()
    {
        yield return new WaitForSeconds(speechDelay);

        if (audioSource != null && speechAudio != null)
        {
            audioSource.Stop();
            audioSource.clip = speechAudio;
            audioSource.volume = volume;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }
        else
        {
            Debug.LogWarning(
                "Speech Audio 或 Audio Source 没有设置。"
            );
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(disappearTrigger);
        }
        else
        {
            Debug.LogWarning(
                "Character Animator 没有设置。"
            );
        }

        yield return new WaitForSeconds(
            disappearAnimationDuration
        );

        GameObject target = characterToHide != null
            ? characterToHide
            : gameObject;

        if (destroyAfterDisappear)
        {
            Destroy(target);
        }
        else
        {
            target.SetActive(false);
        }
    }
}