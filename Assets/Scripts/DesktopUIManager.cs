using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DesktopUIManager : MonoBehaviour
{
    [Header("Desktop References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform gridRoot;
    [SerializeField] private GameObject desktopIconPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI spaceText;
    [SerializeField] private TextMeshProUGUI pathText;
    [SerializeField] private RectTransform recycleBinRect;

    [Header("Default Icons")]
    [SerializeField] private Sprite defaultFolderIcon;
    [SerializeField] private Sprite defaultFileIcon;

    [Header("Path Display")]
    [SerializeField] private string rootPathLabel = "M:\\Desktop\\";

    [Header("Confirm Dialog")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    public Canvas Canvas => canvas;

    private readonly Stack<MemoryFile> folderStack = new Stack<MemoryFile>();
    private DesktopIcon draggedIcon;
    private DesktopIcon selectedIcon;
    private DesktopIcon pendingIcon;
    private bool awaitingConfirm;

    private void Start()
    {
        if (FileSystemManager.Instance == null)
        {
            Debug.LogError("FileSystemManager 不存在，请先挂载并配置它。");
            return;
        }

        backButton.onClick.AddListener(GoBack);
        confirmYesButton.onClick.AddListener(OnConfirmYes);
        confirmNoButton.onClick.AddListener(OnConfirmNo);
        confirmPanel.SetActive(false);

        FileSystemManager.Instance.OnFileDeleted += HandleFileDeleted;

        ShowRoot();
        RefreshSpaceText();
        RefreshPathText();
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnFileDeleted -= HandleFileDeleted;
    }

    private void HandleFileDeleted(MemoryFile file)
    {
        selectedIcon = null;
        RefreshGrid();
        RefreshSpaceText();
    }

    private void ShowRoot()
    {
        folderStack.Clear();
        RefreshGrid();
        RefreshBackButton();
        RefreshPathText();
    }

    private void ShowFolder(MemoryFile folder)
    {
        folderStack.Push(folder);
        RefreshGrid();
        RefreshBackButton();
        RefreshPathText();
    }

    private void GoBack()
    {
        if (folderStack.Count == 0) return;
        folderStack.Pop();
        RefreshGrid();
        RefreshBackButton();
        RefreshPathText();
    }

    private void RefreshGrid()
    {
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        IReadOnlyList<MemoryFile> items = GetCurrentItems();
        foreach (MemoryFile file in items)
        {
            if (file.isDeleted) continue;

            GameObject go = Instantiate(desktopIconPrefab, gridRoot);
            DesktopIcon icon = go.GetComponent<DesktopIcon>();

            Sprite iconSprite = file.fileIcon;
            if (iconSprite == null)
                iconSprite = file.isFolder ? defaultFolderIcon : defaultFileIcon;

            icon.Setup(this, file, iconSprite);
        }
    }

    private IReadOnlyList<MemoryFile> GetCurrentItems()
    {
        if (folderStack.Count > 0)
            return folderStack.Peek().children;
        return FileSystemManager.Instance.DesktopItems;
    }

    private void RefreshBackButton()
    {
        if (backButton == null) return;
        backButton.gameObject.SetActive(folderStack.Count > 0);
    }

    private void RefreshSpaceText()
    {
        if (spaceText == null) return;
        spaceText.text = string.Format("剩余空间: {0} / {1} MB",
            FileSystemManager.Instance.AvailableSpace.ToString("F0"),
            FileSystemManager.Instance.TotalSpace.ToString("F0"));
    }

    private void RefreshPathText()
    {
        if (pathText == null) return;

        List<string> segments = new List<string>();
        foreach (MemoryFile folder in folderStack)
            segments.Add(folder.fileName);

        segments.Reverse();

        char separator = rootPathLabel.Contains("\\") ? '\\' : '/';

        string root = rootPathLabel;
        if (!root.EndsWith("/") && !root.EndsWith("\\"))
            root += separator;

        string path = root;
        if (segments.Count > 0)
            path += string.Join(separator.ToString(), segments);

        pathText.text = path;
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
        if (icon == null || icon.File == null) return;
        if (icon.File.isFolder)
            ShowFolder(icon.File);
    }

    public void OnIconDragBegin(DesktopIcon icon)
    {
        draggedIcon = icon;
    }

    public void OnIconDragEnd(DesktopIcon icon)
    {
        if (awaitingConfirm) return;

        if (IsPointerOverRecycleBin())
        {
            RequestConfirmDelete(icon);
            return;
        }

        StartCoroutine(AnimateIconBack(icon));
    }

    public void RequestConfirmDeleteFromBin()
    {
        if (draggedIcon != null && !awaitingConfirm)
            RequestConfirmDelete(draggedIcon);
    }

    private void RequestConfirmDelete(DesktopIcon icon)
    {
        if (icon == null || icon.File == null) return;

        awaitingConfirm = true;
        pendingIcon = icon;

        if (confirmMessageText != null)
            confirmMessageText.text = "确定要永久删除「" + icon.File.fileName + "」吗？";

        confirmPanel.SetActive(true);
    }

    public void OnConfirmYes()
    {
        awaitingConfirm = false;
        confirmPanel.SetActive(false);

        MemoryFile target = pendingIcon != null ? pendingIcon.File : null;
        pendingIcon = null;
        draggedIcon = null;

        if (target != null)
            FileSystemManager.Instance.TryDeleteFile(target);
    }

    public void OnConfirmNo()
    {
        awaitingConfirm = false;
        confirmPanel.SetActive(false);

        DesktopIcon icon = pendingIcon;
        pendingIcon = null;
        draggedIcon = null;

        if (icon != null)
            StartCoroutine(AnimateIconBack(icon));
    }

    private IEnumerator AnimateIconBack(DesktopIcon icon)
    {
        if (icon == null || icon.Rect == null) yield break;

        RectTransform rect = icon.Rect;
        Vector2 start = rect.anchoredPosition;
        Vector2 target = GetCellAnchoredPosition(icon.OriginalSiblingIndex);

        float duration = 0.2f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rect.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        icon.transform.SetSiblingIndex(icon.OriginalSiblingIndex);
    }

    private bool IsPointerOverRecycleBin()
    {
        if (recycleBinRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(recycleBinRect, Input.mousePosition, null);
    }

    private Vector2 GetCellAnchoredPosition(int index)
    {
        GridLayoutGroup grid = gridRoot.GetComponent<GridLayoutGroup>();
        if (grid == null) return Vector2.zero;

        Rect rect = gridRoot.rect;
        Vector2 cellStep = grid.cellSize + grid.spacing;

        int columns = Mathf.Max(1, Mathf.FloorToInt((rect.width + grid.spacing.x) / cellStep.x));
        int row = index / columns;
        int col = index % columns;

        Vector2 start = new Vector2(
            rect.xMin + grid.padding.left + grid.cellSize.x * 0.5f,
            rect.yMax - grid.padding.top - grid.cellSize.y * 0.5f);

        return start + new Vector2(col * cellStep.x, -row * cellStep.y);
    }
}
