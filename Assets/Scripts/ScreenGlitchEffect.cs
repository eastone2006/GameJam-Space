using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [Tooltip("花屏加重/消失的持续时间（秒）")]
    [SerializeField] private float glitchDuration = 2f;
    [SerializeField] private float minFlickerInterval = 0.02f;
    [SerializeField] private float maxFlickerInterval = 0.06f;

    [Header("Memory Scene Mapping")]
    [Tooltip("核心记忆 memoryId 对应的记忆场景名（删除后花屏加重并进入该场景，播完回 Warehouse）")]
    [SerializeField] private List<MemorySceneMapping> memoryScenes = new List<MemorySceneMapping>
    {
        new MemorySceneMapping { memoryId = "birthday", sceneName = "BirthdayScene_B" },
        new MemorySceneMapping { memoryId = "travel", sceneName = "TravelMemoryScene_B" }
    };

    [Header("Mothers Voice")]
    [Tooltip("删除该 memoryId 时只播放音频、不跳场景")]
    [SerializeField] private string mothersVoiceMemoryId = "mothers_voice";
    [SerializeField] private AudioClip mothersVoiceAudio;
    [SerializeField, Range(0f, 1f)] private float mothersVoiceVolume = 1f;

    public static bool PendingMemoryTransition { get; private set; }
    public static bool MothersVoicePlaying { get; private set; }

    private static bool returningFromMemory;

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

        PendingMemoryTransition = false;

        // 花屏遮罩绝不拦截输入，保证特效期间玩家仍可点击/操作
        if (glitchOverlay != null)
            glitchOverlay.raycastTarget = false;
        if (noiseOverlay != null)
            noiseOverlay.raycastTarget = false;

        ResetOverlay();

        if (returningFromMemory)
        {
            returningFromMemory = false;
            TriggerGlitchFadeOut();
        }
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
        if (file == null) return;

        if (TryGetScene(file.memoryId, out string sceneName))
        {
            TriggerMemoryTransition(sceneName);
        }
        else if (file.memoryId == mothersVoiceMemoryId)
        {
            PlayMothersVoiceAudio();
        }
        else
        {
            TriggerGlitchFadeOut();
        }
    }

    private void PlayMothersVoiceAudio()
    {
        if (audioSource == null || mothersVoiceAudio == null) return;

        if (glitchRoutine != null)
            StopCoroutine(glitchRoutine);
        glitchRoutine = null;

        audioSource.volume = mothersVoiceVolume;
        audioSource.PlayOneShot(mothersVoiceAudio);

        MothersVoicePlaying = true;
        StartCoroutine(ClearMothersVoicePlaying(mothersVoiceAudio.length));
    }

    private IEnumerator ClearMothersVoicePlaying(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        MothersVoicePlaying = false;
    }

    public void TriggerMemoryTransition(string sceneName)
    {
        if (glitchRoutine != null)
            StopCoroutine(glitchRoutine);

        PlayGlitchSound();
        returningFromMemory = true;
        PendingMemoryTransition = true;
        glitchRoutine = StartCoroutine(GlitchLoop(true, () => LoadMemoryScene(sceneName)));
    }

    public void TriggerGlitchFadeOut()
    {
        if (glitchRoutine != null)
            StopCoroutine(glitchRoutine);

        PlayGlitchSound();
        glitchRoutine = StartCoroutine(GlitchLoop(false, null));
    }

    public void TriggerGlitch()
    {
        TriggerGlitchFadeOut();
    }

    private IEnumerator GlitchLoop(bool easeIn, Action onEnd)
    {
        float elapsed = 0f;
        float duration = glitchDuration; // 严格锁定当前设定的总时间（例如 2 秒）

        while (elapsed < duration)
        {
            // 计算当前进度的百分比 (0 到 1)
            float t = Mathf.Clamp01(elapsed / duration);
            float intensity = easeIn ? t : (1f - t);

            // 应用当前强度的花屏画面与音量
            ApplyGlitchFrame(intensity);

            // 计算下一次闪烁的随机间隔
            float waitTime = UnityEngine.Random.Range(minFlickerInterval, maxFlickerInterval);
            
            // 关键修复：把等待的时间累加到 elapsed 中，确保真实耗时严格等于 glitchDuration
            elapsed += waitTime;
            
            yield return new WaitForSeconds(waitTime);
        }

        // 动画结束，瞬间将强度拉满（1）或归零（0）
        ApplyGlitchFrame(easeIn ? 1f : 0f);

        // 触发后续回调（如进入记忆场景或结束花屏）
        onEnd?.Invoke();
        glitchRoutine = null;
    }

    private void ApplyGlitchFrame(float intensity)
    {
        if (glitchOverlayRect != null)
        {
            glitchOverlayRect.anchoredPosition = UnityEngine.Random.value < 0.6f
                ? new Vector2(UnityEngine.Random.Range(-maxOffset, maxOffset) * intensity, 0f)
                : Vector2.zero;
        }

        if (glitchOverlay != null)
        {
            Color color = glitchOverlay.color;
            color.a = UnityEngine.Random.Range(0f, maxAlpha) * intensity;
            glitchOverlay.color = color;
        }

        if (noiseOverlay != null && noiseOverlay.texture != null)
        {
            Rect uv = noiseOverlay.uvRect;
            uv.x = UnityEngine.Random.Range(0f, 1f);
            uv.y = UnityEngine.Random.Range(0f, 1f);
            noiseOverlay.uvRect = uv;
        }

        if (audioSource != null)
            audioSource.volume = maxVolume * intensity;
    }

    private void PlayGlitchSound()
    {
        if (audioSource == null || glitchSound == null) return;

        audioSource.Stop();
        audioSource.clip = glitchSound;
        audioSource.loop = false;
        audioSource.Play();
    }

    private bool TryGetScene(string memoryId, out string sceneName)
    {
        foreach (MemorySceneMapping mapping in memoryScenes)
        {
            if (mapping.memoryId == memoryId)
            {
                sceneName = mapping.sceneName;
                return true;
            }
        }
        sceneName = null;
        return false;
    }

    private void LoadMemoryScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneManager.LoadScene(sceneName);
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

[Serializable]
public class MemorySceneMapping
{
    public string memoryId;
    public string sceneName;
}
