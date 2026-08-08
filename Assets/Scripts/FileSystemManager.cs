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
    public IList<MemoryFile> GetRootItems() => desktopItems;

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

    public void MoveFileToFolder(MemoryFile file, MemoryFile targetFolder)
    {
        if (file == null || file == targetFolder) return;
        if (targetFolder != null && IsWithinSubtree(file, targetFolder)) return;

        IList<MemoryFile> source = FindParentList(file);
        if (source == null) return;

        source.Remove(file);

        if (targetFolder == null)
            desktopItems.Add(file);
        else
            targetFolder.children.Add(file);
    }

    public bool IsWithinSubtree(MemoryFile root, MemoryFile candidate)
    {
        if (root == candidate) return true;
        if (!root.isFolder) return false;

        foreach (MemoryFile child in root.children)
        {
            if (IsWithinSubtree(child, candidate))
                return true;
        }
        return false;
    }

    public string GetFolderPath(MemoryFile folder)
    {
        if (folder == null) return string.Empty;

        List<string> segments = new List<string>();
        FindFolderPath(desktopItems, folder, segments);
        return string.Join("/", segments);
    }

    private bool FindFolderPath(IList<MemoryFile> list, MemoryFile folder, List<string> segments)
    {
        foreach (MemoryFile item in list)
        {
            if (item == folder)
            {
                segments.Add(item.fileName);
                return true;
            }

            if (item.isFolder)
            {
                segments.Add(item.fileName);
                if (FindFolderPath(item.children, folder, segments))
                    return true;
                segments.RemoveAt(segments.Count - 1);
            }
        }
        return false;
    }

    private IList<MemoryFile> FindParentList(MemoryFile file)
    {
        if (desktopItems.Contains(file))
            return desktopItems;

        foreach (MemoryFile item in desktopItems)
        {
            IList<MemoryFile> found = FindParentListInChildren(item, file);
            if (found != null)
                return found;
        }
        return null;
    }

    private IList<MemoryFile> FindParentListInChildren(MemoryFile folder, MemoryFile file)
    {
        if (!folder.isFolder) return null;

        if (folder.children.Contains(file))
            return folder.children;

        foreach (MemoryFile child in folder.children)
        {
            IList<MemoryFile> found = FindParentListInChildren(child, file);
            if (found != null)
                return found;
        }
        return null;
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
