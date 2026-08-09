using System.Collections;
using UnityEngine;

public class WakeUpAudioEffect : MonoBehaviour
{
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioSource breathingSource;

    [SerializeField] private float fadeStartTime = 0.6f;
    [SerializeField] private float fadeDuration = 2.2f;

    [SerializeField] private float heartbeatVolume = 0.35f;
    [SerializeField] private float breathingVolume = 0.25f;

    private void Awake()
    {
        // 防止 AudioSource 的 Play On Awake 在场景加载时就抢先播放
        if (heartbeatSource != null)
        {
            heartbeatSource.playOnAwake = false;
            heartbeatSource.Stop();
        }
        else
        {
            Debug.LogWarning("WakeUpAudioEffect: heartbeatSource 未在 Inspector 中赋值，点击 Start 后将不会播放心跳音。", this);
        }

        if (breathingSource != null)
        {
            breathingSource.playOnAwake = false;
            breathingSource.Stop();
        }
        else
        {
            Debug.LogWarning("WakeUpAudioEffect: breathingSource 未在 Inspector 中赋值，点击 Start 后将不会播放呼吸音。", this);
        }
    }

    private void Start()
    {
        if (GameStartController.Instance != null && GameStartController.IntroPlayed)
            return;

        StartCoroutine(GameStartController.RunAfterStart(PlayWakeUpAudio()));
    }

    private IEnumerator PlayWakeUpAudio()
    {
        if (heartbeatSource != null)
        {
            heartbeatSource.loop = true;
            heartbeatSource.volume = heartbeatVolume;
            heartbeatSource.Play();
        }

        if (breathingSource != null)
        {
            breathingSource.loop = false;
            breathingSource.volume = breathingVolume;
            breathingSource.Play();
        }

        yield return new WaitForSecondsRealtime(fadeStartTime);

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(timer / fadeDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (heartbeatSource != null)
            {
                heartbeatSource.volume =
                    Mathf.Lerp(heartbeatVolume, 0f, smoothProgress);
            }

            if (breathingSource != null)
            {
                breathingSource.volume =
                    Mathf.Lerp(breathingVolume, 0f, smoothProgress);
            }

            yield return null;
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
        }

        if (breathingSource != null)
        {
            breathingSource.Stop();
        }
    }
}