using System.Collections;
using UnityEngine;

public class WakeUpCameraEffect : MonoBehaviour
{
    [SerializeField] private float delay = 0.6f;
    [SerializeField] private float duration = 2.2f;
    [SerializeField] private float rotationAmount = 1.2f;
    [SerializeField] private float bobAmount = 0.02f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void Start()
    {
        if (GameStartController.Instance != null && GameStartController.IntroPlayed)
            return;

        StartCoroutine(GameStartController.RunAfterStart(WakeUpMovement()));
    }

    private IEnumerator WakeUpMovement()
    {
        yield return new WaitForSecondsRealtime(delay);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / duration);
            float strength = 1f - Mathf.SmoothStep(0f, 1f, progress);

            float roll = Mathf.Sin(timer * 4f)
                         * rotationAmount
                         * strength;

            float verticalMovement = Mathf.Sin(timer * 3f)
                                     * bobAmount
                                     * strength;

            transform.localRotation =
                startRotation * Quaternion.Euler(0f, 0f, roll);

            transform.localPosition =
                startPosition + Vector3.up * verticalMovement;

            yield return null;
        }

        transform.localPosition = startPosition;
        transform.localRotation = startRotation;
    }
}