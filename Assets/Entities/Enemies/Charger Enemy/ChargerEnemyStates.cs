using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basic enemy state for basic functions that all states can inherit from 
// Used to implement empty functions or shared behaviours 
// Individual states can then override only the functions they need

public class ChargerEnemyState : EnemyState
{
    public override void Attack()
    {

    }

    public override void DoEnemyAction()
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
}
public class ChargerIdleState : ChargerEnemyState
{
    public ChargerIdleState(EnemyBehaviour enemy)
    {
    }
}
