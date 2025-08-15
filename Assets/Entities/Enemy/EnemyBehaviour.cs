using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : Entity
{
    public NavMeshAgent agent;
    public EnemyState state;

    public float attackRange = 1f;
    protected override void Start()
    {
        base.Start();
        state = new EnemyChaseState(this);
    }
    private void Update()
    {
        agent.SetDestination(PayloadBehaviour.Instance.transform.position);
    }
    public override void OnDeath()
    {
    }

    protected override void OnDamage()
    {
    }

    protected override void OnHeal()
    {
    }
}
