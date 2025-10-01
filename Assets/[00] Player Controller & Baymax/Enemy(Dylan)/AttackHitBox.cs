using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))] // set to Kinematic in Inspector
public class AttackHitBox : MonoBehaviour
{
    public int damage = 1;
    public LayerMask targetLayers;
    public bool drawHitEvents = false;

    bool active = false;
    Transform attacker;
    readonly HashSet<EnemyHealth> hitThisWindow = new HashSet<EnemyHealth>();

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// <summary>Called when attack starts — enables hit detection</summary>
    public void Begin(Transform attackerTransform)
    {
        attacker = attackerTransform;
        hitThisWindow.Clear();
        active = true;
    }

    /// <summary>Called when attack ends — disables hit detection</summary>
    public void End()
    {
        active = false;
        attacker = null;
        hitThisWindow.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit Enemy");
        if (!active) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        if (!other.TryGetComponent(out EnemyHealth eh))
            eh = other.GetComponentInParent<EnemyHealth>();

        if (eh == null) return;
        if (hitThisWindow.Contains(eh)) return;

        hitThisWindow.Add(eh);
        var atkPos = attacker ? attacker.position : transform.position;
        eh.TakeDamage(damage, atkPos);

        if (drawHitEvents)
            Debug.DrawLine(transform.position, other.bounds.center, Color.magenta, 0.25f);
    }
}
