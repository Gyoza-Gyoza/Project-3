using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : Entity
{
    [SerializeField] private GameObject flickerSign;
    [SerializeField] private float hitUpforce = 1f;
    [SerializeField] private float hitHorforce = 1f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private HitBox hb;
    [HideInInspector] public NavMeshAgent agent;
    public EnemyState state;
    private Rigidbody rb;
    private Animator animator;
    private bool flying = false;
    private bool isAttacking = false;
    public bool IsAttacking
    { get { return isAttacking; } }

    public float payloadRange = 1f;
    public float aggroRange = 1f;
    public float attackRange = 1f;

    private GameObject target;

    public GameObject Target
    {
        get { return target; }
        set { target = value; }
    }

    protected override void Start()
    {
        base.Start();
        state = new EnemyChaseState(this);
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        hb.HitBoxListeners += DamagePlayer;
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        state.DoEnemyAction();
    }
    public override void OnDeath()
    {
        GameObject.Destroy(this.gameObject);
    }

    protected override void OnDamage()
    {
        StartCoroutine(DamageFlicker());
        //Quaternion f = Quaternion.Euler(new Vector3(45, Vector3.Angle(Discon_PlayerController.Instance.transform.position, this.transform.position), 0)).normalized;
        Stunned();
    }

    private void Stunned()
    {
        agent.enabled = false;
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + .2f , this.transform.position.z);
        Vector3 difference = this.transform.position -  Discon_PlayerController.Instance.transform.position;
        Vector3 horNormed = new Vector3(difference.x, 0, difference.z).normalized;
        Vector3 force = Vector3.up * hitUpforce + horNormed * hitHorforce;
        rb.AddForce(force, ForceMode.Impulse);
    }

    IEnumerator DamageFlicker()
    {
        flickerSign.SetActive(true);
        yield return new WaitForSeconds(.1f);
        flickerSign.SetActive(false);
        yield break;
    }

    protected override void OnHeal()
    {
    }


    public void DamagePlayer(GameObject toDamage)
    {
        if (toDamage.tag == "Player")
        {
            //toDamage.GetComponent<PlayerController>().TakeDamage(damageAmount);
            toDamage.GetComponent<Discon_PlayerController>().TakeDamage(damageAmount);
        }
    }
    public void Attack()
    {
        animator.SetTrigger("Attacking");
        //StartCoroutine(AttackSequence());
    }
    IEnumerator AttackSequence()
    {
        isAttacking = true;
        agent.isStopped = true;
        yield return new WaitForSeconds(1f);
        hb.gameObject.GetComponent<BoxCollider>().enabled = true;
        yield return new WaitForSeconds(.1f);
        hb.gameObject.GetComponent<BoxCollider>().enabled = false;
        yield return new WaitForSeconds(.5f);
        agent.isStopped = false;
        isAttacking = false;
        yield break;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && flying == false)
        {
            agent.enabled = true;
        }
    }
}
