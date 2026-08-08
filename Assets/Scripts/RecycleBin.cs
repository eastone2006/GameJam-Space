using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecycleBin : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private DesktopUIManager uiManager;

    private Image image;
    private Color normalColor;
    private readonly Color hoverColor = new Color(1f, 0.55f, 0.55f, 1f);

    private void Awake()
    {
        image = GetComponent<Image>();
        normalColor = image != null ? image.color : Color.white;
    }

    private void Start()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<DesktopUIManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        uiManager?.RequestConfirmDeleteFromBin();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (image != null)
            image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null)
            image.color = normalColor;
    }
}
