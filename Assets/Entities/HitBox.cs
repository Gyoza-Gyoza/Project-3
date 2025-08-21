using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private bool hurtsPlayer = false;
    [SerializeField] private bool hurtsEnemy = false;
    private GameObject owner;
    private BoxCollider boxCollider;
    
    public void SetOwner(GameObject input)
    {
        owner = input;
    }

    private void OnTriggerEnter(Collider other)
    {
            if (other.gameObject.tag == "Player" && hurtsPlayer)
            {
                
            }
            if (other.gameObject.tag == "Enemy" && hurtsEnemy)
            {
            owner.GetComponent<PlayerController>().BasicDamage(other.gameObject.GetComponent<EnemyBehaviour>());
            }

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
