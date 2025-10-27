using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class EnemySpawn : Entity, IDamageable
{
    public bool isSpawning { get; private set; } = false;
    public GameObject flicker;
    public Material flickerMat;
    public float flickerStay = 1f;
    public float flickerFade = 1f;
    public float iniAlpha = 0f;
    private bool _flickering = false;
    private float _count = 0f;
    private float _calcAlpha = 0f;

    public void StartSpawning()
    { isSpawning = true; }
    public void StopSpawning()
    { isSpawning = false; Debug.Log("Spawn has stopped spawning"); }


    IEnumerator Damaged()
    {
        _flickering = true;

        flicker.SetActive(true);
        _calcAlpha = iniAlpha;
        //if (flickerMat.HasFloat("_Alpha"))
        //{
        //    _calcAlpha = flickerMat.GetFloat("_Alpha");
        //}
        //else
        //{
        //    Debug.Log("Float cannot be found");
        //    _calcAlpha = 1;
        //}

        _count = 0f;

        while (_flickering)
        {
            _count += Time.deltaTime;

            if (_count > flickerStay)
            {
                if (_count > flickerStay + flickerFade)
                {
                    _flickering = false;
                }
                float calc = (1f - ((_count - flickerStay)/ flickerFade)) * _calcAlpha;
                flickerMat.SetFloat("_Alpha", calc);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }

        flicker.SetActive(false);
        yield break;
    }

    public override void OnDeath()
    {
        //base.OnDeath();
        Debug.Log("Spawn died");
        StopSpawning();
        this.gameObject.SetActive(false);
    }

    protected override void OnDamage()
    {
        if (_flickering)
        {
            _count = 0f;
        }
        else
        {
            StartCoroutine(Damaged());
        }


        Debug.Log("Spawn is taking damage");
        //throw new System.NotImplementedException();
    }

    protected override void OnHeal()
    {
        //throw new System.NotImplementedException();
    }
}
