using System.Collections;
using UnityEngine;

public class MemoryCameraMovement : MonoBehaviour
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [SerializeField] private float startDelay = 0.8f;
    [SerializeField] private float moveDuration = 6f;

    private void Start()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("Camera points have not been assigned.");
            return;
        }

        transform.SetPositionAndRotation(
            startPoint.position,
            startPoint.rotation
        );

        StartCoroutine(MoveCamera());
    }

    private IEnumerator MoveCamera()
    {
        yield return new WaitForSeconds(startDelay);

        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / moveDuration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(
                startPoint.position,
                endPoint.position,
                smoothProgress
            );

            transform.rotation = Quaternion.Slerp(
                startPoint.rotation,
                endPoint.rotation,
                smoothProgress
            );

            yield return null;
        }

        transform.SetPositionAndRotation(
            endPoint.position,
            endPoint.rotation
        );
    }
}