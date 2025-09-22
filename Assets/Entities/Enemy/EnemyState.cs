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
        Debug.Log("Enemy entering Chase State");
    }

    public override void EnterStateAction()
    {

    }

    public override void DoEnemyAction()
    {
        //Debug.Log("Chase Do Enemy Action being called");
        if (enemy.agent.isActiveAndEnabled)
        {
            enemy.agent.SetDestination(PayloadBehaviour.Instance.transform.position);
        }


        if (Vector3.Distance(enemy.transform.position, PayloadBehaviour.Instance.transform.position) <= enemy.payloadRange)
        {
            ReachTargetAction();
        }

        if (Vector3.Distance(enemy.transform.position, Discon_PlayerController.Instance.transform.position) <= enemy.aggroRange)
        {
            Debug.Log("Player in Aggro range");
            enemy.state = new EnemyAttackState(enemy);

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
        Debug.Log("Enemy entering Attack State");
    }
    public override void EnterStateAction()
    {

    }

    public override void DoEnemyAction()
    {
        if (enemy.agent.isActiveAndEnabled)
        {
            enemy.agent.SetDestination(Discon_PlayerController.Instance.transform.position);
        }

        if (Vector3.Distance(enemy.transform.position, Discon_PlayerController.Instance.transform.position) > enemy.aggroRange)
        {
            enemy.state = new EnemyChaseState(enemy);
        }
        else if (Vector3.Distance(enemy.transform.position, Discon_PlayerController.Instance.transform.position) <= enemy.attackRange && !enemy.IsAttacking)
        {
            ReachTargetAction();
        }
    }

    public override void ReachTargetAction()
    {
        //Attack

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

    }
    public override void ReachTargetAction()
    {
        //Attack
    }
}
