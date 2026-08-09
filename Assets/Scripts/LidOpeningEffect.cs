using System.Collections;
using UnityEngine;

public class LidOpeningEffect : MonoBehaviour
{
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;

    [SerializeField] private float closedTime = 0.8f;
    [SerializeField] private float openingDuration = 2.5f;

    private void Start()
    {
        if (GameStartController.Instance != null && GameStartController.IntroPlayed)
        {
            SetLidScale(0f);
            gameObject.SetActive(false);
            return;
        }

        StartCoroutine(GameStartController.RunAfterStart(OpenEyes()));
    }

    private IEnumerator OpenEyes()
    {
        // 开始时，上下眼皮各遮住半个屏幕
        SetLidScale(0.5f);

        yield return new WaitForSecondsRealtime(closedTime);

        float timer = 0f;

        while (timer < openingDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / openingDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 从 0.5 缩小至 0
            float lidHeight = Mathf.Lerp(0.5f, 0f, smoothProgress);
            SetLidScale(lidHeight);

            yield return null;
        }

        SetLidScale(0f);
        GameStartController.MarkIntroPlayed();
        gameObject.SetActive(false);
    }

    private void SetLidScale(float height)
    {
        Vector3 topScale = topLid.localScale;
        topScale.y = height;
        topLid.localScale = topScale;

        Vector3 bottomScale = bottomLid.localScale;
        bottomScale.y = height;
        bottomLid.localScale = bottomScale;
    }
}