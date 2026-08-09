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
    [Tooltip("删除 AI 本体需要尝试的次数（期间 AI 会逃逸进随机文件夹）")]
    [SerializeField] private int aiDeleteAttemptsRequired = 3;

    public Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    canvas = FindObjectOfType<Canvas>();
            }
            return canvas;
        }
    }

    public RectTransform CanvasRect
    {
        get
        {
            Canvas resolved = Canvas;
            return resolved != null ? resolved.GetComponent<RectTransform>() : null;
        }
    }

    private readonly List<FolderWindow> windows = new List<FolderWindow>();
    private DesktopUIManager desktop;
    private DesktopIcon draggedIcon;
    private DesktopIcon pendingIcon;
    private bool awaitingConfirm;

    private static int aiDeleteAttempts;
    private bool aiAttemptPending;
    private MemoryFile aiPendingTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        desktop = FindObjectOfType<DesktopUIManager>(true);
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

        AITextDialogController.Instance?.NotifyFolderOpened(folder);

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

        if (folderIcon == null && targetHost == icon.Host)
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

        if (targetFolder == null && desktop == null)
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

        if (aiDeleteAttempts < aiDeleteAttemptsRequired && FileOrDescendantIsAi(icon.File))
        {
            aiPendingTarget = PickRandomFolder(icon.File);

            aiAttemptPending = true;
            awaitingConfirm = true;
            pendingIcon = icon;

            int attempt = aiDeleteAttempts + 1;
            string targetName = aiPendingTarget != null ? aiPendingTarget.fileName : "the desktop";

            if (confirmMessageText != null)
            {
                confirmMessageText.text = string.Format(
                    "CRITICAL WARNING: Unauthorized purge detected. Attempt {0}. Force termination may corrupt local reality. Proceed anyway?",
                    attempt, aiDeleteAttemptsRequired, targetName);
            }

            confirmPanel.SetActive(true);
            return;
        }

        awaitingConfirm = true;
        pendingIcon = icon;

        if (confirmMessageText != null)
            confirmMessageText.text = "Are you sure you want to permanently delete \"" + icon.File.fileName + "\"?";

        confirmPanel.SetActive(true);
    }

    public void OnConfirmYes()
    {
        if (aiAttemptPending)
        {
            aiAttemptPending = false;
            awaitingConfirm = false;
            confirmPanel.SetActive(false);

            DesktopIcon icon = pendingIcon;
            pendingIcon = null;
            draggedIcon = null;

            if (icon == null || icon.File == null) return;

            aiDeleteAttempts++;

            if (aiDeleteAttempts >= aiDeleteAttemptsRequired)
            {
                FileSystemManager.Instance.TryDeleteFile(icon.File);
                Destroy(icon.gameObject);
            }
            else
            {
                MemoryFile target = aiPendingTarget;
                aiPendingTarget = null;

                if (target != null)
                    FileSystemManager.Instance.MoveFileToFolder(icon.File, target);

                RefreshAll();
                Destroy(icon.gameObject);
            }
            return;
        }

        awaitingConfirm = false;
        confirmPanel.SetActive(false);

        DesktopIcon normalIcon = pendingIcon; // 变量名避开冲突
        pendingIcon = null;
        draggedIcon = null;

        if (normalIcon != null && normalIcon.File != null)
        {
            FileSystemManager.Instance.TryDeleteFile(normalIcon.File);
            Destroy(normalIcon.gameObject);
        }
    }

    public void OnConfirmNo()
    {
        if (aiAttemptPending)
        {
            aiAttemptPending = false;
            awaitingConfirm = false;
            confirmPanel.SetActive(false);

            DesktopIcon icon = pendingIcon;
            pendingIcon = null;
            draggedIcon = null;
            aiPendingTarget = null;

            if (icon != null)
                icon.ReturnToHost();
            return;
        }

        awaitingConfirm = false;
        confirmPanel.SetActive(false);

        DesktopIcon cancelIcon = pendingIcon;
        pendingIcon = null;
        draggedIcon = null;

        if (cancelIcon != null)
            cancelIcon.ReturnToHost();
    }

    private MemoryFile PickRandomFolder(MemoryFile aiFile)
    {
        List<MemoryFile> candidates = new List<MemoryFile>();
        CollectExistingFolders(FileSystemManager.Instance.GetRootItems(), candidates);

        MemoryFile currentParent = FileSystemManager.Instance.FindParentFolder(aiFile);
        if (currentParent != null)
            candidates.Remove(currentParent);

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void CollectExistingFolders(IList<MemoryFile> items, List<MemoryFile> folders)
    {
        foreach (MemoryFile item in items)
        {
            if (item.isDeleted || !item.isFolder) continue;

            // 跳过核心记忆文件夹及其整个子树，AI 不会躲进重要文件夹
            if (item.type == MemoryType.CoreMemory) continue;

            folders.Add(item);
            CollectExistingFolders(item.children, folders);
        }
    }

    private bool FileOrDescendantIsAi(MemoryFile file)
    {
        if (file == null) return false;
        if (file.memoryId == "ai") return true;

        foreach (MemoryFile child in file.children)
        {
            if (FileOrDescendantIsAi(child))
                return true;
        }
        return false;
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