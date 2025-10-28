using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basic enemy state for basic functions that all states can inherit from 
// Used to implement empty functions or shared behaviours 
// Individual states can then override only the functions they need
public class BasicEnemyState : EnemyState
{
    protected BasicEnemyBehaviour enemy;
    public BasicEnemyState(BasicEnemyBehaviour enemy)
    {
        this.enemy = enemy;
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
    public override void Attack()
    {
    }
    public override void OnCollide()
    {
    }
}
public class EnemyChaseState : BasicEnemyState
{
    public EnemyChaseState(BasicEnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Chase State");
        if (enemy.PreviousState is not EnemyKnockUpState) enemy.Animator.Play("Walk");
        else enemy.Animator.SetTrigger("Recover");
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
            enemy.State = new EnemyPayloadState(enemy);
        }

        if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.attackRange)
        {
            enemy.State = new EnemyAttackState(enemy);
        }
    }
    private Transform GetTarget()
    {
        // Chooses target based on aggro range 
        if (Vector3.Distance(enemy.transform.position, PlayerController3P.Instance.transform.position) <= enemy.aggroRange)
        {
            return PlayerController3P.Instance.transform;
        }
        else
        {
            return PayloadBehaviour.Instance.transform;
        }
    }
}

public class EnemyAttackState : BasicEnemyState
{
    private float timer = 0f;
    public EnemyAttackState(BasicEnemyBehaviour enemy) : base(enemy)
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
        enemy.State = new EnemyChaseState(enemy);
    }
}
public class EnemyPayloadState : BasicEnemyState
{
    bool pushing = false;
    public EnemyPayloadState(BasicEnemyBehaviour enemy) : base(enemy)
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
        enemy.State = new EnemyAttackState(enemy);
    }
}

public class EnemyTauntState : BasicEnemyState
{
    private Transform target;
    public EnemyTauntState(BasicEnemyBehaviour enemy, Transform target) : base(enemy)
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
}

public class EnemyStunState : BasicEnemyState
{
    protected float timer;
    protected float duration;
    protected bool stunned;
    public EnemyStunState(BasicEnemyBehaviour enemy, float duration) : base(enemy)
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
        enemy.State = new EnemyChaseState(enemy);
    }
}
public class EnemyKnockUpState : EnemyStunState
{
    private Vector3 downwardForce;
    private float recoveryTime;
    private bool landed;
    private float downDrag;
    // Knockup state takes stun state and switches the stun duration to airtime duration
    public EnemyKnockUpState(BasicEnemyBehaviour enemy, float duration, Vector3 force,
        float upDrag, float downwardForce, float downDrag, float recoveryTime) : base(enemy, duration)
    {
        this.downwardForce = new Vector3(0f, -downwardForce, 0f);
        this.recoveryTime = recoveryTime;
        this.downDrag = downDrag;

        // Ensures nothing funny happens 
        enemy.agent.enabled = false;
        enemy.Rb.velocity = Vector3.zero;
        enemy.Rb.angularVelocity = Vector3.zero;
        enemy.Rb.isKinematic = false;
        enemy.Rb.freezeRotation = true;

        // Applies force 
        enemy.Rb.drag = upDrag;
        enemy.Rb.AddForce(force, ForceMode.Impulse);
        stunned = true;
    }
    public override void DoEnemyAction()
    {
        timer += Time.deltaTime;

        // First stage: in the air
        // Reusing stun bool for airtime
        if (stunned)
        {
            if (timer >= duration)
            {
                enemy.Rb.drag = downDrag;
                enemy.Rb.AddForce(downwardForce, ForceMode.Impulse);
                stunned = false;
            }
        }
        // Second stage: landed and recovering 
        else
        {
            if (landed)
            {
                if (timer >= recoveryTime)
                {
                    enemy.agent.nextPosition = enemy.transform.position;
                    enemy.agent.enabled = true;
                    enemy.Rb.isKinematic = true;
                    enemy.Rb.freezeRotation = false;

                    enemy.State = new EnemyChaseState(enemy);
                }
            }
        }
    }
    public override void OnLanding()
    {
        if (stunned) return;

        landed = true;
        timer = 0f;
        enemy.Rb.velocity = new Vector3(enemy.Rb.velocity.x, 0f, enemy.Rb.velocity.z); // Nullify Y velocity on landing
        enemy.Animator.Play("Land");
    }
}
public class EnemyDeathState : EnemyKnockUpState
{
    public EnemyDeathState(BasicEnemyBehaviour enemy, float duration, Vector3 force,
        float upDrag, float downwardForce, float downDrag, float recoveryTime)
        : base(enemy, duration, force, upDrag, downwardForce, downDrag, recoveryTime)
    {

    }
    public override void OnLanding()
    {
        if (stunned) return;
        enemy.PlayDeathCoroutine();
    }
}