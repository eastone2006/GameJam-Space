using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Ending Scene")]
    [Tooltip("两种结局（删掉除 AI 外所有文件 / 删掉 AI）都跳转到的医院场景")]
    [SerializeField] private string endingSceneName = "HospitalScene_B";

    [Header("Memory IDs")]
    [SerializeField] private string aiMemoryId = "ai";

    [Header("All-Deleted Ending")]
    [Tooltip("全部文件（除 AI）删完后，延迟多少秒再跳结局（给记忆音频/转场留时间）")]
    [SerializeField] private float endingAllDelay = 1.5f;

    [Header("Lifecycle")]
    [SerializeField] private bool persistAcrossScenes = true;

    public event Action<string> OnEndingTriggered;
    public event Action OnAiInducementFailed;

    private readonly HashSet<string> deletedMemories = new HashSet<string>();

    public IReadOnlyCollection<string> DeletedMemories => deletedMemories;

    private bool pendingAllDeletedEnding;
    private float allDeletedWaitUntil;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (FileSystemManager.Instance != null)
        {
            FileSystemManager.Instance.OnCoreMemoryDeleted += HandleCoreMemoryDeleted;
            FileSystemManager.Instance.OnFileDeleted += HandleFileDeleted;
        }
        else
        {
            Debug.LogError("GameManager: FileSystemManager 不存在，无法监听删除事件。");
        }
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
        {
            FileSystemManager.Instance.OnCoreMemoryDeleted -= HandleCoreMemoryDeleted;
            FileSystemManager.Instance.OnFileDeleted -= HandleFileDeleted;
        }

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!pendingAllDeletedEnding) return;

        // 记忆场景转场进行中时先不跳结局，等回到主场景再说
        if (ScreenGlitchEffect.PendingMemoryTransition) return;
        if (Time.time < allDeletedWaitUntil) return;

        pendingAllDeletedEnding = false;
        TriggerEnding("all_deleted");
    }

    private void HandleCoreMemoryDeleted(MemoryFile file)
    {
        if (file == null) return;

        deletedMemories.Add(file.memoryId);

        if (file.memoryId == aiMemoryId)
            HandleAiDeleted();
    }

    private void HandleFileDeleted(MemoryFile file)
    {
        if (FileSystemManager.Instance != null
            && FileSystemManager.Instance.IsEverythingExceptAiDeleted())
        {
            pendingAllDeletedEnding = true;
            allDeletedWaitUntil = Time.time + endingAllDelay;
        }
    }

    private void HandleAiDeleted()
    {
        TriggerEnding("awakening");
    }

    public void TriggerEnding(string endingType)
    {
        OnEndingTriggered?.Invoke(endingType);

        switch (endingType)
        {
            case "awakening":
            case "all_deleted":
                LoadScene(endingSceneName);
                break;
            default:
                Debug.LogWarning("GameManager: 未知结局类型 \"" + endingType + "\"");
                break;
        }
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("GameManager: 结局场景名称为空，无法跳转。");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
