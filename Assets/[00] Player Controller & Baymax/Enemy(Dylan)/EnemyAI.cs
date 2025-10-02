using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State { Chase, Attack, Hit, Death }
    public State CurrentState = State.Chase;

    [Header("Targets")]
    private Transform player;
    private Transform payload;

    [Header("Ranges")]
    public float aggroRange = 8f;
    public float attackRange = 1.8f;

    [Header("Navigation")]
    public float repathInterval = 0.3f;
    public float faceTurnSpeed = 720f;

    [Header("Knockback (Musou-style, no air re-hits)")]
    public float kbHorizontalDistance = 4.0f; // meters pushed (mostly during air)
    public float kbAirTime = 0.22f;           // short pop time
    public float kbUpVelocity = 4.5f;         // upward kick
    public float gravityScale = 3.0f;         // heavier fall = snappier

    [Header("Ground Skid")]
    public float skidDuration = 0.25f;
    public float skidStartSpeed = 10f;
    public float skidEndSpeed = 0f;

    [Header("Flash")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;
    public float emissionBoost = 2.5f;

    [Header("Debug")]
    public bool drawDebug = true;

    // internals
    NavMeshAgent agent;
    Transform currentTarget;
    float lastRepathTime;
    Vector3 lastTargetPos = Vector3.positiveInfinity;
    bool isKnockingBack;
    Coroutine _knockCR;

    // SRP-friendly flash data
    Renderer[] rends;
    MaterialPropertyBlock[] mpbs;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColorID = Shader.PropertyToID("_Color");
    static readonly int EmissionID   = Shader.PropertyToID("_EmissionColor");
    Color[] savedBaseColors, savedEmissionColors;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        rends = GetComponentsInChildren<Renderer>(true);
        mpbs  = new MaterialPropertyBlock[rends.Length];
        savedBaseColors   = new Color[rends.Length];
        savedEmissionColors = new Color[rends.Length];

        for (int i = 0; i < rends.Length; i++)
        {
            mpbs[i] = new MaterialPropertyBlock();
            var mat = rends[i].sharedMaterial;

            if (mat != null && mat.HasProperty(BaseColorID))
                savedBaseColors[i] = mat.GetColor(BaseColorID);
            else if (mat != null && mat.HasProperty(LegacyColorID))
                savedBaseColors[i] = mat.GetColor(LegacyColorID);
            else
                savedBaseColors[i] = Color.white;

            if (mat != null && mat.HasProperty(EmissionID))
                savedEmissionColors[i] = mat.GetColor(EmissionID);
            else
                savedEmissionColors[i] = Color.black;
        }
    }

    void Start()
    {
        if (player  == null) player  = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (payload == null) payload = GameObject.FindGameObjectWithTag("Payload")?.transform;

        currentTarget = payload;
        EnsureOnNavMesh(3f);
    }

    void Update()
    {
        if (CurrentState == State.Death) return;

        switch (CurrentState)
        {
            case State.Chase:  UpdateChase();  break;
            case State.Attack: UpdateAttack(); break;
            case State.Hit:    break; // coroutine controls movement
        }

        if (drawDebug)
        {
            DrawCircleXZ(transform.position, aggroRange, Color.yellow);
            DrawCircleXZ(transform.position, attackRange, Color.red);
        }
    }

    void UpdateChase()
    {
        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
        currentTarget = (distToPlayer <= aggroRange && player) ? player : payload;

        if (Time.time - lastRepathTime >= repathInterval)
        {
            if (!EnsureOnNavMesh(3f))
            {
                lastRepathTime = Time.time;
                return;
            }

            Vector3 tgt = currentTarget.position;
            if ((tgt - lastTargetPos).sqrMagnitude > 0.25f)
            {
                if (agent.enabled) agent.SetDestination(tgt);
                lastTargetPos = tgt;
            }
            lastRepathTime = Time.time;
        }

        if (Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            EnterAttack();
    }

    void UpdateAttack()
    {
        Vector3 to = currentTarget.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, faceTurnSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, currentTarget.position) > attackRange)
            ExitAttackToChase();
    }

    void EnterAttack()
    {
        CurrentState = State.Attack;
        if (agent.enabled) agent.isStopped = true;
    }

    void ExitAttackToChase()
    {
        CurrentState = State.Chase;
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            lastTargetPos = currentTarget.position;
            lastRepathTime = Time.time;
        }
    }

    // REVERTED ENTRY POINT (no air-rehit logic)
    public void OnHit(Vector3 hitFromPosition)
    {
        if (CurrentState == State.Death) return;

        // restart knockback fresh on every hit
        if (_knockCR != null) StopCoroutine(_knockCR);
        _knockCR = StartCoroutine(KnockbackMusouRoutine(hitFromPosition));

        // play flash each time
        StartCoroutine(FlashRoutine());
    }

    IEnumerator KnockbackMusouRoutine(Vector3 hitFromPos)
    {
        CurrentState = State.Hit;
        isKnockingBack = true;

        bool wasEnabled = agent.enabled;
        if (wasEnabled) agent.enabled = false;

        // Backwards from hit point (horizontal only)
        Vector3 dir = (transform.position - hitFromPos).normalized;
        dir.y = 0f; dir.Normalize();

        // Phase A: short air pop (heavy gravity)
        float T  = Mathf.Max(0.05f, kbAirTime);
        float vx = kbHorizontalDistance / (T * 1.25f); // most distance in air
        float vy = kbUpVelocity;
        Vector3 vel = dir * vx + Vector3.up * vy;

        float elapsed = 0f, maxAir = T * 1.5f;
        while (true)
        {
            transform.position += vel * Time.deltaTime;
            vel += Physics.gravity * gravityScale * Time.deltaTime;
            elapsed += Time.deltaTime;

            // land using NavMesh height to avoid "sink/snap"
            if (vel.y <= 0f)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                {
                    if (transform.position.y <= navHit.position.y + 0.05f)
                    {
                        transform.position = navHit.position;
                        break;
                    }
                }
            }

            if (elapsed >= maxAir) break;
            yield return null;
        }

        // ensure we're on navmesh before skid
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit land, 3f, NavMesh.AllAreas))
            transform.position = land.position;

        // Phase B: brief ground skid along dir
        float t = 0f;
        while (t < skidDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / skidDuration);
            float speed = Mathf.Lerp(skidStartSpeed, skidEndSpeed, k * k);

            Vector3 next = transform.position + dir * speed * Time.deltaTime;
            if (NavMesh.SamplePosition(next, out NavMeshHit navSlide, 2f, NavMesh.AllAreas))
                next = navSlide.position;

            transform.position = next;
            yield return null;
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            agent.isStopped = false;
        }

        isKnockingBack = false;
        CurrentState = State.Chase;
    }

    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i]; r.GetPropertyBlock(mpbs[i]);
            var mat = r.sharedMaterial;

            if (mat != null && mat.HasProperty(BaseColorID))
                mpbs[i].SetColor(BaseColorID, flashColor);
            else if (mat != null && mat.HasProperty(LegacyColorID))
                mpbs[i].SetColor(LegacyColorID, flashColor);

            if (mat != null && mat.HasProperty(EmissionID))
            {
                Color boosted = flashColor * emissionBoost; boosted.a = 1f;
                mpbs[i].SetColor(EmissionID, boosted);
            }
            r.SetPropertyBlock(mpbs[i]);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i]; r.GetPropertyBlock(mpbs[i]);
            var mat = r.sharedMaterial;

            if (mat != null && mat.HasProperty(BaseColorID))
                mpbs[i].SetColor(BaseColorID, savedBaseColors[i]);
            else if (mat != null && mat.HasProperty(LegacyColorID))
                mpbs[i].SetColor(LegacyColorID, savedBaseColors[i]);

            if (mat != null && mat.HasProperty(EmissionID))
                mpbs[i].SetColor(EmissionID, savedEmissionColors[i]);

            r.SetPropertyBlock(mpbs[i]);
        }
    }

    public void Die()
    {
        if (CurrentState == State.Death) return;
        CurrentState = State.Death;
        if (agent) agent.enabled = false;
        gameObject.SetActive(false);
    }

    void DrawCircleXZ(Vector3 c, float r, Color color)
    {
        int seg = 32; Vector3 prev = c + new Vector3(r, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Debug.DrawLine(prev, next, color); prev = next;
        }
    }

    bool EnsureOnNavMesh(float maxSnapDistance = 3f)
    {
        if (!agent || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        if (NavMesh.SamplePosition(transform.position, out var hit, maxSnapDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            return true;
        }
        return false;
    }
}
