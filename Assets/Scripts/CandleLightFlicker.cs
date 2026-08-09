using UnityEngine;

[RequireComponent(typeof(Light))]
public class CandleLightFlicker : MonoBehaviour
{
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float intensityVariation = 0.2f;
    [SerializeField] private float rangeVariation = 0.08f;

    private Light candleLight;
    private float originalIntensity;
    private float originalRange;
    private float noiseOffset;

    private void Awake()
    {
        candleLight = GetComponent<Light>();

        originalIntensity = candleLight.intensity;
        originalRange = candleLight.range;

        noiseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (GameStartController.Instance != null && !GameStartController.HasStarted)
            return;

        float noise = Mathf.PerlinNoise(
            noiseOffset,
            Time.time * flickerSpeed
        );

        float variation = (noise - 0.5f) * 2f;

        candleLight.intensity =
            originalIntensity *
            (1f + variation * intensityVariation);

        candleLight.range =
            originalRange *
            (1f + variation * rangeVariation);
    }

    private void OnDisable()
    {
        if (candleLight != null)
        {
            candleLight.intensity = originalIntensity;
            candleLight.range = originalRange;
        }
    }
}