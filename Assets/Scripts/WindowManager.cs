using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform windowsContainer;
    [SerializeField] private RectTransform desktopArea;
    [SerializeField] private RectTransform recycleBinRect;

    [Header("Window Prefab")]
    [SerializeField] private GameObject folderWindowPrefab;

    [Header("Confirm Dialog")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmMessageText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Lifecycle")]
    [SerializeField] private bool persistAcrossScenes = true;

    public Canvas Canvas => canvas;
    public RectTransform CanvasRect => canvas != null ? canvas.GetComponent<RectTransform>() : null;

    private readonly List<FolderWindow> windows = new List<FolderWindow>();
    private DesktopUIManager desktop;
    private DesktopIcon draggedIcon;
    private DesktopIcon pendingIcon;
    private bool awaitingConfirm;

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
    }

    private void Start()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        desktop = FindObjectOfType<DesktopUIManager>();
        if (desktopArea == null && desktop != null)
            desktopArea = desktop.ContentRoot;

        confirmYesButton.onClick.AddListener(OnConfirmYes);
        confirmNoButton.onClick.AddListener(OnConfirmNo);
        confirmPanel.SetActive(false);

        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnFileDeleted += HandleFileDeleted;
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnFileDeleted -= HandleFileDeleted;

        if (Instance == this)
            Instance = null;
    }

    private void HandleFileDeleted(MemoryFile file)
    {
        List<FolderWindow> toClose = new List<FolderWindow>();
        foreach (FolderWindow w in windows)
        {
            if (w.Folder.isDeleted)
                toClose.Add(w);
        }
        foreach (FolderWindow w in toClose)
            CloseWindow(w);

        RefreshAll();
    }

    public void OpenFolderWindow(MemoryFile folder)
    {
        if (folder == null) return;

        FolderWindow existing = FindWindow(folder);
        if (existing != null)
        {
            BringToFront(existing);
            return;
        }

        GameObject go = Instantiate(folderWindowPrefab, windowsContainer);
        FolderWindow window = go.GetComponent<FolderWindow>();

        string rootLabel = desktop != null ? desktop.RootPathLabel : "Desktop/";
        window.Initialize(folder, rootLabel);

        windows.Add(window);
        BringToFront(window);
    }

    public void CloseWindow(FolderWindow window)
    {
        if (window == null) return;

        windows.Remove(window);
        Destroy(window.gameObject);
    }

    public void BringToFront(FolderWindow window)
    {
        if (window == null) return;
        window.transform.SetAsLastSibling();
    }

    private FolderWindow FindWindow(MemoryFile folder)
    {
        foreach (FolderWindow w in windows)
        {
            if (w.Folder == folder)
                return w;
        }
        return null;
    }

    public void RefreshAll()
    {
        if (desktop != null)
        {
            desktop.RefreshIcons();
            desktop.RefreshSpaceText();
        }

        foreach (FolderWindow w in windows)
            w.RefreshIcons();
    }

    public void OnDragBegin(DesktopIcon icon)
    {
        draggedIcon = icon;
    }

    public void OnDragEnd(DesktopIcon icon)
    {
        if (awaitingConfirm) return;

        Vector2 pointer = Input.mousePosition;
        List<RaycastResult> results = RaycastUI(pointer);

        DesktopIcon folderIcon = FindFolderIconTarget(results, icon);
        if (folderIcon != null)
        {
            HandleContainerDrop(icon, folderIcon.Host, folderIcon.File, folderIcon);
            return;
        }

        if (IsPointerOverRecycleBin(pointer))
        {
            RequestConfirmDelete(icon);
            return;
        }

        FolderWindow window = FindWindowTarget(results);
        if (window != null)
        {
            HandleContainerDrop(icon, window, window.Folder, null);
            return;
        }

        if (IsPointerOverDesktopArea(pointer))
        {
            HandleContainerDrop(icon, desktop, null, null);
            return;
        }

        icon.ReturnToHost();
    }

    private void HandleContainerDrop(DesktopIcon icon, IIconHost targetHost, MemoryFile targetFolder, DesktopIcon folderIcon)
    {
        MemoryFile file = icon.File;
        if (file == null)
        {
            icon.ReturnToHost();
            return;
        }

        if (targetHost == icon.Host)
        {
            icon.ReturnToHost();
            return;
        }

        if (file == targetFolder)
        {
            icon.ReturnToHost();
            return;
        }

        if (targetFolder != null && FileSystemManager.Instance.IsWithinSubtree(file, targetFolder))
        {
            icon.ReturnToHost();
            return;
        }

        FileSystemManager.Instance.MoveFileToFolder(file, targetFolder);

        RefreshAll();
        Destroy(icon.gameObject);
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

        DesktopIcon icon = pendingIcon;
        pendingIcon = null;
        draggedIcon = null;

        if (icon != null && icon.File != null)
        {
            FileSystemManager.Instance.TryDeleteFile(icon.File);
            Destroy(icon.gameObject);
        }
    }

    public void OnConfirmNo()
    {
        awaitingConfirm = false;
        confirmPanel.SetActive(false);

        DesktopIcon icon = pendingIcon;
        pendingIcon = null;
        draggedIcon = null;

        if (icon != null)
            icon.ReturnToHost();
    }

    public void ClampIconToDesktop(DesktopIcon icon)
    {
        if (icon == null || icon.Rect == null || desktopArea == null) return;

        RectTransform iconRect = icon.Rect;
        Vector3 position = iconRect.position;

        Vector3[] desktopCorners = new Vector3[4];
        desktopArea.GetWorldCorners(desktopCorners);
        Vector3 bottomLeft = desktopCorners[0];
        Vector3 topRight = desktopCorners[2];

        float halfWidth = iconRect.rect.width * 0.5f;
        float halfHeight = iconRect.rect.height * 0.5f;

        Vector3 right = iconRect.right;
        Vector3 up = iconRect.up;

        Vector3 iconBottomLeft = position - right * halfWidth - up * halfHeight;
        Vector3 iconTopRight = position + right * halfWidth + up * halfHeight;

        Vector3 delta = Vector3.zero;
        if (iconBottomLeft.x < bottomLeft.x) delta.x += bottomLeft.x - iconBottomLeft.x;
        if (iconTopRight.x > topRight.x) delta.x -= iconTopRight.x - topRight.x;
        if (iconBottomLeft.y < bottomLeft.y) delta.y += bottomLeft.y - iconBottomLeft.y;
        if (iconTopRight.y > topRight.y) delta.y -= iconTopRight.y - topRight.y;

        iconRect.position += delta;
    }

    private bool IsPointerOverRecycleBin(Vector2 pointer)
    {
        if (recycleBinRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(recycleBinRect, pointer, null);
    }

    private bool IsPointerOverDesktopArea(Vector2 pointer)
    {
        if (desktopArea == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(desktopArea, pointer, null);
    }

    private List<RaycastResult> RaycastUI(Vector2 pointer)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current == null) return results;

        PointerEventData data = new PointerEventData(EventSystem.current) { position = pointer };
        EventSystem.current.RaycastAll(data, results);
        return results;
    }

    private DesktopIcon FindFolderIconTarget(List<RaycastResult> results, DesktopIcon dragged)
    {
        foreach (RaycastResult r in results)
        {
            DesktopIcon icon = r.gameObject.GetComponent<DesktopIcon>();
            if (icon != null && icon != dragged && icon.File != null && icon.File.isFolder)
                return icon;
        }
        return null;
    }

    private FolderWindow FindWindowTarget(List<RaycastResult> results)
    {
        foreach (RaycastResult r in results)
        {
            FolderWindow window = r.gameObject.GetComponentInParent<FolderWindow>();
            if (window != null)
                return window;
        }
        return null;
    }
}
