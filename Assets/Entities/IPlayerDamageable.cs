using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerDamageable //player can damage this
{
    void TakeDamage(int damageAmount);
}

public interface IEnemyDamageable //enemies can damage this
{
    void TakeDamage(int damageAmount);
}

