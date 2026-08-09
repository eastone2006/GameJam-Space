using System.Collections.Generic;
using UnityEngine;

public abstract class IconHostBase : MonoBehaviour, IIconHost
{
    [Header("Icon Display")]
    [SerializeField] protected GameObject iconPrefab;
    [SerializeField] protected Sprite defaultFolderIcon;
    [SerializeField] protected Sprite defaultFileIcon;

    [Header("Icon Layout")]
    [SerializeField] protected int iconsPerRow = 5;
    [SerializeField] protected float horizontalSpacing = 45f;
    [SerializeField] protected float verticalSpacing = 45f;
    [SerializeField] protected float paddingTop = 24f;
    [SerializeField] protected float paddingLeft = 24f;
    [SerializeField] protected float iconScale = 1.5f;

    public abstract RectTransform ContentRoot { get; }
    public abstract IList<MemoryFile> CurrentItems { get; }

    protected DesktopIcon selectedIcon;

    public void RefreshIcons()
    {
        for (int i = ContentRoot.childCount - 1; i >= 0; i--)
            Destroy(ContentRoot.GetChild(i).gameObject);
        selectedIcon = null;

        int index = 0;
        foreach (MemoryFile file in CurrentItems)
        {
            if (file.isDeleted) continue;

            CreateIcon(file, index);
            index++;
        }
    }

    protected virtual void CreateIcon(MemoryFile file, int index)
    {
        GameObject go = Instantiate(iconPrefab, ContentRoot);
        DesktopIcon icon = go.GetComponent<DesktopIcon>();

        Sprite sprite = file.fileIcon;
        if (sprite == null)
            sprite = file.isFolder ? defaultFolderIcon : defaultFileIcon;

        icon.Setup(this, file, sprite);

        RectTransform rect = icon.Rect;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.localScale = Vector3.one * iconScale;

        Vector2 gridPosition = GetGridPosition(index);
        icon.SetDefaultPosition(gridPosition);

        rect.anchoredPosition = file.hasSavedPosition ? file.savedPosition : gridPosition;
    }

    protected Vector2 GetScaledIconSize()
    {
        Vector2 baseSize = iconPrefab.GetComponent<RectTransform>().rect.size;
        return baseSize * iconScale;
    }

    protected Vector2 GetGridPosition(int index)
    {
        Rect bounds = ContentRoot.rect;
        Vector2 iconSize = GetScaledIconSize();
        float cellWidth = iconSize.x + horizontalSpacing;
        float cellHeight = iconSize.y + verticalSpacing;

        int columns = Mathf.Max(1, iconsPerRow);
        int row = index / columns;
        int col = index % columns;

        float startX = bounds.xMin + paddingLeft + iconSize.x * 0.5f;
        float startY = bounds.yMax - paddingTop - iconSize.y * 0.5f;

        return new Vector2(startX + col * cellWidth, startY - row * cellHeight);
    }

    public void OnIconClicked(DesktopIcon icon)
    {
        if (icon == null || icon == selectedIcon) return;

        selectedIcon = icon;
        foreach (Transform child in ContentRoot)
        {
            DesktopIcon existing = child.GetComponent<DesktopIcon>();
            if (existing != null)
                existing.SetSelected(existing == icon);
        }
    }

    public void OnIconDoubleClicked(DesktopIcon icon)
    {
        if (icon == null || icon.File == null || !icon.File.isFolder) return;
        WindowManager.Instance.OpenFolderWindow(icon.File);
    }
}
