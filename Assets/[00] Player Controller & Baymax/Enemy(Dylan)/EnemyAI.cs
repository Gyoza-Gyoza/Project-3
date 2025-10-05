using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Chase, Attack, Hit, Death }
    public State CurrentState = State.Chase;
    [SerializeField] private bool chargerEnemy = false;

    [Header("Animator")]
    [SerializeField] Animator anim;

    [Header("Targets")]
    [SerializeField] Transform player;
    [SerializeField] Transform payload;

    [Header("Ranges")]
    public float aggroRange = 8f;
    public float attackRange = 1.8f;

    [Header("Navigation")]
    public float repathInterval = 0.3f;
    public float faceTurnSpeed = 720f;

    [Header("Combat")]
    public EnemyContactHitbox contactHitbox;
    public EnemyAttackHitbox attackHitbox;
    public int attackDamage = 10;
    public int contactDamage = 5;
    public float contactTickInterval = 0.7f;

    public float attackCooldown = 1.0f;
    public float reattackDelay = 0.05f;

    [Header("Knockback")]
    public float kbHorizontalDistance = 4.0f;
    public float kbAirTime = 0.22f;
    public float kbUpVelocity = 4.5f;
    public float gravityScale = 3.0f;
    public float skidDuration = 0.25f;
    public float skidStartSpeed = 10f;
    public float skidEndSpeed = 0f;

    [Header("Death (sink)")]
    public GameObject deathVfxPrefab;
    public float sinkDistance = 1.5f;
    public float sinkDuration = 0.6f;

    [Header("Ground detection")]
    public LayerMask groundMask;       // assign floor layers
    [Header("Collision")]
    public LayerMask collisionMask;    // assign walls/level layers
    public float skin = 0.02f;

    [Header("Flash (URP)")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.08f;

    // internals
    NavMeshAgent agent;
    Transform currentTarget;
    float lastRepathTime;
    Vector3 lastTargetPos = Vector3.positiveInfinity;

    Coroutine _attackLoopCR;
    Coroutine _knockCR;

    bool deathQueued = false;

    CapsuleCollider bodyCol;

    // flash data
    Renderer[] rends;
    MaterialPropertyBlock[] mpbs;
    Color[] savedBaseColors;

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColorID = Shader.PropertyToID("_Color");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        bodyCol = GetComponent<CapsuleCollider>();

        // gather renderers for flash
        rends = GetComponentsInChildren<Renderer>(true);
        mpbs = new MaterialPropertyBlock[rends.Length];
        savedBaseColors = new Color[rends.Length];

        for (int i = 0; i < rends.Length; i++)
        {
            mpbs[i] = new MaterialPropertyBlock();
            var mat = rends[i].sharedMaterial;

            if (mat && mat.HasProperty(BaseColorID))
                savedBaseColors[i] = mat.GetColor(BaseColorID);
            else if (mat && mat.HasProperty(LegacyColorID))
                savedBaseColors[i] = mat.GetColor(LegacyColorID);
            else
                savedBaseColors[i] = Color.white;
        }
    }

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (payload == null) payload = GameObject.FindGameObjectWithTag("Payload")?.transform;

        currentTarget = payload;

        if (agent) agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.8f);

        if (attackHitbox) attackHitbox.damage = attackDamage;
        if (contactHitbox)
        {
            contactHitbox.damage = contactDamage;
            contactHitbox.tickInterval = contactTickInterval;
        }

        EnsureOnNavMesh(3f);
    }

    void Update()
    {
        if (CurrentState == State.Death) return;

        if (anim) anim.SetFloat("Speed", (agent && agent.enabled) ? agent.velocity.magnitude : 0f);

        switch (CurrentState)
        {
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Hit: break;
        }
    }

    // ---------- Chase ----------
    void UpdateChase()
    {
        if(!chargerEnemy)
        {
            float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : float.MaxValue;
            currentTarget = (distToPlayer <= aggroRange && player) ? player : payload;
        }
        else
        {
            currentTarget = payload;
        }

        if (Time.time - lastRepathTime >= repathInterval)
        {
            if (!EnsureOnNavMesh(3f)) { lastRepathTime = Time.time; return; }

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

    // ---------- Attack ----------
    void UpdateAttack()
    {
        if (!currentTarget || Vector3.Distance(transform.position, currentTarget.position) > attackRange)
        {
            ExitAttackToChase();
            return;
        }

        Vector3 to = currentTarget.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, faceTurnSpeed * Time.deltaTime);
        }
    }

    void EnterAttack()
    {
        if (CurrentState == State.Attack) return;

        CurrentState = State.Attack;
        if (agent.enabled) agent.isStopped = true;

        if (contactHitbox) contactHitbox.Enable();

        if (_attackLoopCR != null) StopCoroutine(_attackLoopCR);
        _attackLoopCR = StartCoroutine(AttackLoop());
    }

    void ExitAttackToChase()
    {
        if (CurrentState != State.Attack) return;

        CurrentState = State.Chase;

        if (_attackLoopCR != null) { StopCoroutine(_attackLoopCR); _attackLoopCR = null; }
        if (contactHitbox) contactHitbox.Disable();

        if (agent && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            lastTargetPos = currentTarget.position;
            lastRepathTime = Time.time;
        }
    }

    IEnumerator AttackLoop()
    {
        yield return null;

        while (CurrentState == State.Attack)
        {
            if (anim) anim.SetTrigger("Attack");

            float t = 0f;
            while (t < attackCooldown)
            {
                t += Time.deltaTime;
                if (!currentTarget || Vector3.Distance(transform.position, currentTarget.position) > attackRange)
                {
                    ExitAttackToChase();
                    yield break;
                }
                yield return null;
            }

            if (reattackDelay > 0f) yield return new WaitForSeconds(reattackDelay);
        }
    }

    public void AttackHitOn() { if (attackHitbox) attackHitbox.Enable(transform); }
    public void AttackHitOff() { if (attackHitbox) attackHitbox.Disable(); }

    // ---------- Hit / Knockback ----------
    public void OnHit(Vector3 hitFromPosition)
    {
        if (CurrentState == State.Death) return;

        StopCoroutine(nameof(FlashRoutine));
        StartCoroutine(FlashRoutine());

        if (_knockCR != null) StopCoroutine(_knockCR);
        _knockCR = StartCoroutine(KnockbackRoutine(hitFromPosition));

        if (anim) anim.SetTrigger("Hit");
    }

    IEnumerator KnockbackRoutine(Vector3 hitFromPos)
    {
        CurrentState = State.Hit;

        if (_attackLoopCR != null) { StopCoroutine(_attackLoopCR); _attackLoopCR = null; }
        if (contactHitbox) contactHitbox.Disable();
        if (attackHitbox) attackHitbox.Disable();

        bool wasEnabled = agent.enabled;
        if (wasEnabled) agent.enabled = false;

        Vector3 dir = (transform.position - hitFromPos).normalized;
        dir.y = 0f; dir.Normalize();

        float T = Mathf.Max(0.05f, kbAirTime);
        float vx = kbHorizontalDistance / (T * 1.25f);
        float vy = kbUpVelocity;
        Vector3 vel = dir * vx + Vector3.up * vy;

        float elapsed = 0f, maxAir = T * 1.5f;
        while (true)
        {
            MoveWithCollisions(vel * Time.deltaTime);
            vel += Physics.gravity * gravityScale * Time.deltaTime;
            elapsed += Time.deltaTime;

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

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit land, 3f, NavMesh.AllAreas))
            transform.position = land.position;

        float t = 0f;
        while (t < skidDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / skidDuration);
            float speed = Mathf.Lerp(skidStartSpeed, skidEndSpeed, k * k);

            Vector3 step = dir * speed * Time.deltaTime;
            MoveWithCollisions(step);

            yield return null;
        }

        if (deathQueued)
        {
            yield return StartCoroutine(DeathSinkSequence());
            yield break;
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            agent.isStopped = false;
        }

        CurrentState = State.Chase;

        if (currentTarget && Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            EnterAttack();
    }

    public void QueueDeath()
    {
        if (CurrentState == State.Death) return;
        deathQueued = true;
    }

    IEnumerator DeathSinkSequence()
    {
        CurrentState = State.Death;

        if (_attackLoopCR != null) { StopCoroutine(_attackLoopCR); _attackLoopCR = null; }
        if (contactHitbox) contactHitbox.Disable();
        if (attackHitbox) attackHitbox.Disable();
        if (agent) agent.enabled = false;

        if (anim) anim.SetTrigger("Death");

        AudioManager.Instance.PlaySFX("Death");

        if (deathVfxPrefab)
        {
            var vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
            var ps = vfx.GetComponent<ParticleSystem>();
            Destroy(vfx, ps ? ps.main.duration + ps.main.startLifetime.constantMax + 0.25f : 3f);
        }

        Vector3 start = transform.position;
        Vector3 end = start + Vector3.down * Mathf.Abs(sinkDistance);

        float t = 0f;
        while (t < sinkDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / sinkDuration);
            transform.position = Vector3.Lerp(start, end, k);
            yield return null;
        }

        //gameObject.SetActive(false);
        Destroy(this.gameObject);
    }

    // ---------- Flash ----------
    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            var mpb = mpbs[i];
            r.GetPropertyBlock(mpb);
            var mat = r.sharedMaterial;

            if (mat && mat.HasProperty(BaseColorID))
                mpb.SetColor(BaseColorID, flashColor);
            else if (mat && mat.HasProperty(LegacyColorID))
                mpb.SetColor(LegacyColorID, flashColor);

            r.SetPropertyBlock(mpb);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < rends.Length; i++)
        {
            var r = rends[i];
            var mpb = mpbs[i];
            mpb.Clear(); // clear overrides
            r.SetPropertyBlock(mpb);
        }
    }

    // ---------- Helpers ----------
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

    // Collision-aware move
    void MoveWithCollisions(Vector3 delta)
    {
        if (bodyCol == null || delta.sqrMagnitude < 1e-8f)
        {
            transform.position += delta;
            return;
        }

        float radius = bodyCol.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float height = Mathf.Max(bodyCol.height * transform.lossyScale.y, radius * 2f + 0.001f);
        Vector3 up = transform.up;

        Vector3 centerWS = transform.TransformPoint(bodyCol.center);
        float half = (height * 0.5f) - radius;
        Vector3 p1 = centerWS + up * half;
        Vector3 p2 = centerWS - up * half;

        Vector3 dir = delta.normalized;
        float dist = delta.magnitude;

        if (Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, dist + skin, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float travel = Mathf.Max(0f, hit.distance - skin);
            transform.position += dir * travel;

            float remaining = dist - travel;
            Vector3 slideDir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
            Vector3 slideDelta = slideDir * Mathf.Max(0f, remaining);

            if (slideDelta.sqrMagnitude > 1e-8f &&
                Physics.CapsuleCast(p1 + dir * travel, p2 + dir * travel, radius, slideDir, out RaycastHit hit2, slideDelta.magnitude + skin, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float travel2 = Mathf.Max(0f, hit2.distance - skin);
                transform.position += slideDir * travel2;
            }
            else
            {
                transform.position += slideDelta;
            }
        }
        else
        {
            transform.position += delta;
        }
    }
}
