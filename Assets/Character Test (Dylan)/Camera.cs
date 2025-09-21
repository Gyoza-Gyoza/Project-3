using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;   // Player root
    public Transform pivot;    // CameraPivot (child)
    public Camera cam;         // Main Camera

    [Header("Follow")]
    public float followSmooth = 12f;

    [Header("Orbit")]
    public float sensX = 180f;
    public float sensY = 130f;
    public float minPitch = -25f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("Framing")]
    public Vector3 shoulderOffset = new Vector3(0.35f, 0f, 0f);

    [Header("Zoom")]
    public float distance = 4.8f;
    public float minDistance = 2.2f;
    public float maxDistance = 6.5f;
    public float zoomSpeed = 3.0f;

    [Header("Collision")]
    public float collisionRadius = 0.25f;
    public LayerMask collisionMask = ~0;

    float yaw, pitch;
    Vector3 followVel;

    void OnEnable()
    {
        if (!cam) cam = Camera.main;
        LockCursor(true);

        // Initialize from current pivot rotation if present
        if (pivot)
        {
            var e = pivot.eulerAngles;
            yaw = e.y;
            pitch = ClampPitch(e.x);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) LockCursor(true);
    }

    void LateUpdate()
    {
        if (!target || !pivot || !cam) return;

        // Smooth follow
        transform.position = Vector3.SmoothDamp(
            transform.position, target.position,
            ref followVel, 1f / Mathf.Max(followSmooth, 0.001f)
        );

        // Orbit
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        yaw   += mx * sensX * Time.deltaTime;
        pitch += (invertY ? 1f : -1f) * my * sensY * Time.deltaTime;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        pivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // Desired camera position (shoulder offset + boom)
        Vector3 desiredLocal = shoulderOffset + new Vector3(0f, 0f, -distance);
        Vector3 worldPivot = pivot.position;
        Vector3 worldDesired = worldPivot + pivot.rotation * desiredLocal;

        // Collision (spherecast)
        Vector3 dir = (worldDesired - worldPivot).normalized;
        float dist = Vector3.Distance(worldPivot, worldDesired);
        if (Physics.SphereCast(worldPivot, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            cam.transform.position = worldPivot + dir * Mathf.Max(hit.distance - 0.05f, 0.05f);
        }
        else
        {
            cam.transform.position = worldDesired;
        }
        cam.transform.rotation = pivot.rotation;
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    float ClampPitch(float x)
    {
        if (x > 180f) x -= 360f;
        return Mathf.Clamp(x, minPitch, maxPitch);
    }

    // ===== Aim utilities =====

    /// World-space ray from the center of the screen (camera forward).
    public Ray GetAimRay()
    {
        return new Ray(cam.transform.position, cam.transform.forward);
    }

    /// Get an aim point in world space, by raycasting from camera.
    /// If nothing hit, returns a point far ahead.
    public Vector3 GetAimPoint(float maxDistance, LayerMask mask)
    {
        Ray ray = GetAimRay();
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * maxDistance;
    }

    /// Get flat (XZ) direction from the player to the camera's forward.
    public Vector3 GetFlatAimDirection(Transform from)
    {
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude < 1e-4f ? from.forward : fwd.normalized;
    }
}
