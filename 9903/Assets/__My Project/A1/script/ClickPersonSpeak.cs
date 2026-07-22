using System.Collections;
using UnityEngine;

public class ClickPersonSpeak : MonoBehaviour
{
    [Header("Character")]
    [SerializeField] private Animator characterAnimator;

    [Tooltip("人物正在移动时，锁定这个物体的位置。通常拖人物自己。")]
    [SerializeField] private Transform characterRoot;

    [Header("Voice")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceClip;

    [SerializeField] private float voiceDelay = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Header("Settings")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed;
    private bool isPlaying;
    private bool freezeCharacter;

    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    private void Awake()
    {
        if (characterRoot == null)
        {
            characterRoot = transform;
        }
    }

    private void LateUpdate()
    {
        if (!freezeCharacter || characterRoot == null)
        {
            return;
        }

        characterRoot.position = frozenPosition;
        characterRoot.rotation = frozenRotation;
    }

    public void StopAndSpeak()
    {
        Debug.Log("ClickPersonSpeak: StopAndSpeak called.");

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

        if (characterRoot != null)
        {
            frozenPosition = characterRoot.position;
            frozenRotation = characterRoot.rotation;
            freezeCharacter = true;
        }

        if (characterAnimator != null)
        {
            characterAnimator.speed = 0f;
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
                "ClickPersonSpeak: Voice Clip has not been assigned."
            );
        }

        isPlaying = false;
    }

    public void ResumeCharacter()
    {
        freezeCharacter = false;

        if (characterAnimator != null)
        {
            characterAnimator.enabled = true;
            characterAnimator.speed = 1f;
        }
    }

    public void ResetInteraction()
    {
        hasPlayed = false;
        isPlaying = false;
    }
}