using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static Unity.VisualScripting.Member;

public class EnemyBehaviour : Entity
{
    [SerializeField] private Animator animator;
    public Animator Animator
    { get { return animator; } }
    //[SerializeField] private GameObject flickerSign;
    public float burnAdjAmount;
    public float speedAdjAmount;
    public float retreatAdjAmount;
    [SerializeField] private TextMeshProUGUI debugText;

    // Internal Variables
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public GroundCheck groundCheck;
    private EnemyState state;
    public EnemyState State
    {
        get { return state; }
        set
        {
            previousState = state; 
            state = value;
            //if (debugText != null) debugText.text = value.ToString();
        }
    }
    private EnemyState previousState;
    public EnemyState PreviousState
    { get { return previousState; } }
    protected Rigidbody rb;
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
        rb = GetComponent<Rigidbody>();
        if (!TryGetComponent<NavMeshAgent>(out agent)) Debug.Log("Enemy Agent component not found");
        if (!TryGetComponent<Rigidbody>(out rb)) Debug.Log("Enemy Rigibody component not found");
    }
    protected override void Start()
    {
        base.Start();
        InitializeStats();
    }
    protected virtual void Update()
    {
        state?.DoEnemyAction();

        if (Input.GetKeyDown(KeyCode.M)) TakeDamage(1, gameObject);

        //debugText.text = $"\nRb.isKinematic: {Rb.isKinematic}, \nDrag: {Rb.drag}, \nVelocity: {Rb.velocity}, \nGrounded: {groundCheck.Grounded}";
    }
    protected virtual void FixedUpdate()
    {
        state?.DoEnemyActionFixed();
    }
    protected virtual void InitializeStats()
    {
        if (agent != null) agent.speed = MovementSpeed; 
    }
    public virtual void Attack()
    {
        
    }
    public void StopAttack()
    {
        agent.updateRotation = true;
        animator.ResetTrigger("Attacking");
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
            agent.updateRotation = true;
        }
    }
    public virtual void TakeDamageNoKnockback()
    {

    }
    protected override void OnHeal()
    {

    }
    protected override void OnDamaged(GameObject source)
    {
        if (PlayerController3P.instance.canPlaySlimeHitSFX)
        {
            PlayerController3P.instance.slimeHitAudioSource.Play();
            PlayerController3P.instance.canPlaySlimeHitSFX = false;
        }
    }
    public override void OnDeath()
    {
        LevelDirector.Instance.RemoveEnemy(this);
    }

    public void Kickstart()
    {
        //Debug.LogAssertion("Kickstart called");
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
            agent.updateRotation = true;
            //Debug.LogAssertion("Navmesh turned on");
        }
    }
}
