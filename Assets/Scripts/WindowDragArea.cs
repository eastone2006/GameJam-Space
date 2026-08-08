using UnityEngine;
using UnityEngine.EventSystems;

public class WindowDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        FolderWindow window = GetComponentInParent<FolderWindow>();
        if (window != null)
            window.BeginMoveWindow();
    }

    public void OnDrag(PointerEventData eventData)
    {
        FolderWindow window = GetComponentInParent<FolderWindow>();
        if (window != null)
            window.MoveWindow(eventData.delta);
    }
}
