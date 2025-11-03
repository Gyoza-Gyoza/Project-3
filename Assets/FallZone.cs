using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallZone : MonoBehaviour
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
            LevelDirector.Instance.Respawn();
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
                LevelDirector.Instance.Respawn();
            //}
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            other.GetComponent<EnemyBehaviour>().TakeDamage(10000);
        }
    }
}
