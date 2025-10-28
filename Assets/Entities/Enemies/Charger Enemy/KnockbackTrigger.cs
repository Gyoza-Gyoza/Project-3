using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackTrigger : MonoBehaviour
{
    [SerializeField] private GameObject parent;

    private void OnTriggerEnter(Collider other)
    {
            Debug.Log("Knockback Trigger hit " + other.gameObject.name);
        if (other.TryGetComponent<IDamageable>(out IDamageable basicEnemy))
        {
            if (other.gameObject != parent) basicEnemy.TakeDamage(0, gameObject);
        }
    }
}
