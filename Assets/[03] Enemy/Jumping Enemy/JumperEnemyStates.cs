using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Jumper enemy state for Jumper functions that all states can inherit from 
// Used to implement empty functions or shared behaviours 
// Individual states can then override only the functions they need
public class JumperEnemyState : EnemyState
{
    protected JumperEnemyBehaviour enemy;
    public JumperEnemyState(JumperEnemyBehaviour enemy)
    {
        this.enemy = enemy;
    }
    public override void DoEnemyAction()
    {
    }
    public override void DoEnemyActionFixed()
    {
    }
    public override void ReachTargetAction()
    {
    }
    public override void OnLanding()
    {
    }
    public override void OnCollide()
    {
    }
    public override void OnDamaged()
    { 
    }
}

//Prepstate
//Jumpstate
//Landstate


public class JumperEnemyPrepJumpState: JumperEnemyState
{
    float timer = 0f;
    protected JumperEnemyBehaviour enemy;
    public JumperEnemyPrepJumpState(JumperEnemyBehaviour enemy) : base(enemy)
    {
        this.enemy = enemy;
        timer = 0f;
    }
    public override void DoEnemyAction()
    {
        timer += Time.deltaTime;
        if (timer >= enemy.idleTime)
        {
            timer = 0f;
            Jump();
        }
    }
    public override void DoEnemyActionFixed()
    {
    }
    public override void ReachTargetAction()
    {
    }
    public override void OnLanding()
    {
    }
    public override void OnCollide()
    {
    }
    public override void OnDamaged()
    {
    }

    public void Jump()
    {
        /*
        Vector3 difference = PlayerController3P.instance.transform.position - enemy.transform.position;
        Vector3 direction = (difference).normalized;
        float distance = difference.magnitude;
        */
        Debug.Log("Jumping");


        // Ensures nothing funny happens 
        enemy.agent.enabled = false;
        enemy.Rb.velocity = Vector3.zero;
        enemy.Rb.angularVelocity = Vector3.zero;
        enemy.Rb.isKinematic = false;
        enemy.Rb.freezeRotation = true;


        const float g = 9.81f;

        // Horizontal distance (XZ)
        Vector3 horizontal = new Vector3(PlayerController3P.instance.transform.position.x - enemy.transform.position.x, 0f, PlayerController3P.instance.transform.position.z - enemy.transform.position.z);
        float dx = horizontal.magnitude;

        // Vertical difference
        float dy = PlayerController3P.instance.transform.position.y - enemy.transform.position.y;

        float v = enemy.LaunchSpeed;
        float v2 = v * v;

        // Discriminant for quadratic
        float D = v2 * v2 - g * (g * dx * dx + 2f * dy * v2);

        if (D < 0f)
        {
            Debug.Log("Target unreachable at this speed.");
            return; // No solution
        }

        float sqrtD = Mathf.Sqrt(D);

        // Two possible angles
        float angleHigh = Mathf.Atan((v2 + sqrtD) / (g * dx));
        float angleLow = Mathf.Atan((v2 - sqrtD) / (g * dx));

        // Choose arc
        //float angle = useHighArc ? angleHigh : angleLow;

        // Build launch velocity vector
        horizontal.Normalize();

        Vector3 launchVelocity =
            horizontal * Mathf.Cos(angleHigh) * v +
            Vector3.up * Mathf.Sin(angleHigh) * v;

        // Clear current vertical velocity for consistency
        Vector3 vel = enemy.Rb.velocity;
        vel.y = 0;
        enemy.Rb.velocity = vel;

        // Launch
        enemy.Rb.velocity = launchVelocity;
    }

}

public class JumperEnemyChaseState : JumperEnemyState
{
    float timer;
    public JumperEnemyChaseState(JumperEnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Chase State");
        //if (enemy.PreviousState is not JumperEnemyKnockUpState) enemy.Animator.Play("Walk");
        //else enemy.Animator.SetTrigger("Recover");

        if (enemy.agent.isOnNavMesh) enemy.agent.isStopped = false;
        enemy.agent.updateRotation = true;
        enemy.agent.SetDestination(GetTarget().position);

        timer = 0f;
    }
    public override void DoEnemyAction()
    {
        timer += Time.deltaTime;
        if (timer >= enemy.targetCheckInterval)
        {
            timer = 0f;
            Chase();
        }
        CheckWhoIsInRange();
    }
    private void Chase()
    {
        if (enemy.agent.isActiveAndEnabled)
        {
            NavMeshHit navMesh;
            if (enemy.agent.isOnNavMesh) enemy.agent.SetDestination(GetTarget().position);
            else
            {
                if (NavMesh.SamplePosition(enemy.transform.position, out navMesh, Mathf.Infinity, NavMesh.AllAreas))
                {
                    enemy.agent.Warp(navMesh.position);
                    enemy.agent.SetDestination(GetTarget().position);
                }
            }
        }
    }
    private Transform GetTarget()
    {
        // Chooses target based on aggro range 
        if (Vector3.Distance(enemy.transform.position, PlayerController3P.instance.transform.position) <= enemy.aggroRange)
        {
            return PlayerController3P.instance.transform;
        }
        else
        {
            return PayloadBehaviour.instance.transform;
        }
    }
    private void CheckWhoIsInRange()
    {
        if (Vector3.Distance(enemy.transform.position, PayloadBehaviour.instance.transform.position) <= enemy.payloadRange)
        {
            enemy.State = new JumperEnemyPayloadState(enemy);
        }

        if (Vector3.Distance(enemy.transform.position, PlayerController3P.instance.transform.position) <= enemy.attackRange)
        {
            enemy.State = new JumperEnemyAttackState(enemy);
        }
    }
}

