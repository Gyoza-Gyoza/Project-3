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
    public float burnAdjAmount;
    public float speedAdjAmount;
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
        //Do something else
        LevelDirector.Instance.EnemyCount -= 1;
        GameObject.Destroy(this.gameObject);
    }

    protected override void OnDamage()
    {
        StartCoroutine(DamageFlicker());
        //Quaternion f = Quaternion.Euler(new Vector3(45, Vector3.Angle(PlayerController3P.Instance.transform.position, this.transform.position), 0)).normalized;
        Stunned();
    }

    private void Stunned()
    {
        agent.enabled = false;
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + .2f , this.transform.position.z);
        Vector3 difference = this.transform.position -  PlayerController3P.Instance.transform.position;
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
            toDamage.GetComponent<PlayerController3P>().TakeDamage(damageAmount);
        }
    }
    public void Attack()
    {
        Debug.Log("Enemy Attack Called");
        //agent.isStopped = true;
        agent.updateRotation = false;
        this.gameObject.transform.LookAt(PlayerController3P.Instance.transform);
        //this.transform.rotation;
        animator.SetTrigger("Attacking");
    }

    public void StopAttack()
    {
        agent.updateRotation = true;
        animator.ResetTrigger("Attacking");
    }

private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && flying == false)
        {
            agent.enabled = true;
        }
    }
}
