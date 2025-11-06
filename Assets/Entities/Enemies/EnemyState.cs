using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// EnemyState defines which functions that all enemy states must implement
public abstract class EnemyState
{
    public abstract void DoEnemyAction();
    public abstract void DoEnemyActionFixed();
    public abstract void ReachTargetAction();
    public abstract void OnLanding();
    public abstract void OnCollide();
    public abstract void OnDamaged();
}

