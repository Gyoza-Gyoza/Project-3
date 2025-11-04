using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChargerEnemyBehaviour : EnemyBehaviour
{
    [SerializeField] private float deathTime = 1f;
    [SerializeField] private GameObject mesh;
    private SkinnedMeshRenderer[] meshRenderers;
    private Material oriMat;
    [SerializeField] private Material hitMat;
    public float attackInterval, attackRange;

    [Header("Physics Variables")]
    [SerializeField] private float hitHorforce = 1f;
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private float deathHitHorForce = 1f;

    protected override void Awake()
    {
        base.Awake();
        meshRenderers = mesh.GetComponentsInChildren<SkinnedMeshRenderer>();
        oriMat = meshRenderers[0].material;
    }
    protected override void Start()
    {
        base.Start();
        State = new ChargerEnemyChaseState(this);
    }
    public override void Attack()
    {

    }
    protected override void OnDamage(GameObject source)
    {
        StartCoroutine(DamageFlicker());
        State = new ChargerEnemyStunState(this, stunDuration, CalculateKnockBack(PlayerController3P.Instance.transform));
    }
    private Vector3 CalculateKnockBack(Transform source)
    {
        // Calculate force and direction 
        Vector3 difference = this.transform.position - source.position;
        Vector3 direction = new Vector3(difference.x, 0, difference.z).normalized;
        Vector3 force = Vector3.up + direction * hitHorforce;

        return force;
    }
    public override void TakeDamageNoKnockback()
    {
        State = new ChargerEnemyStunState(this, 2.5f, Vector3.zero);
        StartCoroutine(DamageFlicker());
    }
    IEnumerator DamageFlicker()
    {
        foreach (SkinnedMeshRenderer mr in meshRenderers)
        {
            mr.material = hitMat;
        }

        yield return new WaitForSeconds(.1f);

        foreach (SkinnedMeshRenderer mr in meshRenderers)
        {
            mr.material = oriMat;
        }
    }
    protected override void OnEnable()
    {
        State = new ChargerEnemyChaseState(this);
        transform.localScale = Vector3.one;
    }
    public override void OnDeath()
    {
        base.OnDeath();
        if (State is not ChargerEnemyDeathState)
            State = new ChargerEnemyDeathState(this, stunDuration, CalculateKnockBack(PlayerController3P.Instance.transform));
    }
    public void PlayDeathCoroutine()
    {
        StartCoroutine(DeathCouroutine());
    }
    IEnumerator DeathCouroutine()
    {
        agent.enabled = false;
        float count = 0f;
        while (count <= deathTime)
        {
            count += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1f, 0f, 1f), count / deathTime);
            yield return null;
        }
        GameObjectPool.ReturnObject(gameObject);
        yield break;
    }
}
