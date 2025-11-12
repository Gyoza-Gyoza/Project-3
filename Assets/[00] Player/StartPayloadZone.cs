using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartPayloadZone : MonoBehaviour
{
    //bool triggered = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {

            //if (!triggered)
            //{
            //    triggered = true;
            PayloadBehaviour.instance.StartPayload();
            //}
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag ==  "Player")
        {

            //if (!triggered)
            //{
            //    triggered = true;
            PayloadBehaviour.instance.StartPayload();
            //}
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            other.GetComponent<EnemyBehaviour>().OnDeath();
        }
    }
}
