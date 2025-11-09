using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TauntDevice : Device
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float deactivationAnimationDuration;
    [SerializeField] private GameObject activationEffect;
    [SerializeField] private float activationDelay;
    private Animator anim;
    private SphereCollider col;
    private SphereCollider trigger;
    private Rigidbody rb;
    private List<BasicEnemyBehaviour> enemiesInRange = new List<BasicEnemyBehaviour>();

    private void Awake()
    {
        SphereCollider[] cols = GetComponents<SphereCollider>();
        foreach (SphereCollider collider in cols)
        {
            if (collider.isTrigger) trigger = collider;
            else col = collider;
        }
        trigger.radius = Range;
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        InitializeDevice();
    }
    private void Update()
    {
        if (isActive)
        {
            timer += Time.deltaTime; 

            if (timer >= Duration)
            {
                DeactivateDevice();
            }
        }
    }
    private void InitializeDevice()
    {
        timer = 0f;
        enemiesInRange.Clear();
        rb.isKinematic = false;
    }
    public override void ActivateDevice()
    {
        anim.Play("Enable");

        Collider[] hits = Physics.OverlapSphere(transform.position, Range, enemyLayer);

        foreach (Collider hit in hits)
        {
            BasicEnemyBehaviour enemy = hit.GetComponent<BasicEnemyBehaviour>();

            if (enemy != null)
            {
                enemiesInRange.Add(enemy);
            }
            enemy.State = new BasicEnemyTauntState(enemy, transform);
        }

        isActive = true;
    }
    private IEnumerator ActivateDeviceCoroutine()
    {
        yield return new WaitForSeconds(activationDelay);
        ActivateDevice();
    }
    public void DeactivateDevice()
    {
        Debug.Log("Deactivating Taunt Device");
        StartCoroutine(DeactiveCoroutine());
    }
    private IEnumerator DeactiveCoroutine()
    {
        isActive = false;
        foreach (BasicEnemyBehaviour enemy in enemiesInRange)
        {
            if (enemy != null && enemy.State is BasicEnemyTauntState)
            {
                enemy.State = new BasicEnemyChaseState(enemy);
            }
        }
        // Play deactivation animation here
        yield return new WaitForSeconds(deactivationAnimationDuration);
        GameObjectPool.ReturnObject(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 7) // Environment layer is 7
        {
            rb.isKinematic = true;
            StartCoroutine(ActivateDeviceCoroutine());
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            BasicEnemyBehaviour enemy = other.GetComponent<BasicEnemyBehaviour>();
            if (enemy != null)
            {
                if (!enemiesInRange.Contains(enemy))
                {
                    enemiesInRange.Add(enemy);
                    enemy.State = new BasicEnemyTauntState(enemy, transform);
                }
            }
        }
    }
}
