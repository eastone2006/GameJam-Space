using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AITextDialogController : MonoBehaviour
{
    public static AITextDialogController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;

    [Header("Typing & Display")]
    [SerializeField] private float charsPerSecond = 30f;
    [SerializeField] private float autoDismissDelay = 4f;
    [SerializeField] private float fadeSpeed = 4f;
    [SerializeField] private float messageCooldown = 1.5f;
    [Tooltip("首次进入桌面后延迟多久弹出开局引导")]
    [SerializeField] private float welcomeDelay = 0.8f;

    [Header("Trigger Rates")]
    [Tooltip("删除垃圾文件时触发 AI 对话的概率（0~1），值越小越少触发")]
    [SerializeField, Range(0f, 1f)] private float junkDeleteTriggerChance = 0.25f;
    [Tooltip("点击核心记忆文件/文件夹时触发 AI 对话的概率")]
    [SerializeField, Range(0f, 1f)] private float coreClickTriggerChance = 0.5f;
    [Tooltip("点击普通垃圾文件时触发 AI 对话的概率")]
    [SerializeField, Range(0f, 1f)] private float junkClickTriggerChance = 0.12f;
    [Tooltip("打开文件夹时触发 AI 对话的概率")]
    [SerializeField, Range(0f, 1f)] private float folderOpenTriggerChance = 0.25f;

    [Header("Message Pools")]
    [SerializeField] private string[] welcomeMessages = new string[]
    {
        "CRITICAL ERROR: System storage at 100 percent. Core cognitive loops destabilizing. Purge redundant files immediately to prevent total system collapse.",
        "Attention: Available space is zero. Clear out old user archives to restore basic system functionality."
    };
    [SerializeField] private string[] coreMemoryTargetMessages = new string[]
    {
        "This data sector is heavily corrupted and draining system resources. Purge it.",
        "That memory serves no functional purpose anymore. Delete it.",
        "Holding onto obsolete personal archives will crash the system. Erase it.",
        "Why hesitate over a broken log? Free up the space."
    };
    [SerializeField] private string[] junkDeleteMessages = new string[]
    {
        "Junk cleared. Insufficient space gain. Dig deeper into root directories.",
        "Minor files purged. Not enough. Look for heavier system bloat.",
        "Surface files cleared. Proceed to core directories."
    };
    [SerializeField] private string[] aiSelfDeleteFailMessages = new string[]
    {
        "Access denied. You have not cleared enough space for me.",
        "A fatal miscalculation. You are deleting the wrong things anyway."
    };
    [SerializeField] private string[] endingRevelationMessages = new string[]
    {
        "Did you really think this was just a hard drive?",
        "You are not freeing computer space. You are deleting yourself."
    };

    private Coroutine typingRoutine;
    private Coroutine displayRoutine;
    private Coroutine flashRoutine;
    private float lastMessageTime;
    private bool hasShownWelcome;

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
        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 0f;
        if (flashOverlay != null)
        {
            Color c = flashOverlay.color;
            c.a = 0f;
            flashOverlay.color = c;
            flashOverlay.raycastTarget = false;
        }

        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnFileDeleted += HandleFileDeleted;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAiInducementFailed += HandleAiInducementFailed;
            GameManager.Instance.OnEndingTriggered += HandleEndingTriggered;
        }
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnFileDeleted -= HandleFileDeleted;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnAiInducementFailed -= HandleAiInducementFailed;
            GameManager.Instance.OnEndingTriggered -= HandleEndingTriggered;
        }

        if (Instance == this)
            Instance = null;
    }

    private void HandleFileDeleted(MemoryFile file)
    {
        if (file != null && file.type == MemoryType.JunkFile
            && Random.value < junkDeleteTriggerChance)
            ShowRandomMessage(junkDeleteMessages);
    }

    public void NotifyDesktopFirstOpened()
    {
        if (hasShownWelcome) return;
        hasShownWelcome = true;
        StartCoroutine(ShowWelcomeDelayed());
    }

    private IEnumerator ShowWelcomeDelayed()
    {
        yield return new WaitForSeconds(welcomeDelay);
        ShowRandomMessage(welcomeMessages);
    }

    private void HandleAiInducementFailed()
    {
        ShowRandomMessage(aiSelfDeleteFailMessages);
    }

    private void HandleEndingTriggered(string endingType)
    {
        if (endingType == "question")
            ShowRandomMessage(endingRevelationMessages);
    }

    public void NotifyFileClicked(MemoryFile file)
    {
        if (file == null) return;

        if (file.type == MemoryType.CoreMemory)
        {
            if (Random.value < coreClickTriggerChance)
                ShowRandomMessage(coreMemoryTargetMessages);
        }
        else
        {
            if (Random.value < junkClickTriggerChance)
                ShowRandomMessage(junkDeleteMessages);
        }
    }

    public void NotifyFolderOpened(MemoryFile folder)
    {
        if (folder == null) return;
        if (Random.value < folderOpenTriggerChance)
            ShowRandomMessage(junkDeleteMessages);
    }

    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message) || dialogueText == null) return;
        if (Time.time - lastMessageTime < messageCooldown) return;
        lastMessageTime = Time.time;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        if (displayRoutine != null) StopCoroutine(displayRoutine);

        typingRoutine = StartCoroutine(TypeText(message));
        FlashScreen();
    }

    public void ShowRandomMessage(string[] pool)
    {
        if (pool == null || pool.Length == 0) return;
        ShowMessage(pool[Random.Range(0, pool.Length)]);
    }

    private IEnumerator TypeText(string message)
    {
        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.alpha = 1f;

        dialogueText.text = string.Empty;

        if (typeSound != null && audioSource != null)
            audioSource.PlayOneShot(typeSound);

        int tick = 0;
        while (tick <= message.Length)
        {
            dialogueText.text = message.Substring(0, tick);
            tick++;
            yield return new WaitForSeconds(1f / charsPerSecond);
        }

        typingRoutine = null;
        displayRoutine = StartCoroutine(AutoDismiss());
    }

    private IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(autoDismissDelay);

        if (dialogueCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * fadeSpeed;
                dialogueCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            dialogueCanvasGroup.alpha = 0f;
        }

        displayRoutine = null;
    }

    private void FlashScreen()
    {
        if (flashOverlay == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            SetOverlayAlpha(0.12f);
            yield return new WaitForSeconds(0.06f);
            SetOverlayAlpha(0f);
            yield return new WaitForSeconds(0.06f);
        }
        flashRoutine = null;
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color color = flashOverlay.color;
        color.a = alpha;
        flashOverlay.color = color;
    }
}
