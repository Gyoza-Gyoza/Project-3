using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Entity
{
    [Header("Movement Variables")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float dashAmount = 5f, dashDuration = 0.2f, dashCooldown = 2f;
    private float dashCooldownTimer;

    [Header("Ground check variables")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded = false;

    private int itemsCollected = 0;
    public int ItemsCollected
    { get { return itemsCollected; } }

    private InputManager inputManager;
    private Rigidbody rb;
    private PlayerState playerState = PlayerState.Idle;
    public PlayerState PlayerState
    {
        get { return playerState; }
        set { playerState = value; }
    }
    private Vector3 movement;
    public static PlayerController Instance;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (Instance == null) Instance = this;
        else Destroy(Instance);
    }
    protected override void Start()
    {
        base.Start();
        inputManager = InputManager.Instance;
        basicHB.SetOwner(this.gameObject);

    }
    private void Update()
    {
        if (dashCooldownTimer < dashCooldown)
        {
            dashCooldownTimer += Time.deltaTime; // Starts ticking up when its on cd
        }
        else dashCooldownTimer = dashCooldown; // Sets it to the cooldown time to show that it's ready

        GetInput();
        GroundCheck();
        Movement();
    }
    private void GetInput()
    {
        if (PlayerState == PlayerState.Dashing) return;

        // Handles the basic player movement 

        movement = Vector3.zero; // Resets the movement value

        // Gets each axis input
        if (Input.GetKey(inputManager.GetKey(KeyInput.Forward))) movement.z += 1f;
        else if (Input.GetKey(inputManager.GetKey(KeyInput.Backward))) movement.z -= 1f;

        if (Input.GetKey(inputManager.GetKey(KeyInput.Right))) movement.x += 1f;
        else if (Input.GetKey(inputManager.GetKey(KeyInput.Left))) movement.x -= 1f;

        movement = (transform.right * movement.x + transform.forward * movement.z).normalized; // Calculates the movement of each axis and normalizes it

        // Get attack input
        if (Input.GetKey(inputManager.GetKey(KeyInput.Attack_Basic))) BasicAttack();
        if (Input.GetKey(inputManager.GetKey(KeyInput.Attack_Basic))) SpecialAttack();
    }
    private void Movement()
    {
        rb.MovePosition(rb.position + movement * MovementSpeed * Time.deltaTime);

        if (Input.GetKeyDown(inputManager.GetKey(KeyInput.Dash)))
        {
            if (playerState != PlayerState.Dashing && dashCooldownTimer == dashCooldown)
            {
                StartCoroutine(Dash());
            }
        }

        if (Input.GetKeyDown(inputManager.GetKey(KeyInput.Jump)) && isGrounded) Jump(jumpHeight);
    }
    private void GroundCheck()
    {
        isGrounded = Physics.CheckBox(groundCheck.position, transform.localScale / 2, Quaternion.identity, groundLayer);
    }
    private void Jump(float amount)
    {
        PlayerState = PlayerState.Jumping;
        rb.AddForce(Vector3.up * amount, ForceMode.Impulse);
    }
    private IEnumerator Dash()
    {
        PlayerState = PlayerState.Dashing;
        float timer = 0f;
        Vector3 initialPosition = transform.position;

        while (timer <= dashDuration)
        {
            timer += Time.deltaTime;
            rb.MovePosition(Vector3.Lerp(initialPosition, initialPosition + movement * dashAmount, timer / dashDuration));
            yield return null;
        }

        PlayerState = PlayerState.Idle;
        dashCooldown = 0f;
    }

    protected override void OnHeal()
    {
    }

    protected override void OnDamage()
    {
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
    }

    public override void OnDeath()
    {
    }

    [Header("AttackFields")]
    [SerializeField] private TwoJawScript jawScript;
    [SerializeField] private HitBox basicHB;
    [SerializeField] private float basicTiming = .2f;
    [SerializeField] private int basicDamage = 1;
    private bool isAttacking = false;

    private void BasicAttack()
    {
        /*
        if (isAttacking == false)
        {
            StartCoroutine(BasicAttackSequence());
        }
        */
        jawScript.Attack();
    }

    IEnumerator BasicAttackSequence()
    {
        isAttacking = true;
        basicHB.gameObject.SetActive(true);
        yield return new WaitForSeconds(basicTiming);
        basicHB.gameObject.SetActive(false);
        isAttacking = false;
        yield break;
    }

    public void BasicDamage(EnemyBehaviour toDamage)
    {
        toDamage.TakeDamage(basicDamage);
    }

    private void SpecialAttack()
    {

    }
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
}
public enum PlayerState
{
        Idle,
        Walking,
        Sprinting,
        Jumping,
        Falling,
        Dashing
}
