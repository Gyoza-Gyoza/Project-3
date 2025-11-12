using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    Collider col;
    Rigidbody rb;
    bool active;
    Transform attacker; // set by EnemyAI when enabling

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false; // hard-off by default

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Called by EnemyAI (from animation events)
    public void Enable(Transform attackerTransform)
    {
        attacker = attackerTransform != null ? attackerTransform : transform;
        active = true;
        col.enabled = true;

        // Optional immediate sweep so we hit if Player is already inside
        // (You can remove this if you prefer only Enter/Stay)
        var box = col as BoxCollider;
        if (box != null)
        {
            Vector3 worldCenter = transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
            var hits = Physics.OverlapBox(worldCenter, halfExtents + Vector3.one * 0.005f, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) TryHit(h);
        }
    }

    public void Disable()
    {
        active = false;
        attacker = null;
        col.enabled = false;
    }

    void OnTriggerEnter(Collider other) { TryHit(other); }
    void OnTriggerStay(Collider other)  { TryHit(other); }

    void TryHit(Collider other)
    {
        if (!active) return;
        if (!other.CompareTag("Player")) return;

        //Not sure if to use playercontroller instance or entity abstract class
        PlayerController3P.instance.TakeDamage(damage, gameObject);
        active = false;
        col.enabled = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var c = GetComponent<Collider>() as BoxCollider;
        if (!c) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = (c.enabled ? new Color(1,0,0,0.35f) : new Color(0,0,1,0.15f));
        Gizmos.DrawCube(c.center, c.size);
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(c.center, c.size);
    }
#endif
}
