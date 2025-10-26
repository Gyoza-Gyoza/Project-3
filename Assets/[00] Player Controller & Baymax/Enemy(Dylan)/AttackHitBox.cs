using System.Collections;
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
    public LayerMask targetLayers; // set this to Enemy

    [Header("Hit SFX (contextual)")]
    [Tooltip("AudioManager key for a single-enemy hit")]
    public string singleHitSfxKey = "enemy_hit_single";

    [Tooltip("AudioManager key for a multi-enemy sweep")]
    public string multiHitSfxKey = "enemy_hit_multi";

    [Tooltip("Minimum enemies in the swing to count as 'multi'")]
    [Range(2, 10)] public int minMultiCount = 2;

    [Tooltip("Time to wait (seconds) after the first hit to decide single vs multi")]
    [Range(0.01f, 0.2f)] public float decisionWindow = 0.08f;

    [Header("Aggregated VFX (one VFX per swing at avg hit position)")]
    [Tooltip("Randomized pool used when the swing resolves as a SINGLE hit")]
    public List<GameObject> singleHitVFXList = new List<GameObject>();
    public float singleHitVFXDuration = 0.9f;

    [Tooltip("Randomized pool used when the swing resolves as a MULTI hit")]
    public List<GameObject> multiHitVFXList = new List<GameObject>();
    public float multiHitVFXDuration = 1.2f;

    [Tooltip("Enable/disable spawning the aggregated VFX at the average sampled position")]
    public bool spawnAggregatedVFX = true;

    [Header("Debug")]
    public bool drawHitEvents = false;

    // internals
    bool _active;
    Transform _attacker;
    readonly HashSet<EnemyHealth> _hitThisWindow = new HashSet<EnemyHealth>();
    BoxCollider _box;
    Rigidbody _rb;

    // sfx/vfx decision internals
    int _normalHitCount;
    Vector3 _posSum;                 // sum of sampled hit positions (ClosestPoint)
    bool _decidedThisSwing;
    Coroutine _decideRoutine;

    void Awake()
    {
        _box = GetComponent<BoxCollider>();
        _box.isTrigger = true;
        _box.enabled = false;

        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    /// Open the hit window; also sweeps for overlaps immediately.
    public void Begin(Transform attackerTransform)
    {
        //_attacker = attackerTransform;
        //_hitThisWindow.Clear();
        //_active = true;
        //_box.enabled = true;

        //// reset decision state
        //_normalHitCount = 0;
        //_posSum = Vector3.zero;
        //_decidedThisSwing = false;
        //if (_decideRoutine != null) { StopCoroutine(_decideRoutine); _decideRoutine = null; }

        //// sweep anything already inside
        //Vector3 worldCenter = transform.TransformPoint(_box.center);
        //Vector3 halfExtents = Vector3.Scale(_box.size * 0.5f, transform.lossyScale);
        //Quaternion rot = transform.rotation;

        //var hits = Physics.OverlapBox(
        //    worldCenter,
        //    halfExtents + Vector3.one * 0.005f,
        //    rot,
        //    targetLayers,
        //    QueryTriggerInteraction.Ignore
        //);

        //for (int i = 0; i < hits.Length; i++)
        //    ApplyHitIfValid(hits[i]);
    }

    /// Close the hit window and finalize sfx/vfx choice if needed.
    public void End()
    {
        // finalize with whatever we’ve collected in this swing
        //FinalizeHitSfxDecision();

        //_active = false;
        //_attacker = null;
        //_hitThisWindow.Clear();
        //_box.enabled = false;

        //if (_decideRoutine != null) { StopCoroutine(_decideRoutine); _decideRoutine = null; }
    }

    void OnTriggerEnter(Collider other) { ApplyHitIfValid(other); }
    void OnTriggerStay(Collider other)  { ApplyHitIfValid(other); }

    void ApplyHitIfValid(Collider other)
    {
        if (!_active) return;

        // layer mask
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        // find EnemyHealth once
        if (!other.TryGetComponent(out EnemyHealth eh))
            eh = other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        // one hit per enemy per swing
        if (_hitThisWindow.Contains(eh)) return;
        _hitThisWindow.Add(eh);

        // apply damage (and any knockback you already do)
        eh.TakeDamage(damage, (_attacker ? _attacker.position : transform.position));

        // --- Option A: sample hit position using closest point to this hitbox (tighter feel)
        // Using the hitbox transform position as the source point for ClosestPoint.
        Vector3 sampleFrom = transform.position;
        Vector3 sfxPos = other.ClosestPoint(sampleFrom);

        _normalHitCount++;
        _posSum += sfxPos;

        // start short decision window on first hit
        if (_normalHitCount == 1 && !_decidedThisSwing)
        {
            if (_decideRoutine != null) StopCoroutine(_decideRoutine);
            _decideRoutine = StartCoroutine(DecideSfxAfterWindow());
        }

        if (drawHitEvents)
            Debug.DrawLine(transform.position, sfxPos, Color.magenta, 0.2f);
    }

    IEnumerator DecideSfxAfterWindow()
    {
        float t = decisionWindow;
        while (t > 0f && !_decidedThisSwing)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        FinalizeHitSfxDecision();
    }

    void FinalizeHitSfxDecision()
    {
        if (_decidedThisSwing) return;

        if (_normalHitCount <= 0)
        {
            _decidedThisSwing = true;
            return; // nothing hit → no SFX/VFX
        }

        bool isMulti = (_normalHitCount >= minMultiCount);
        string keyToPlay = isMulti ? multiHitSfxKey : singleHitSfxKey;

        // average of sampled positions (closest-point samples)
        Vector3 avgPos = _posSum / Mathf.Max(1, _normalHitCount);

        // ---- SFX (unchanged from your original contract) ----
        if (!string.IsNullOrEmpty(keyToPlay))
        {
            // Keep using your existing AudioManager API. No changes made.
            AudioManager.Instance?.PlaySFX(keyToPlay, avgPos);
        }

        // ---- Aggregated VFX with random selection ----
        if (spawnAggregatedVFX)
        {
            GameObject chosenPrefab = null;
            float destroyAfter = 0.5f;

            if (isMulti && multiHitVFXList != null && multiHitVFXList.Count > 0)
            {
                int idx = Random.Range(0, multiHitVFXList.Count);
                chosenPrefab = multiHitVFXList[idx];
                destroyAfter = multiHitVFXDuration;
            }
            else if (!isMulti && singleHitVFXList != null && singleHitVFXList.Count > 0)
            {
                int idx = Random.Range(0, singleHitVFXList.Count);
                chosenPrefab = singleHitVFXList[idx];
                destroyAfter = singleHitVFXDuration;
            }

            if (chosenPrefab != null)
            {
                var go = Instantiate(chosenPrefab, avgPos, Quaternion.identity);
                Destroy(go, Mathf.Max(0.05f, destroyAfter)); // swap to pooling in production
            }
        }

        _decidedThisSwing = true;
        if (_decideRoutine != null) { StopCoroutine(_decideRoutine); _decideRoutine = null; }
    }

#if UNITY_EDITOR
    //void OnDrawGizmos()
    //{
    //    if (_box == null) _box = GetComponent<BoxCollider>();
    //    Gizmos.matrix = transform.localToWorldMatrix;

    //    Gizmos.color = (_active && _box.enabled) ? new Color(1f, 0f, 0f, 0.3f)
    //                : _box.enabled ? new Color(1f, 1f, 0f, 0.2f)
    //                : new Color(0f, 0f, 1f, 0.15f);

    //    Gizmos.DrawCube(_box.center, _box.size);
    //    Gizmos.color = Color.black;
    //    Gizmos.DrawWireCube(_box.center, _box.size);
    //}
#endif
}
