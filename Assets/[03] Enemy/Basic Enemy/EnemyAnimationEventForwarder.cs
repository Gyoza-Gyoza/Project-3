using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationEventForwarder : MonoBehaviour
{
    [SerializeField] private BasicEnemyBehaviour enemy;
    public void PlayAttackVFX()
    {
        enemy.PlayAttackVFX();
    }
}
