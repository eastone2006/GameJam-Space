using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FolderWindow : MonoBehaviour, IPointerDownHandler, IIconHost
{
    [Header("Window Structure")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private RectTransform contentRoot;

    [Header("Icon Display")]
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private Sprite defaultFolderIcon;
    [SerializeField] private Sprite defaultFileIcon;

    [Header("Window Layout")]
    [SerializeField] private int iconsPerRow = 5;
    [SerializeField] private float horizontalSpacing = 12f;
    [SerializeField] private float verticalSpacing = 12f;
    [SerializeField] private float paddingTop = 10f;
    [SerializeField] private float paddingLeft = 10f;
    [SerializeField] private Vector2 minSize = new Vector2(200f, 220f);

    public MemoryFile Folder { get; private set; }
    public RectTransform ContentRoot => contentRoot;
    public IList<MemoryFile> CurrentItems => Folder != null ? Folder.children : s_empty;

    private static readonly List<MemoryFile> s_empty = new List<MemoryFile>();

    private RectTransform rect;
    private DesktopIcon selectedIcon;

    private enum DragMode { None, Move, Resize }
    private DragMode dragMode = DragMode.None;
    private Vector2 resizeStartSize;
    private Vector2 resizeStartPosition;
    private Vector2 resizeStartPointer;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Initialize(MemoryFile folder, string rootPathLabel)
    {
        Folder = folder;

        if (titleText != null)
            titleText.text = BuildPathText(rootPathLabel);

        RefreshIcons();
    }

    private string BuildPathText(string rootPathLabel)
    {
        string relative = FileSystemManager.Instance != null
            ? FileSystemManager.Instance.GetFolderPath(Folder)
            : (Folder != null ? Folder.fileName : string.Empty);

        char separator = rootPathLabel.Contains("\\") ? '\\' : '/';

        string root = rootPathLabel;
        if (!root.EndsWith("/") && !root.EndsWith("\\"))
            root += separator;

        return string.IsNullOrEmpty(relative) ? root : root + relative;
    }

    public void Close()
    {
        if (WindowManager.Instance != null)
            WindowManager.Instance.CloseWindow(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (WindowManager.Instance != null)
            WindowManager.Instance.BringToFront(this);
    }

    public void BeginMoveWindow()
    {
        if (WindowManager.Instance != null)
            WindowManager.Instance.BringToFront(this);

        dragMode = DragMode.Move;
    }

    public void MoveWindow(Vector2 delta)
    {
        if (dragMode != DragMode.Move || WindowManager.Instance == null) return;

        rect.anchoredPosition += delta / WindowManager.Instance.Canvas.scaleFactor;
        ClampToScreen();
    }

    public void BeginResizeWindow(Vector2 pointer)
    {
        if (WindowManager.Instance != null)
            WindowManager.Instance.BringToFront(this);

        dragMode = DragMode.Resize;
        resizeStartSize = rect.sizeDelta;
        resizeStartPosition = rect.anchoredPosition;
        resizeStartPointer = pointer;
    }

    public void ResizeWindow(Vector2 delta)
    {
        if (dragMode != DragMode.Resize || WindowManager.Instance == null) return;

        Vector2 localDelta = delta / WindowManager.Instance.Canvas.scaleFactor;

        float newWidth = Mathf.Max(minSize.x, resizeStartSize.x + localDelta.x);
        float newHeight = Mathf.Max(minSize.y, resizeStartSize.y - localDelta.y);

        float dWidth = newWidth - resizeStartSize.x;
        float dHeight = newHeight - resizeStartSize.y;

        rect.sizeDelta = new Vector2(newWidth, newHeight);
        rect.anchoredPosition = resizeStartPosition + new Vector2(dWidth * 0.5f, -dHeight * 0.5f);
    }

    public void RefreshIcons()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
        selectedIcon = null;

        if (Folder == null) return;

        int index = 0;
        foreach (MemoryFile file in Folder.children)
        {
            if (file.isDeleted) continue;

            CreateIcon(file, index);
            index++;
        }
    }

    private void CreateIcon(MemoryFile file, int index)
    {
        GameObject go = Instantiate(iconPrefab, contentRoot);
        DesktopIcon icon = go.GetComponent<DesktopIcon>();

        Sprite sprite = file.fileIcon;
        if (sprite == null)
            sprite = file.isFolder ? defaultFolderIcon : defaultFileIcon;

        icon.Setup(this, file, sprite);

        RectTransform iconRect = icon.Rect;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 gridPosition = GetGridPosition(index);
        icon.SetDefaultPosition(gridPosition);

        iconRect.anchoredPosition = file.hasSavedPosition ? file.savedPosition : gridPosition;
    }

    private Vector2 GetGridPosition(int index)
    {
        Rect bounds = contentRoot.rect;
        Vector2 iconSize = iconPrefab.GetComponent<RectTransform>().sizeDelta;
        float cellWidth = iconSize.x + horizontalSpacing;
        float cellHeight = iconSize.y + verticalSpacing;

        int columns = Mathf.Max(1, iconsPerRow);
        int row = index / columns;
        int col = index % columns;

        float startX = bounds.xMin + paddingLeft + iconSize.x * 0.5f;
        float startY = bounds.yMax - paddingTop - iconSize.y * 0.5f;

        return new Vector2(startX + col * cellWidth, startY - row * cellHeight);
    }

    public void OnIconClicked(DesktopIcon icon)
    {
        if (icon == null || icon == selectedIcon) return;

        selectedIcon = icon;
        foreach (Transform child in contentRoot)
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

    private void ClampToScreen()
    {
        if (WindowManager.Instance == null || WindowManager.Instance.CanvasRect == null) return;

        Vector3[] canvasCorners = new Vector3[4];
        WindowManager.Instance.CanvasRect.GetWorldCorners(canvasCorners);
        Vector3 canvasBottomLeft = canvasCorners[0];
        Vector3 canvasTopRight = canvasCorners[2];

        Vector3[] windowCorners = new Vector3[4];
        rect.GetWorldCorners(windowCorners);
        Vector3 windowBottomLeft = windowCorners[0];
        Vector3 windowTopRight = windowCorners[2];

        Vector3 delta = Vector3.zero;
        if (windowBottomLeft.x < canvasBottomLeft.x) delta.x += canvasBottomLeft.x - windowBottomLeft.x;
        if (windowTopRight.x > canvasTopRight.x) delta.x -= windowTopRight.x - canvasTopRight.x;
        if (windowBottomLeft.y < canvasBottomLeft.y) delta.y += canvasBottomLeft.y - windowBottomLeft.y;
        if (windowTopRight.y > canvasTopRight.y) delta.y -= windowTopRight.y - canvasTopRight.y;

        rect.position += delta;
    }
}
