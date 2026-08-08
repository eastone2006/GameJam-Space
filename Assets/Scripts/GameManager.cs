using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Ending Scenes")]
    [SerializeField] private string awakeningSceneName = "Hospital";
    [SerializeField] private string questionSceneName = "QuestionEnding";

    [Header("AI Ending Threshold")]
    [Tooltip("删除 AI 时，剩余空间达到总容量的该比例即触发苏醒结局")]
    [SerializeField] private float awakeningSpaceRatio = 0.8f;

    [Header("Memory IDs")]
    [SerializeField] private string aiMemoryId = "ai";
    [SerializeField] private List<string> coreMemoryIds = new List<string>
    {
        "birthday",
        "mothers_voice",
        "graduation",
        "travel"
    };

    [Header("Lifecycle")]
    [SerializeField] private bool persistAcrossScenes = true;

    public event Action<string> OnEndingTriggered;
    public event Action OnAiInducementFailed;

    private readonly HashSet<string> deletedMemories = new HashSet<string>();

    public IReadOnlyCollection<string> DeletedMemories => deletedMemories;

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
        }
        else
        {
            Debug.LogError("GameManager: FileSystemManager 不存在，无法监听核心记忆删除事件。");
        }
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnCoreMemoryDeleted -= HandleCoreMemoryDeleted;

        if (Instance == this)
            Instance = null;
    }

    private void HandleCoreMemoryDeleted(MemoryFile file)
    {
        if (file == null) return;

        deletedMemories.Add(file.memoryId);

        if (file.memoryId == aiMemoryId)
        {
            HandleAiDeleted();
            return;
        }

        if (AreAllCoreMemoriesDeleted())
            TriggerEnding("question");
    }

    private void HandleAiDeleted()
    {
        float totalSpace = FileSystemManager.Instance.TotalSpace;
        float availableRatio = totalSpace > 0f
            ? FileSystemManager.Instance.AvailableSpace / totalSpace
            : 0f;

        if (availableRatio >= awakeningSpaceRatio)
        {
            TriggerEnding("awakening");
        }
        else
        {
            OnAiInducementFailed?.Invoke();
        }
    }

    private bool AreAllCoreMemoriesDeleted()
    {
        foreach (string id in coreMemoryIds)
        {
            if (!deletedMemories.Contains(id))
                return false;
        }
        return true;
    }

    public void TriggerEnding(string endingType)
    {
        OnEndingTriggered?.Invoke(endingType);

        switch (endingType)
        {
            case "awakening":
                LoadScene(awakeningSceneName);
                break;
            case "question":
                LoadScene(questionSceneName);
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
