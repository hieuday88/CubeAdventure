using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class CubeCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private bool snapOnEnable = true;

    [Header("Dead Zone (optional)")]
    [SerializeField] private bool useDeadZone = true;
    [SerializeField] private Vector2 deadZone = new Vector2(0.2f, 0.15f);

    private Vector3 velocity;
    private Vector3 trackerPosition;

    private void OnEnable()
    {
        ResolveTargetIfNull();
        trackerPosition = target != null ? target.position : transform.position;

        if (snapOnEnable)
        {
            SnapToTarget();
        }

        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        ResolveTargetIfNull();
        if (target == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        Vector3 current = transform.position;

        UpdateTrackerPosition(target.position);
        Vector3 desired = trackerPosition + offset;

        if (!followX)
        {
            desired.x = current.x;
        }

        if (!followY)
        {
            desired.y = current.y;
        }

        float clampedSmoothTime = Mathf.Max(0.0001f, smoothTime);
        float clampedMaxSpeed = maxSpeed > 0f ? maxSpeed : Mathf.Infinity;

        float nextX = current.x;
        float nextY = current.y;

        if (followX)
        {
            nextX = smoothTime <= 0f
                ? desired.x
                : Mathf.SmoothDamp(current.x, desired.x, ref velocity.x, clampedSmoothTime, clampedMaxSpeed, dt);
        }
        else
        {
            velocity.x = 0f;
        }

        if (followY)
        {
            nextY = smoothTime <= 0f
                ? desired.y
                : Mathf.SmoothDamp(current.y, desired.y, ref velocity.y, clampedSmoothTime, clampedMaxSpeed, dt);
        }
        else
        {
            velocity.y = 0f;
        }

        transform.position = new Vector3(nextX, nextY, desired.z);
    }

    private void SnapToTarget()
    {
        ResolveTargetIfNull();
        if (target == null)
        {
            return;
        }

        Vector3 current = transform.position;
        trackerPosition = target.position;
        Vector3 desired = trackerPosition + offset;

        if (!followX)
        {
            desired.x = current.x;
        }

        if (!followY)
        {
            desired.y = current.y;
        }

        transform.position = desired;
    }

    private void ResolveTargetIfNull()
    {
        if (target != null)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.cubeControllerRef != null)
        {
            target = GameManager.Instance.cubeControllerRef.transform;
        }
    }

    private void UpdateTrackerPosition(Vector3 targetPosition)
    {
        if (!followX)
        {
            trackerPosition.x = transform.position.x - offset.x;
        }
        else
        {
            if (!useDeadZone || deadZone.x <= 0f)
            {
                trackerPosition.x = targetPosition.x;
            }
            else
            {
                float dx = targetPosition.x - trackerPosition.x;
                float limit = deadZone.x;
                if (Mathf.Abs(dx) > limit)
                {
                    trackerPosition.x = targetPosition.x - Mathf.Sign(dx) * limit;
                }
            }
        }

        if (!followY)
        {
            trackerPosition.y = transform.position.y - offset.y;
        }
        else
        {
            if (!useDeadZone || deadZone.y <= 0f)
            {
                trackerPosition.y = targetPosition.y;
            }
            else
            {
                float dy = targetPosition.y - trackerPosition.y;
                float limit = deadZone.y;
                if (Mathf.Abs(dy) > limit)
                {
                    trackerPosition.y = targetPosition.y - Mathf.Sign(dy) * limit;
                }
            }
        }

        trackerPosition.z = targetPosition.z;
    }
}
