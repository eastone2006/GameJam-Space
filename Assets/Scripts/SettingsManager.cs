using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("UI 引用")]
    public Slider volumeSlider;
    public Toggle muteToggle;
    private float previousVolume = 1f;

    void Start()
    {
        
        List<string> options = new List<string>();
        int currentResIndex = 0;


        // 2. 游戏启动时，读取并应用玩家保存的设置
        LoadSettings(currentResIndex);
    }

    // --- 读取逻辑 ---
    private void LoadSettings(int defaultResIndex)
    {
        // 读取音量：如果没有保存过（第一次玩），默认给 1f (最大声)
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        volumeSlider.value = savedVolume;
        AudioListener.volume = savedVolume;

        // 读取静音状态：PlayerPrefs 不支持 bool，我们用 0 代表 false，1 代表 true
        int savedMute = PlayerPrefs.GetInt("IsMuted", 0);
        muteToggle.isOn = (savedMute == 1);
        if (savedMute == 1) AudioListener.volume = 0f;
    }

    // --- 保存逻辑 (在改变 UI 时触发) ---

    // 1. 设置并保存主音量
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        muteToggle.isOn = (volume == 0);
        
        // 存入硬盘
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save(); 
    }

    // 2. 设置并保存静音
    public void SetMute(bool isMuted)
    {
        if (isMuted)
        {
            previousVolume = AudioListener.volume > 0 ? AudioListener.volume : 1f;
            AudioListener.volume = 0f;
            volumeSlider.value = 0f; 
        }
        else
        {
            AudioListener.volume = previousVolume;
            volumeSlider.value = previousVolume; 
        }

        // 存入硬盘 (true 存为 1，false 存为 0)
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }
}