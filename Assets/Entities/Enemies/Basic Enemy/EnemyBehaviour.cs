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
    //[SerializeField] private GameObject flickerSign;
    public float burnAdjAmount;
    public float speedAdjAmount;
    public float retreatAdjAmount;

    // Internal Variables
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public GroundCheck groundCheck;
    private EnemyState state;
    public EnemyState State
    {
        get 
        {
            Debug.Log($"{name} entering {state.GetType()} state");
            return state; 
        }
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

    private GameObject target;

    public GameObject Target
    {
        get { return target; }
        set { target = value; }
    }
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }
    protected override void Start()
    {
        base.Start();
        InitializeStats();
    }
    private void Update()
    {
        state.DoEnemyAction();

        if (Input.GetKeyDown(KeyCode.M)) TakeDamage(1, gameObject);
    }
    protected virtual void InitializeStats()
    {
        agent.speed = MovementSpeed; 
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
    protected override void OnHeal()
    {

    }
    protected override void OnDamage(GameObject source)
    {

    }
    public override void OnDeath()
    {
        LevelDirector.Instance.EnemyCount -= 1;
    }
}
