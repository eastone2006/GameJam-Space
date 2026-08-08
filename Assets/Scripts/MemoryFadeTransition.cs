using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MemoryFadeTransition : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDelay = 7.2f;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float whiteScreenDuration = 0.3f;

    [SerializeField] private string returnSceneName = "WarehouseScene";

    private void Start()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("Fade Image has not been assigned.");
            return;
        }

        SetAlpha(0f);
        StartCoroutine(FadeAndReturn());
    }

    private IEnumerator FadeAndReturn()
    {
        yield return new WaitForSeconds(fadeDelay);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / fadeDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            SetAlpha(smoothProgress);
            yield return null;
        }

        SetAlpha(1f);

        yield return new WaitForSeconds(whiteScreenDuration);

        SceneManager.LoadScene(returnSceneName);
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}