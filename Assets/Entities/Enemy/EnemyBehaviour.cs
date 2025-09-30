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
    [SerializeField] private float deathTime = 1f;
    public float burnAdjAmount;
    public float speedAdjAmount;
    public float retreatAdjAmount;
    [HideInInspector] public NavMeshAgent agent;
    public EnemyState state;
    private Rigidbody rb;
    private Animator animator;
    [SerializeField] private GameObject ball;
    [SerializeField] private GameObject meshOffset;
    [SerializeField] private GameObject deathParticleSystem;
    [SerializeField] private Material hitMat;
    [SerializeField] private Material oriMat;

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
        StartCoroutine(DeathCouroutine());
    }

    IEnumerator DeathCouroutine()
    {
        agent.enabled = false;
        deathParticleSystem.SetActive(true);
        ball.SetActive(false);
        KnockbackFromPlayer();
        float count = 0f;
        while (count <=  deathTime)
        {
            count += Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        meshOffset.SetActive(false);
        ParticleSystem particleSystem = deathParticleSystem.GetComponent<ParticleSystem>();
        particleSystem.Stop();
        while (particleSystem.IsAlive())
        {
            yield return new WaitForSeconds(Time.deltaTime);
        }
        GameObject.Destroy(this.gameObject);
        yield break;
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
        KnockbackFromPlayer();
    }

    private void KnockbackFromPlayer()
    {
        Vector3 difference = this.transform.position - PlayerController3P.Instance.transform.position;
        Vector3 horNormed = new Vector3(difference.x, 0, difference.z).normalized;
        Vector3 force = Vector3.up * hitUpforce + horNormed * hitHorforce;
        rb.AddForce(force, ForceMode.Impulse);
    }

    IEnumerator DamageFlicker()
    {
        meshOffset.GetComponent<MeshRenderer>().material = hitMat;
        yield return new WaitForSeconds(.1f);
        meshOffset.GetComponent<MeshRenderer>().material = oriMat;
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
