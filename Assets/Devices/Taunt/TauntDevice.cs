using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TauntDevice : Device
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float deactivationAnimationDuration; 
    private SphereCollider col;
    private List<EnemyBehaviour> enemiesInRange = new List<EnemyBehaviour>();

    private void Awake()
    {
        col = GetComponent<SphereCollider>();
        col.radius = Range;
    }
    private void OnEnable()
    {
        InitializeDevice();
        ActivateDevice();
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
    }
    public override void ActivateDevice()
    {
        // Play activation animation here

        Collider[] hits = Physics.OverlapSphere(transform.position, Range, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyBehaviour enemy = hit.GetComponent<EnemyBehaviour>();

            if (enemy != null)
            {
                enemiesInRange.Add(enemy);
            }
            enemy.state = new EnemyTauntState(enemy, transform);
        }

        isActive = true;
    }
    public void DeactivateDevice()
    {
        Debug.Log("Deactivating Taunt Device");
        StartCoroutine(DeactiveCoroutine());
    }
    private IEnumerator DeactiveCoroutine()
    {
        isActive = false;
        foreach (EnemyBehaviour enemy in enemiesInRange)
        {
            if (enemy != null && enemy.state is EnemyTauntState)
            {
                enemy.state = new EnemyChaseState(enemy);
            }
        }
        // Play deactivation animation here
        yield return new WaitForSeconds(deactivationAnimationDuration);
        GameObjectPool.ReturnObject(gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyBehaviour enemy = other.GetComponent<EnemyBehaviour>();
            if (enemy != null)
            {
                if (!enemiesInRange.Contains(enemy))
                {
                    enemiesInRange.Add(enemy);
                    enemy.state = new EnemyTauntState(enemy, transform);
                }
            }
        }
    }
}
