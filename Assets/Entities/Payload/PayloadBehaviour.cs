using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PayloadBehaviour : Entity
{
    [SerializeField] private float turnSpeed;

    private List<Stage> stages;
    private NavMeshAgent agent;
    private bool reachedCheckpoint;

    public static PayloadBehaviour Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(Instance);

        agent = GetComponent<NavMeshAgent>();
    }
    protected override void Start()
    {
        base.Start();
        stages = SpawnDirector.Instance.CurrentLevel.Stages;
        agent.speed = MovementSpeed;
        agent.angularSpeed = turnSpeed;
        //agent.Warp(stages[SpawnDirector.Instance.CurrentStage].Checkpoint);
        agent.SetDestination(stages[SpawnDirector.Instance.CurrentStage].Checkpoint);
    }
    private void Update()
    {
        Movement();
    }
    private void Movement()
    {
        if (agent.remainingDistance <= 0.05f)
        {
            SpawnDirector.Instance.ReachedCheckpoint();
            agent.SetDestination(stages[SpawnDirector.Instance.CurrentStage].Checkpoint);
        }
    }
    protected override void OnHeal()
    {
    }
    protected override void OnDamage()
    {
    }
    public override void OnDeath()
    {
    }
}
