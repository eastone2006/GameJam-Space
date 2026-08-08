using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class DesktopIcon : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;

    public MemoryFile File { get; private set; }
    public IIconHost Host { get; private set; }
    public RectTransform Rect { get; private set; }
    public bool IsDragging { get; private set; }
    public Vector2 DragStartPosition { get; private set; }
    public Vector2 DefaultPosition { get; private set; }

    private CanvasGroup canvasGroup;
    private Transform originalParent;

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(IIconHost host, MemoryFile file, Sprite iconSprite)
    {
        Host = host;
        File = file;

        if (backgroundImage != null && iconSprite != null)
            backgroundImage.sprite = iconSprite;

        if (nameText != null)
            nameText.text = file.isFolder
                ? string.Format("{0}\n{1} MB", file.fileName, file.TotalSize.ToString("F0"))
                : string.Format("{0}\n{1} MB", file.fileName, file.size.ToString("F0"));
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = selected
            ? new Color(0.7f, 0.85f, 1f, 0.9f)
            : new Color(1f, 1f, 1f, 0.9f);
    }

    public void SetDefaultPosition(Vector2 position)
    {
        DefaultPosition = position;
    }

    public void SavePosition()
    {
        if (File == null) return;
        File.savedPosition = Rect.anchoredPosition;
        File.hasSavedPosition = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Host == null) return;

        if (eventData.clickCount >= 2)
        {
            Host.OnIconDoubleClicked(this);
        }
        else
        {
            if (File != null)
                AITextDialogController.Instance?.NotifyFileClicked(File);
            Host.OnIconClicked(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Host == null || WindowManager.Instance == null) return;

        DragStartPosition = Rect.anchoredPosition;
        originalParent = transform.parent;
        IsDragging = true;
        canvasGroup.blocksRaycasts = false;

        if (WindowManager.Instance.CanvasRect != null)
            transform.SetParent(WindowManager.Instance.CanvasRect, true);
        transform.SetAsLastSibling();

        WindowManager.Instance.OnDragBegin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging || WindowManager.Instance == null) return;

        Rect.anchoredPosition += eventData.delta / WindowManager.Instance.Canvas.scaleFactor;
        WindowManager.Instance.ClampIconToDesktop(this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        IsDragging = false;
        WindowManager.Instance.OnDragEnd(this);
    }

    public void ReturnToHost()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
            originalParent = null;
        }

        canvasGroup.blocksRaycasts = true;
        SavePosition();
    }
}
