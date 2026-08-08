using System.Collections.Generic;
using UnityEngine;

public interface IIconHost
{
    RectTransform ContentRoot { get; }
    IList<MemoryFile> CurrentItems { get; }

    void OnIconClicked(DesktopIcon icon);
    void OnIconDoubleClicked(DesktopIcon icon);
    void RefreshIcons();
}
