using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PayloadBehaviour : Entity
{
    [SerializeField] private float turnSpeed;
    [SerializeField] private float returnSpeed;
    



    [SerializeField] private Slider gasSlider;

    [SerializeField] private float burningRate = 0.5f;
    [SerializeField] private float fillingRate = 1f;
    [SerializeField] private int maxGas = 100;
    private bool fillingGas = false;
    private bool burningGas = false;
    private int currentGas;

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
    { get { return interactRadius * transform.localScale.z; } } // Hard coding :( //Its okay Hard coding is fine
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

        if (currentGas > 0 && !burningGas)
        {
            //Debug.Log("Burning conditions triggered");
            StartBurningGas();
        }

        gasSlider.transform.parent.transform.LookAt(Discon_PlayerController.Instance.transform, Vector3.up);
    }

    #region -----------------------Gas--------------------------------
    private void UpdateGasSlider()
    {
        gasSlider.value = (float)currentGas/(float)maxGas;
    }

    public void StartFillingGas()
    { fillingGas = true; StartCoroutine(fillGas());}

    public void StopFillingGas()
    { fillingGas = false; }

    IEnumerator fillGas()
    {
        float count = 0f;

        //Debug.Log("Starting to fill gas");
        while (fillingGas)
        {
            UpdateGasSlider();
            count += Time.deltaTime;
            if (count > 1f / fillingRate) 
            {
                count -= (1f / fillingRate);
                if (Discon_PlayerController.Instance.RemoveGas(1) == false)
                {
                    StopFillingGas();
                }
                else
                {
                    currentGas += 1;
                }
            }

            if (currentGas >= maxGas)
            {
                currentGas = maxGas;
                StopFillingGas();
            }


            yield return new WaitForSeconds(Time.deltaTime);
        }
        //Debug.Log("Stop to fill gas");
        yield break;
    }

    public void StartBurningGas()
    { 
        burningGas = true;
        ForwardFacing();
        agent.speed = MovementSpeed;
        StartCoroutine(burnGas());
    }

    public void StopBurningGas()
    {
        Debug.Log("Stop Burning");
        burningGas = false;
        BackwardFacing();
        agent.speed = returnSpeed;
    }

    IEnumerator burnGas()
    {
        float count = 0f;


        while (currentGas > 0)
        {
            PayloadBehaviour.Instance.agent.isStopped = false;
            //Debug.Log($"Payload Moving, current Gas {currentGas}");
            UpdateGasSlider();
            count += Time.deltaTime;
            if (count > 1f / burningRate)
            {
                count -= (1f / burningRate);
                currentGas -= 1;
            }

            //Debug.Log("Gas still burning");
            yield return new WaitForSeconds(Time.deltaTime);
        }
        currentGas = 0;
        StopBurningGas();

        //PayloadBehaviour.Instance.agent.isStopped = true;
        //Debug.Log($"Payload Stopped, current Gas is {currentGas}");
        yield break;
    }

    #endregion

    #region -----------------------Facing--------------------------------
    public void ForwardFacing()
    {
        Debug.Log("Front Facing");
        //agent.SetDestination(stages[LevelDirector.Instance.CurrentStage].);
        if (stages[LevelDirector.Instance.CurrentStage] is Escort stage)
        {
            Debug.Log("Front Facing Success");
            agent.SetDestination(stage.Checkpoint);
        }
    }
    public void BackwardFacing()
    {
        Debug.Log("Back Facing");
        if (stages[LevelDirector.Instance.CurrentStage] is Escort stage)
        {
            Debug.Log("Back Success");
            agent.SetDestination(stage.PreviousCheckpoint);
        }
    }
    #endregion


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
