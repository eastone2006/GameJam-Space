using UnityEngine;
using UnityEngine.SceneManagement;

public class MemorySceneLauncher_B : MonoBehaviour
{
    [Header("Temporary Debug Keys")]
    [SerializeField] private bool enableDebugKeys = true;

    private void Update()
    {
        if (!enableDebugKeys)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            LoadBirthdayMemory();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadTravelMemory();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            LoadHospitalEnding();
        }
    }

    public void LoadBirthdayMemory()
{
    LoadWithTransition(
        "BirthdayScene_B",
        0.6f,
        0.8f
    );
}

public void LoadTravelMemory()
{
    LoadWithTransition(
        "TravelMemoryScene_B",
        0.6f,
        0.8f
    );
}

    public void LoadHospitalEnding()
    {
        LoadWithTransition(
            "HospitalScene_B",
            1f,
            0.15f
        );
    }

    private void LoadWithTransition(
        string sceneName,
        float fadeToWhiteDuration,
        float fadeFromWhiteDuration)
    {
        if (SceneTransitionManager_B.Instance != null)
        {
            SceneTransitionManager_B.Instance.TransitionToScene(
                sceneName,
                fadeToWhiteDuration,
                fadeFromWhiteDuration
            );
        }
        else
        {
            Debug.LogWarning(
                "Transition Manager was not found."
            );

            SceneManager.LoadScene(sceneName);
        }
    }
}