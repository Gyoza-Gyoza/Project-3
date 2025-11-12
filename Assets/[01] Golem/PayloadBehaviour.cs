using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PayloadBehaviour : Entity //Singleton<PayloadBehaviour>
{
    private float hinderedMovementSpeed;
    [SerializeField] private float payloadMovementSpeed;
    [SerializeField] private bool startPayload = false;

    public void StartPayload()
    {
        if (startPayload == false)
        {
            SetStepsActive(true);
        }
    }

    public void StopPayload()
    {
        if (startPayload == true)
        {
            SetStepsActive(false);
        }
    }


    public float MovementSpeed
    {
        get { return payloadMovementSpeed; }
        set { 
              this.payloadMovementSpeed = value;
            if (agent == null)
            {
                Debug.Log("Agent is empty");
            }
            this.agent.acceleration = this.agent.speed = payloadMovementSpeed;
        }        
    }
    [SerializeField] private float turnSpeed;
    [SerializeField] private float returnSpeed;
    private float extraReturnSpeed;


    [SerializeField] private Slider gasSlider;

    /* //=== EZE'S GAS REMOVE EDIT ===
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
    */ //=== EZE'S GAS REMOVE EDIT ===

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

    [Header("Proximity Shake")]
    public ThirdPersonCamera camRig;
    [SerializeField] private float minRange = 1f;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private float minShakeAmount = 2f;
    [SerializeField] private float maxShakeAmount = 10f;
    [SerializeField] private float minShakeDuration = 0.06f;
    [SerializeField] private float maxShakeDuration = 0.12f;
    [SerializeField] private float manualShakeDelay = 0.12f;
    private Coroutine _shakeCoroutine = null;
    private bool _shakePending = false;

    //instance
    public static PayloadBehaviour instance = null;

    protected void Awake()
    {

        //Instancing
        if (instance == null) instance = this;
        else Destroy(this);

    }
    protected override void Start()
    {
        base.Start();

        this.agent = GetComponent<NavMeshAgent>();

        if (this.agent == null)
        {
            Debug.Log("Agent is empty from the start");
        }

        stepper = GetComponent<PayloadStepper>();

        interactRadius = GetComponent<CapsuleCollider>().radius;
        IniLineRenderer();


        stages = LevelDirector.Instance.Stages;

        InitializeAgent();
        /* //=== EZE'S GAS REMOVE EDIT ===
        CurrentGas = iniGas;
        */ //=== EZE'S GAS REMOVE EDIT ===

        //=== EZE'S GAS REMOVE EDIT ===
        if (startPayload)
        {
            SetStepsActive(true);
        }
        
        //=== EZE'S GAS REMOVE EDIT ===
    }
    private void Update()
    {
        if (LevelDirector.Instance.CurrentStageCounter == null)
        {
            Debug.Log("Level director instance missing");
        }

        if (LevelDirector.Instance.CurrentStageCounter < stages.Length) stages[LevelDirector.Instance.CurrentStageCounter].DoPayloadBehaviour();

        /* //=== EZE'S GAS REMOVE EDIT ===
        if (CurrentGas > 0 && !burningGas)
        {
            //Debug.Log("Burning conditions triggered");
            StartBurningGas();
        }

        gasSlider.transform.parent.transform.LookAt(PlayerController3P.instance.transform, Vector3.up);
        */ //=== EZE'S GAS REMOVE EDIT ===

        if (agent.hasPath)
        {
            DrawPath();
        }

        //TickStep(Time.deltaTime);           // drive manual movement if a step is active
        //agent.nextPosition = transform.position; // keep agent synced to our manual motion

    }

    /* //=== EZE'S GAS REMOVE EDIT ===

    #region --------------------------Gas--------------------------------
    private void UpdateGasSlider()
    {
        gasSlider.value = (float)CurrentGas / (float)maxGas;
        HUDController.Instance.SetPayloadHealth((float)CurrentGas / (float)maxGas);
    }

    public void StartFillingGas()
    { 
        if (!fillingGas)
        {
            fillingGas = true;
            StartCoroutine(fillGas());
        }
    }

    public void StopFillingGas()
    { fillingGas = false; }

    IEnumerator fillGas()
    {
        float count = 0f;

        //Debug.Log("Starting to fill playerEmber");
        while (fillingGas)
        {
            HUDController.Instance.SetPayloadHealth((float)CurrentGas / (float)maxGas);
            count += Time.deltaTime;
            if (count > 1f / fillingRate)
            {
                count -= (1f / fillingRate);
                if (PlayerController3P.instance.RemoveGas(1) == false)
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
            //PayloadBehaviour.instance.agent.isStopped = false;
            //Debug.Log($"Payload Moving, current Gas {CurrentGas}");
            HUDController.Instance.SetPayloadHealth((float)CurrentGas / (float)maxGas);
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

        //PayloadBehaviour.instance.agent.isStopped = true;
        //Debug.Log($"Payload Stopped, current Gas is {currentGas}");
        yield break;
    }
    public void RemoveGas(float gasToRemove)
    {
        CurrentGas -= gasToRemove;
    }

    

#endregion
    */ //=== EZE'S GAS REMOVE EDIT ===

    #region ------------------Enemy Surrounding Behaviour----------------
    public void EnemyPushing(float burnAdj, float moveSpeedAdj/*, float returnSpeedAdj*/)
    {
        //Debug.Log("Enemy Pushing");
        /* //=== EZE'S GAS REMOVE EDIT ===
        extraBurningRate += burnAdj;
        */ //=== EZE'S GAS REMOVE EDIT ===
        hinderedMovementSpeed += moveSpeedAdj;
        HUDController.Instance.StartHighlight();
    }


    public void EnemyExit(float burnAdj, float moveSpeedAdj/*, float returnSpeedAdj*/)
    {
        //Debug.Log("Enemy Stop Pushing");
        /* //=== EZE'S GAS REMOVE EDIT ===
        extraBurningRate -= burnAdj;
        */ //=== EZE'S GAS REMOVE EDIT ===
        hinderedMovementSpeed -= moveSpeedAdj;
        HUDController.Instance.StopHighlight();
    }
    #endregion


    private void InitializeAgent()
    {
        agent.acceleration = agent.speed = payloadMovementSpeed;
        agent.angularSpeed = turnSpeed;

        //agent.updatePosition = false;

        if (stages[LevelDirector.Instance.CurrentStageCounter] is Escort escort) agent.Warp(escort.EscortPosition);
        CompleteStage(); //Complete the beginning one
        Debug.Log("Completing first stage (Initial spawn point)");
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

    private void OnTriggerStay(Collider other)
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
    [SerializeField] private float stepInterval = 2.4f;
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
            //Debug.Log(agent.isStopped);
            agent.isStopped = true;

            yield return new WaitForSeconds(stepInterval);

            leftStep = !leftStep;

            if (!stepping && !leftStep) break;
        }

        animator.SetBool("Stop", true);
    }

    public void PlayStepLeftSFX()
    {
        AudioManager.instance.PlaySFX("Golem_LeftStomp", transform.position);
        
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(DelayedShakeCoroutine());
    }

    public void PlayStepRightSFX()
    {
        AudioManager.instance.PlaySFX("Golem_RightStomp", transform.position);
        
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(DelayedShakeCoroutine());
    }

    public void PlayStopSFX()
    {
        AudioManager.instance.PlaySFX("Golem_Stop", transform.position);
    }

    public void PlayPreStopSFX()
    {
        AudioManager.instance.PlaySFX("Golem_PreStop", transform.position);
    }

    public void PlayStandSFX()
    {
        AudioManager.instance.PlaySFX("Golem_Stand", transform.position);
    }

    private bool TryGetProximityShake(out float outAmount, out float outDuration)
    {
        outAmount = minShakeAmount;
        outDuration = minShakeDuration;

        float dist = Vector3.Distance(PlayerController3P.instance.transform.position, transform.position);
        if (dist > maxRange) return false;

        float t = Mathf.InverseLerp(maxRange, minRange, dist);
        outAmount = Mathf.Lerp(minShakeAmount, maxShakeAmount, t);
        outDuration = Mathf.Lerp(minShakeDuration, maxShakeDuration, t);
        return true;
    }
    private IEnumerator DelayedShakeCoroutine()
    {
        _shakePending = true;
        yield return new WaitForSeconds(manualShakeDelay);
        _shakePending = false;
        if (camRig == null) yield break;

        if (TryGetProximityShake(out float shakeAmt, out float shakeDur))
        {
            camRig.Shake(shakeAmt, shakeDur);
        }
        _shakeCoroutine = null;
    }

    #endregion

    #region Overrides

    public override void TakeDamage(int amount, GameObject source = null)
    {
        base.TakeDamage(amount, source);
    }
    protected override void OnHeal()
    {
        //throw new System.NotImplementedException();
        HUDController.Instance.SetPayloadHealth(health / MaxHealth);
    }

    protected override void OnDamaged(GameObject source)
    {
        HUDController.Instance.SetPayloadHealth((float)health / (float)MaxHealth);
    }

    bool dead = false;

    public override void OnDeath()
    {
        if (dead != true)
        {
            LevelDirector.Instance.LoseGame();
            dead = true;
        }
    }

    #endregion

}
