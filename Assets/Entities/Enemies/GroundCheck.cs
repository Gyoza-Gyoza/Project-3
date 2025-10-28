using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            EnemyBehaviour enemy = transform.parent.GetComponent<EnemyBehaviour>();
            enemy.State.OnLanding();
        }
    }
}
