using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basic enemy state for basic functions that all states can inherit from 
// Used to implement empty functions or shared behaviours 
// Individual states can then override only the functions they need

public class DrainerEnemyState : EnemyState
{
    protected DrainerEnemyBehaviour enemy;
    public DrainerEnemyState(DrainerEnemyBehaviour enemy)
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
    public override void OnCollide()
    {

    }
}

// Idle state where the enemy checks periodically if it can attack the target
public class DrainerEnemyIdleState : DrainerEnemyState
{
    private float targetCheckFrequency;
    private float timer;
    public DrainerEnemyIdleState(DrainerEnemyBehaviour enemy) : base(enemy)
    {
        targetCheckFrequency = enemy.TargetCheckFrequency;
        timer = 0f;
    }
    public override void DoEnemyAction()
    {
        timer += Time.deltaTime;

        if (timer >= 1 / targetCheckFrequency)
        {
            timer -= 1 / targetCheckFrequency;

            if (enemy.CanHitTarget()) enemy.State = new DrainerEnemyDrainState(enemy);
        }
    }
}
public class DrainerEnemyDrainState : DrainerEnemyState
{
    public DrainerEnemyDrainState(DrainerEnemyBehaviour enemy) : base(enemy)
    {

    }
    public override void DoEnemyAction()
    {
        Vector3 target = PayloadBehaviour.Instance.transform.position;
        target.y += enemy.payloadTargetOffsetY;
        enemy.transform.LookAt(new Vector3(target.x, enemy.transform.position.y, target.z));
        enemy.Line.SetPositions(new Vector3[] { enemy.LineStart.transform.position, target });
        enemy.Attack(); 
    }
}
public class DrainerEnemyDefendState : DrainerEnemyState
{
    public DrainerEnemyDefendState(DrainerEnemyBehaviour enemy) : base(enemy)
    {
        Defend();
    }
    public override void DoEnemyAction()
    {
        Vector3 target = PlayerController3P.Instance.transform.position;
        enemy.transform.LookAt(new Vector3(target.x, enemy.transform.position.y, target.z));
        if (Vector3.Distance(PlayerController3P.Instance.transform.position, enemy.transform.position) >= enemy.DefendRange)
        {
            enemy.defense = 1f;
            enemy.State = new DrainerEnemyIdleState(enemy);
        }
    }
    public void Defend()
    {
        enemy.defense = enemy.DefendingDefense;
    }
}