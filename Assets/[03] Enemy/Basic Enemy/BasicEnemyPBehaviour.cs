using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class BasicEnemyPBehaviour : EnemyBehaviour
{
    public float aggroRange = 1f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;
    public bool isDead = false;
    [Tooltip("How often the enemy checks for targets, measured in once per x seconds")]
    public float targetCheckInterval = 3f;
    [SerializeField] private HitBox hb;
    [SerializeField] private HitBox payloadhb;

    [Header("Visual Variables")]
    [SerializeField] protected GameObject skin;
    private SkinnedMeshRenderer skinRenderer;
    [SerializeField] protected Material hitMat;
    protected Material oriMat;
    [SerializeField] private GameObject ball;
    [SerializeField] private GameObject mesh;
    [SerializeField] private GameObject deathParticleSystem;
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
    protected override void Awake()
    {
        base.Awake();
        oriMat = skin.GetComponent<SkinnedMeshRenderer>().material;
        hb.HitBoxListeners += DamagePlayer;
        payloadhb.HitBoxListeners += DamagePayload;
        skinRenderer = skin.GetComponent<SkinnedMeshRenderer>();
    }
    protected override void Start()
    {
        base.Start();
        State = new BasicEnemyPChaseState(this);
        isDead = false;
    }
    public override void Attack()
    {
        Debug.Log("Enemy Attack Called");
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updateRotation = false;
        Animator.Play("Attack");
    }
    protected override void OnDamaged(GameObject source)
    {
        base.OnDamaged(source);
        if (State is BasicEnemyDeathState) return;
        StartCoroutine(DamageFlicker());
        State = new BasicEnemyPKnockUpState(this, knockupDuration, CalculateKnockBack
            (false, source.transform), upDrag, fallForce, downDrag, recoveryTime);
        Debug.Log("Enemy on damage called");
    }
    public override void TakeDamageNoKnockback()
    {
        State = new BasicEnemyPStunState(this, 2.5f); 
        StartCoroutine(DamageFlicker());
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

    public bool ReturnDeathState()
    {
        if (isDead) return true;
        else return false;
    }
    public override void OnDeath()
    {
        //Do something else
        //Debug.Log("Enemy on death called");
        base.OnDeath();
        isDead = true;
        State = new BasicEnemyPDeathState(this, deathKnockUpDuration, CalculateKnockBack
            (true, PlayerController3P.instance.transform), deathUpDrag, deathFallForce, deathDownDrag, recoveryTime);
    }
    public virtual void DamagePlayer(GameObject toDamage)
    {
        if (toDamage.TryGetComponent<PlayerController3P>(out PlayerController3P player))
        {
            player.TakeDamage(Damage, gameObject);
        }
    }

    public virtual void DamagePayload(GameObject toDamage)
    {
        if (toDamage.TryGetComponent<PayloadBehaviour>(out PayloadBehaviour payload))
        {
            payload.TakeDamage(Damage, gameObject);
        }
    }
    public void PlayDeathCoroutine()
    {
        StartCoroutine(DeathCouroutine());
    }
    IEnumerator DamageFlicker()
    {
        skinRenderer.material = hitMat;
        yield return new WaitForSeconds(.22f);
        skinRenderer.material = oriMat;
        yield break;
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
            yield return null;
        }
        mesh.SetActive(false);
        ParticleSystem particleSystem = deathParticleSystem.GetComponent<ParticleSystem>();
        //particleSystem.Stop();
        yield return new WaitUntil(() => !particleSystem.IsAlive());

        GameObjectPool.ReturnObject(gameObject);
        yield break;
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        ball.SetActive(true);
        mesh.SetActive(true);
        State = new BasicEnemyPChaseState(this);
        transform.localScale = Vector3.one;
    }
}
