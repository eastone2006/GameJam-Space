using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DesktopUIManager : MonoBehaviour, IIconHost
{
    [Header("Desktop References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private GameObject desktopIconPrefab;
    [SerializeField] private TextMeshProUGUI spaceText;
    [SerializeField] private TextMeshProUGUI pathText;

    [Header("Default Icons")]
    [SerializeField] private Sprite defaultFolderIcon;
    [SerializeField] private Sprite defaultFileIcon;

    [Header("Path Display")]
    [SerializeField] private string rootPathLabel = "M:\\Desktop\\";

    [Header("Desktop Layout")]
    [SerializeField] private KeyCode resetLayoutKey = KeyCode.R;
    [SerializeField] private int iconsPerRow = 5;
    [SerializeField] private float horizontalSpacing = 20f;
    [SerializeField] private float verticalSpacing = 20f;
    [SerializeField] private float paddingTop = 20f;
    [SerializeField] private float paddingLeft = 20f;

    public Canvas Canvas => canvas;
    public RectTransform ContentRoot => gridRoot;
    public IList<MemoryFile> CurrentItems => FileSystemManager.Instance.GetRootItems();
    public string RootPathLabel => rootPathLabel;

    private DesktopIcon selectedIcon;

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
            pathText.text = rootPathLabel;
    }

    private void Update()
    {
        if (Input.GetKeyDown(resetLayoutKey))
            ResetDesktopLayout();
    }

    public void RefreshIcons()
    {
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);
        selectedIcon = null;

        int index = 0;
        foreach (MemoryFile file in CurrentItems)
        {
            if (file.isDeleted) continue;

            CreateIcon(file, index);
            index++;
        }
    }

    private void CreateIcon(MemoryFile file, int index)
    {
        GameObject go = Instantiate(desktopIconPrefab, gridRoot);
        DesktopIcon icon = go.GetComponent<DesktopIcon>();

        Sprite sprite = file.fileIcon;
        if (sprite == null)
            sprite = file.isFolder ? defaultFolderIcon : defaultFileIcon;

        icon.Setup(this, file, sprite);

        RectTransform rect = icon.Rect;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 gridPosition = GetGridPosition(index);
        icon.SetDefaultPosition(gridPosition);

        rect.anchoredPosition = file.hasSavedPosition ? file.savedPosition : gridPosition;
    }

    private Vector2 GetGridPosition(int index)
    {
        Rect bounds = gridRoot.rect;
        Vector2 iconSize = desktopIconPrefab.GetComponent<RectTransform>().sizeDelta;
        float cellWidth = iconSize.x + horizontalSpacing;
        float cellHeight = iconSize.y + verticalSpacing;

        int columns = Mathf.Max(1, iconsPerRow);
        int row = index / columns;
        int col = index % columns;

        float startX = bounds.xMin + paddingLeft + iconSize.x * 0.5f;
        float startY = bounds.yMax - paddingTop - iconSize.y * 0.5f;

        return new Vector2(startX + col * cellWidth, startY - row * cellHeight);
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
        spaceText.text = string.Format("Available Space: {0} / {1} MB",
            FileSystemManager.Instance.AvailableSpace.ToString("F0"),
            FileSystemManager.Instance.TotalSpace.ToString("F0"));
    }

    public void OnIconClicked(DesktopIcon icon)
    {
        if (icon == null || icon == selectedIcon) return;

        selectedIcon = icon;
        foreach (Transform child in gridRoot)
        {
            DesktopIcon existing = child.GetComponent<DesktopIcon>();
            if (existing != null)
                existing.SetSelected(existing == icon);
        }
    }

    public void OnIconDoubleClicked(DesktopIcon icon)
    {
        if (icon == null || icon.File == null || !icon.File.isFolder) return;
        WindowManager.Instance.OpenFolderWindow(icon.File);
    }
}
