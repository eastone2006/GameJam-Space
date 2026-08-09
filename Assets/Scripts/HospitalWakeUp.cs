using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HospitalWakeUp : MonoBehaviour
{
    [SerializeField] private Image whiteFadeImage;
    [SerializeField] private float whiteHoldDuration = 1f;
    [SerializeField] private float wakeUpDuration = 3f;

    private void Awake()
    {
        if (whiteFadeImage == null)
        {
            Debug.LogWarning("White Fade Image has not been assigned.");
            return;
        }

        if (GameStartController.Instance != null && GameStartController.IntroPlayed)
        {
            whiteFadeImage.gameObject.SetActive(false);
            SetAlpha(0f);
            return;
        }

        whiteFadeImage.gameObject.SetActive(true);
        SetAlpha(1f);
    }

    private void Start()
    {
        if (whiteFadeImage == null) return;

        if (GameStartController.Instance != null && GameStartController.IntroPlayed)
        {
            whiteFadeImage.gameObject.SetActive(false);
            SetAlpha(0f);
            return;
        }

        StartCoroutine(GameStartController.RunAfterStart(WakeUp()));
    }

    private IEnumerator WakeUp()
    {
        yield return new WaitForSeconds(whiteHoldDuration);

        float timer = 0f;

        while (timer < wakeUpDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / wakeUpDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            SetAlpha(1f - smoothProgress);

            yield return null;
        }

        SetAlpha(0f);
        whiteFadeImage.gameObject.SetActive(false);
        GameStartController.MarkIntroPlayed();
    }

    private void SetAlpha(float alpha)
    {
        Color color = whiteFadeImage.color;
        color.a = alpha;
        whiteFadeImage.color = color;
    }
}