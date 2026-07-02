using System.Collections;
        using UnityEngine;
        using UnityEngine.UI;

public class HospitalIntro : MonoBehaviour
    {
        [Header("Cameras")]
        [SerializeField] private Camera introCamera;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private GameObject playerObject;

        [Header("Camera Positions")]
        [SerializeField] private Transform wakeLying;
        [SerializeField] private Transform wakeSitting;
        [SerializeField] private Transform wakeStanding;

        [Header("UI")]
        [SerializeField] private Image blackFade;
        [SerializeField] private Image whiteHaze;

        [Header("Audio")]
        [SerializeField] private AudioSource hospitalAmbience;
        [SerializeField] private AudioSource heartMonitor;
        [SerializeField] private AudioSource tinnitus;

        [Header("Timing")]
        [SerializeField] private float blackScreenTime = 1.5f;
        [SerializeField] private float fadeTime = 3f;
        [SerializeField] private float sitUpTime = 2f;
        [SerializeField] private float standUpTime = 1.5f;

        private void Start()
        {
            StartCoroutine(PlayIntro());
        }

        private IEnumerator PlayIntro()
        {
            if (playerObject != null)
            {
                playerObject.SetActive(false);
            }

            if (introCamera != null)
            {
                introCamera.gameObject.SetActive(true);
                introCamera.transform.SetPositionAndRotation(
                    wakeLying.position,
                    wakeLying.rotation
                );
            }

            SetImageAlpha(blackFade, 1f);
            SetImageAlpha(whiteHaze, 0.6f);

            if (hospitalAmbience != null) hospitalAmbience.Play();
            if (heartMonitor != null) heartMonitor.Play();

            yield return new WaitForSeconds(blackScreenTime);

            yield return StartCoroutine(FadeImage(blackFade, 1f, 0f, fadeTime));

            if (tinnitus != null) tinnitus.Play();

            yield return StartCoroutine(
                MoveCamera(wakeLying, wakeSitting, sitUpTime, true)
            );

            yield return StartCoroutine(
                MoveCamera(wakeSitting, wakeStanding, standUpTime, false)
            );

            yield return StartCoroutine(FadeImage(whiteHaze, 0.6f, 0f, 2f));

            if (tinnitus != null) tinnitus.Stop();

            if (playerObject != null)
            {
                playerObject.SetActive(true);
            }

            if (playerCamera != null)
            {
                playerCamera.transform.SetPositionAndRotation(
                    wakeStanding.position,
                    wakeStanding.rotation
                );
            }

            if (introCamera != null)
            {
                introCamera.gameObject.SetActive(false);
            }
        }

        private IEnumerator MoveCamera(
            Transform startPoint,
            Transform endPoint,
            float duration,
            bool dizzy)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / duration);

                Vector3 position = Vector3.Lerp(
                    startPoint.position,
                    endPoint.position,
                    t
                );

                Quaternion rotation = Quaternion.Slerp(
                    startPoint.rotation,
                    endPoint.rotation,
                    t
                );

                if (dizzy)
                {
                    position += new Vector3(
                        Mathf.Sin(timer * 10f) * 0.015f,
                        Mathf.Cos(timer * 8f) * 0.01f,
                        0f
                    );
                }

                introCamera.transform.SetPositionAndRotation(position, rotation);

                yield return null;
            }
        }

        private IEnumerator FadeImage(
            Image image,
            float startAlpha,
            float endAlpha,
            float duration)
        {
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
                SetImageAlpha(image, alpha);
                yield return null;
            }

            SetImageAlpha(image, endAlpha);
        }

        private void SetImageAlpha(Image image, float alpha)
        {
            if (image == null) return;

            Color colour = image.color;
            colour.a = alpha;
            image.color = colour;
        }
    }