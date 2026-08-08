using UnityEngine;

public class Computer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform screenAnchor;

    public Transform ScreenAnchor => screenAnchor != null ? screenAnchor : transform;
}
