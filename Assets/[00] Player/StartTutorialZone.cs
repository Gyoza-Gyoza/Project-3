using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartTutorialZone : MonoBehaviour
{
    bool triggered = false;
    public GameObject tutorialWall;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (triggered && LevelDirector.Instance.AreAllSpawnersGone())
        {
            tutorialWall.SetActive(false);
            this.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag ==  "Player")
        {
            triggered = true;
            LevelDirector.Instance.CanSpawnEnemies = true;
            //this.gameObject.SetActive(false);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            other.GetComponent<EnemyBehaviour>().OnDeath();
        }
    }
}
