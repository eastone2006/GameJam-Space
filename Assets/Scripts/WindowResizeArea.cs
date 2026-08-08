using UnityEngine;
using UnityEngine.EventSystems;

public class WindowResizeArea : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        FolderWindow window = GetComponentInParent<FolderWindow>();
        if (window != null)
            window.BeginResizeWindow(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        FolderWindow window = GetComponentInParent<FolderWindow>();
        if (window != null)
            window.ResizeWindow(eventData.delta);
    }
}
