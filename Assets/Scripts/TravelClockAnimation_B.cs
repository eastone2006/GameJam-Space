using UnityEngine;

public class TravelClockAnimation_B : MonoBehaviour
{
    [SerializeField] private Transform hourPivot;
    [SerializeField] private Transform minutePivot;

    [Header("Animation Speed")]
    [SerializeField] private float minuteRotationSpeed = 18f;

    [Header("Starting Time")]
    [Range(0, 11)]
    [SerializeField] private int startingHour = 10;

    [Range(0, 59)]
    [SerializeField] private int startingMinute = 10;

    private float currentMinuteAngle;
    private float currentHourAngle;

    private void Start()
    {
        currentMinuteAngle = startingMinute * 6f;

        currentHourAngle =
            startingHour * 30f
            + startingMinute * 0.5f;

        ApplyRotation();
    }

    private void Update()
    {
        currentMinuteAngle +=
            minuteRotationSpeed * Time.deltaTime;

        // 时针速度是分针的十二分之一
        currentHourAngle +=
            minuteRotationSpeed / 12f * Time.deltaTime;

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (minutePivot != null)
        {
            minutePivot.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -currentMinuteAngle
                );
        }

        if (hourPivot != null)
        {
            hourPivot.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -currentHourAngle
                );
        }
    }
}