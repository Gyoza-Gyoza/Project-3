using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlatformStick : MonoBehaviour
{
    [Tooltip("Layers that contain moving platforms (e.g. your Payload's colliders)")]
    public LayerMask platformLayer;

    [Tooltip("Minimum dot product between hit normal and up to consider as 'standing on'")]
    [Range(0f, 1f)]
    public float requiredUpNormal = 0.5f;

    [Tooltip("Time (seconds) to wait without contact before detaching (debounce)")]
    public float detachDelay = 0.12f;

    [Tooltip("Enable debug logs for attach/detach events")]
    public bool debugLogs = false;

    CharacterController controller;
    Transform currentPlatform;
    Vector3 lastPlatformPos;
    float lastHitTime = -999f;

    void Awake() => controller = GetComponent<CharacterController>();

    // Called when CharacterController collides during controller.Move(...)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.isTrigger) return;
        if (((1 << hit.collider.gameObject.layer) & platformLayer.value) == 0) return;
        if (Vector3.Dot(hit.normal, Vector3.up) < requiredUpNormal) return;

        Transform root = hit.collider.transform.root;

        // attach if new
        if (currentPlatform != root)
        {
            currentPlatform = root;
            lastPlatformPos = currentPlatform.position;
            if (debugLogs) Debug.Log($"PlatformStick: attached to {currentPlatform.name}");
        }

        // refresh contact time every time we hit
        lastHitTime = Time.time;
    }

    // Apply platform motion and perform detach check after player movement
    void LateUpdate()
    {
        // If we have a platform, apply its delta movement
        if (currentPlatform != null)
        {
            Vector3 posDelta = currentPlatform.position - lastPlatformPos;
            if (posDelta.sqrMagnitude > 0.000001f)
            {
                controller.Move(posDelta);
            }
            lastPlatformPos = currentPlatform.position;
        }

        // Detach when we have not received any contact for detachDelay seconds
        if (currentPlatform != null && (Time.time - lastHitTime) > detachDelay)
        {
            if (debugLogs) Debug.Log("PlatformStick: detach (no contact)");
            currentPlatform = null;
        }
    }
}
