using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3P : Entity
{
    [Header("Refs")]
    public ThirdPersonCamera camRig;
    public Animator animator;
    [SerializeField] private Volume globalVolume;
    private bool dead = false;

    [Header("Attaack Damages")]
    public int attack1_Damage = 20;
    public int attack2_Damage = 30;
    public int attack3_Damage = 50;
    public int plunge_Damage = 60;

    [Header("Regen")]
    public float healthRegenRate = 0.5f;
    public float healthRegenCooldown = 5f;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float rotationSpeed = 12f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.4f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;
    public float coyoteTime = 0.08f;
    public float jumpBuffer = 0.10f;
    private float jumpDelay = 0.00f;

    [Header("Double Jump")]
    private bool enableDoubleJump = true;
    public float doubleJumpHeight = 1.2f;
    private bool resetYOnDoubleJump = true;

    // --- Ground / landing debounce ----
    [SerializeField, Tooltip("Minimum time in seconds the player must be airborne to trigger a landing effect")]
    private float minAirTimeForLanding = 0.06f;   // tune 0.04 - 0.12 to taste

    [SerializeField, Tooltip("Minimum time between landed SFX/VFX plays (seconds)")]
    private float landCooldown = 0.08f;           // prevents immediate retriggering

    private float lastAirTime = float.NegativeInfinity;
    private float lastLandTime = float.NegativeInfinity;

    // ------------------ DASH ------------------------------------------------------------------------------------------
    [Header("Dash (Common)")]
    private bool useBackdashAnimation = true;
    [Range(-1f, 1f)] public float backDashDotThreshold = 0f;
    public string backDashTrigger = "BackDashStart";
    public string fwdDashTrigger = "DashStart";
    public string airDashTrigger = "AirDashStart";
    private bool dashDefaultBackwards = true;
    private bool rotateToDashDirection = false;

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

    // ------------------ ATTACK COMBO ------------------------------------------------------------------------------------------
    [Header("Attack Combo (LMB)")]
    public string atk1Trigger = "Attack1";
    public string atk2Trigger = "Attack2";
    public string atk3Trigger = "Attack3";
    public string atkToIdle1Trigger = "AttackToIdle1";
    public string atkToIdle2Trigger = "AttackToIdle2";

    [Header("Attack 1 Timing")]
    public float atk1Duration = 1.0f;
    public float atk1ChainLead = 0.30f;

    [Header("Attack 2 Timing")]
    public float atk2Duration = 1.0f;
    public float atk2ChainLead = 0.30f;

    [Header("Attack 3 Timing")]
    public float atk3Duration = 1.0f;
    public float atk3ChainLead = 0.30f;

    [Header("Lunge")]
    public float atk1LungeDistance = 2.5f;
    public float atk1LungeDuration = 0.15f;
    public float atk1LungeDelay = 0.05f;

    public float atk2LungeDistance = 3.0f;
    public float atk2LungeDuration = 0.18f;
    public float atk2LungeDelay = 0.05f;

    [Header("Attack 3: Jump Arc")]
    public float atk3ImpulseDelay = 0.08f;
    public float atk3ForwardSpeed = 7.0f;
    public float atk3UpwardSpeed = 8.0f;
    public float atk3ForwardDuration = 0.45f;
    public float atk3UpwardDuration = 0.30f;

    // ------------------ PLUNGE ATTACK ------------------------------------------------------------------------------------------
    [Header("Plunge Attack")]
    public float plungeDelay = 0.15f;
    public float plungeForwardSpeed = 8f;
    public float plungeDownwardSpeed = 15f;
    public float plungeDuration = 0.5f;
    public string plungeTrigger = "Plunge";
    public float heightModifierPerOneExtra = .05f;
    public float heightThreshold = 3.5f;
    private float _plungemodifierCount = 0f;
    private Vector3 _plungeStartPoint = new Vector3(0, 0, 0);

    // ------------------ HIT BOXES ------------------------------------------------------------------------------------------
    [Header("Hitboxes")]
    [SerializeField] private HitBox basicHB_1;
    [SerializeField] private HitBox basicHB_2;
    [SerializeField] private HitBox basicHB_3;
    [SerializeField] private HitBox plungeHB;

    // ------------------ VFX ------------------------------------------------------------------------------------------
    [Header("VFX")]
    [SerializeField] private Transform VFXZeroPoint;
    [SerializeField] private VFX[] slashVFX;
    [SerializeField] private VFX[] dashVFX;
    [SerializeField] private float lensDistortionDuration, playerGlowDuration;
    private Coroutine dashEffectsCoroutine;
    private LensDistortion lensDistortion;
    private List<MeshRenderer> playerMat = new List<MeshRenderer>();
    private bool dashBackwards = false;

    // ------------------ EMBER ------------------------------------------------------------------------------------------
    [Header("Ember")]
    [SerializeField] private int maxGas = 50;
    [SerializeField] private int startingGas = 0;
    private int currentGas;

    // ------------------ TAUNT DEVICE ------------------------------------------------------------------------------------------
    [Header("Taunt Devices")]
    [SerializeField] private GameObject tauntDevicePrefab;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float upwardsForce = 5f;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float tauntCooldown = 1f;
    bool _tauntThrown = false;


    // ----------------- AUDIO ------------------------------------------------------------------------------------------
    [SerializeField] private AudioSource walkAudioSource;

    // ------------------ INTERNALS ------------------------------------------------------------------------------------------
    private CharacterController controller;
    private Vector3 velocity;
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
    private int attackIndex = 0;
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

    // Health runtime
    private bool regeneratingHealth = false;

    // Instance
    public static PlayerController3P Instance;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        lastJumpPressedTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;

        if (Instance == null) Instance = this;
        else Destroy(Instance);

        if (globalVolume && globalVolume.profile && globalVolume.profile.TryGet(out LensDistortion ld))
        {
            lensDistortion = ld;
            lensDistortion.active = true;
        }
        else Debug.LogWarning("LensDistortion not found.");
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
        {
            playerMat.Add(mr);
            //Debug.Log("Added mat " + mr.material.name);
        }
    }

    protected override void Start()
    {
        base.Start();

        currentGas = startingGas;
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
        HUDController.Instance.SetPlayerEmber((float)currentGas / (float)maxGas);
        //mainAudioSource = this.GetComponent<AudioSource>();
        bool groundedNow = controller.isGrounded;
        wasGrounded = groundedNow;
        if (groundedNow) lastGroundedTime = Time.time;

        if (animator)
        {
            animator.SetBool("Grounded", groundedNow);
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsAttacking", false);
        }

        doubleJumpAvailable = enableDoubleJump && !groundedNow;

        InitializeHitboxes();
    }

    void Update()
    {
        if (LevelDirector.Instance.lost) return;
        HandleDashInput();   // RMB cancels attacks
        HandleAttackInput(); // LMB starts/queues
        HandleDeviceInput(); // Device inputs
        UpdateDash();        // Timers
        UpdateAttack();      // Timers
        HandleMovement();    // Movement depends on dash/attack
        HandleJump();        // Blocked during attacks
        UpdatePlunge();      // Timers
        //UpdateHealth();
    }

    // -------------------- Health NOT USED ------------------------------------
    public void UpdateHealth()
    {
        if (Health <= MaxHealth && !regeneratingHealth)
        {
            StartCoroutine(RegenHealth());
        }
        else
        {
            StopRegen();
        }
    }

    public void StopRegen()
    { regeneratingHealth = false; }

    IEnumerator RegenHealth()
    {

        regeneratingHealth = true;
        float count = 0f;
        float buffercount = 0f;

        while (regeneratingHealth)
        {
            if (buffercount < healthRegenCooldown)
            {
                buffercount += Time.deltaTime;
            }
            else
            {
                count += Time.deltaTime;
                if (count > 1f / healthRegenRate)
                {
                    Heal(1);
                    count = 0f;
                }
            }
        }

        regeneratingHealth = false;

        yield break;
    }

    // -------------------- Movement ------------------------------------------------------------------------------------------
    #region Movement

    void HandleMovement()
    {
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(ix, iz);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 camFwd = camRig ? camRig.GetFlatAimDirection(transform) : Vector3.forward;
        Vector3 camRight = Vector3.Cross(Vector3.up, camFwd).normalized;
        Vector3 moveDirInput = camFwd * input.y + camRight * input.x;

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

    // -------------------- Jump ------------------------------------------------------------------------------------------
    void HandleJump()
    {
        bool grounded = controller.isGrounded;
        if (grounded) lastGroundedTime = Time.time;

        if (wasGrounded && !grounded)
        {
            lastAirTime = Time.time; // mark when airborne
            if (animator) animator.SetBool("Grounded", false);
        }

        if (!wasGrounded && grounded)
        {
            // Only trigger landing if we were in the air at least minAirTimeForLanding
            bool longEnoughAir = (Time.time - lastAirTime) >= minAirTimeForLanding;
            bool cooldownOK = (Time.time - lastLandTime) >= landCooldown;

            if (longEnoughAir && cooldownOK)
            {
                AudioManager.Instance.PlaySFX("Land");
                lastLandTime = Time.time;

                if (animator)
                {
                    animator.ResetTrigger("Land");
                    animator.SetTrigger("Land");
                    animator.SetBool("Grounded", true);
                }
            }
            else
            {
                // still update animator grounded bool so we don't get stuck in 'air' state
                if (animator) animator.SetBool("Grounded", true);
            }

            doubleJumpAvailable = enableDoubleJump;
            jumpRequested = false;
            jumpImpulseApplied = false;
        }

        // Jump input disabled during attacks
        if (!isAttacking && Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
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
                lastGroundedTime = -999f;

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
            AudioManager.Instance.PlaySFX("Jump");
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

        AudioManager.Instance.PlaySFX("DoubleJump");

        if (animator)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("DoubleJump");
            animator.SetBool("Grounded", false);
        }
    }

    #endregion

    // -------------------- Dash ------------------------------------------------------------------------------------------
    #region Dash
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

        AudioManager.Instance.PlaySFX("Dash");          //Play Dash SFX
        StartCoroutine(SpawnVFX(dashVFX[0]));           //Dash VFX
        if (dashEffectsCoroutine != null)
        {
            StopCoroutine(dashEffectsCoroutine);
            dashEffectsCoroutine = null;
        }
        dashEffectsCoroutine = StartCoroutine(DashEffects());
        // Someone forgot to start their coroutines :(
        camRig.DashFOVKick(8f, 0.08f, 0.04f, 0.10f);    //Camera FOV
        camRig.Shake(10f, 0.12f);                       //Camera Shake
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

        AudioManager.Instance.PlaySFX("Dash");          //Play Dash SFX
        StartCoroutine(SpawnVFX(dashVFX[0]));           //Dash VFX
        if (dashEffectsCoroutine != null)
        {
            StopCoroutine(dashEffectsCoroutine);
            dashEffectsCoroutine = null;
        }
        dashEffectsCoroutine = StartCoroutine(DashEffects());

        camRig.DashFOVKick(8f, 0.08f, 0.04f, 0.10f);    //Camera FOV
        camRig.Shake(10f, 0.12f);                       //Camera Shake
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
    }

    void EndAirDash()
    {
        isAirDashing = false;
        airDashCooldownUntil = Time.time + airDashCooldown;
    }

    private IEnumerator DashEffects()
    {
        lensDistortion.intensity.value = -0.8f;
        float timer = 0f;
        while (timer <= Mathf.Max(lensDistortionDuration, playerGlowDuration))
        {
            timer += Time.deltaTime;
            lensDistortion.intensity.value = Mathf.Lerp(-0.5f, 0f, timer / lensDistortionDuration);

            foreach (MeshRenderer mr in playerMat)
            {
                mr.material.SetFloat("_GlowAmount", Mathf.Lerp(1, 0, timer / playerGlowDuration));
            }

            yield return null;
        }
    }

    #endregion

    // -------------------- Attack Input & Update ------------------------------------------------------------------------------------------
    #region Attack Inputs

    private void InitializeHitboxes()
    {
        basicHB_1.HitBoxListeners += Attack_1_Damage;
        basicHB_2.HitBoxListeners += Attack_2_Damage;
        basicHB_3.HitBoxListeners += Attack_3_Damage;
        plungeHB.HitBoxListeners += Plunge_Damage;
    }

    //private void DealDamage(GameObject entityToDamage)
    //{
    //    if(entityToDamage.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour enemy))
    //    {
    //        enemy.TakeDamage(Damage);
    //    }
    //}

    public void Attack_1_Damage(GameObject entityToDamage)
    {
        if (entityToDamage.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour objectToDamage))
        {
            objectToDamage.TakeDamage(attack1_Damage, gameObject);
        }
    }

    public void Attack_2_Damage(GameObject entityToDamage)
    {
        if (entityToDamage.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour objectToDamage))
        {
            objectToDamage.TakeDamage(attack2_Damage, gameObject);
        }
    }
    public void Attack_3_Damage(GameObject entityToDamage)
    {
        if (entityToDamage.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour objectToDamage))
        {
            objectToDamage.TakeDamage(attack3_Damage, gameObject);
        }
    }
    public void Plunge_Damage(GameObject entityToDamage)
    {
        if (entityToDamage.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour objectToDamage))
        {
            objectToDamage.TakeDamage((int)((float)plunge_Damage * (1f + _plungemodifierCount)), gameObject);
        }
    }



    void HandleAttackInput()
    {
        if (!controller.isGrounded && !isAttacking && !isPlunging) // Plunge: only if airborne and not already attacking

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
                if (IsInChainWindow()) // If inside chain window: queue next, and snap facing if WASD is held.
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

        Vector3 facingForThisAttack;    // Determine facing for this attack (consume any queued override decided during chain press)

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

        if (index == 1 || index == 2)   // Setup movement burst for each attack
        {
            float dist = (index == 1) ? atk1LungeDistance : atk2LungeDistance;
            float dur = (index == 1) ? atk1LungeDuration : atk2LungeDuration;
            float del = (index == 1) ? atk1LungeDelay : atk2LungeDelay;
            PrepareLunge(dist, dur, del, facingForThisAttack);
        }
        else // Attack 3
        {
            isLunging = false;  // not used in A3
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
                AudioManager.Instance.PlaySFX("A1");
            }
            else if (index == 2)
            {
                animator.ResetTrigger(atk2Trigger);
                animator.SetTrigger(atk2Trigger);
                StartCoroutine(SpawnVFX(slashVFX[1]));
                AudioManager.Instance.PlaySFX("A2");
            }
            else
            {
                animator.ResetTrigger(atk3Trigger);
                animator.SetTrigger(atk3Trigger);
                StartCoroutine(SpawnVFX(slashVFX[2]));
                AudioManager.Instance.PlaySFX("A3");
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

    float GetCurrentAttackDuration() //Used to determine snappiness of next input
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

    #endregion

    // -------------------- Lunge ------------------------------------------------------------------------------------------
    #region Lunge

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

    #endregion

    // -------------------- Attack 3 Jump ------------------------------------------------------------------------------------------
    #region Attack 3

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

    #endregion

    // -------------------- Plunge Attack ------------------------------------------------------------------------------------------
    #region Plunge

    void BeginPlunge()
    {
        AudioManager.Instance.PlaySFX("Plunge");
        isPlunging = true;
        plungeTimer = 0f;
        plungeImpulseStarted = false;

        //Eze's Code Start
        _plungemodifierCount = 0f;

        _plungeStartPoint = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);

        //Eze's Code End

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

        //Eze's Code Start

        float heightTraveled = _plungeStartPoint.y - this.transform.position.y;



        if (heightTraveled >= heightThreshold)
        {
            _plungemodifierCount = (heightTraveled - heightThreshold) * heightModifierPerOneExtra;
        }

        //Eze's Code End

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

        camRig.Shake(40.0f, 0.5f);
    }

    #endregion

    // -------------------- Devices ------------------------------------------------------------------------------------------
    #region Devices

    private void HandleDeviceInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ThrowDevice();
        }
    }
    void ThrowDevice()
    {

        if (!_tauntThrown)
        {
            // spawn grenade
            GameObject grenade = Instantiate(tauntDevicePrefab, throwPoint.position, throwPoint.rotation);

            // apply physics
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            Vector3 force = Camera.main.transform.forward * throwForce + Vector3.up * upwardsForce;
            rb.AddForce(force, ForceMode.VelocityChange);

            StartCoroutine(TauntCooldown());
        }
        else
        {
            Debug.Log("Taunt is on cooldown");
        }
    }

    IEnumerator TauntCooldown()
    {
        float count = 0f;
        _tauntThrown = true;
        while (_tauntThrown)
        {
            count += Time.fixedDeltaTime;
            if (count >= tauntCooldown)
            {
                _tauntThrown = false;
            }
            else
            {
                HUDController.Instance.SetTauntSlider(count / tauntCooldown);
            }
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }

        HUDController.Instance.SetTauntSlider(1f);

        yield break;

    }    

        #endregion

        // -------------------- Helpers ------------------------------------------------------------------------------------------
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

    private IEnumerator SpawnVFX(VFX vfxToSpawn, bool parent = false)
    {
        yield return new WaitForSeconds(vfxToSpawn.delay);
        var go = Instantiate(vfxToSpawn.vfxPrefab, VFXZeroPoint.position, VFXZeroPoint.rotation, parent ? transform : null);
        Destroy(go, 5f);
    }

    // -------------------- Ember ------------------------------------------------------------------------------------------
    #region Ember

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
            HUDController.Instance.SetPlayerEmber((float)currentGas / (float)maxGas);
            return true;
        }
        //Add the rest of effects that relies on this
    }

    public bool RemoveGas(int amount)
    {
        if (currentGas <= 0)
        {
            currentGas = 0;
            return false;
        }
        else
        {
            currentGas -= amount;
            HUDController.Instance.SetPlayerEmber((float)currentGas / (float)maxGas);
            return true;
        }
    }

    #endregion

    // -------------------- Item ------------------------------------------------------------------------------------------
    #region Item

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

    // -------------------- Entity Overrides ------------------------------------------------------------------------------------------
    #region Entity Overrides
    protected override void OnHeal()
    {
        //throw new System.NotImplementedException();
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
    }

    protected override void OnDamage(GameObject source)
    {
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
        StopRegen();
    }

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