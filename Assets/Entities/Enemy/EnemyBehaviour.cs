using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : Entity
{
    [SerializeField] private GameObject flickerSign;
    [SerializeField] private float damageAmount = 1f;
    [HideInInspector] public NavMeshAgent agent;
    public EnemyState state;
    private Rigidbody rb;
    private bool flying = false;

    public float attackRange = 1f;
    protected override void Start()
    {
        base.Start();
        state = new EnemyChaseState(this);
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

    }
    private void Update()
    {
        agent.SetDestination(PayloadBehaviour.Instance.transform.position);
        if (agent.isOnNavMesh)
        {
            agent.enabled = true;
        }
    }
    public override void OnDeath()
    {
        GameObject.Destroy(this.gameObject);
    }

    protected override void OnDamage()
    {
        StartCoroutine(DamageFlicker());
        //Quaternion f = Quaternion.Euler(new Vector3(45, Vector3.Angle(PlayerController.Instance.transform.position, this.transform.position), 0)).normalized;
        Stunned();
    }

    private void Stunned()
    {
        agent.enabled = false;
        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + .2f , this.transform.position.z);    
        rb.AddForce(Vector3.up * damageAmount, ForceMode.Impulse);
        agent.height
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") && flying == false)
        {
            agent.enabled = true;
        }
    }
}
