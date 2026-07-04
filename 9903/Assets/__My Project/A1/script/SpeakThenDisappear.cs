using System.Collections;
using UnityEngine;

public class SpeakThenDisappear : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("人物说话用的 Audio Source。")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("人物要说的话。")]
    [SerializeField] private AudioClip speechAudio;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Animation")]
    [Tooltip("人物模型上的 Animator。")]
    [SerializeField] private Animator characterAnimator;

    [Tooltip("音频结束后触发的 Animator Trigger 名称。")]
    [SerializeField] private string disappearTrigger = "Disappear";

    [Tooltip("消失动画实际长度。")]
    [SerializeField] private float disappearAnimationDuration = 2f;

    [Header("Disappear Target")]
    [Tooltip("最后要隐藏的人物根物体。留空则隐藏当前物体。")]
    [SerializeField] private GameObject characterToHide;

    [Tooltip("勾选后，人物消失时直接 Destroy；不勾则 SetActive(false)。")]
    [SerializeField] private bool destroyAfterDisappear = false;

    [Header("Start Settings")]
    [Tooltip("勾选后，场景开始就自动播放。")]
    [SerializeField] private bool playOnStart = false;

    private bool hasPlayed;

    private void Start()
    {
        if (playOnStart)
        {
            StartSpeakingSequence();
        }
    }

    // 可从 InteractableGeneral、CityTimeController、UnityEvent 调用。
    public void StartSpeakingSequence()
    {
        if (hasPlayed)
        {
            return;
        }

        StartCoroutine(SpeakingRoutine());
    }

    private IEnumerator SpeakingRoutine()
    {
        hasPlayed = true;

        if (audioSource != null && speechAudio != null)
        {
            audioSource.PlayOneShot(speechAudio, volume);

            yield return new WaitForSeconds(
                speechAudio.length
            );
        }

        if (characterAnimator != null &&
            !string.IsNullOrEmpty(disappearTrigger))
        {
            characterAnimator.SetTrigger(disappearTrigger);
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

    // 需要重新让人物出现时使用。
    public void ResetSequence()
    {
        hasPlayed = false;

        if (characterToHide != null)
        {
            characterToHide.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }
}