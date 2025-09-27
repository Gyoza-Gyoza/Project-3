using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    public delegate void HitBoxTriggerEvent(GameObject hit);
    public event HitBoxTriggerEvent HitBoxListeners;
    private Entity owner;
    private BoxCollider boxCollider;

    private void OnTriggerEnter(Collider other)
    {
        HitBoxListeners(other.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
