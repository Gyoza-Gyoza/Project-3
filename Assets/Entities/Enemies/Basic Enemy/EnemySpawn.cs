using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : Entity
{
    public bool isSpawning { get; private set; } = false;

    public void StartSpawning()
    { isSpawning = true; }
    public void StopSpawning()
    { isSpawning = false; }


    public override void OnDeath()
    {
        //base.OnDeath();
        StopSpawning();
        this.gameObject.SetActive(false);
    }

    protected override void OnDamage()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnHeal()
    {
        //throw new System.NotImplementedException();
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
