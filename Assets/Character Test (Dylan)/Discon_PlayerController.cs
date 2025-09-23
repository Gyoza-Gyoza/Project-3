using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Discon_PlayerController : Entity
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.6f;
    [SerializeField] private float gravity = -36f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator anim;
    //[SerializeField] private Rigidbody rb;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    public LayerMask groundMask = ~0;

    [Header("Landing Guard")]
    public float minAirTime = 0.08f;

    // ---------- Attack / Combo ----------
    [Header("Attack Combo (Animator-driven)")]
    public string attack1StateName = "Attack1";
    public string attack2StateName = "Attack2";
    public string attack3StateName = "Attack3";
    public string locomotionStateName = "Locomotion";
    public string attackTriggerParam = "AttackPress";

    // ---------- Attack Dash (short burst over time) ----------
    [Header("Attack Dash (short burst)")]
    public bool requireGroundedForDash = true;

    public float attack1DashDistance = 2.2f;
    public float attack1DashDuration = 0.10f;

    public float attack2DashDistance = 2.8f;
    public float attack2DashDuration = 0.12f;

    public float dashEnterWindow = 0.20f;

    [Header("Extra Fields")]
    public float dashDistance = 6f;
    public float dashDuration = .5f;

    // --- internals ---
    CharacterController cc;
    float rotVel;
    float verticalVelocity;
    float cachedStepOffset;
    float airTime = 0f;

    // dash internals
    bool  _didDashThisState = false;
    int   _lastStateHash = 0;
    bool  _burstActive = false;
    float _burstTimeLeft = 0f;
    float _burstSpeed = 0f;

    bool  _dashActive = false;
    float _dashSpeed = 0f;
    float _dashTimeLeft = 0f;
    Vector3 dashDirection = Vector3.zero;

    Vector2 input = Vector2.zero;
    Vector3 dir = Vector3.zero;


    //Instance
    public static Discon_PlayerController Instance;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cachedStepOffset = cc.stepOffset;

        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!anim) anim = GetComponentInChildren<Animator>();

        if (!groundCheck)
        {
            var g = new GameObject("GroundedCheck").transform;
            g.SetParent(transform);
            float y = -cc.height * 0.5f + cc.skinWidth + 0.02f;
            g.localPosition = new Vector3(0f, y, 0f);
            groundCheck = g;
        }

        //Ezekiel's Injection
        InitializeHitboxs();


        if (Instance == null) Instance = this;
        else Destroy(Instance);
    }

    protected override void Start()
    {
        base.Start();
        currentGas = startingGas;
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
        HUDController.Instance.SetGas((float)currentGas / (float)maxGas);
    }

    void Update()
    {
        // ===== Store Input
        input = InputAxis();

        // ===== Attack Input (Left Mouse Button)
        if (Input.GetMouseButtonDown(0))
        {
            anim?.SetTrigger(attackTriggerParam);
        }

        // ===== Animator state
        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        bool inAtk1 = state.IsName(attack1StateName);
        bool inAtk2 = state.IsName(attack2StateName);
        bool inAtk3 = state.IsName(attack3StateName);

        // ===== Input (movement) — LOCK WASD if in attack
        dir = Vector3.zero;
        if (!(inAtk1 || inAtk2 || inAtk3))
        {
            //Vector2 input = InputAxis();
            
            if (input.sqrMagnitude > 0.0001f)
            {
                float camYaw = cameraTransform ? cameraTransform.eulerAngles.y : transform.eulerAngles.y; //Checks if there is a camera transform
                float targetYaw = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + camYaw;

                float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref rotVel, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                dir = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward;
                dir.Normalize();
            }
            
        }

        // ===== Grounding & step offset
        bool groundedBefore = IsGrounded();
        cc.stepOffset = groundedBefore ? cachedStepOffset : 0f;

        if (groundedBefore && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // ===== Jump (only if not attacking)
        if (!(inAtk1 || inAtk2 || inAtk3) && groundedBefore && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            anim?.SetTrigger("Jump");
            airTime = 0f;
        }

        // ===== Gravity & base move
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = dir * moveSpeed;
        velocity.y = verticalVelocity;

        // ===== Dash burst trigger
        if (state.fullPathHash != _lastStateHash)
        {
            _didDashThisState = false;
            _lastStateHash = state.fullPathHash;
        }

        if (!_didDashThisState && (inAtk1 || inAtk2) && state.normalizedTime <= dashEnterWindow)
        {
            if (!requireGroundedForDash || IsGrounded())
            {
                float dist = inAtk1 ? attack1DashDistance : attack2DashDistance;
                float dur  = Mathf.Max(0.01f, inAtk1 ? attack1DashDuration : attack2DashDuration);

                _burstSpeed    = dist / dur;
                _burstTimeLeft = dur;
                _burstActive   = true;

                _didDashThisState = true;
            }
        }

        // ===== Apply dash burst
        if (_burstActive)
        {
            cc.Move(transform.forward * (_burstSpeed * Time.deltaTime));
            velocity.x = 0f;
            velocity.z = 0f;

            _burstTimeLeft -= Time.deltaTime;
            if (_burstTimeLeft <= 0f)
                _burstActive = false;
        }

        // ===== Trigger Dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashActive == false)
        { 
            _dashActive = true;
            _dashSpeed = dashDistance / dashDuration;
            _dashTimeLeft = dashDuration;
            dashDirection = new Vector3(input.x, 0, input.y);
        }

        // ===== Dashing burst
        if (_dashActive)
        {
            cc.Move(dir * (_dashSpeed * Time.deltaTime));
            velocity.x = 0f;
            velocity.z = 0f;

            _dashTimeLeft -= Time.deltaTime;
            if (_dashTimeLeft <= 0f)
                _dashActive = false;
        }
        

        // ===== Apply base movement
        cc.Move(velocity * Time.deltaTime);

        // ===== Post-move grounded & landing
        bool groundedNow = IsGrounded();
        float prevAirTime = airTime;

        if (groundedNow) { airTime = 0f; }
        else { airTime = (airTime + Time.deltaTime); }

        if (!groundedBefore && groundedNow && prevAirTime >= minAirTime)
        {
            anim?.ResetTrigger("Jump");
            anim?.SetTrigger("Land");
            //if (verticalVelocity < -2f) verticalVelocity = -2f;
        }
    }

    // ---------- Helpers ----------
    Vector2 InputAxis()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return new Vector2(h, v);
    }
    
    bool IsGrounded()
    {
        if (cc.isGrounded) return true;
        Vector3 origin = groundCheck.position + Vector3.up * 0.02f;
        return Physics.CheckSphere(origin, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position + Vector3.up * 0.02f, groundCheckRadius);
    }

    #region ---------------Attack Fields------------------------
    [SerializeField] private HitBox basicHB_1;
    [SerializeField] private HitBox basicHB_2;
    [SerializeField] private HitBox basicHB_3;
    [SerializeField] private int basicDamage_1 = 1;
    [SerializeField] private int basicDamage_2 = 1;
    [SerializeField] private int basicDamage_3 = 1;

    private void InitializeHitboxs()
    {
        basicHB_1.HitBoxListeners += BasicDamage1;
        basicHB_2.HitBoxListeners += BasicDamage2;
        basicHB_3.HitBoxListeners += BasicDamage3;
    }

    public void BasicDamage1(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(basicDamage_1);
        }
    }

    public void BasicDamage2(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(basicDamage_2);
        }
    }

    public void BasicDamage3(GameObject toDamage)
    {
        if (toDamage.tag == "Enemy")
        {
            toDamage.GetComponent<EnemyBehaviour>().TakeDamage(basicDamage_3);
        }
    }
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
    }

    public override void OnDeath()
    {
        //throw new System.NotImplementedException();
    }
    #endregion
}
