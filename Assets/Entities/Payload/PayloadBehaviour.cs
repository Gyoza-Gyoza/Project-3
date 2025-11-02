using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PayloadBehaviour : Singleton<PayloadBehaviour>
{
    private float hinderedMovementSpeed;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float returnSpeed;
    private float extraReturnSpeed;

    [SerializeField] private Slider gasSlider;

    [SerializeField] private float burningRate = 0.5f;
    private bool burningGas = false;
    private float extraBurningRate = 0f;

    [SerializeField] private float fillingRate = 1f;
    private bool fillingGas = false;

    [SerializeField] private float maxGas = 100;
    [SerializeField] private float iniGas = 0;
    private float currentGas;

    public float CurrentGas
    {
        get { return currentGas; }
        set 
        { 
            currentGas = value;
            if (currentGas < 0) currentGas = 0;
            UpdateGasSlider(); 
        }
    }

    [SerializeField] private Animator animator;

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
    private PayloadStepper stepper;

    protected override void Awake()
    {
        base.Awake();

    }
    protected void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        //if (agent != null)
        //{
        //    agent.updatePosition = false;
        //    agent.updateRotation = false;
        //}

        stepper = GetComponent<PayloadStepper>();

        interactRadius = GetComponent<CapsuleCollider>().radius;
        IniLineRenderer();


        stages = LevelDirector.Instance.Stages;

        InitializeAgent();
        CurrentGas = iniGas;
    }
    private void Update()
    {
        if (LevelDirector.Instance.CurrentStageCounter < stages.Length) stages[LevelDirector.Instance.CurrentStageCounter].DoPayloadBehaviour();

        if (CurrentGas > 0 && !burningGas)
        {
            //Debug.Log("Burning conditions triggered");
            StartBurningGas();
        }

        gasSlider.transform.parent.transform.LookAt(PlayerController3P.Instance.transform, Vector3.up);

        if (agent.hasPath)
        {
            DrawPath();
        }
        //TickStep(Time.deltaTime);           // drive manual movement if a step is active
        //agent.nextPosition = transform.position; // keep agent synced to our manual motion

    }

    #region --------------------------Gas--------------------------------
    private void UpdateGasSlider()
    {
        gasSlider.value = (float)CurrentGas / (float)maxGas;
        HUDController.Instance.SetPayloadEmber((float)CurrentGas / (float)maxGas);
    }

    public void StartFillingGas()
    { fillingGas = true; StartCoroutine(fillGas()); }

    public void StopFillingGas()
    { fillingGas = false; }

    IEnumerator fillGas()
    {
        float count = 0f;

        //Debug.Log("Starting to fill playerEmber");
        while (fillingGas)
        {
            HUDController.Instance.SetPayloadEmber((float)CurrentGas / (float)maxGas);
            count += Time.deltaTime;
            if (count > 1f / fillingRate)
            {
                count -= (1f / fillingRate);
                if (PlayerController3P.Instance.RemoveGas(1) == false)
                {
                    StopFillingGas();
                }
                else
                {
                    CurrentGas += 1;
                }
            }

            if (CurrentGas >= maxGas)
            {
                CurrentGas = maxGas;
                StopFillingGas();
            }


            yield return new WaitForSeconds(Time.deltaTime);
        }
        //Debug.Log("Stop to fill playerEmber");
        yield break;
    }

    public void StartBurningGas()
    {
        // Debug.Log("Starting to burn playerEmber");
        // burningGas = true;
        // ForwardFacing();
        // agent.speed = MovementSpeed - hinderedMovementSpeed;
        // animator.SetBool("Walking", true);
        // StartCoroutine(burnGas());

        // existing logic to start burning
        burningGas = true;

        // #EZE -- Coroutine to start burning gas 
        StartCoroutine(burnGas());
        SetStepsActive(true);

        //// start walking animation flag
        //if (animator != null)
        //{
        //    animator.SetBool("Walking", true);
        //}

        //// resume the stepper (unpause agent movement)
        //if (stepper != null)
        //{
        //    stepper.ResumeStepper();
        //}
        //else
        //{
        //    // fallback: un-stop agent so old code still moves (not preferred)
        //    if (agent != null) agent.isStopped = false;
        //}
    }

    public void StopBurningGas()
    {
        // //Debug.Log("Stop Burning");
        // burningGas = false;
        // //BackwardFacing();
        // //agent.speed = returnSpeed + extraReturnSpeed;
        // animator.SetBool("Walking", false);
        // agent.isStopped = true;

        burningGas = false;
        SetStepsActive(false);

        // clear walking animation flag
        //if (animator != null)
        //    animator.SetBool("Walking", false);

        //// Pause the stepper and stop pathing so the golem stands still
        //if (stepper != null)
        //{
        //    stepper.StopStepperImmediate();
        //    // clear agent path to ensure it doesn't later automatically go to previous escortPosition
        //    if (agent != null)
        //    {
        //        //agent.ResetPath();
        //        agent.isStopped = true;
        //    }
        //}
        //else
        //{
        //    // fallback: stop the agent
        //    if (agent != null)
        //    {
        //        agent.isStopped = true;
        //        //agent.ResetPath();
        //    }
        //}
    }

    IEnumerator burnGas()
    {
        float count = 0f;


        while (CurrentGas > 0)
        {
            //PayloadBehaviour.Instance.agent.isStopped = false;
            //Debug.Log($"Payload Moving, current Gas {CurrentGas}");
            HUDController.Instance.SetPayloadEmber((float)CurrentGas / (float)maxGas);
            count += Time.deltaTime;
            if (count > 1f / (burningRate + extraBurningRate))
            {
                count -= (1f / (burningRate + extraBurningRate));
                CurrentGas -= 1;
            }

            //Debug.Log("Gas still burning");
            yield return new WaitForSeconds(Time.deltaTime);
        }
        CurrentGas = 0;
        StopBurningGas();

        //PayloadBehaviour.Instance.agent.isStopped = true;
        //Debug.Log($"Payload Stopped, current Gas is {currentGas}");
        yield break;
    }
    public void RemoveGas(float gasToRemove)
    {
        CurrentGas -= gasToRemove;
    }

    #endregion

    //#region -----------------------Facing--------------------------------

    //private bool isFacingForward = true;
    //public void ForwardFacing()
    //{
    //    Debug.Log("Front Facing on payload called");
    //    //agent.SetDestination(stages[LevelDirector.Instance.CurrentStage].);
    //    if (stages[LevelDirector.Instance.CurrentStage] is Escort stage)
    //    {
    //        Debug.Log("Front Facing Success");
    //        stage.FaceForward();
    //        isFacingForward = true;
    //        //agent.SetDestination(stage.EscortPosition);
    //    }
    //}
    //public void BackwardFacing()
    //{
    //    Debug.Log("Back Facing on payload called");
    //    if (stages[LevelDirector.Instance.CurrentStage] is Escort stage)
    //    {
    //        Debug.Log("Back Success");
    //        stage.FaceBackwards();
    //        isFacingForward = false;
    //        //agent.SetDestination(stage.PreviousStage);
    //    }
    //}
    //#endregion

    #region ------------------Enemy Surrounding Behaviour----------------
    public void EnemyPushing(float burnAdj, float moveSpeedAdj/*, float returnSpeedAdj*/)
    {
        //Debug.Log("Enemy Pushing");
        extraBurningRate += burnAdj;
        hinderedMovementSpeed += moveSpeedAdj;
        //extraReturnSpeed += returnSpeedAdj;
        HUDController.Instance.StartHighlight();
    }


    public void EnemyExit(float burnAdj, float moveSpeedAdj/*, float returnSpeedAdj*/)
    {
        //Debug.Log("Enemy Stop Pushing");
        extraBurningRate -= burnAdj;
        hinderedMovementSpeed -= moveSpeedAdj;
        //extraReturnSpeed -= returnSpeedAdj;
        HUDController.Instance.StopHighlight();
    }
    #endregion


    private void InitializeAgent()
    {
        agent.speed = movementSpeed;
        agent.angularSpeed = turnSpeed;

        //agent.updatePosition = false;

        if (stages[LevelDirector.Instance.CurrentStageCounter] is Escort escort) agent.Warp(escort.EscortPosition);
        CompleteStage(); //Complete the beginning one
        agent.isStopped = true;
    }
    public void CompleteStage()
    {
        //Debug.Log($"Complete Stage called for stage {LevelDirector.Instance.CurrentStage + 1}");
        LevelDirector.Instance.CompletedStage();
        if (LevelDirector.Instance.CurrentStageCounter < stages.Length)
        {
            stages[LevelDirector.Instance.CurrentStageCounter].StartStage();
            // Ensures that the player in range behaviour stays the same when changing stages
            if (playerInRange) stages[LevelDirector.Instance.CurrentStageCounter].PlayerInRange();
            else stages[LevelDirector.Instance.CurrentStageCounter].PlayerOutOfRange();

            if (stages[LevelDirector.Instance.CurrentStageCounter] is Escort escort) StartCoroutine(GetPath(escort));
        }
        else LevelDirector.Instance.CompleteLevel();
    }
    private IEnumerator GetPath(Escort escort)
    {
        while (agent.pathPending) yield return null;

        escort.EscortDistance = agent.remainingDistance;

        yield break;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerInRange = true;
            stages[LevelDirector.Instance.CurrentStageCounter].PlayerInRange();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerInRange = false;
            stages[LevelDirector.Instance.CurrentStageCounter].PlayerOutOfRange();
        }
    }

    private LineRenderer lineRenderer;
    [SerializeField] private float rendererWidth = .15f;
    [SerializeField] private Color32 forwardStartColor = Color.green;
    [SerializeField] private Color32 forwardEndColor = Color.green;
    [SerializeField] private Color32 backwardStartColor = Color.red;
    [SerializeField] private Color32 backwardEndColor = Color.red;
    [SerializeField] private float verticleOffset = 1f;

    private void IniLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = rendererWidth;
        lineRenderer.endWidth = rendererWidth;
    }

    private void DrawPath()
    {
        lineRenderer.positionCount = agent.path.corners.Length;
        lineRenderer.SetPosition(0, transform.position + new Vector3(0, verticleOffset, 0));

        lineRenderer.startColor = forwardStartColor;
        lineRenderer.endColor = forwardEndColor;

        //if (isFacingForward)
        //{
        //    lineRenderer.startColor = forwardStartColor;
        //    lineRenderer.endColor = forwardEndColor;
        //}
        //else
        //{
        //    lineRenderer.startColor = backwardStartColor;
        //    lineRenderer.endColor = backwardEndColor;
        //}

        if (agent.path.corners.Length < 2)
        {
            return;
        }

        for (int i = 0; i < agent.path.corners.Length; i++)
        {
            Vector3 pointPosition = new Vector3(agent.path.corners[i].x, agent.path.corners[i].y, agent.path.corners[i].z);
            lineRenderer.SetPosition(i, pointPosition + new Vector3(0, verticleOffset, 0));
        }
    }

    #region ------------------ Golem Step ----------------

    // --- Step Locomotion (manual stride timing) ---
    //[Header("Step Locomotion")]
    //[SerializeField] private float strideDistance = 0.8f;
    //[SerializeField] private float strideDuration = 0.35f;

    //private bool _isStepping;
    //private Vector3 _stepDir;
    //private Vector3 _stepStartPos;
    //private Vector3 _stepTarget;
    //private float _stepTimer;
    //[SerializeField] private float navSampleMaxDist = 2f; // how far to look for the surface
    //[SerializeField] float rotationSpeed = 6f; // 4–10 feels good for a heavy golem

    //public void StepStart_L() => BeginStep();
    //public void StepEnd_L() => EndStep();
    //public void StepStart_R() => BeginStep();
    //public void StepEnd_R() => EndStep();

    //private void BeginStep()
    //{
    //    if (!burningGas || !agent.hasPath) { _isStepping = false; return; }

    //    // Use next corner for direction (includes turns on ramps)
    //    Vector3 toCorner = agent.steeringTarget - transform.position;
    //    toCorner.y = 0f;
    //    if (toCorner.sqrMagnitude < 0.0001f) toCorner = transform.forward;
    //    toCorner.Normalize();

    //    float maxAllowed = Mathf.Max(0f, agent.remainingDistance - agent.stoppingDistance);
    //    float thisStep = Mathf.Min(strideDistance, maxAllowed);

    //    _stepStartPos = transform.position;
    //    var flatTarget = _stepStartPos + toCorner * thisStep;

    //    // *** critical: put the target ON the navmesh/ramp ***
    //    _stepTarget = SnapToNavMeshY(flatTarget);

    //    _stepTimer = 0f;
    //    _isStepping = thisStep > 0f;
    //}

    //private void EndStep()
    //{
    //    if (_isStepping)
    //    {
    //        transform.position = _stepTarget;
    //        _isStepping = false;
    //        agent.nextPosition = transform.position;
    //    }
    //}

    //private void TickStep(float dt)
    //{
    //    if (!_isStepping) return;

    //    _stepTimer += dt;
    //    float t = Mathf.Clamp01(_stepTimer / strideDuration);

    //    // your existing movement (snapped to navmesh)
    //    Vector3 p = Vector3.Lerp(_stepStartPos, _stepTarget, t);
    //    p = SnapToNavMeshY(p);
    //    transform.position = p;

    //    // NEW: face the agent’s steering target
    //    FaceSteeringTarget(dt);

    //    // keep agent synced to your manual movement
    //    agent.nextPosition = transform.position;

    //    if (t >= 1f) EndStep();
    //}


    //private Vector3 SnapToNavMeshY(Vector3 pos)
    //{
    //    if (NavMesh.SamplePosition(pos, out var hit, navSampleMaxDist, NavMesh.AllAreas))
    //        pos.y = hit.position.y;
    //    else
    //    {
    //        // Fallback: raycast against level colliders if no NavMesh found (optional)
    //        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down, out var rh, 10f))
    //            pos.y = rh.point.y;
    //    }
    //    return pos;
    //}

    //private void FaceSteeringTarget(float dt)
    //{
    //    // If the agent has a path, look toward its steering target (next corner)
    //    if (agent != null && !agent.pathPending)
    //    {
    //        Vector3 lookDir = agent.steeringTarget - transform.position;
    //        lookDir.y = 0f; // keep yaw-only rotation

    //        // Fallback: if steering target is on top of us (e.g., end of path), keep current forward
    //        if (lookDir.sqrMagnitude > 0.001f)
    //        {
    //            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * rotationSpeed);
    //        }
    //    }
    //}

    [Tooltip("Duration that the payload will move before stopping")]
    [SerializeField] private float stepDuration = 0.35f;
    [Tooltip("Pause before starting the next step")]
    [SerializeField] private float stepInterval = 0.5f;
    private bool leftStep;
    private bool stepping;

    public void SetStepsActive(bool state)
    {
        stepping = state;
        if (stepping)
        {
            leftStep = true;
            StartCoroutine(PlayStep());
        }
    }
    private IEnumerator PlayStep()
    {
        animator.SetBool("Stop", false);

        while (true)
        {
            string clip = leftStep ? "LeftStep" : "RightStep";
            string sfx = leftStep ? "Golem_LeftStomp" : "Golem_RightStomp";

            animator.Play(clip);
            agent.isStopped = false; 

            yield return new WaitForSeconds(stepDuration);
            Debug.Log(agent.isStopped);
            agent.isStopped = true;

            yield return new WaitForSeconds(stepInterval);

            leftStep = !leftStep;

            if (!stepping && !leftStep) break;
        }

        animator.SetBool("Stop", true);
    }

    public void PlayStepLeftSFX()
    {
        AudioManager.Instance.PlaySFX("Golem_LeftStomp", transform.position);
    }

    public void PlayStepRightSFX()
    {
        AudioManager.Instance.PlaySFX("Golem_RightStomp", transform.position);
    }

    public void PlayStopSFX()
    {
        AudioManager.Instance.PlaySFX("Golem_Stop", transform.position);
    }

    public void PlayPreStopSFX()
    {
        AudioManager.Instance.PlaySFX("Golem_PreStop", transform.position);
    }

    public void PlayStandSFX()
    {
        AudioManager.Instance.PlaySFX("Golem_Stand", transform.position);
    }

    #endregion

}
