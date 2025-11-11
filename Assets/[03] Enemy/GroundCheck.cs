using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [SerializeField] private EnemyBehaviour enemy;
    private bool grounded = false; 
    public bool Grounded // Used in case enemy gets trapped when getting knocked up
    { get { return grounded; } }
    private void Awake()
    {
        enemy.groundCheck = this;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            enemy.State.OnLanding();
            grounded = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            other.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            grounded = false;
        }
    }
}
