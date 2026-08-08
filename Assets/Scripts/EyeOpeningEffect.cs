using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CanvasGroup))]
public class EyeOpeningEffect : MonoBehaviour
{
    [SerializeField] private Volume wakeUpVolume;
    [SerializeField] private float blackScreenTime = 0.6f;
    [SerializeField] private float openingDuration = 2f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        StartCoroutine(OpenEyes());
    }

    private IEnumerator OpenEyes()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (wakeUpVolume != null)
        {
            wakeUpVolume.weight = 1f;
        }

        yield return new WaitForSecondsRealtime(blackScreenTime);

        float timer = 0f;

        while (timer < openingDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / openingDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 黑幕逐渐消失
            canvasGroup.alpha = 1f - smoothProgress;

            // 模糊逐渐恢复清晰
            if (wakeUpVolume != null)
            {
                wakeUpVolume.weight = 1f - smoothProgress;
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (wakeUpVolume != null)
        {
            wakeUpVolume.weight = 0f;
        }

        gameObject.SetActive(false);
    }
}