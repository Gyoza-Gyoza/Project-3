using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PayloadBehaviour : Entity
{
    [SerializeField] private float turnSpeed;

    public float CheckpointProgress
    { 
        get { return Mathf.Clamp01((checkpointDistance - agent.remainingDistance) / checkpointDistance); } 
    }

    private List<Stage> stages;
    private NavMeshAgent agent;
    private float checkpointDistance;
    private bool isMoving;

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
        stages = LevelDirector.Instance.CurrentLevel.Stages;

        InitializeAgent();
    }
    private void Update()
    {
        ReachedDestination();
    }
    private void InitializeAgent()
    {
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.speed = MovementSpeed;
        agent.angularSpeed = turnSpeed;
        agent.Warp(stages[LevelDirector.Instance.CurrentStage].Checkpoint);
        GetNextCheckpoint();
        agent.isStopped = true;
    }
    private void GetNextCheckpoint()
    {
        LevelDirector.Instance.ReachedCheckpoint();
        if (LevelDirector.Instance.CurrentStage < stages.Count) agent.SetDestination(stages[LevelDirector.Instance.CurrentStage].Checkpoint);
        StartCoroutine(GetPath());
    }
    private IEnumerator GetPath()
    {
        while (agent.pathPending) yield return null;

        checkpointDistance = agent.remainingDistance;
        Debug.Log($"Checkpoint distance is {checkpointDistance}, remaining distance is {agent.remainingDistance}");
    }
    private void ReachedDestination()
    {
        if (agent.remainingDistance <= 0.05f) GetNextCheckpoint();
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
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            isMoving = true;
            agent.isStopped = false;
            Debug.Log("Payload moving");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            isMoving = false;
            agent.isStopped = true;
            Debug.Log("Payload stopped");
        }
    }
}