public class JumperEnemyAttackState : JumperEnemyState
{
    private float timer = 0f;
    public JumperEnemyAttackState(JumperEnemyBehaviour enemy) : base(enemy)
    {
        //Debug.Log("Enemy entering Attack State");
        enemy.Attack();
    }
    public override void DoEnemyAction()
    {
        //if (enemy.agent.isActiveAndEnabled)
        //    enemy.agent.SetPlayerController3P.instance.transform.positionination(PlayerController3P.instance.transform.position);

        //if (Vector3.Distance(enemy.transform.position, PlayerController3P.instance.transform.position) > enemy.aggroRange)
        //{
        //    enemy.StopAttack();
        //    enemy.state = new EnemyChaseState(enemy);
        //}
        //else if (Vector3.Distance(enemy.transform.position, PlayerController3P.instance.transform.position) <= enemy.attackRange && !enemy.IsAttacking)
        //{
        //    ReachTargetAction();
        //}
        //else
        //{
        //    enemy.StopAttack();
        //}

        // Keeps enemy facing player while attacking
        enemy.gameObject.transform.LookAt(new Vector3(PlayerController3P.instance.transform.position.x,
            enemy.gameObject.transform.position.y, PlayerController3P.instance.transform.position.z), Vector3.up);

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
        enemy.State = new JumperEnemyChaseState(enemy);
    }
}
public class JumperEnemyPayloadState : JumperEnemyState
{
    bool attackingPayload = false;
    public JumperEnemyPayloadState(JumperEnemyBehaviour enemy) : base(enemy)
    {

    }
    public override void DoEnemyAction()
    {
        float d = Vector3.Distance(enemy.transform.position, PlayerController3P.instance.transform.position);
        if (d <= enemy.aggroRange) ReachTargetAction();
        else
        {
            if (attackingPayload == false)
            {
                PayloadBehaviour.instance.EnemyPushing(enemy.burnAdjAmount, enemy.speedAdjAmount/*, enemy.retreatAdjAmount*/);
                attackingPayload = true;
            }
            enemy.Attack();
        }

    }
    public override void ReachTargetAction()
    {
        if (attackingPayload == true)
        {
            PayloadBehaviour.instance.EnemyExit(enemy.burnAdjAmount, enemy.speedAdjAmount/*, enemy.retreatAdjAmount*/);
        }
        enemy.State = new JumperEnemyAttackState(enemy);
    }
}

public class JumperEnemyTauntState : JumperEnemyState
{
    private Transform target;
    public JumperEnemyTauntState(JumperEnemyBehaviour enemy, Transform target) : base(enemy)
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

public class JumperEnemyStunState : JumperEnemyState
{
    protected float timer;
    protected float duration;
    protected bool stunned;
    public bool Stunned
    { get { return stunned; } }
    public JumperEnemyStunState(JumperEnemyBehaviour enemy, float duration) : base(enemy)
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
        enemy.State = new JumperEnemyChaseState(enemy);
    }
}

/*
public class JumperEnemyKnockUpState : JumperEnemyStunState
{
    private Vector3 downwardForce;
    private float recoveryTime;
    private bool landed;
    private float downDrag;
    // Knockup state takes stun state and switches the stun duration to airtime duration
    public JumperEnemyKnockUpState(JumperEnemyBehaviour enemy, float duration, Vector3 force,
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
    }
    public override void DoEnemyActionFixed()
    {
        // First stage: in the air
        // Reusing stun bool for airtime
        if (stunned)
        {
            if (timer >= duration)
            {
                enemy.Rb.drag = downDrag;
                enemy.Rb.AddForce(downwardForce, ForceMode.Impulse);
                stunned = false;
                landed = enemy.groundCheck.Grounded;
                timer = 0f;
            }
        }
        // Second stage: landed and recovering 
        else
        {
            if (landed || enemy.groundCheck.Grounded)
            {
                if (timer >= recoveryTime)
                {
                    enemy.agent.nextPosition = enemy.transform.position;
                    enemy.agent.enabled = true;
                    enemy.Rb.isKinematic = true;
                    enemy.Rb.freezeRotation = false;

                    enemy.State = new JumperEnemyChaseState(enemy);
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
public class JumperEnemyDeathState : JumperEnemyKnockUpState
{
    public JumperEnemyDeathState(JumperEnemyBehaviour enemy, float duration, Vector3 force,
        float upDrag, float downwardForce, float downDrag, float recoveryTime)
        : base(enemy, duration, force, upDrag, downwardForce, downDrag, recoveryTime)
    {
        if (enemy.groundCheck.Grounded)
        {
            if (!stunned) OnLanding();
        }
    }   
    public override void OnLanding()
    {
        if (stunned) return;
        enemy.PlayDeathCoroutine();
    }
}
*/