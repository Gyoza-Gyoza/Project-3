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
    public float aggroRange = 8f;      // prefer player if within this range
    public float attackRange = 1.8f;   // switch to Attack when within this range

    [Header("Navigation")]
    public float repathInterval = 0.3f;    // throttle SetDestination
    public float faceTurnSpeed = 720f;     // deg/sec while in Attack (turn to face)

    [Header("Hit Reaction")]
    public float knockbackDistance = 1.2f;
    public float knockbackDuration = 0.12f;
    public Color flashColor = Color.white; // flash tint
    public float flashDuration = 0.08f;
    public float emissionBoost = 2.5f;     // extra pop if Emission is enabled on material

    [Header("Debug")]
    public bool drawDebug = true;

    // --- internals ---
    NavMeshAgent agent;
    Transform currentTarget;
    float lastRepathTime;
    Vector3 lastTargetPos = Vector3.positiveInfinity;
    bool isKnockingBack;

    // Renderers + MPBs for SRP-friendly color/emission flash
    Renderer[] rends;
    MaterialPropertyBlock[] mpbs;

    // URP/HDRP property IDs
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); // URP Lit
    static readonly int LegacyColorID = Shader.PropertyToID("_Color");     // fallback
    static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");

    // Per-renderer saved values
    Color[] savedBaseColors;
    Color[] savedEmissionColors;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Gather renderers & prepare MPBs
        rends = GetComponentsInChildren<Renderer>(true);
        mpbs = new MaterialPropertyBlock[rends.Length];
        savedBaseColors = new Color[rends.Length];
        savedEmissionColors = new Color[rends.Length];

        for (int i = 0; i < rends.Length; i++)
        {
            mpbs[i] = new MaterialPropertyBlock();
            var mat = rends[i].sharedMaterial;

            // Prefer URP _BaseColor, else legacy _Color
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
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (payload == null)
            payload = GameObject.FindGameObjectWithTag("Payload")?.transform;

        currentTarget = payload; // default behaviour: go for payload
        EnsureOnNavMesh(3f);
    }

    void Update()
    {
        if (CurrentState == State.Death) return;

        switch (CurrentState)
        {
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Hit:    /* handled in coroutine */ break;
        }

        if (drawDebug)
        {
            DrawCircleXZ(transform.position, aggroRange, Color.yellow);
            DrawCircleXZ(transform.position, attackRange, Color.red);
        }
    }

    // --- CHASE ---
    void UpdateChase()
    {
        // Choose target: prefer player if within aggroRange, else payload
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        currentTarget = (distToPlayer <= aggroRange) ? player : payload;

        // Throttled re-path to reduce cost
        if (Time.time - lastRepathTime >= repathInterval)
        {
            if (!EnsureOnNavMesh(3f))
            {
                lastRepathTime = Time.time;
                return;
            }
            
            Vector3 tgt = currentTarget.position;
            if ((tgt - lastTargetPos).sqrMagnitude > 0.25f) // ~0.5m movement threshold
            {
                if (agent.enabled) agent.SetDestination(tgt);
                lastTargetPos = tgt;
            }
            lastRepathTime = Time.time;
        }

        // Enter attack if close to current target
        if (Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            EnterAttack();
    }

    // --- ATTACK ---
    void UpdateAttack()
    {
        // Turn to face target (no attack animation/logic yet)
        Vector3 to = currentTarget.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, faceTurnSpeed * Time.deltaTime);
        }

        // Leave attack if target moves out of range
        if (Vector3.Distance(transform.position, currentTarget.position) > attackRange)
            ExitAttackToChase();
    }

    void EnterAttack()
    {
        CurrentState = State.Attack;
        if (agent.enabled)
        {
            agent.isStopped = true; // hold position while "attacking"
        }
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

    // --- HIT (knockback + flash) ---
    public void OnHit(Vector3 hitFromDirection)
    {
        if (CurrentState == State.Death || isKnockingBack) return;

        StopAllCoroutines();
        StartCoroutine(KnockbackRoutine(hitFromDirection));
        StartCoroutine(FlashRoutine());
    }

    System.Collections.IEnumerator KnockbackRoutine(Vector3 hitFromDir)
    {
        CurrentState = State.Hit;
        isKnockingBack = true;

        if (agent.enabled)
        {
            agent.isStopped = true;
            agent.updatePosition = false; // we will move Transform directly
        }

        Vector3 start = transform.position;
        Vector3 end = start + (-hitFromDir.normalized) * knockbackDistance;

        float t = 0f;
        while (t < knockbackDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / knockbackDuration);
            float eased = 1f - (1f - k) * (1f - k); // ease-out
            transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        if (agent.enabled)
        {
            agent.Warp(transform.position); // resync internal agent pos
            agent.updatePosition = true;
            agent.isStopped = false;
        }

        isKnockingBack = false;
        CurrentState = State.Chase; // resume simple behaviour
        lastTargetPos = Vector3.positiveInfinity; // force repath on next tick
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        // Apply flash: tint base color + optional emission punch
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            r.GetPropertyBlock(mpbs[i]);

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

        // Restore
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            r.GetPropertyBlock(mpbs[i]);

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

    // --- DEATH ---
    public void Die()
    {
        if (CurrentState == State.Death) return;
        CurrentState = State.Death;
        if (agent) agent.enabled = false;
        gameObject.SetActive(false); // simple despawn
    }

    // --- Debug helpers ---
    void DrawCircleXZ(Vector3 c, float r, Color color)
    {
        int seg = 32;
        Vector3 prev = c + new Vector3(r, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 next = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Debug.DrawLine(prev, next, color);
            prev = next;
        }
    }
    
    // Snap this agent onto the nearest NavMesh polygon if it's currently off-mesh.
    // Returns true if on-mesh (or successfully snapped), false if no mesh nearby.
    bool EnsureOnNavMesh(float maxSnapDistance = 3f)
    {
        if (!agent || !agent.enabled) return false;

        if (agent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out var hit, maxSnapDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position); // teleports + syncs agent internal position
            return true;
        }

        return false; // no mesh within range
    }
}
