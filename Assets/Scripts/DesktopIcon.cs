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
    public RectTransform Rect { get; private set; }
    public bool IsDragging { get; private set; }
    public Vector2 DragStartPosition { get; private set; }
    public Vector2 DefaultPosition { get; private set; }

    private CanvasGroup canvasGroup;
    private DesktopUIManager manager;

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(DesktopUIManager uiManager, MemoryFile file, Sprite iconSprite)
    {
        manager = uiManager;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null) return;

        if (eventData.clickCount >= 2)
            manager.OnIconDoubleClicked(this);
        else
            manager.OnIconClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager == null) return;

        DragStartPosition = Rect.anchoredPosition;
        IsDragging = true;
        canvasGroup.blocksRaycasts = false;
        manager.OnIconDragBegin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging || manager == null) return;

        Rect bounds = manager.GetDesktopBounds();
        float halfWidth = Rect.rect.width * 0.5f;
        float halfHeight = Rect.rect.height * 0.5f;

        float minX = bounds.xMin + halfWidth;
        float maxX = bounds.xMax - halfWidth;
        float minY = bounds.yMin + halfHeight;
        float maxY = bounds.yMax - halfHeight;

        Vector2 position = Rect.anchoredPosition + eventData.delta / manager.Canvas.scaleFactor;
        position.x = Mathf.Clamp(position.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
        position.y = Mathf.Clamp(position.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));

        Rect.anchoredPosition = position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        IsDragging = false;
        canvasGroup.blocksRaycasts = true;
        SavePosition();
        manager?.OnIconDragEnd(this);
    }

    public void SavePosition()
    {
        if (File == null) return;
        File.savedPosition = Rect.anchoredPosition;
        File.hasSavedPosition = true;
    }
}
