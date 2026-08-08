using System.Collections;
using UnityEngine;

public class HospitalCameraWakeShake : MonoBehaviour
{
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float shakeDuration = 3.2f;

    [Header("Shake Settings")]
    [SerializeField] private float swayFrequency = 0.9f;
    [SerializeField] private float yawAmount = 2.2f;
    [SerializeField] private float rollAmount = 1.2f;
    [SerializeField] private float positionAmount = 0.015f;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        StartCoroutine(WakeUpShake());
    }

    private IEnumerator WakeUpShake()
    {
        yield return new WaitForSeconds(startDelay);

        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(timer / shakeDuration);

            // 晃动幅度逐渐减弱
            float strength =
                1f - Mathf.SmoothStep(0f, 1f, progress);

            float phase =
                timer * swayFrequency * Mathf.PI * 2f;

            float yaw =
                Mathf.Sin(phase) * yawAmount * strength;

            float roll =
                Mathf.Sin(phase * 0.75f)
                * rollAmount * strength;

            float horizontalMovement =
                Mathf.Sin(phase * 0.8f)
                * positionAmount * strength;

            float verticalMovement =
                Mathf.Sin(phase * 1.1f)
                * positionAmount * 0.5f * strength;

            transform.localPosition =
                originalLocalPosition
                + new Vector3(
                    horizontalMovement,
                    verticalMovement,
                    0f
                );

            transform.localRotation =
                originalLocalRotation
                * Quaternion.Euler(0f, yaw, roll);

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
    }
}