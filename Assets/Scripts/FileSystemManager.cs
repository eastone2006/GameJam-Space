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

        if (desktopItems.Count == 0)
            GenerateDefaultFileSystem();
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

    public void GenerateDefaultFileSystem()
    {
        desktopItems.Clear();

        desktopItems.Add(CreateFile("Assignments", 12f));
        desktopItems.Add(CreateFile("Screenshots", 25f));
        desktopItems.Add(CreateFile("TempLog", 8f));
        desktopItems.Add(CreateFile("OldProjects", 40f));
        desktopItems.Add(CreateFile("SetupCache", 15f));

        desktopItems.Add(CreateFolder("Downloads", MemoryType.JunkFile, null,
            CreateFile("Setup_Files", 18f),
            CreateFile("Browser_Cache", 9f)));

        desktopItems.Add(CreateFolder("Old_Backups", MemoryType.JunkFile, null,
            CreateFile("Backup_2021", 22f),
            CreateFile("Backup_2022", 26f)));

        desktopItems.Add(CreateFolder("System_Temp", MemoryType.JunkFile, null,
            CreateFile("Log_Files", 6f),
            CreateFile("Dump_Data", 11f)));

        desktopItems.Add(CreateFolder("Cached_Media", MemoryType.JunkFile, null,
            CreateFile("Thumbnails", 5f),
            CreateFile("Stream_Buffer", 14f)));

        desktopItems.Add(CreateFolder("Birthday", MemoryType.CoreMemory, "birthday",
            CreateFile("Birthday_Photo_001", 10f),
            CreateFile("Cake_Candle_Video", 15f)));

        desktopItems.Add(CreateFolder("Mothers Voice", MemoryType.CoreMemory, "mothers_voice",
            CreateFile("Moms_Voice_001", 12f),
            CreateFile("Bedtime_Story", 18f)));

        desktopItems.Add(CreateFolder("Travel", MemoryType.CoreMemory, "travel",
            CreateFile("Roadtrip_Clips", 20f),
            CreateFile("Beach_Photo", 8f)));

        desktopItems.Add(CreateFile("AI.exe", 30f, MemoryType.CoreMemory, "ai"));
    }

    private static MemoryFile CreateFile(string fileName, float size,
        MemoryType type = MemoryType.JunkFile, string memoryId = null)
    {
        return new MemoryFile
        {
            fileName = fileName,
            memoryId = memoryId ?? string.Empty,
            fileIcon = null,
            type = type,
            size = size,
            isDeleted = false,
            isFolder = false
        };
    }

    private static MemoryFile CreateFolder(string fileName, MemoryType type, string memoryId,
        params MemoryFile[] children)
    {
        MemoryFile folder = new MemoryFile
        {
            fileName = fileName,
            memoryId = memoryId ?? string.Empty,
            fileIcon = null,
            type = type,
            size = 0f,
            isDeleted = false,
            isFolder = true
        };
        folder.children.AddRange(children);
        return folder;
    }

    public bool IsEverythingExceptAiDeleted()
    {
        return CountRemainingNonAi(desktopItems) == 0;
    }

    private int CountRemainingNonAi(IList<MemoryFile> items)
    {
        int count = 0;
        foreach (MemoryFile item in items)
        {
            if (item.isDeleted) continue;
            if (item.memoryId == "ai") continue;

            count += 1 + CountRemainingNonAi(item.children);
        }
        return count;
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
        return string.Join("\\", segments);
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

    public MemoryFile FindParentFolder(MemoryFile file)
    {
        foreach (MemoryFile item in desktopItems)
        {
            MemoryFile parent = FindParentFolderInChildren(item, file);
            if (parent != null)
                return parent;
        }
        return null;
    }

    private MemoryFile FindParentFolderInChildren(MemoryFile folder, MemoryFile file)
    {
        if (folder.isFolder && folder.children.Contains(file))
            return folder;

        foreach (MemoryFile child in folder.children)
        {
            MemoryFile parent = FindParentFolderInChildren(child, file);
            if (parent != null)
                return parent;
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
