using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3P : Entity
{
    [Header("Refs")]
    public ThirdPersonCamera camRig;
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float rotationSpeed = 12f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.4f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;
    public float coyoteTime = 0.08f;
    public float jumpBuffer = 0.10f;
    [Tooltip("Delay between pressing Jump and applying force.")]
    public float jumpDelay = 0.00f; // keep at 0 for snappy jump

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpHeight = 1.2f;
    [Tooltip("If true, zero downward Y before applying double jump so it always pops crisply.")]
    public bool resetYOnDoubleJump = true;

    // ------------------ DASH ------------------
    [Header("Dash (Common)")]
    public bool dashDefaultBackwards = true;

    [Header("Dash Facing / Animation")]
    public bool useBackdashAnimation = true;
    [Range(-1f, 1f)] public float backDashDotThreshold = 0f;
    public string backDashTrigger = "BackDashStart";
    public string fwdDashTrigger  = "DashStart";
    public string airDashTrigger  = "AirDashStart";
    public bool rotateToDashDirection = true;

    [Header("Dash (Ground)")]
    public float dashDistance = 6f;
    public float dashStartDuration = 0.25f;
    public float dashCooldown = 0.6f;
    public bool dashUsesMoveDirection = true;
    [Range(0f, 60f)] public float groundDashUpAngleDeg = 0f;

    [Header("Dash (Air)")]
    public float airDashDistance = 5f;
    public float airDashDuration = 0.25f;
    public float airDashCooldown = 0.8f;
    [Range(0f, 60f)] public float airDashUpAngleDeg = 15f;

    // ------------------ ATTACK COMBO ------------------
    [Header("Attack Combo (LMB)")]
    public string atk1Trigger = "Attack1";
    public string atk2Trigger = "Attack2";
    public string atk3Trigger = "Attack3";
    public string atkToIdle1Trigger = "AttackToIdle1";
    public string atkToIdle2Trigger = "AttackToIdle2";

    [Header("Attack 1 Timing")]
    public float atk1Duration = 1.0f;
    [Tooltip("Last X seconds of the attack where LMB can be buffered to chain.")]
    public float atk1ChainLead = 0.30f;

    [Header("Attack 2 Timing")]
    public float atk2Duration = 1.0f;
    public float atk2ChainLead = 0.30f;

    [Header("Attack 3 Timing")]
    public float atk3Duration = 1.0f;
    public float atk3ChainLead = 0.30f;

    [Header("Lunge (only Attack 1 & 2)")]
    public float atk1LungeDistance = 2.5f;
    public float atk1LungeDuration = 0.15f;
    public float atk1LungeDelay = 0.05f;

    public float atk2LungeDistance = 3.0f;
    public float atk2LungeDuration = 0.18f;
    public float atk2LungeDelay = 0.05f;

    [Header("Attack 3: Jump Arc (separate forward & upward)")]
    [Tooltip("Wait this long after Attack 3 starts, then apply the impulse.")]
    public float atk3ImpulseDelay = 0.08f;

    [Tooltip("Horizontal speed set at the impulse moment.")]
    public float atk3ForwardSpeed = 7.0f;

    [Tooltip("Vertical speed set at the impulse moment.")]
    public float atk3UpwardSpeed = 8.0f;

    [Tooltip("How long forward velocity is applied after impulse.")]
    public float atk3ForwardDuration = 0.45f;

    [Tooltip("How long upward velocity is preserved before gravity fully takes over.")]
    public float atk3UpwardDuration = 0.30f;

    // ------------------ PLUNGE ATTACK ------------------
    [Header("Plunge Attack")]
    public float plungeDelay = 0.15f;
    public float plungeForwardSpeed = 8f;
    public float plungeDownwardSpeed = 15f;
    public float plungeDuration = 0.5f;
    public string plungeTrigger = "Plunge"; // Animator trigger

    // ------------------ VFX ------------------
    [Header("VFX")]
    [SerializeField] private Transform VFXZeroPoint;
    [SerializeField] private VFX[] slashVFX;
    [SerializeField] private VFX[] impactVFX;

    // ------------------ Devices ------------------
    [Header("Devices")]
    [SerializeField] private GameObject tauntDevicePrefab;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float upwardsForce = 5f;
    [SerializeField] private Transform throwPoint;


    // ----------------- Audio -----------------------
    [SerializeField] private AudioClip[] SFXs;

    [SerializeField]private AudioSource mainAudioSource;
    [SerializeField] private AudioSource walkAudioSource;

    // ------------------ Internals ------------------
    private CharacterController controller;
    private Vector3 velocity; // Y; horizontal is moved via controller.Move in sections
    private bool wasGrounded;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private bool jumpRequested;
    private bool jumpImpulseApplied;
    private float jumpApplyAtTime;
    private bool doubleJumpAvailable;

    // Dash runtime
    private bool isGroundDashing = false;
    private bool isAirDashing = false;
    private float dashTimer = 0f;
    private float groundDashCooldownUntil = 0f;
    private float airDashCooldownUntil = 0f;
    private Vector3 dashDir = Vector3.zero;

    // Attack runtime
    private bool isAttacking = false;
    private int attackIndex = 0; // 0=none, 1,2,3
    private float attackTimer = 0f;
    private bool queuedNext = false;

    // Chain-window facing override
    private bool queuedFacingOverride = false;
    private Vector3 queuedFacing = Vector3.zero;

    // Lunge runtime (Atk1 / Atk2)
    private bool isLunging = false;
    private float lungeTimer = 0f;
    private float lungeDuration = 0f;
    private float lungeDelay = 0f;
    private bool lungeStarted = false;
    private Vector3 lungeDir = Vector3.zero;
    private float lungeSpeed = 0f;

    // Attack 3 Arc runtime
    private bool atk3ArcActive = false;
    private float atk3ArcTimer = 0f;
    private bool atk3ImpulseFired = false;
    private Vector3 atk3ArcDir = Vector3.zero;
    private Vector3 atk3HorizVel = Vector3.zero;
    private float atk3UpwardEndTime = 0f;

    // Plunge runtime 
    private bool isPlunging = false;
    private float plungeTimer = 0f;
    private Vector3 plungeDir = Vector3.zero;
    private bool plungeImpulseStarted = false;

    // Instance
    public static PlayerController3P Instance;


    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        lastJumpPressedTime = float.NegativeInfinity;
        lastGroundedTime    = float.NegativeInfinity;


        //Ezekiel's Injection
        InitializeHitboxs();

        //Singleton
        if (Instance == null) Instance = this;
        else Destroy(Instance);
    }

    protected override void Start()
    {
        base.Start();
        currentGas = startingGas;
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
        HUDController.Instance.SetGas((float)currentGas / (float)maxGas);
        //mainAudioSource = this.GetComponent<AudioSource>();
        bool groundedNow = controller.isGrounded;
        wasGrounded = groundedNow;
        if (groundedNow) lastGroundedTime = Time.time;

        if (animator)
        {
            animator.SetBool("Grounded", groundedNow);
            animator.SetFloat("Speed", 0f);
            //animator.SetFloat("YVel", 0f);
            //animator.SetBool("IsDashing", false);
            animator.SetBool("IsAttacking", false);
        }

        doubleJumpAvailable = enableDoubleJump && !groundedNow;
    }

    void Update()
    {
        HandleDashInput();   // RMB cancels attacks
        HandleAttackInput(); // LMB starts/queues
        HandleDeviceInput(); // Device inputs
        UpdateDash();        // timers
        UpdateAttack();      // timers
        HandleMovement();    // movement depends on dash/attack
        HandleJump();        // blocked during attacks
        UpdatePlunge();     
    }

    // -------------------- Movement --------------------
    void HandleMovement()
    {
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(ix, iz);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 camFwd = camRig ? camRig.GetFlatAimDirection(transform) : Vector3.forward;
        Vector3 camRight = Vector3.Cross(Vector3.up, camFwd).normalized;
        Vector3 moveDirInput = camFwd * input.y + camRight * input.x;

        // if (moveDirInput.magnitude > 0)
        // {
        //     if (!walkAudioSource.isPlaying)
        //     {
        //         walkAudioSource.Play();
        //     }
        // }
        // else
        // {
        //     walkAudioSource.Stop();
        // }

        float rawPlanarSpeed = moveDirInput.magnitude * walkSpeed;

        if (isGroundDashing || isAirDashing)
        {
            float dashSpeed = isGroundDashing ? dashDistance / Mathf.Max(0.0001f, dashStartDuration)
                                              : airDashDistance / Mathf.Max(0.0001f, airDashDuration);
            Vector3 dashVel = dashDir * dashSpeed;
            Vector3 finalMove = isGroundDashing
                ? new Vector3(dashVel.x, dashVel.y + velocity.y, dashVel.z)
                : dashVel;
            controller.Move(finalMove * Time.deltaTime);


            if (rotateToDashDirection)
            {
                Vector3 face = dashDir; face.y = 0f;
                if (face.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face.normalized, Vector3.up), 20f * Time.deltaTime);
            }
        }
        else if (isAttacking || isPlunging)
        {
            if (isPlunging && !plungeImpulseStarted)
            {
                // no movement at all
                // (skip calling Move with any Y to avoid CC grounded quirks)
            }
            else
            {
                // existing behavior for attacks / active plunge impulse
                Vector3 finalMove = new Vector3(0f, velocity.y, 0f);
                controller.Move(finalMove * Time.deltaTime);
            }
        }
        else
        {
            // Normal locomotion
            if (moveDirInput.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirInput, Vector3.up);
                float scaledRotSpeed = rotationSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, scaledRotSpeed * Time.deltaTime);
            }

            Vector3 horizontal = moveDirInput * walkSpeed;
            Vector3 finalMove = new Vector3(horizontal.x, velocity.y, horizontal.z);
            controller.Move(finalMove * Time.deltaTime);
        }

        if (animator)
        {
            animator.SetFloat("Speed", (isAttacking ? 0f : rawPlanarSpeed));
            //animator.SetFloat("YVel", velocity.y);
            //animator.SetBool("IsDashing", isGroundDashing || isAirDashing);
            animator.SetBool("IsAttacking", isAttacking);
        }

        bool walkingNow = !isAttacking && !isGroundDashing && !isAirDashing && controller.isGrounded && rawPlanarSpeed > 0.1f;

        if (walkAudioSource != null)
        {
            if (walkingNow)
            {
                if (!walkAudioSource.isPlaying)
                    walkAudioSource.Play();
            }
            else
            {
                if (walkAudioSource.isPlaying)
                    walkAudioSource.Stop();
            }
        }

    }

    // -------------------- Jump (incl. Double Jump) --------------------
    void HandleJump()
    {
        bool grounded = controller.isGrounded;
        if (grounded) lastGroundedTime = Time.time;

        if (wasGrounded && !grounded)
        {
            if (animator) animator.SetBool("Grounded", false);
        }

        if (!wasGrounded && grounded)
        {
            //PlaySFX(SFXs[6]);
            AudioManager.Play("Land"); 
            doubleJumpAvailable = enableDoubleJump;
            jumpRequested = false;
            jumpImpulseApplied = false;

            if (animator)
            {
                animator.ResetTrigger("Land");
                animator.SetTrigger("Land");
                animator.SetBool("Grounded", true);
            }
        }

        // Jump input disabled during attacks
        if (!isAttacking && Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        bool canCoyote    = (Time.time - lastGroundedTime) <= coyoteTime;
        bool bufferedJump = (Time.time - lastJumpPressedTime) <= jumpBuffer;

        if (!grounded && !canCoyote)
        {
            if (!isAttacking && enableDoubleJump && doubleJumpAvailable && Input.GetButtonDown("Jump"))
            {
                DoDoubleJump();
            }
        }
        else
        {
            if (!isAttacking && !jumpRequested && bufferedJump && canCoyote)
            {
                jumpRequested = true;
                jumpImpulseApplied = false;
                jumpApplyAtTime = Time.time + Mathf.Max(0f, jumpDelay);

                lastJumpPressedTime = -999f;
                lastGroundedTime    = -999f;

                if (animator)
                {
                    animator.ResetTrigger("Land");
                    animator.SetTrigger("Jump");
                    animator.SetBool("Grounded", false);
                }
            }
        }

        // Apply jump impulse (delayed if any)
        if (!isGroundDashing && !isAirDashing && !isAttacking && jumpRequested && !jumpImpulseApplied && Time.time >= jumpApplyAtTime)
        {
            float jumpSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            velocity.y = jumpSpeed;
            jumpImpulseApplied = true;
            //PlaySFX(SFXs[4]);
            AudioManager.Play("Jump"); 
        }

        // Gravity
        if (isAirDashing)
        {
            velocity.y = 0f;
        }
        else
        {
            if (isPlunging && !plungeImpulseStarted)
            {
                velocity.y = 0f;            // keep perfectly suspended
                // IMPORTANT: do NOT add gravity this frame
            }
            else
            {
                if (controller.isGrounded && velocity.y < 0f) velocity.y = groundedGravity;
                velocity.y += gravity * Time.deltaTime;
            }
        }

        wasGrounded = grounded;
    }

    void DoDoubleJump()
    {
        doubleJumpAvailable = false;
        jumpRequested = false;
        jumpImpulseApplied = true;

        if (resetYOnDoubleJump && velocity.y < 0f) velocity.y = 0f;

        float jumpSpeed = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);
        velocity.y = jumpSpeed;

        //PlaySFX(SFXs[5]);
        AudioManager.Play("DoubleJump"); 

        if (animator)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("DoubleJump");
            animator.SetBool("Grounded", false);
        }
    }

    // -------------------- Dash --------------------
    void HandleDashInput()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        bool groundedNow = controller.isGrounded;

        // Dashes cancel the attack
        if (isAttacking)
        {
            CancelAttackState();
        }

        if (groundedNow)
        {
            if (!isGroundDashing && Time.time >= groundDashCooldownUntil) BeginGroundDash();
        }
        else
        {
            if (!isAirDashing && Time.time >= airDashCooldownUntil) BeginAirDash();
        }
    }

    Vector3 ComputeFlatDashDir(bool useMoveDir)
    {
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(ix, iz);
        bool hasInput = input.sqrMagnitude > 0.0001f;
        if (hasInput) input.Normalize();

        if (!useMoveDir || !hasInput)
        {
            Vector3 dir = (dashDefaultBackwards ? -transform.forward : transform.forward);
            dir.y = 0f;
            return dir.normalized;
        }

        if (camRig)
        {
            Vector3 camFwd = camRig.GetFlatAimDirection(transform);
            Vector3 camRight = Vector3.Cross(Vector3.up, camFwd).normalized;
            Vector3 moveDirInput = camFwd * input.y + camRight * input.x;
            moveDirInput.y = 0f;
            return moveDirInput.sqrMagnitude > 0.0001f ? moveDirInput.normalized
                                                       : ((dashDefaultBackwards ? -transform.forward : transform.forward));
        }
        else
        {
            Vector3 moveDirInput = transform.forward * input.y + transform.right * input.x;
            moveDirInput.y = 0f;
            return moveDirInput.sqrMagnitude > 0.0001f ? moveDirInput.normalized
                                                       : ((dashDefaultBackwards ? -transform.forward : transform.forward));
        }
    }

    Vector3 AddUpwardAngle(Vector3 flatDir, float angleDeg)
    {
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) flatDir = transform.forward;
        flatDir.Normalize();
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 tilted = (flatDir * Mathf.Cos(rad)) + (Vector3.up * Mathf.Sin(rad));
        return tilted.normalized;
    }

    void BeginGroundDash()
    {
        isGroundDashing = true;
        isAirDashing = false;

        Vector3 flat = ComputeFlatDashDir(dashUsesMoveDirection);
        dashDir = AddUpwardAngle(flat, groundDashUpAngleDeg);
        dashTimer = Mathf.Max(0.0001f, dashStartDuration);

        if (animator)
        {
            //animator.SetBool("IsDashing", true);

            Vector3 flatFacing = transform.forward; flatFacing.y = 0f; flatFacing.Normalize();
            Vector3 flatDash = dashDir; flatDash.y = 0f; flatDash.Normalize();
            float dot = Vector3.Dot(flatDash, flatFacing);
            bool isBackDash = dot < backDashDotThreshold;

            if (useBackdashAnimation && isBackDash)
            {
                animator.ResetTrigger(backDashTrigger);
                animator.SetTrigger(backDashTrigger);
            }
            else
            {
                animator.ResetTrigger(fwdDashTrigger);
                animator.SetTrigger(fwdDashTrigger);
            }
        }

        //PlaySFX(SFXs[3]);
        AudioManager.Play("Dash"); 
    }

    void BeginAirDash()
    {
        isAirDashing = true;
        isGroundDashing = false;

        Vector3 flat = ComputeFlatDashDir(dashUsesMoveDirection);
        dashDir = AddUpwardAngle(flat, airDashUpAngleDeg);

        dashTimer = Mathf.Max(0.0001f, airDashDuration);
        velocity.y = 0f;

        if (animator)
        {
            //animator.SetBool("IsDashing", true);
            animator.SetBool("Grounded", false);

            Vector3 flatFacing = transform.forward; flatFacing.y = 0f; flatFacing.Normalize();
            Vector3 flatDash = dashDir; flatDash.y = 0f; flatDash.Normalize();
            float dot = Vector3.Dot(flatDash, flatFacing);
            bool isBackDash = dot < backDashDotThreshold;

            if (useBackdashAnimation && isBackDash)
            {
                animator.ResetTrigger(backDashTrigger);
                animator.SetTrigger(backDashTrigger);
            }
            else
            {
                animator.ResetTrigger(airDashTrigger);
                animator.SetTrigger(airDashTrigger);
            }
        }

    }

    void UpdateDash()
    {
        if (isGroundDashing || isAirDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                if (isGroundDashing) EndGroundDash();
                else EndAirDash();
            }
        }
    }

    void EndGroundDash()
    {
        isGroundDashing = false;
        groundDashCooldownUntil = Time.time + dashCooldown;
        //if (animator) animator.SetBool("IsDashing", false);
    }

    void EndAirDash()
    {
        isAirDashing = false;
        airDashCooldownUntil = Time.time + airDashCooldown;
        //if (animator) animator.SetBool("IsDashing", false);
    }

    // -------------------- Attack Input & Update --------------------
    void HandleAttackInput()
    {
        // Plunge: only if airborne and not already attacking
        if (!controller.isGrounded && !isAttacking && !isPlunging)
        {
            if (Input.GetMouseButtonDown(0))
            {
                BeginPlunge();
                return; // don't go into normal combo logic
            }
        }

        if (isGroundDashing || isAirDashing) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!isAttacking)
            {
                BeginAttack(1);
            }
            else
            {
                // If inside chain window: queue next, and snap facing if WASD is held.
                if (IsInChainWindow())
                {
                    queuedNext = true;

                    Vector3 desiredFacing;
                    bool hasMoveInput = TryGetCameraRelativeMove(out desiredFacing);

                    if (hasMoveInput)
                    {
                        desiredFacing.y = 0f;
                        if (desiredFacing.sqrMagnitude > 0.0001f)
                        {
                            transform.rotation = Quaternion.LookRotation(desiredFacing, Vector3.up);
                            queuedFacingOverride = true;
                            queuedFacing = desiredFacing.normalized;
                        }
                    }
                    else
                    {
                        queuedFacingOverride = true;
                        Vector3 f = transform.forward; f.y = 0f;
                        queuedFacing = (f.sqrMagnitude > 0.0001f) ? f.normalized : Vector3.forward;
                    }
                }
            }
        }
    }

    void BeginAttack(int index)
    {
        isAttacking = true;
        attackIndex = index;
        attackTimer = 0f;

        // Determine facing for this attack (consume any queued override decided during chain press)
        Vector3 facingForThisAttack;
        if (queuedFacingOverride && queuedFacing.sqrMagnitude > 0.0001f)
        {
            facingForThisAttack = queuedFacing.normalized;
            transform.rotation = Quaternion.LookRotation(facingForThisAttack, Vector3.up);
        }
        else
        {
            Vector3 f = transform.forward; f.y = 0f;
            facingForThisAttack = (f.sqrMagnitude > 0.0001f) ? f.normalized : Vector3.forward;
        }
        queuedFacingOverride = false; // consume
        queuedNext = false;           // reset for this stage

        // Setup movement burst for each attack
        if (index == 1 || index == 2)
        {
            float dist = (index == 1) ? atk1LungeDistance : atk2LungeDistance;
            float dur  = (index == 1) ? atk1LungeDuration : atk2LungeDuration;
            float del  = (index == 1) ? atk1LungeDelay    : atk2LungeDelay;
            PrepareLunge(dist, dur, del, facingForThisAttack);
        }
        else // Attack 3
        {
            isLunging = false;      // not used in A3
            lungeStarted = false;

            PrepareAtk3Arc(facingForThisAttack);
        }


        if (animator)
        {
            if (index == 1) 
            {
                animator.ResetTrigger(atk1Trigger); 
                animator.SetTrigger(atk1Trigger);
                StartCoroutine(SpawnVFX(slashVFX[0]));
                //PlaySFX(SFXs[0]);
                AudioManager.Play("A1"); 
            }
            else if (index == 2) 
            { 
                animator.ResetTrigger(atk2Trigger); 
                animator.SetTrigger(atk2Trigger); 
                StartCoroutine(SpawnVFX(slashVFX[1]));
                //PlaySFX(SFXs[1]);
                AudioManager.Play("A2"); 
            }
            else 
            { 
                animator.ResetTrigger(atk3Trigger); 
                animator.SetTrigger(atk3Trigger); 
                StartCoroutine(SpawnVFX(slashVFX[2]));
                //PlaySFX(SFXs[2]);
                AudioManager.Play("A3"); 
            }
            animator.SetBool("IsAttacking", true);
        }
    }

    void UpdateAttack()
    {
        if (!isAttacking) return;

        attackTimer += Time.deltaTime;

        // Active per-attack motion
        if (attackIndex == 1 || attackIndex == 2)
            UpdateLunge();
        else if (attackIndex == 3)
        {
            UpdateAtk3Arc();
        }

        float dur = GetCurrentAttackDuration();

        if (attackTimer >= dur)
        {
            if (queuedNext)
            {
                if (attackIndex == 1) BeginAttack(2);
                else if (attackIndex == 2) BeginAttack(3);
                else EndAttackToIdle();
            }
            else
            {
                if (attackIndex == 1)
                {
                    if (animator) 
                    { 
                        animator.ResetTrigger(atkToIdle1Trigger); 
                        animator.SetTrigger(atkToIdle1Trigger); 
                    }
                    EndAttackState();
                }
                else if (attackIndex == 2)
                {
                    if (animator) 
                    { 
                        animator.ResetTrigger(atkToIdle2Trigger); 
                        animator.SetTrigger(atkToIdle2Trigger); 
                    }
                    EndAttackState();
                }
                else
                {
                    EndAttackToIdle();
                }
            }
        }
    }

    void EndAttackToIdle()
    {
        EndAttackState();
    }

    void EndAttackState()
    {
        isAttacking = false;
        attackIndex = 0;
        attackTimer = 0f;
        queuedNext = false;
        queuedFacingOverride = false;

        // Clear movement bursts
        isLunging = false;
        lungeStarted = false;

        atk3ArcActive = false;
        atk3ImpulseFired = false;
        atk3HorizVel = Vector3.zero;

        if (animator) animator.SetBool("IsAttacking", false);
    }

    void CancelAttackState()
    {
        EndAttackState();
    }

    bool IsInChainWindow()
    {
        float dur = GetCurrentAttackDuration();
        float lead = GetCurrentChainLead();
        return isAttacking && attackTimer >= (dur - lead) && attackTimer <= dur;
    }

    float GetCurrentAttackDuration()
    {
        switch (attackIndex)
        {
            case 1: return atk1Duration;
            case 2: return atk2Duration;
            case 3: return atk3Duration;
            default: return 0f;
        }
    }

    float GetCurrentChainLead()
    {
        switch (attackIndex)
        {
            case 1: return atk1ChainLead;
            case 2: return atk2ChainLead;
            case 3: return atk3ChainLead;
            default: return 0f;
        }
    }

    // -------------------- Lunge (Atk 1 & 2) --------------------
    void PrepareLunge(float distance, float duration, float delay, Vector3 facingOverride)
    {
        isLunging = true;
        lungeStarted = false;
        lungeTimer = 0f;
        lungeDuration = Mathf.Max(0.0001f, duration);
        lungeDelay = Mathf.Max(0f, delay);

        Vector3 f = facingOverride; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f)
        {
            f = transform.forward; f.y = 0f;
        }
        lungeDir = f.normalized;
        lungeSpeed = distance / lungeDuration;
    }

    void UpdateLunge()
    {
        if (!isLunging) return;

        lungeTimer += Time.deltaTime;

        if (!lungeStarted)
        {
            if (lungeTimer >= lungeDelay)
            {
                lungeStarted = true;
            }
            else
            {
                return;
            }
        }

        if (lungeStarted && lungeTimer <= (lungeDelay + lungeDuration))
        {
            Vector3 lungeVel = lungeDir * lungeSpeed;
            Vector3 finalMove = new Vector3(lungeVel.x, 0f, lungeVel.z) * Time.deltaTime;
            controller.Move(finalMove);
        }
        else
        {
            isLunging = false;
        }
    }

    // -------------------- Attack 3: Single-Impulse Jump Arc --------------------
    void PrepareAtk3Arc(Vector3 facingOverride)
    {
        atk3ArcActive = true;
        atk3ArcTimer = 0f;
        atk3ImpulseFired = false;

        atk3ArcDir = facingOverride; atk3ArcDir.y = 0f;
        if (atk3ArcDir.sqrMagnitude < 0.0001f) atk3ArcDir = transform.forward;
        atk3ArcDir.Normalize();

        atk3HorizVel = Vector3.zero;
        atk3UpwardEndTime = 0f;
    }

    void UpdateAtk3Arc()
    {
        if (!atk3ArcActive) return;

        atk3ArcTimer += Time.deltaTime;

        // Apply the impulse once after the delay
        if (!atk3ImpulseFired && atk3ArcTimer >= atk3ImpulseDelay)
        {
            atk3ImpulseFired = true;

            // Upward
            velocity.y = atk3UpwardSpeed;
            atk3UpwardEndTime = atk3ArcTimer + atk3UpwardDuration;

            // Forward
            atk3HorizVel = atk3ArcDir * atk3ForwardSpeed;
        }

        if (atk3ImpulseFired)
        {
            // Apply forward while inside forward duration
            if (atk3ArcTimer <= atk3ImpulseDelay + atk3ForwardDuration)
            {
                controller.Move(atk3HorizVel * Time.deltaTime);
            }

            // Cancel upward velocity after upward duration ends
            if (atk3ArcTimer >= atk3UpwardEndTime)
            {
                if (velocity.y > 0f) velocity.y = 0f;
            }
        }

        // When both durations have passed, we can end arc state
        if (atk3ArcTimer > atk3ImpulseDelay + Mathf.Max(atk3ForwardDuration, atk3UpwardDuration))
        {
            atk3ArcActive = false;
            atk3HorizVel = Vector3.zero;
        }
    }

    #region === Plunge ===
    // -------------------- Plunge Attack --------------------
    void BeginPlunge()
    {
        //PlaySFX(SFXs[7]);
        AudioManager.Play("Plunge"); 
        isPlunging = true;
        plungeTimer = 0f;
        plungeImpulseStarted = false;

        velocity = Vector3.zero;  // stop dead

        plungeDir = transform.forward; plungeDir.y = 0f;
        if (plungeDir.sqrMagnitude < 0.001f) plungeDir = Vector3.forward;
        plungeDir.Normalize();

        if (animator)
        {
            animator.ResetTrigger(plungeTrigger);
            animator.SetTrigger(plungeTrigger);
            animator.SetBool("IsAttacking", true);
        }
    }

    void UpdatePlunge()
    {
        if (!isPlunging) return;

        plungeTimer += Time.deltaTime;

        if (!plungeImpulseStarted && plungeTimer >= plungeDelay)
            plungeImpulseStarted = true;

        if (plungeImpulseStarted && plungeTimer <= plungeDelay + plungeDuration)
        {
            Vector3 slamVel = (plungeDir * plungeForwardSpeed) + (Vector3.down * plungeDownwardSpeed);
            controller.Move(slamVel * Time.deltaTime);
        }

        if ((plungeImpulseStarted && plungeTimer >= plungeDelay + plungeDuration) || controller.isGrounded)
            EndPlunge();
    }


    void EndPlunge()
    {
        isPlunging = false;
        plungeTimer = 0f;
        plungeImpulseStarted = false;
        StartCoroutine(SpawnVFX(slashVFX[3]));

        if (animator)
        {
            animator.SetBool("IsAttacking", false);
        }
    }

    // -------------------- Devices --------------------
    private void HandleDeviceInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ThrowDevice();
        }
    }
    void ThrowDevice()
    {
        // spawn grenade
        GameObject grenade = Instantiate(tauntDevicePrefab, throwPoint.position, throwPoint.rotation);

        // apply physics
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        Vector3 force = Camera.main.transform.forward * throwForce + Vector3.up * upwardsForce;
        rb.AddForce(force, ForceMode.VelocityChange);
    }
    #endregion

    // -------------------- Helpers --------------------
    bool TryGetCameraRelativeMove(out Vector3 dir)
    {
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(ix, iz);

        if (input.sqrMagnitude > 0.0001f)
        {
            input.Normalize();
            if (camRig)
            {
                Vector3 camFwd = camRig.GetFlatAimDirection(transform);
                Vector3 camRight = Vector3.Cross(Vector3.up, camFwd).normalized;
                Vector3 moveDirInput = camFwd * input.y + camRight * input.x;
                moveDirInput.y = 0f;
                if (moveDirInput.sqrMagnitude > 0.0001f)
                {
                    dir = moveDirInput.normalized;
                    return true;
                }
            }
            else
            {
                Vector3 moveDirInput = transform.forward * input.y + transform.right * input.x;
                moveDirInput.y = 0f;
                if (moveDirInput.sqrMagnitude > 0.0001f)
                {
                    dir = moveDirInput.normalized;
                    return true;
                }
            }
        }
        dir = Vector3.zero;
        return false;
    }

    #region ---------------Attack Fields------------------------
    [SerializeField] private HitBox attack1HB;
    [SerializeField] private HitBox attack2HB;
    [SerializeField] private HitBox attack3HB_sweep;
    [SerializeField] private HitBox attack3HB_slam;
    [SerializeField] private HitBox plungeHB;
    [SerializeField] private int    attack1Damage = 1;
    [SerializeField] private int    attack2Damage = 1;
    [SerializeField] private int    attack3Damage_sweep = 1;
    [SerializeField] private int    attack3Damage_slam = 1;
    [SerializeField] private int    plungeDamage = 1;

    private void InitializeHitboxs()
    {
        attack1HB.HitBoxListeners       += Attack1;
        attack2HB.HitBoxListeners       += Attack2;
        attack3HB_sweep.HitBoxListeners += Attack3Sweep;
        attack3HB_slam.HitBoxListeners  += Attack3Slam;
        plungeHB.HitBoxListeners        += AttackPlunge;
    }

    public void Attack1(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(attack1Damage);
            StartCoroutine(SpawnVFX(impactVFX[0]));
        }
    }

    public void Attack2(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(attack2Damage);
            StartCoroutine(SpawnVFX(impactVFX[1]));
        }
    }

    public void Attack3Sweep(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(attack3Damage_sweep);
        }
    }

    public void Attack3Slam(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(attack3Damage_slam);
        }
    }

    public void AttackPlunge(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(plungeDamage);
        }
    }
    private IEnumerator SpawnVFX(VFX vfxToSpawn)
    {
        yield return new WaitForSeconds(vfxToSpawn.delay);
        Instantiate(vfxToSpawn.vfxPrefab, VFXZeroPoint.position, VFXZeroPoint.rotation);
    }

    public void PlaySFX(AudioClip sfxToPlay)
    {
        mainAudioSource.clip = sfxToPlay;
        mainAudioSource.Play();
    }

    /*
    private IEnumerator PlaySFX(SFXs sfxToPlay)
    {
        yield return new WaitForSeconds(sfxToPlay.delay);
        mainAudioSource.clip = sfxToPlay.clip;
        mainAudioSource.Play();
    }
    */

    #endregion

    #region --------------Item------------------

    private int itemsCollected = 0;
    public int ItemsCollected
    { get { return itemsCollected; } }

    public void OnCollect()
    {
        itemsCollected++;
        // Add any additional logic for collecting items, such as updating UI or playing sound effects
    }
    public int DropOffItems()
    {
        int amount = itemsCollected; // Returns the amount of items dropped off
        itemsCollected -= itemsCollected;
        return amount;
        // Add any additional logic for dropping off items, such as updating UI or playing sound effects
    }
    #endregion

    #region ----------------Gas--------------------------
    [Header("Gas")]
    [SerializeField] private int maxGas = 50;
    [SerializeField] private int startingGas = 0;
    private int currentGas;

    public bool AddGas(int amount)
    {
        if (currentGas >= maxGas)
        {
            currentGas = maxGas;
            return false;
        }
        else
        {
            currentGas += amount;
            HUDController.Instance.SetGas((float)currentGas / (float)maxGas);
            return true;
        }
        //Add the rest of effects that relies on this
    }

    public bool RemoveGas(int amount)
    {
        //Debug.Log("Remove Gas called");
        if (currentGas <= 0)
        {
            currentGas = 0;
            return false;
        }
        else
        {
            currentGas -= amount;
            HUDController.Instance.SetGas((float)currentGas / (float)maxGas);
            return true;
        }
    }
    #endregion

    #region Entity Overrides
    protected override void OnHeal()
    {
        //throw new System.NotImplementedException();
    }

    protected override void OnDamage()
    {
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
        HUDController.Instance.DamageFlicker();
    }

    private bool dead = false;

    public override void OnDeath()
    {
        //throw new System.NotImplementedException();
        if (dead != true)
        {
            LevelDirector.Instance.LoseGame();
            dead = true;
        }
    }
    #endregion
}
