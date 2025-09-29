using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyState
{
    protected EnemyBehaviour enemy;
    public EnemyState(EnemyBehaviour enemy)
    {
        this.enemy = enemy;
    }

    public abstract void EnterStateAction();

    public abstract void DoEnemyAction();

    public abstract void ReachTargetAction();
}

public class EnemyChaseState : EnemyState
{

    public EnemyChaseState(EnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Chase State");
    }

    public override void EnterStateAction()
    {

    }

    public override void DoEnemyAction()
    {
        //Debug.Log("Chase Do Enemy Action being called");
        if (enemy.agent.isActiveAndEnabled)
        {
            Debug.Log("Agent is active and enabled, resuming chase");
            enemy.agent.SetDestination(PayloadBehaviour.Instance.transform.position);
        }

        if (Vector3.Distance(enemy.transform.position, PayloadBehaviour.Instance.transform.position) <= enemy.payloadRange)
        {
            ReachTargetAction();
        }

        if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.aggroRange)
        {
            Debug.Log("Player in Aggro range");
            enemy.state = new EnemyAttackState(enemy);

        }
    }
    public override void ReachTargetAction()
    { 
        enemy.state = new EnemyPayloadState(enemy);
    }
}

public class EnemyAttackState : EnemyState
{


    public EnemyAttackState(EnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Attack State");
    }
    public override void EnterStateAction()
    {

    }

    public override void DoEnemyAction()
    {
        if (enemy.agent.isActiveAndEnabled)
            enemy.agent.SetDestination(PlayerController3P.Instance.transform.position);

        if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) > enemy.aggroRange)
        {
            enemy.StopAttack();
            enemy.state = new EnemyChaseState(enemy);
        }
        else if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.attackRange && !enemy.IsAttacking)
        {
            ReachTargetAction();
        }
        else
        {
            enemy.StopAttack();
        }
    }

    public override void ReachTargetAction()
    {
        enemy.Attack();
    }

}
public class EnemyPayloadState : EnemyState
{
    public EnemyPayloadState(EnemyBehaviour enemy) : base(enemy)
    {

    }
    public override void EnterStateAction()
    {

    }
    public override void DoEnemyAction()
    {
        float d = Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position);
        if (d <= enemy.aggroRange) ReachTargetAction();
        else PayloadBehaviour.Instance.EnemyPushing(enemy.burnAdjAmount, enemy.speedAdjAmount);
    }
    public override void ReachTargetAction()
    {
        PayloadBehaviour.Instance.EnemyExit(enemy.burnAdjAmount, enemy.speedAdjAmount);
        enemy.state = new EnemyAttackState(enemy);
    }
}

public class EnemyTauntState : EnemyState
{
    private Transform target;
    public EnemyTauntState(EnemyBehaviour enemy, Transform target) : base(enemy)
    {
        this.target = target;
        enemy.agent.SetDestination(target.transform.position);
    }
    public override void EnterStateAction()
    {
    }
    public override void DoEnemyAction()
    {
        if (Vector3.Distance(enemy.transform.position, target.position) <= enemy.aggroRange) ReachTargetAction();
    }
    public override void ReachTargetAction()
    {
        enemy.Attack();
    }
}
