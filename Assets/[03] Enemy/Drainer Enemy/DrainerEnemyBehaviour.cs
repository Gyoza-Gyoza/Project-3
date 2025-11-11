using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrainerEnemyBehaviour : EnemyBehaviour
{
    [SerializeField] SkinnedMeshRenderer skin;
    [SerializeField] private float targetCheckFrequency = 10f;
    [SerializeField] protected Material hitMat;
    protected Material oriMat;
    [SerializeField] private GameObject shield;
    [SerializeField] private float shieldUpTime = 0.5f;
    public float TargetCheckFrequency
    { get { return targetCheckFrequency; } }
    [SerializeField] private float aggroRange = 30f;
    [SerializeField] private float defendingDefense = 2f;
    [SerializeField] private float defendRange = 10f;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Transform lineStart; 
    public float payloadTargetOffsetY = 1.5f;
    public Transform LineStart
    { get { return lineStart; } }
    public LineRenderer Line
    { get { return line; } }
    public float DefendRange
    { get { return defendRange; } }
    public float DefendingDefense
    { get { return defendingDefense; } }
    public float defense
    { get; set; }

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody>();
        oriMat = skin.GetComponent<SkinnedMeshRenderer>().material;
    }
    protected override void Start()
    {
        base.Start();
        State = new DrainerEnemyIdleState(this);
        defense = 1f;
    }
    protected override void Update()
    {
        base.Update();
    }
    public override void TakeDamage(int amount, GameObject source)
    {
        Debug.Log($"{name} taking {amount} damage");
        health -= (int)(amount / defense);
        if (health > 0) OnDamaged(source);
        if (health <= 0)
        {
            health = 0; // Ensure health doesn't go below zero
            OnDeath();
        }
    }
    public bool CanHitTarget()
    {
        if (Vector3.Distance(transform.position, PayloadBehaviour.Instance.transform.position) <= aggroRange)
        {
            Vector3 direction = (PayloadBehaviour.Instance.transform.position - transform.position).normalized;
            
            if (Physics.Raycast(lineStart.position, direction , out RaycastHit hit, Mathf.Infinity))
            {
                if (hit.collider.gameObject.CompareTag("Payload"))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool CheckPlayerInRange()
    {
        if (Vector3.Distance(transform.position, PlayerController3P.Instance.transform.position) <= defendRange)
        {
            return true;
        }
        return false;
    }
    public override void Attack()
    {
        PayloadBehaviour.Instance.RemoveGas(burnAdjAmount * Time.deltaTime);
    }
    public void SetShieldActive(bool active)
    {
        StopCoroutine("SetShieldActiveCoroutine");
        StartCoroutine(SetShieldActiveCoroutine(active));
    }
    private IEnumerator SetShieldActiveCoroutine(bool active)
    {
        float timer = 0f;
        Vector3 start = active ? Vector3.zero : Vector3.one; 
        Vector3 end = active ? Vector3.one : Vector3.zero;

        while (timer <= shieldUpTime)
        {
            timer += Time.deltaTime;

            shield.transform.localScale = Vector3.Lerp(start, end, timer / shieldUpTime);
            yield return null;
        }
    }
    protected override void OnDamaged(GameObject source)
    {
        StartCoroutine(DamageFlicker());
        Debug.Log(Health);
    }
    public override void OnDeath()
    {
        State = new DrainerEnemyDeathState(this);
        GameObjectPool.ReturnObject(gameObject);
    }
    IEnumerator DamageFlicker()
    {
        skin.GetComponent<SkinnedMeshRenderer>().material = hitMat;
        yield return new WaitForSeconds(.1f);
        skin.GetComponent<SkinnedMeshRenderer>().material = oriMat;
        yield break;
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        State = new DrainerEnemyIdleState(this);
        defense = 1f;
    }
}
