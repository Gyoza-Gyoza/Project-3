using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class IdleWalkJumpController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.6f;
    public float gravity = -36f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator anim;

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
    }

    void Update()
    {
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
        Vector3 dir = Vector3.zero;
        if (!(inAtk1 || inAtk2 || inAtk3))
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 input = new Vector2(h, v);

            if (input.sqrMagnitude > 0.0001f)
            {
                float camYaw = cameraTransform ? cameraTransform.eulerAngles.y : transform.eulerAngles.y;
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

        // ===== Apply base movement
        cc.Move(velocity * Time.deltaTime);

        // ===== Post-move grounded & landing
        bool groundedNow = IsGrounded();
        float prevAirTime = airTime;
        airTime = groundedNow ? 0f : (airTime + Time.deltaTime);

        if (!groundedBefore && groundedNow && prevAirTime >= minAirTime)
        {
            anim?.ResetTrigger("Jump");
            anim?.SetTrigger("Land");
            if (verticalVelocity < -2f) verticalVelocity = -2f;
        }
    }

    // ---------- Helpers ----------
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
}
