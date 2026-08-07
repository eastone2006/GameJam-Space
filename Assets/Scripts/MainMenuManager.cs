using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    // 1. 开始游戏逻辑
    public void StartGame()
    {
        // 加载游戏场景。请确保将游戏主场景添加到了 File -> Build Settings 中
        // 这里的 "1" 是游戏场景的索引（假设主菜单是 0）
        SceneManager.LoadScene(1);
    }

    // 2. 打开设置界面
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // 3. 打开存档界面
    public void OpenSaveLoad()
    {
        mainMenuPanel.SetActive(false);
    }

    // 4. 返回主菜单（用于设置和存档界面）
    public void BackToMainMenu()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // 附加：退出游戏
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");
        Application.Quit();
    }
}