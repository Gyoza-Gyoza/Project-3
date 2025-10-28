using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : Entity, IPlayerDamageable
{
    [SerializeField] private Animator animator;
    public Animator Animator
    { get { return animator; } }
    //[SerializeField] private GameObject flickerSign;
    [SerializeField] private HitBox hb;
    public float burnAdjAmount;
    public float speedAdjAmount;
    public float retreatAdjAmount;

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
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }
    protected override void Start()
    {
        base.Start();
        hb.HitBoxListeners += DamagePlayer;
        InitializeStats();
    }
    private void Update()
    {
        state.DoEnemyAction();

        if (Input.GetKeyDown(KeyCode.M)) TakeDamage(1);
    }
    protected virtual void InitializeStats()
    {
        agent.speed = MovementSpeed; 
    }
    public virtual void DamagePlayer(GameObject toDamage)
    {
        if (toDamage.tag == "Player")
        {
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
    protected virtual void OnEnable()
    {
        agent.enabled = true;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    protected override void OnHeal()
    {

    }

    protected override void OnDamage()
    {

    }

    public override void OnDeath()
    {

    }
}
