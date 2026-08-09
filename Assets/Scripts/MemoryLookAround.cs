using UnityEngine;

public class MemoryLookAround : MonoBehaviour
{
    [SerializeField] private float sensitivity = 180f;
    [SerializeField] private float verticalLimit = 65f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Vector3 rotation = transform.eulerAngles;
        yaw = rotation.y;
        pitch = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X")
            * sensitivity * Time.deltaTime;

        float mouseY = Input.GetAxis("Mouse Y")
            * sensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -verticalLimit, verticalLimit);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}