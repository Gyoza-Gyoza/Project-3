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

    public abstract void DoEnemyAction();

    public abstract void ReachTargetAction();
    public abstract void OnLanding();
}

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Chase State");
        enemy.Animator.Play("Walk");
        enemy.agent.isStopped = false;
        enemy.agent.updateRotation = true;
    }
    public override void DoEnemyAction()
    {
        //Debug.Log("Chase Do Enemy Action being called");
        if (enemy.agent.isActiveAndEnabled)
        {
            //Debug.Log("Agent is active and enabled, resuming chase");
            enemy.agent.SetDestination(GetTarget().position);
        }

        if (Vector3.Distance(enemy.transform.position, PayloadBehaviour.Instance.transform.position) <= enemy.payloadRange)
        {
            enemy.state = new EnemyPayloadState(enemy);
        }

        if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.attackRange)
        {
            //Debug.Log("Player in Aggro range");
            enemy.state = new EnemyAttackState(enemy);
        }
    }
    private Transform GetTarget()
    {
        // Chooses target based on aggro range 
        if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.aggroRange)
        {
            Debug.Log("Targetting player");
            return PlayerController3P.Instance.transform;
        }
        else
        {
            Debug.Log("Targetting payload");
            return PayloadBehaviour.Instance.transform;
        }
    }
    public override void ReachTargetAction()
    { 

    }

    public override void OnLanding()
    {

    }
}

public class EnemyAttackState : EnemyState
{
    private float timer = 0f;
    public EnemyAttackState(EnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Attack State");
        enemy.Attack();
    }
    public override void DoEnemyAction()
    {
        //if (enemy.agent.isActiveAndEnabled)
        //    enemy.agent.SetDestination(PlayerController3P.Instance.transform.position);

        //if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) > enemy.aggroRange)
        //{
        //    enemy.StopAttack();
        //    enemy.state = new EnemyChaseState(enemy);
        //}
        //else if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.attackRange && !enemy.IsAttacking)
        //{
        //    ReachTargetAction();
        //}
        //else
        //{
        //    enemy.StopAttack();
        //}

        // Keeps enemy facing player while attacking
        enemy.gameObject.transform.LookAt(new Vector3(PlayerController3P.Instance.transform.position.x,
            enemy.gameObject.transform.position.y, PlayerController3P.Instance.transform.position.z), Vector3.up);

        // Cools down before starting to chase again 
        timer += Time.deltaTime;
        if (timer >= enemy.attackCooldown)
        {
            timer = 0f;
            ReachTargetAction();
        }
    }

    public override void ReachTargetAction()
    {
        enemy.state = new EnemyChaseState(enemy);
    }
    public override void OnLanding()
    {

    }
}
public class EnemyPayloadState : EnemyState
{
    bool pushing = false;
    public EnemyPayloadState(EnemyBehaviour enemy) : base(enemy)
    {

    }
    public override void DoEnemyAction()
    {
        float d = Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position);
        if (d <= enemy.aggroRange) ReachTargetAction();
        else if (pushing == false)
        {
            PayloadBehaviour.Instance.EnemyPushing(enemy.burnAdjAmount, enemy.speedAdjAmount/*, enemy.retreatAdjAmount*/);
            pushing = true;
        }

    }
    public override void ReachTargetAction()
    {
        if (pushing == true)
        {
            PayloadBehaviour.Instance.EnemyExit(enemy.burnAdjAmount, enemy.speedAdjAmount/*, enemy.retreatAdjAmount*/);
        }
        enemy.state = new EnemyAttackState(enemy);
    }
    public override void OnLanding()
    {

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
    public override void DoEnemyAction()
    {
        if (Vector3.Distance(enemy.transform.position, target.position) <= enemy.aggroRange) ReachTargetAction();
    }
    public override void ReachTargetAction()
    {
        enemy.Attack();
    }
    public override void OnLanding()
    {

    }
}

public class EnemyStunState : EnemyState
{
    private float timer;
    private float duration;
    protected bool stunned;
    public EnemyStunState(EnemyBehaviour enemy, float duration) : base(enemy)
    {
        timer = 0f;
        this.duration = duration;
        stunned = true;
        enemy.Animator.Play("Idle");
    }
    public override void DoEnemyAction()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            stunned = false;
            ReachTargetAction();
        }
    }
    public override void ReachTargetAction()
    {
        enemy.state = new EnemyChaseState(enemy);
    }
    public override void OnLanding()
    {

    }
}
public class EnemyKnockUpState : EnemyStunState
{
    private Vector3 downwardForce;
    // Knockup state takes stun state and switches the stun duration to airtime duration
    public EnemyKnockUpState(EnemyBehaviour enemy, float duration, Vector3 force, float downwardForce) : base(enemy, duration)
    {
        this.downwardForce = new Vector3(0f, -downwardForce, 0f);

        // Ensures nothing funny happens 
        enemy.agent.enabled = false;
        enemy.Rb.velocity = Vector3.zero;
        enemy.Rb.angularVelocity = Vector3.zero;
        enemy.Rb.isKinematic = false;
        enemy.Rb.freezeRotation = true;

        // Applies force 
        enemy.Rb.AddForce(force, ForceMode.Impulse);
    }
    public override void ReachTargetAction()
    {
        enemy.Rb.AddForce(downwardForce, ForceMode.Impulse);
    }
    public override void OnLanding()
    {
        if (stunned) return;

        // Resets settings upon landing 
        enemy.agent.nextPosition = enemy.transform.position;
        enemy.agent.enabled = true;
        enemy.Rb.isKinematic = true;
        enemy.Rb.freezeRotation = false;
        Debug.Log("Landed");

        enemy.state = new EnemyChaseState(enemy);
    }
}
public class EnemyDeathState : EnemyState
{
    public EnemyDeathState(EnemyBehaviour enemy) : base(enemy)
    {
        Debug.Log("Enemy entering Death State");
    }
    public override void DoEnemyAction()
    {
        
    }
    public override void ReachTargetAction()
    {
    }
    public override void OnLanding()
    {
    }
}