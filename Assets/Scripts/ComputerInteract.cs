using System.Collections;
using UnityEngine;

public class ComputerInteract : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSPlayerController playerController;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject desktopUI;
    [SerializeField] private GameObject promptUI;

    [Header("Computer Targets")]
    [SerializeField] private Transform screenAnchor;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private bool instantAlign = false;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;

    public bool IsUsingComputer { get; private set; }
    public bool IsTransitioning { get; private set; }

    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;

    private void Start()
    {
        desktopUI.SetActive(false);
        if (promptUI != null)
            promptUI.SetActive(false);

        if (GameStartController.Instance != null && !GameStartController.HasStarted)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (IsTransitioning) return;

        if (IsUsingComputer)
        {
            if (Input.GetKeyDown(exitKey) || Input.GetKeyDown(interactKey))
                StartCoroutine(ExitComputerRoutine());
            return;
        }

        bool nearComputer = IsNearComputer();
        if (promptUI != null)
            promptUI.SetActive(nearComputer);

        if (nearComputer && Input.GetKeyDown(interactKey))
            StartCoroutine(EnterComputerRoutine());
    }

    private IEnumerator EnterComputerRoutine()
    {
        IsTransitioning = true;
        playerController.SetControlsEnabled(false);

        savedCameraPosition = playerCamera.position;
        savedCameraRotation = playerCamera.rotation;

        if (screenAnchor != null)
        {
            if (instantAlign)
            {
                playerCamera.SetPositionAndRotation(screenAnchor.position, screenAnchor.rotation);
            }
            else
            {
                Vector3 startPosition = playerCamera.position;
                Quaternion startRotation = playerCamera.rotation;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / transitionDuration;
                    float k = Mathf.SmoothStep(0f, 1f, t);

                    playerCamera.position = Vector3.Lerp(startPosition, screenAnchor.position, k);
                    playerCamera.rotation = Quaternion.Slerp(startRotation, screenAnchor.rotation, k);
                    yield return null;
                }

                playerCamera.SetPositionAndRotation(screenAnchor.position, screenAnchor.rotation);
            }
        }

        desktopUI.SetActive(true);
        playerController.SetCursorCaptured(false);
        if (promptUI != null)
            promptUI.SetActive(false);

        AITextDialogController.Instance?.NotifyDesktopFirstOpened();

        IsUsingComputer = true;
        IsTransitioning = false;
    }

    private IEnumerator ExitComputerRoutine()
    {
        IsTransitioning = true;
        desktopUI.SetActive(false);

        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;

        if (instantAlign)
        {
            playerCamera.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
        }
        else
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / transitionDuration;
                float k = Mathf.SmoothStep(0f, 1f, t);

                playerCamera.position = Vector3.Lerp(startPosition, savedCameraPosition, k);
                playerCamera.rotation = Quaternion.Slerp(startRotation, savedCameraRotation, k);
                yield return null;
            }

            playerCamera.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
        }

        playerController.SetCursorCaptured(true);
        playerController.SetControlsEnabled(true);

        IsUsingComputer = false;
        IsTransitioning = false;
    }

    public void CloseDesktop()
    {
        if (IsUsingComputer && !IsTransitioning)
            StartCoroutine(ExitComputerRoutine());
    }

    private bool IsNearComputer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<Computer>() != null)
                return true;
        }
        return false;
    }
}
