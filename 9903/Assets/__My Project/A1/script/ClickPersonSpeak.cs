using System.Collections;
using UnityEngine;

public class ClickPersonSpeak : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private Animator characterAnimator;

    [Header("Voice")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceClip;

    [Tooltip("点击后等待多久再说话。")]
    [SerializeField] private float voiceDelay = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Settings")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed;
    private bool isPlaying;
    private float originalAnimatorSpeed = 1f;

    public void StopAndSpeak()
    {
        if (isPlaying)
        {
            return;
        }

        if (playOnlyOnce && hasPlayed)
        {
            return;
        }

        StartCoroutine(StopAndSpeakRoutine());
    }

    private IEnumerator StopAndSpeakRoutine()
    {
        isPlaying = true;
        hasPlayed = true;

        Debug.Log("Person clicked: stopping animation.");

        if (characterAnimator != null)
        {
            originalAnimatorSpeed = characterAnimator.speed;

            // 先暂停当前动画
            characterAnimator.speed = 0f;

            // 再关闭 Animator，避免动画继续改 Position / Rotation
            characterAnimator.enabled = false;
        }

        yield return new WaitForSeconds(voiceDelay);

        if (audioSource != null && voiceClip != null)
        {
            audioSource.Stop();
            audioSource.clip = voiceClip;
            audioSource.volume = volume;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning(
                "ClickPersonSpeak: Voice Clip 还没有拖进去。"
            );
        }

        isPlaying = false;
    }

    public void ResumeAnimation()
    {
        if (characterAnimator == null)
        {
            return;
        }

        characterAnimator.enabled = true;
        characterAnimator.speed = originalAnimatorSpeed;
    }

    public void ResetPersonSpeak()
    {
        hasPlayed = false;
        isPlaying = false;
    }
}