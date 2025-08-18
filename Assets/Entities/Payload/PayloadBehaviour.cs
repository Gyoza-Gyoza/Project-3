using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PayloadBehaviour : Entity
{
    [SerializeField] private float turnSpeed;

    //public float CheckpointProgress
    //{ 
    //    get { return Mathf.Clamp01((checkpointDistance - agent.remainingDistance) / checkpointDistance); } 
    //}

    private Stage[] stages;
    private NavMeshAgent agent;
    public NavMeshAgent Agent
    {
        get { return agent; }
    }
    //private float checkpointDistance;
    //public float CheckpointDistance
    //{
    //    get { return checkpointDistance; }
    //    set { checkpointDistance = value; }
    //}
    private float interactRadius;
    public float InteractRadius
    { get { return interactRadius * transform.localScale.z; } } // Hard coding :(
    private bool playerInRange = false;
    public static PayloadBehaviour Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(Instance);

        agent = GetComponent<NavMeshAgent>();
        interactRadius = GetComponent<SphereCollider>().radius;
    }
    protected override void Start()
    {
        base.Start();
        stages = LevelDirector.Instance.Stages;

        InitializeAgent();
    }
    private void Update()
    {
        if (LevelDirector.Instance.CurrentStage < stages.Length) stages[LevelDirector.Instance.CurrentStage].DoPayloadBehaviour();
    }
    private void InitializeAgent()
    {
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.speed = MovementSpeed;
        agent.angularSpeed = turnSpeed;
        if (stages[LevelDirector.Instance.CurrentStage] is Escort escort) agent.Warp(escort.Checkpoint);
        CompleteStage();
        agent.isStopped = true;
    }
    public void CompleteStage()
    {
        LevelDirector.Instance.CompletedStage();
        if (LevelDirector.Instance.CurrentStage < stages.Length)
        {
            stages[LevelDirector.Instance.CurrentStage].StartStage();
            // Ensures that the player in range behaviour stays the same when changing stages
            if (playerInRange) stages[LevelDirector.Instance.CurrentStage].PlayerInRange();
            else stages[LevelDirector.Instance.CurrentStage].PlayerOutOfRange();

            if (stages[LevelDirector.Instance.CurrentStage] is Escort escort) StartCoroutine(GetPath(escort));
        }
        else LevelDirector.Instance.CompleteLevel();
    }
    private IEnumerator GetPath(Escort escort)
    {
        while (agent.pathPending) yield return null;

        escort.EscortDistance = agent.remainingDistance;
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
            playerInRange = true;
            stages[LevelDirector.Instance.CurrentStage].PlayerInRange();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            playerInRange = false;
            stages[LevelDirector.Instance.CurrentStage].PlayerOutOfRange();
        }
    }
}
