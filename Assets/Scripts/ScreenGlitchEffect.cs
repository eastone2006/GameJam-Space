using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenGlitchEffect : MonoBehaviour
{
    public static ScreenGlitchEffect Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private Image glitchOverlay;
    [SerializeField] private RectTransform glitchOverlayRect;
    [Tooltip("可选：带噪点纹理的 RawImage，用于滚动 UV 制造雪花/扫描线")]
    [SerializeField] private RawImage noiseOverlay;

    [Header("Audio")]
    [Tooltip("花屏音效（实际只播放 glitchDuration 秒）")]
    [SerializeField] private AudioClip glitchSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

    [Header("Intensity")]
    [SerializeField] private float maxOffset = 28f;
    [SerializeField] private float maxAlpha = 0.3f;
    [Tooltip("特效与花屏声的持续时间（秒）")]
    [SerializeField] private float glitchDuration = 10f;
    [SerializeField] private float minFlickerInterval = 0.02f;
    [SerializeField] private float maxFlickerInterval = 0.06f;

    private Coroutine glitchRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnCoreMemoryDeleted += HandleCoreMemoryDeleted;

        ResetOverlay();
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnCoreMemoryDeleted -= HandleCoreMemoryDeleted;

        if (audioSource != null)
            audioSource.Stop();

        if (Instance == this)
            Instance = null;
    }

    private void HandleCoreMemoryDeleted(MemoryFile file)
    {
        if (file != null)
            TriggerGlitch();
    }

    public void TriggerGlitch()
    {
        if (glitchRoutine != null)
            StopCoroutine(glitchRoutine);

        PlayGlitchSound();

        glitchRoutine = StartCoroutine(GlitchRoutine());
    }

    private void PlayGlitchSound()
    {
        if (audioSource == null || glitchSound == null) return;

        audioSource.Stop();
        audioSource.clip = glitchSound;
        audioSource.loop = false;
        audioSource.Play();
    }

    private IEnumerator GlitchRoutine()
    {
        float duration = glitchDuration;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = 1f - (elapsed / duration);

            if (glitchOverlayRect != null)
            {
                glitchOverlayRect.anchoredPosition = Random.value < 0.6f
                    ? new Vector2(Random.Range(-maxOffset, maxOffset) * intensity, 0f)
                    : Vector2.zero;
            }

            if (glitchOverlay != null)
            {
                Color color = glitchOverlay.color;
                color.a = Random.Range(0f, maxAlpha) * intensity;
                glitchOverlay.color = color;
            }

            if (noiseOverlay != null && noiseOverlay.texture != null)
            {
                Rect uv = noiseOverlay.uvRect;
                uv.x = Random.Range(0f, 1f);
                uv.y = Random.Range(0f, 1f);
                noiseOverlay.uvRect = uv;
            }

            if (audioSource != null)
                audioSource.volume = maxVolume * intensity;

            yield return new WaitForSeconds(Random.Range(minFlickerInterval, maxFlickerInterval));
        }

        if (audioSource != null)
        {
            audioSource.volume = 0f;
            audioSource.Stop();
        }

        ResetOverlay();
        glitchRoutine = null;
    }

    private void ResetOverlay()
    {
        if (glitchOverlayRect != null)
            glitchOverlayRect.anchoredPosition = Vector2.zero;

        if (glitchOverlay != null)
        {
            Color color = glitchOverlay.color;
            color.a = 0f;
            glitchOverlay.color = color;
        }
    }
}
