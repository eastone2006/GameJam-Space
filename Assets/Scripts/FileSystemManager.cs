using System;
using System.Collections.Generic;
using UnityEngine;

public class FileSystemManager : MonoBehaviour
{
    public static FileSystemManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private float totalSpace = 100f;
    [SerializeField] private float initialAvailableSpace = 0.5f;
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private List<MemoryFile> desktopItems = new List<MemoryFile>();

    public float TotalSpace => totalSpace;
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

        AvailableSpace = Mathf.Clamp(initialAvailableSpace, 0f, totalSpace);
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

        AvailableSpace = Mathf.Clamp(AvailableSpace + freedSize, 0f, totalSpace);

        OnFileDeleted?.Invoke(file);

        if (file.type == MemoryType.CoreMemory)
            OnCoreMemoryDeleted?.Invoke(file);

        FireCoreMemoryEventsRecursive(file);

        return true;
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
