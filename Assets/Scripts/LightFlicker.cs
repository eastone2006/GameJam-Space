using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float minimumDelay = 3f;
    [SerializeField] private float maximumDelay = 8f;

    private Light targetLight;
    private float originalIntensity;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        originalIntensity = targetLight.intensity;
    }

    private void Start()
    {
        StartCoroutine(GameStartController.RunAfterStart(FlickerRoutine()));
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minimumDelay, maximumDelay)
            );

            int flickerCount = Random.Range(1, 4);

            for (int i = 0; i < flickerCount; i++)
            {
                targetLight.intensity =
                    originalIntensity * Random.Range(0.15f, 0.45f);

                yield return new WaitForSeconds(
                    Random.Range(0.04f, 0.09f)
                );

                targetLight.intensity = originalIntensity;

                yield return new WaitForSeconds(
                    Random.Range(0.05f, 0.12f)
                );
            }
        }
    }

    private void OnDisable()
    {
        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
        }
    }
}