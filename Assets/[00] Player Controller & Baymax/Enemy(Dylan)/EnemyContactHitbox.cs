using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyContactHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 5;
    public float tickInterval = 0.7f;
    public string playerTag = "Player";

    Collider col;
    Rigidbody rb;
    bool enabledByAI = false;

    readonly HashSet<Entity> touching = new();
    readonly Dictionary<Entity, float> nextTick = new();

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Enable()  => enabledByAI = true;
    public void Disable() => enabledByAI = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        var ent = other.GetComponentInParent<Entity>();
        if (ent == null) return;

        touching.Add(ent);
        if (!nextTick.ContainsKey(ent)) nextTick[ent] = Time.time; // immediate first tick
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        var ent = other.GetComponentInParent<Entity>();
        if (ent == null) return;

        touching.Remove(ent);
        nextTick.Remove(ent);
    }

    void Update()
    {
        if (!enabledByAI) return;
        float now = Time.time;

        foreach (var ent in touching)
        {
            if (ent == null) continue;
            if (now >= nextTick[ent])
            {
                ent.TakeDamage(damage);
                nextTick[ent] = now + tickInterval;
            }
        }
    }
}
