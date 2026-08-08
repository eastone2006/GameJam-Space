using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnCoreMemoryDeleted += HandleCoreMemoryDeleted;
    }

    private void OnDestroy()
    {
        if (FileSystemManager.Instance != null)
            FileSystemManager.Instance.OnCoreMemoryDeleted -= HandleCoreMemoryDeleted;
    }

    private void HandleCoreMemoryDeleted(MemoryFile file)
    {
        switch (file.memoryId)
        {
            case "birthday":
                Debug.Log("你删除了【生日】——主角失去了童年的快乐回忆");
                break;
            case "mothers_voice":
                Debug.Log("你删除了【母亲的声音】");
                break;
            case "graduation":
                Debug.Log("你删除了【毕业典礼】");
                break;
            case "travel":
                Debug.Log("你删除了【旅行回忆】");
                break;
            case "ai":
                Debug.Log("你删除了【AI】——系统面临失衡，结局走向未知");
                break;
            default:
                Debug.Log("删除了核心记忆: " + file.fileName);
                break;
        }
    }
}
