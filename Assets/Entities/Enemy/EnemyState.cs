using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyState
{
    protected EnemyBehaviour enemy;
    public EnemyState(EnemyBehaviour enemy)
    {
        this.enemy = enemy;
    }

    public abstract void DoEnemyAction();

    public abstract void ReachTargetAction();
}

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyBehaviour enemy) : base(enemy)
    {

    }
    public override void DoEnemyAction()
    {
        enemy.agent.SetDestination(PayloadBehaviour.Instance.transform.position);

        if (Vector3.Distance(enemy.transform.position, PayloadBehaviour.Instance.transform.position) <= enemy.attackRange)
        {
            ReachTargetAction();
        }
    }
    public override void ReachTargetAction()
    {
        enemy.state = new EnemyAttackState(enemy);
    }
}

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyBehaviour enemy) : base(enemy)
    {

    }
    public override void DoEnemyAction()
    {

    }
    public override void ReachTargetAction()
    {

    }
}