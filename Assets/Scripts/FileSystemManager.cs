using System;
using System.Collections.Generic;
using UnityEngine;

public class FileSystemManager : MonoBehaviour
{
    public static FileSystemManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private float initialAvailableSpace = 0.5f;
    [SerializeField] private float displayTotalSpace = 100f;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private List<MemoryFile> desktopItems = new List<MemoryFile>();

    [Header("Auto Generate Junk")]
    [SerializeField] private bool autoGenerateJunkFiles = true;
    [SerializeField] private List<JunkFileTemplate> junkTemplates = new List<JunkFileTemplate>();

    public float TotalSpace => displayTotalSpace;
    public float AvailableSpace { get; private set; }
    public IReadOnlyList<MemoryFile> DesktopItems => desktopItems;

    public event Action<MemoryFile> OnFileDeleted;
    public event Action<MemoryFile> OnCoreMemoryDeleted;

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

        AvailableSpace = Mathf.Max(0f, initialAvailableSpace);

        if (autoGenerateJunkFiles && desktopItems.Count == 0)
            GenerateDefaultJunkFiles();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public MemoryFile FindByMemoryId(string memoryId)
    {
        foreach (MemoryFile file in desktopItems)
        {
            MemoryFile result = FindInSubtree(file, memoryId);
            if (result != null)
                return result;
        }
        return null;
    }

    public bool TryDeleteFile(MemoryFile file)
    {
        if (file == null || file.isDeleted) return false;

        float freedSize = file.TotalSize;

        DeleteRecursive(file);

        AvailableSpace += freedSize;

        OnFileDeleted?.Invoke(file);

        if (file.type == MemoryType.CoreMemory)
            OnCoreMemoryDeleted?.Invoke(file);

        FireCoreMemoryEventsRecursive(file);

        return true;
    }

    public void GenerateDefaultJunkFiles()
    {
        List<JunkFileTemplate> templates = junkTemplates.Count > 0
            ? junkTemplates
            : new List<JunkFileTemplate>
            {
                new JunkFileTemplate { fileName = "Assignments", size = 12f },
                new JunkFileTemplate { fileName = "Screenshots", size = 25f },
                new JunkFileTemplate { fileName = "TempLog", size = 8f },
                new JunkFileTemplate { fileName = "OldProjects", size = 40f },
                new JunkFileTemplate { fileName = "SetupCache", size = 15f },
                new JunkFileTemplate { fileName = "DownloadedClips", size = 18f }
            };

        foreach (JunkFileTemplate template in templates)
        {
            desktopItems.Add(new MemoryFile
            {
                fileName = template.fileName,
                memoryId = "",
                fileIcon = null,
                type = MemoryType.JunkFile,
                size = template.size,
                isDeleted = false,
                isFolder = false
            });
        }
    }

    private void DeleteRecursive(MemoryFile file)
    {
        file.isDeleted = true;
        if (!file.isFolder) return;

        foreach (MemoryFile child in file.children)
            DeleteRecursive(child);
    }

    private void FireCoreMemoryEventsRecursive(MemoryFile folder)
    {
        if (!folder.isFolder) return;

        foreach (MemoryFile child in folder.children)
        {
            if (child.type == MemoryType.CoreMemory)
                OnCoreMemoryDeleted?.Invoke(child);
            FireCoreMemoryEventsRecursive(child);
        }
    }

    private MemoryFile FindInSubtree(MemoryFile file, string memoryId)
    {
        if (file.memoryId == memoryId && !file.isDeleted)
            return file;

        if (!file.isFolder) return null;

        foreach (MemoryFile child in file.children)
        {
            MemoryFile result = FindInSubtree(child, memoryId);
            if (result != null)
                return result;
        }
        return null;
    }
}

[Serializable]
public class JunkFileTemplate
{
    public string fileName;
    public float size;
}
