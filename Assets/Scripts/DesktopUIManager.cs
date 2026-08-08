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

    [Header("Desktop Layout")]
    [SerializeField] private KeyCode resetLayoutKey = KeyCode.R;
    [SerializeField] private int iconsPerRow = 5;
    [SerializeField] private float horizontalSpacing = 20f;
    [SerializeField] private float verticalSpacing = 20f;
    [SerializeField] private float paddingTop = 20f;
    [SerializeField] private float paddingLeft = 20f;

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

    private void Update()
    {
        if (Input.GetKeyDown(resetLayoutKey))
            ResetDesktopLayout();
    }

    private void HandleFileDeleted(MemoryFile file)
    {
        DesktopIcon icon = FindIconByFile(file);
        if (icon != null)
        {
            if (selectedIcon == icon)
                selectedIcon = null;

            Destroy(icon.gameObject);
        }

        RefreshSpaceText();
    }

    private DesktopIcon FindIconByFile(MemoryFile file)
    {
        foreach (Transform child in gridRoot)
        {
            DesktopIcon icon = child.GetComponent<DesktopIcon>();
            if (icon != null && icon.File == file)
                return icon;
        }
        return null;
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
        int index = 0;
        foreach (MemoryFile file in items)
        {
            if (file.isDeleted) continue;

            GameObject go = Instantiate(desktopIconPrefab, gridRoot);
            DesktopIcon icon = go.GetComponent<DesktopIcon>();

            Sprite iconSprite = file.fileIcon;
            if (iconSprite == null)
                iconSprite = file.isFolder ? defaultFolderIcon : defaultFileIcon;

            icon.Setup(this, file, iconSprite);

            RectTransform rect = icon.Rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 gridPosition = GetGridPosition(index);
            icon.SetDefaultPosition(gridPosition);

            if (file.hasSavedPosition)
                rect.anchoredPosition = file.savedPosition;
            else
                rect.anchoredPosition = gridPosition;

            index++;
        }
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
        spaceText.text = string.Format("Available Space: {0} / {1} MB",
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
            confirmMessageText.text = "Are you sure you want to permanently delete \"" + icon.File.fileName + "\"?";

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
            StartCoroutine(AnimateIconTo(icon, icon.DragStartPosition));
    }

    private IEnumerator AnimateIconTo(DesktopIcon icon, Vector2 targetPosition)
    {
        if (icon == null || icon.Rect == null) yield break;

        RectTransform rect = icon.Rect;
        Vector2 start = rect.anchoredPosition;

        float duration = 0.2f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rect.anchoredPosition = Vector2.Lerp(start, targetPosition, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        rect.anchoredPosition = targetPosition;
        icon.SavePosition();
    }

    public Rect GetDesktopBounds()
    {
        return gridRoot.rect;
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

    private bool IsPointerOverRecycleBin()
    {
        if (recycleBinRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(recycleBinRect, Input.mousePosition, null);
    }
}
