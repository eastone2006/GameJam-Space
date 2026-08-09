using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DesktopUIManager : IconHostBase
{
    [Header("Desktop References")]
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private TextMeshProUGUI spaceText;
    [SerializeField] private TextMeshProUGUI pathText;

    [Header("Path Display")]
    [SerializeField] private string rootPathLabel = "M:\\Desktop\\";

    [Header("Desktop Layout")]
    [SerializeField] private KeyCode resetLayoutKey = KeyCode.R;

    public override RectTransform ContentRoot => gridRoot;
    public override IList<MemoryFile> CurrentItems => FileSystemManager.Instance.GetRootItems();
    public string RootPathLabel => rootPathLabel;

    private void Start()
    {
        if (FileSystemManager.Instance == null)
        {
            Debug.LogError("FileSystemManager 不存在，请先挂载并配置它。");
            return;
        }

        RefreshIcons();
        RefreshSpaceText();

        if (pathText != null)
        {
            string root = rootPathLabel.Replace('/', '\\');
            if (!root.EndsWith("\\"))
                root += "\\";
            pathText.text = root;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(resetLayoutKey))
            ResetDesktopLayout();
    }

    public void ResetDesktopLayout()
    {
        int index = 0;
        foreach (Transform child in gridRoot)
        {
            DesktopIcon icon = child.GetComponent<DesktopIcon>();
            if (icon == null) continue;

            Vector2 gridPosition = GetGridPosition(index);
            icon.SetDefaultPosition(gridPosition);
            icon.Rect.anchoredPosition = gridPosition;

            if (icon.File != null)
            {
                icon.File.savedPosition = gridPosition;
                icon.File.hasSavedPosition = true;
            }

            index++;
        }
    }

    public void RefreshSpaceText()
    {
        if (spaceText == null) return;
        spaceText.text = string.Format("Available Space: {0}",
            FileSystemManager.Instance.AvailableSpace.ToString("F1"),
            FileSystemManager.Instance.TotalSpace.ToString("F1"));
    }
}
