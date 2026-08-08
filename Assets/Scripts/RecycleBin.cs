using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecycleBin : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Image image;
    private Color normalColor;
    private readonly Color hoverColor = new Color(1f, 0.55f, 0.55f, 1f);

    private void Awake()
    {
        image = GetComponent<Image>();
        normalColor = image != null ? image.color : Color.white;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (WindowManager.Instance != null)
            WindowManager.Instance.RequestConfirmDeleteFromBin();
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
