using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemyBehaviour : EnemyBehaviour
{
    [Header("Visual Variables")]
    [SerializeField] private GameObject ball;
    [SerializeField] private GameObject mesh;
    [SerializeField] private GameObject skin;
    [SerializeField] private GameObject deathParticleSystem;
    [SerializeField] private Material hitMat;
    [SerializeField] private Material oriMat;
    [SerializeField] private float deathTime = 1f;

    [Header("Physics Variables")]
    [SerializeField] private float hitUpforce = 1f;
    [SerializeField] private float hitHorforce = 1f;
    [SerializeField] private float upDrag = 1.5f;
    [SerializeField] private float fallForce = 1f;
    [SerializeField] private float downDrag = 0f;
    [SerializeField] private float knockupDuration = 1f;
    [SerializeField] private float recoveryTime = 1f;
    [SerializeField]
    private float deathHitUpForce = 1f, deathHitHorForce = 1f,
        deathUpDrag = 1.5f, deathFallForce = 1f, deathDownDrag = 0f, deathKnockUpDuration = 1f;
    protected override void Start()
    {
        base.Start();
        State = new EnemyChaseState(this);
        oriMat = skin.GetComponent<SkinnedMeshRenderer>().material;
    }
    protected override void OnDamage()
    {
        StartCoroutine(DamageFlicker());
        //Quaternion f = Quaternion.Euler(new Vector3(45, Vector3.Angle(PlayerController3P.Instance.transform.position, this.transform.position), 0)).normalized;
        State = new EnemyKnockUpState(this, knockupDuration, CalculateKnockBack
            (false, PlayerController3P.Instance.transform), upDrag, fallForce, downDrag, recoveryTime);
        Debug.Log("Enemy on damage called");
    }
    private Vector3 CalculateKnockBack(bool death, Transform source)
    {
        // Calculate force and direction 
        Vector3 difference = this.transform.position - source.position;
        Vector3 direction = new Vector3(difference.x, 0, difference.z).normalized;
        Vector3 force = Vector3.up * (death ? deathHitUpForce : hitUpforce)
            + direction * (death ? deathHitHorForce : hitHorforce);

        return force;
    }
    IEnumerator DamageFlicker()
    {
        skin.GetComponent<SkinnedMeshRenderer>().material = hitMat;
        yield return new WaitForSeconds(.1f);
        skin.GetComponent<SkinnedMeshRenderer>().material = oriMat;
        yield break;
    }
    public override void OnDeath()
    {
        //Do something else
        Debug.Log("Enemy on death called");
        LevelDirector.Instance.EnemyCount -= 1;
        State = new EnemyDeathState(this, deathKnockUpDuration, CalculateKnockBack
            (true, PlayerController3P.Instance.transform), deathUpDrag, deathFallForce, deathDownDrag, recoveryTime);
    }
    public void PlayDeathCoroutine()
    {
        StartCoroutine(DeathCouroutine());
    }
    IEnumerator DeathCouroutine()
    {
        agent.enabled = false;
        deathParticleSystem.SetActive(true);
        ball.SetActive(false);
        float count = 0f;
        while (count <= deathTime)
        {
            count += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1f, 0f, 1f), count / deathTime);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        mesh.SetActive(false);
        ParticleSystem particleSystem = deathParticleSystem.GetComponent<ParticleSystem>();
        particleSystem.Stop();
        while (particleSystem.IsAlive())
        {
            yield return new WaitForSeconds(Time.deltaTime);
        }
        GameObjectPool.ReturnObject(gameObject);
        yield break;
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        ball.SetActive(true);
        mesh.SetActive(true);
    }
}
