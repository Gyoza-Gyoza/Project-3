using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitBox : MonoBehaviour, IDamageable
{
    private PlayerController3P player;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController3P>();
    }

    public void TakeDamage(int damage, GameObject source)
    {
        player.TakeDamage(damage, source);
    }
}
