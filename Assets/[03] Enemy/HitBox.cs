using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public delegate void HitBoxTriggerEvent(GameObject hit);
    public event HitBoxTriggerEvent HitBoxListeners;
    //private Entity owner;
    //private BoxCollider boxCollider;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hitbox is on");
        HitBoxListeners(other.gameObject);
    }
}
