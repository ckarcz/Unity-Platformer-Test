using UnityEngine;

[RequireComponent(typeof(Transform))]
public class FollowCamera2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 locationOffset;

    void FixedUpdate()
    {
        var desiredPosition = target.position + target.rotation * locationOffset;
        var smoothedPosition = Vector2.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}
