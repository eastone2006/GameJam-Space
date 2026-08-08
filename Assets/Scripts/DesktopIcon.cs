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
    public int OriginalSiblingIndex { get; private set; }
    public bool IsDragging { get; private set; }

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
            nameText.text = file.fileName;
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = selected
            ? new Color(0.7f, 0.85f, 1f, 0.9f)
            : new Color(1f, 1f, 1f, 0.9f);
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

        OriginalSiblingIndex = transform.GetSiblingIndex();
        IsDragging = true;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        manager.OnIconDragBegin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging || manager == null) return;
        Rect.anchoredPosition += eventData.delta / manager.Canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        IsDragging = false;
        canvasGroup.blocksRaycasts = true;
        manager?.OnIconDragEnd(this);
    }
}
