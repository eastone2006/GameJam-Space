using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager_B : MonoBehaviour
{
    public static SceneTransitionManager_B Instance { get; private set; }

    [SerializeField] private Image whiteImage;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (whiteImage != null)
        {
            SetAlpha(0f);
            whiteImage.raycastTarget = false;
        }
    }

    public void TransitionToScene(
        string sceneName,
        float fadeToWhiteDuration = 0.25f,
        float fadeFromWhiteDuration = 0.6f)
    {
        if (isTransitioning)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Scene cannot be loaded: " + sceneName
            );
            return;
        }

        StartCoroutine(
            TransitionRoutine(
                sceneName,
                fadeToWhiteDuration,
                fadeFromWhiteDuration
            )
        );
    }

    private IEnumerator TransitionRoutine(
        string sceneName,
        float fadeToWhiteDuration,
        float fadeFromWhiteDuration)
    {
        isTransitioning = true;

        yield return Fade(0f, 1f, fadeToWhiteDuration);

        yield return SceneManager.LoadSceneAsync(sceneName);

        // 等待新场景完成第一帧初始化
        yield return null;

        yield return Fade(1f, 0f, fadeFromWhiteDuration);

        isTransitioning = false;
    }

    private IEnumerator Fade(
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (whiteImage == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetAlpha(endAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / duration);
            float smoothProgress =
                Mathf.SmoothStep(0f, 1f, progress);

            SetAlpha(
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    smoothProgress
                )
            );

            yield return null;
        }

        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = whiteImage.color;
        color.a = alpha;
        whiteImage.color = color;
    }
}