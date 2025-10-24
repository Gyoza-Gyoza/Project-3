using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : Entity
{
    [SerializeField] private Animator animator;
    public Animator Animator
    { get { return animator; } }
    [SerializeField] private GameObject flickerSign;
    [SerializeField] private HitBox hb;
    [SerializeField] private float deathTime = 1f;
    public float burnAdjAmount;
    public float speedAdjAmount;
    public float retreatAdjAmount;

    [Header("Physics Variables")]
    [SerializeField] private float hitUpforce = 1f;
    [SerializeField] private float hitHorforce = 1f;
    [SerializeField] private float upDrag = 1.5f;
    [SerializeField] private float fallForce = 1f;
    [SerializeField] private float downDrag = 0f;
    [SerializeField] private float knockupDuration = 1f;
    [SerializeField] private float recoveryTime = 1f;
    [SerializeField] private float deathHitUpForce = 1f, deathHitHorForce = 1f, 
        deathUpDrag = 1.5f, deathFallForce = 1f, deathDownDrag = 0f, deathKnockUpDuration = 1f;

    [Header("Visual Variables")]
    [SerializeField] private GameObject ball;
    [SerializeField] private GameObject mesh;
    [SerializeField] private GameObject skin;
    [SerializeField] private GameObject deathParticleSystem;
    [SerializeField] private Material hitMat;
    [SerializeField] private Material oriMat;

    // Internal Variables
    [HideInInspector] public NavMeshAgent agent;
    private EnemyState state;
    public EnemyState State
    {
        get { return state; }
        set { previousState = state; state = value; }
    }
    private EnemyState previousState;
    public EnemyState PreviousState
    { get { return previousState; } }
    private Rigidbody rb;
    public Rigidbody Rb
    { get { return rb; } }
    private bool flying = false;
    private bool isAttacking = false;
    public bool IsAttacking
    { get { return isAttacking; } }

    [Header("Enemy Stats")]
    public float payloadRange = 1f;
    public float aggroRange = 1f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;

    private GameObject target;

    public GameObject Target
    {
        get { return target; }
        set { target = value; }
    }

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        hb.HitBoxListeners += DamagePlayer;
        state = new EnemyChaseState(this);
    }
    private void Update()
    {
        state.DoEnemyAction();

        if (Input.GetKeyDown(KeyCode.M)) TakeDamage(Health);
    }
    private void InitializeStats()
    {
        agent.speed = MovementSpeed; 
    }

    protected override void OnDamage()
    {
        StartCoroutine(DamageFlicker());
        //Quaternion f = Quaternion.Euler(new Vector3(45, Vector3.Angle(PlayerController3P.Instance.transform.position, this.transform.position), 0)).normalized;
        state = new EnemyKnockUpState(this, knockupDuration, CalculateKnockBack(false), upDrag, fallForce, downDrag, recoveryTime);
    }

    private void Stunned()
    {
        agent.enabled = false;
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + .2f, this.transform.position.z);
        state = new EnemyKnockUpState(this, knockupDuration, CalculateKnockBack(false), upDrag, fallForce, downDrag, recoveryTime);
    }

    private Vector3 CalculateKnockBack(bool death)
    {
        // Calculate force and direction 
        Vector3 difference = this.transform.position - PlayerController3P.Instance.transform.position;
        Vector3 direction = new Vector3(difference.x, 0, difference.z).normalized;
        Vector3 force = Vector3.up * (death? deathHitUpForce : hitUpforce) 
            + direction * (death ? deathHitHorForce : hitHorforce);

        return force;
    }

    protected override void OnHeal()
    {
    }


    public void DamagePlayer(GameObject toDamage)
    {
        if (toDamage.tag == "Player")
        {
            //toDamage.GetComponent<PlayerController>().TakeDamage(damageAmount);
            toDamage.GetComponent<PlayerController3P>().TakeDamage(Damage);
        }
    }
    public void Attack()
    {
        Debug.Log("Enemy Attack Called");
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updateRotation = false;
        animator.Play("Attack");
    }

    public void StopAttack()
    {
        agent.updateRotation = true;
        animator.ResetTrigger("Attacking");
    }
    IEnumerator DamageFlicker()
    {
        skin.GetComponent<SkinnedMeshRenderer>().material = hitMat;
        yield return new WaitForSeconds(.1f);
        skin.GetComponent<SkinnedMeshRenderer>().material = oriMat;
        yield break;
    }
    public override void OnDeath()
    {
        //Do something else
        Debug.Log("Enemy on death called");
        LevelDirector.Instance.EnemyCount -= 1;
        state = new EnemyDeathState(this, deathKnockUpDuration, CalculateKnockBack(true), deathUpDrag, deathFallForce, deathDownDrag, recoveryTime);
    }
    public void PlayDeathCoroutine()
    {
        StartCoroutine(DeathCouroutine());
    }
    IEnumerator DeathCouroutine()
    {
        agent.enabled = false;
        deathParticleSystem.SetActive(true);
        ball.SetActive(false);
        float count = 0f;
        while (count <= deathTime)
        {
            count += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1f, 0f, 1f), count / deathTime);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        mesh.SetActive(false);
        ParticleSystem particleSystem = deathParticleSystem.GetComponent<ParticleSystem>();
        particleSystem.Stop();
        while (particleSystem.IsAlive())
        {
            yield return new WaitForSeconds(Time.deltaTime);
        }
        GameObjectPool.ReturnObject(gameObject);
        yield break;
    }
    private void OnEnable()
    {
        agent.enabled = true;
        ball.SetActive(true);
        mesh.SetActive(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            state.OnLanding();
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
