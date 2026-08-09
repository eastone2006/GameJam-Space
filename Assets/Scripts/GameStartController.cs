using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameStartController : MonoBehaviour
{
    public static GameStartController Instance { get; private set; }

    public static bool HasStarted { get; private set; }
    public static bool IntroPlayed { get; private set; }

    [Header("Start Panel")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button startButton;

    [Header("Player")]
    [Tooltip("可选：点 Start Game 前禁用玩家控制，开局动画结束后恢复")]
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private float controlEnableTimeout = 8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (!HasStarted)
        {
            if (startPanel != null)
                startPanel.SetActive(true);

            if (startButton != null)
                startButton.onClick.AddListener(StartGame);

            if (playerController != null)
                playerController.SetControlsEnabled(false);

            // 未开始时鼠标必须可见可点，才能点击 Start 按钮
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (startPanel != null)
        {
            startPanel.SetActive(false);
        }
    }

    public void StartGame()
    {
        if (HasStarted) return;
        HasStarted = true;

        if (startPanel != null)
            startPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StartCoroutine(EnableControlsAfterIntro());
    }

    private IEnumerator EnableControlsAfterIntro()
    {
        float timeout = 0f;
        while (!IntroPlayed && timeout < controlEnableTimeout)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        if (playerController != null)
            playerController.SetControlsEnabled(true);
    }

    public static void MarkIntroPlayed()
    {
        IntroPlayed = true;
    }

    public static IEnumerator RunAfterStart(IEnumerator routine)
    {
        while (Instance != null && !HasStarted)
            yield return null;
        yield return routine;
    }
}
