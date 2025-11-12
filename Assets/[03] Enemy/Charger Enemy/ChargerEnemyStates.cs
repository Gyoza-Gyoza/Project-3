using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basic enemy state for basic functions that all states can inherit from 
// Used to implement empty functions or shared behaviours 
// Individual states can then override only the functions they need

public class ChargerEnemyState : EnemyState
{
    protected ChargerEnemyBehaviour enemy;
    public ChargerEnemyState(ChargerEnemyBehaviour enemy)
    {
        this.enemy = enemy;
    }
    public override void DoEnemyAction()
    {

    }
    public override void DoEnemyActionFixed()
    {
    }
    public override void OnCollide()
    {

    }
    public override void OnLanding()
    {

    }
    public override void ReachTargetAction()
    {

    }
    public override void OnDamaged()
    {
    }
}
public class ChargerEnemyChaseState : ChargerEnemyState
{
    public ChargerEnemyChaseState(ChargerEnemyBehaviour enemy) : base(enemy)
    {
        enemy.agent.enabled = true;
        enemy.agent.updateRotation = true;
        enemy.Animator.Play("Walk");
    }
    public override void DoEnemyAction()
    {
        enemy.agent.SetDestination(PayloadBehaviour.instance.transform.position);

        if (Vector3.Distance(enemy.transform.position, PayloadBehaviour.instance.transform.position) <= enemy.attackRange)
        {
            ReachTargetAction();
        }
    }
    public override void ReachTargetAction()
    {
        enemy.State = new ChargerEnemyAttackState(enemy);
    }
}
public class ChargerEnemyAttackState : ChargerEnemyState
{
    float timer = 0f;
    public ChargerEnemyAttackState(ChargerEnemyBehaviour enemy) : base(enemy)
    {
        enemy.agent.isStopped = true;
        enemy.Rb.angularVelocity = Vector3.zero;
        enemy.Animator.Play("Attack");
    }
    public override void DoEnemyAction()
    {
        timer += Time.deltaTime;
        if (timer >= enemy.attackInterval)
        {
            ReachTargetAction();
        }
    }
    public override void ReachTargetAction()
    {
        enemy.State = new ChargerEnemyChaseState(enemy);
    }
}
public class ChargerEnemyStunState : ChargerEnemyState
{
    private float duration;
    private float timer;
    protected bool stunned;
    public bool Stunned
    { get { return stunned; } }
    public ChargerEnemyStunState(ChargerEnemyBehaviour enemy, float duration, Vector3 force) : base(enemy)
    {
        // Ensures nothing funny happens 
        enemy.agent.enabled = false;
        enemy.Rb.velocity = Vector3.zero;
        enemy.Rb.angularVelocity = Vector3.zero;
        enemy.Rb.isKinematic = false;
        enemy.Rb.freezeRotation = true;

        this.duration = duration;
        stunned = true;
        timer = 0f;
        enemy.Animator.Play("Idle");
        enemy.Rb.AddForce(force, ForceMode.Impulse);
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
        enemy.State = new ChargerEnemyChaseState(enemy);
    }
}
public class ChargerEnemyDeathState : ChargerEnemyStunState
{
    public ChargerEnemyDeathState(ChargerEnemyBehaviour enemy, float duration, Vector3 force) : base(enemy, duration, force)
    {

    }
    public override void ReachTargetAction()
    {
        enemy.PlayDeathCoroutine();
    }
}
