using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum MemoryType
{
    JunkFile,
    CoreMemory
}

[Serializable]
public class MemoryFile
{
    public string fileName;
    public string memoryId;
    public Sprite fileIcon;
    public MemoryType type;
    public float size;
    public bool isDeleted;
    public bool isFolder;
    public List<MemoryFile> children = new List<MemoryFile>();

    public float TotalSize
    {
        get
        {
            if (!isFolder) return size;

            float total = 0f;
            foreach (MemoryFile child in children)
            {
                if (!child.isDeleted)
                    total += child.TotalSize;
            }
            return total;
        }
    }
}
