using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    //[SerializeField] private bool hurtsPlayer = false;
    //[SerializeField] private bool hurtsEnemy = false;
    //private GameObject playerOwner;

    public delegate void HitBoxTriggerEvent(GameObject hit);
    public event HitBoxTriggerEvent HitBoxListeners;
    private Entity owner;
    private BoxCollider boxCollider;
    /*
    public void SetPlayerOwner(GameObject input)
    {
        playerOwner = input;

    }
    */

    private void OnTriggerEnter(Collider other)
    {
        /*
        if (other.gameObject.tag == "Player" && hurtsPlayer)
        {

        }
        if (other.gameObject.tag == "Enemy" && hurtsEnemy)
        {
            //playerOwner.GetComponent<PlayerController>().
            //owner.GetComponent<PlayerController>().BasicDamage(other.gameObject.GetComponent<EnemyBehaviour>());
        }
        */
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

/*
public interface IHitBoxOwner
{
    abstract public void DamageEnemy(EnemyBehaviour toDamage, HitBox hitbox);

    abstract public void DamagePlayer(EnemyBehaviour toDamage, HitBox hitbox);

    abstract public int HitBoxCheckDamage(HitBox tocheck);
}
*/
