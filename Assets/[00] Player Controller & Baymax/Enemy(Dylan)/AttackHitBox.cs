using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))] // set Kinematic
public class AttackHitBox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;

    [Header("Filtering")]
    public LayerMask targetLayers; // set to Enemy

    [Header("Debug")]
    public bool drawHitEvents = false;

    // internals
    bool _active;
    Transform _attacker;
    readonly HashSet<EnemyHealth> _hitThisWindow = new HashSet<EnemyHealth>();
    BoxCollider _box;
    Rigidbody _rb;

    void Awake()
    {
        _box = GetComponent<BoxCollider>();
        _box.isTrigger = true;
        _box.enabled = false;     // hard gate off by default

        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    /// Open the hit window; hit anything already overlapping immediately.
    public void Begin(Transform attackerTransform)
    {
        _attacker = attackerTransform;
        _hitThisWindow.Clear();
        _active = true;
        _box.enabled = true;

        // Immediate sweep for targets already inside
        Vector3 worldCenter = transform.TransformPoint(_box.center);
        Vector3 halfExtents = Vector3.Scale(_box.size * 0.5f, transform.lossyScale);
        Quaternion rot = transform.rotation;

        var hits = Physics.OverlapBox(
            worldCenter,
            halfExtents + Vector3.one * 0.005f, // tiny pad
            rot,
            targetLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
            ApplyHitIfValid(hits[i]);
    }

    /// Close the hit window; silence the collider.
    public void End()
    {
        _active = false;
        _attacker = null;
        _hitThisWindow.Clear();
        _box.enabled = false;
    }

    void OnTriggerEnter(Collider other) { ApplyHitIfValid(other); }
    void OnTriggerStay(Collider other)  { ApplyHitIfValid(other); }

    void ApplyHitIfValid(Collider other)
    {
        if (!_active) return;

        // Layer mask filter
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        // Find EnemyHealth
        if (!other.TryGetComponent(out EnemyHealth eh))
            eh = other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        // One hit per swing
        if (_hitThisWindow.Contains(eh)) return;
        _hitThisWindow.Add(eh);

        // Apply damage/knockback
        Vector3 atkPos = _attacker ? _attacker.position : transform.position;
        eh.TakeDamage(damage, (_attacker ? _attacker.position : transform.position));
        
        if (drawHitEvents)
            Debug.DrawLine(transform.position, other.bounds.center, Color.magenta, 0.2f);
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (_box == null) _box = GetComponent<BoxCollider>();

        // Draw box volume in world space
        Gizmos.matrix = transform.localToWorldMatrix;

        // Color depends on state
        if (!_box.enabled)
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);   // blue transparent when collider off
        else if (_active)
            Gizmos.color = new Color(1f, 0f, 0f, 1f);    // red semi-transparent when attack window open
        else
            Gizmos.color = new Color(1f, 1f, 0f, 1f);    // yellow transparent when collider on but inactive (shouldn’t normally happen)

        Gizmos.DrawCube(_box.center, _box.size);
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(_box.center, _box.size);
    }
    #endif
}
