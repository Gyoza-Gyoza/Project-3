using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;   // Player root
    public Transform pivot;    // CameraPivot (child under rig)
    public Camera cam;         // Main Camera

    [Header("Follow")]
    [Tooltip("Higher = snappier follow")]
    public float followSmooth = 12f;

    [Header("Orbit")]
    public float sensX = 180f;
    public float sensY = 130f;
    public float minPitch = -25f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("Framing")]
    [Tooltip("Horizontal shoulder offset, e.g., x=0.35 for over-the-shoulder")]
    public Vector3 shoulderOffset = new Vector3(0.35f, 0f, 0f);

    [Header("Zoom (Boom length)")]
    public float distance = 4.8f;
    public float minDistance = 2.2f;
    public float maxDistance = 6.5f;
    public float zoomSpeed = 3.0f;

    [Header("Collision")]
    [Tooltip("Sphere radius used for camera collision checks")]
    public float collisionRadius = 0.25f;
    [Tooltip("Which layers the camera collides with (exclude Player)")]
    public LayerMask collisionMask = ~0;
    [Tooltip("How fast the boom length blends when colliding / clearing")]
    public float boomSmooth = 14f;

    // Internals
    float yaw, pitch;
    Vector3 followVel;
    float currentDistance; // smoothed boom
    float targetDistance;  // desired boom (post-collision)

    // --- Camera Effects ---
    [Header("Effects")]
    [Tooltip("Base FOV captured on enable; FOV kick returns to this.")]
    public float baseFOV = 60f;

    // Shake runtime
    float shakeTimeLeft = 0f;
    float shakeMagnitude = 0f;
    float shakeSeedX, shakeSeedY;
    Quaternion shakeRotOffset = Quaternion.identity;
    Vector3 shakePosOffset = Vector3.zero;

    // FOV runtime
    Coroutine fovKickCR;

    void OnEnable()
    {
        if (!cam) cam = Camera.main;
        LockCursor(true);

        // Initialize from current LOCAL pivot rotation to avoid a tiny snap
        if (pivot)
        {
            var e = pivot.localEulerAngles;
            yaw   = e.y;
            pitch = ClampPitch(e.x);
        }

        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        targetDistance  = currentDistance;

        if (cam) baseFOV = cam.fieldOfView;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) LockCursor(true);

        // Mouse wheel zoom (adjust desired distance; smoothing happens later)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }
    }

    void LateUpdate()
    {
        if (!target || !pivot || !cam) return;

        // --- Smooth follow of the rig to the target ---
        float followT = 1f / Mathf.Max(followSmooth, 0.001f);
        transform.position = Vector3.SmoothDamp(
            transform.position, target.position, ref followVel, followT);

        // --- Orbit (yaw/pitch on the pivot) ---
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        yaw   += mx * sensX * Time.deltaTime;
        pitch += (invertY ? 1f : -1f) * my * sensY * Time.deltaTime;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        pivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        // --- Compute desired camera position from shoulder + boom ---
        // Desired local position behind pivot on -Z by 'distance'
        Vector3 desiredLocal = shoulderOffset + new Vector3(0f, 0f, -distance);
        Vector3 worldPivot   = pivot.position;
        Vector3 worldDesired = worldPivot + pivot.rotation * desiredLocal;

        // --- Collision: sphere cast from pivot toward desired ---
        Vector3 dir  = (worldDesired - worldPivot);
        float   dist = dir.magnitude;
        Vector3 nDir = dist > 1e-4f ? dir / dist : pivot.forward;

        float safeDist = dist; // default: no hit
        if (Physics.SphereCast(worldPivot, collisionRadius, nDir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Place camera just before the surface using the sphere radius
            safeDist = Mathf.Max(hit.distance - collisionRadius, 0f);
        }

        // Set the target boom length and smooth toward it
        targetDistance  = Mathf.Clamp(safeDist, 0f, dist);
        currentDistance = SmoothToward(currentDistance, targetDistance, boomSmooth);

        // Final camera transform
        cam.transform.position = worldPivot + nDir * currentDistance;
        cam.transform.rotation = pivot.rotation;

        // --- apply runtime effects (shake) ---
        UpdateShake();
        cam.transform.position += shakePosOffset;
        cam.transform.rotation = cam.transform.rotation * shakeRotOffset;
    }

    float SmoothToward(float current, float target, float speed)
    {
        // Exponential smoothing that is framerate-independent
        float t = 1f - Mathf.Exp(-Mathf.Max(0.0001f, speed) * Time.deltaTime);
        return Mathf.Lerp(current, target, t);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    float ClampPitch(float x)
    {
        // Convert 0..360 to signed range for clamping
        if (x > 180f) x -= 360f;
        return Mathf.Clamp(x, minPitch, maxPitch);
    }

    // ===== Utilities you can call from your controller =====

    /// World-space ray from camera center.
    public Ray GetAimRay()
    {
        return new Ray(cam.transform.position, cam.transform.forward);
    }

    /// World aim point by raycast; falls back to far point if nothing hit.
    public Vector3 GetAimPoint(float maxDist, LayerMask mask)
    {
        Ray ray = GetAimRay();
        if (Physics.Raycast(ray, out RaycastHit hit, maxDist, mask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.origin + ray.direction * maxDist;
    }

    /// Flat (XZ) forward from camera relative to a transform.
    public Vector3 GetFlatAimDirection(Transform from)
    {
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude < 1e-4f ? from.forward : fwd.normalized;
    }

    // ====================== SHAKE ======================
    /// <summary>
    /// Triggers a short screen shake. Magnitude ~ 0.2 for subtle, ~0.5 for big.
    /// </summary>
    public void Shake(float magnitude = 0.2f, float duration = 0.15f)
    {
        // If a shake is already playing, take the stronger magnitude and extend time if needed
        shakeMagnitude = Mathf.Max(shakeMagnitude, Mathf.Abs(magnitude));
        shakeTimeLeft  = Mathf.Max(shakeTimeLeft, duration);

        // randomize noise seeds so repeated calls feel different
        shakeSeedX = Random.value * 1000f;
        shakeSeedY = Random.value * 1000f;
    }

    void UpdateShake()
    {
        if (shakeTimeLeft <= 0f)
        {
            shakePosOffset = Vector3.zero;
            shakeRotOffset = Quaternion.identity;
            return;
        }

        // exponential decay
        shakeTimeLeft -= Time.deltaTime;
        float t = Mathf.Clamp01(shakeTimeLeft <= 0f ? 0f : shakeTimeLeft);
        // ease-out envelope so it settles quickly
        float envelope = t * t;

        // Perlin noise for smooth motion rather than jitter
        float nx = (Mathf.PerlinNoise(shakeSeedX, Time.time * 40f) * 2f - 1f);
        float ny = (Mathf.PerlinNoise(shakeSeedY, Time.time * 40f) * 2f - 1f);

        float posAmp = 0.08f * shakeMagnitude;   // positional amplitude (meters)
        float rotAmp = 0.75f * shakeMagnitude;   // rotational amplitude (degrees)

        shakePosOffset = new Vector3(nx, ny, 0f) * posAmp * envelope;
        shakeRotOffset = Quaternion.Euler(ny * rotAmp * envelope, 0f, -nx * rotAmp * envelope);
    }

    // ====================== FOV KICK ======================
    /// <summary>
    /// Quick FOV bump (use on dash): ease in, optional hold, then ease out back to baseFOV.
    /// </summary>
    public void DashFOVKick(float deltaFOV = 8f, float inTime = 0.08f, float holdTime = 0.05f, float outTime = 0.10f)
    {
        if (!cam) return;
        if (fovKickCR != null) StopCoroutine(fovKickCR);
        fovKickCR = StartCoroutine(FOVKickRoutine(deltaFOV, inTime, holdTime, outTime));
    }

    IEnumerator FOVKickRoutine(float delta, float inTime, float holdTime, float outTime)
    {
        float start = baseFOV;
        float peak  = baseFOV + delta;

        // ease in
        float t = 0f;
        while (t < inTime)
        {
            t += Time.deltaTime;
            float a = (inTime <= 0f) ? 1f : Mathf.SmoothStep(0f, 1f, t / inTime);
            cam.fieldOfView = Mathf.Lerp(start, peak, a);
            yield return null;
        }

        // hold
        float h = 0f;
        while (h < holdTime)
        {
            h += Time.deltaTime;
            cam.fieldOfView = peak;
            yield return null;
        }

        // ease out
        float o = 0f;
        while (o < outTime)
        {
            o += Time.deltaTime;
            float a = (outTime <= 0f) ? 1f : Mathf.SmoothStep(0f, 1f, o / outTime);
            cam.fieldOfView = Mathf.Lerp(peak, start, a);
            yield return null;
        }

        cam.fieldOfView = start;
        fovKickCR = null;
    }
}
