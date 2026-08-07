using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    void Awake()
    {
        // 单例模式：确保每次回主菜单不会生成多个音乐播放器
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 核心代码：过场景不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }
}