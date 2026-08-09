using UnityEngine;
using UnityEngine.EventSystems;

public class WindowHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public enum HandleMode { Move, Resize }

    [Tooltip("Move=标题栏拖拽移动；Resize=右下角缩放")]
    [SerializeField] private HandleMode mode = HandleMode.Move;

    public void OnBeginDrag(PointerEventData eventData)
    {
        FolderWindow window = GetComponentInParent<FolderWindow>();
        if (window == null) return;

        if (mode == HandleMode.Move)
            window.BeginMoveWindow();
        else
            window.BeginResizeWindow(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        FolderWindow window = GetComponentInParent<FolderWindow>();
        if (window == null) return;

        if (mode == HandleMode.Move)
            window.MoveWindow(eventData.delta);
        else
            window.ResizeWindow(eventData.delta);
    }
}
