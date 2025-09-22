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


    private InputManager inputManager;
    private Rigidbody rb;
    private Animator animator;
    private PlayerState playerState = PlayerState.Idle;
    public PlayerState PlayerState
    {
        get { return playerState; }
        set { playerState = value; }
    }
    private Vector3 movement;
    public static PlayerController Instance;
    //public static Discon_PlayerController Instance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (Instance == null) Instance = this;
        else Destroy(Instance);

    }
    protected override void Start()
    {
        base.Start();
        inputManager = InputManager.Instance;
        InitializeHitboxs();
        currentGas = startingGas;
        HUDController.Instance.SetHealth((float)Health / (float)MaxHealth);
        HUDController.Instance.SetGas((float)currentGas / (float)maxGas);
        //if (Instance == null) Instance = Discon_PlayerController.Instance;
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

        if (attackBuffering && !animator.GetBool("Attacking"))
        {
            attackBuffering = false;
            animator.SetBool("Attacking", true);
        }
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

        //if (Input.GetKeyDown(inputManager.GetKey(KeyInput.Jump))) Debug.Log($"Jump pressed. Groundcheck is {isGrounded}");


        if (Input.GetKeyDown(inputManager.GetKey(KeyInput.Jump)) && isGrounded) Jump(jumpHeight);
    }
    private void GroundCheck()
    {
        isGrounded = Physics.CheckBox(groundCheck.position, groundCheck.transform.localScale / 2, Quaternion.identity, groundLayer);
    }
    private void Jump(float amount)
    {
        Debug.Log("Jumping");
        PlayerState = PlayerState.Jumping;
        rb.AddForce(Vector3.up * amount, ForceMode.Impulse);
    }
    private IEnumerator Dash() //dashing with lerp will teleport players which is a problem
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

    #region -----------------Attacks--------------------
    [Header("AttackFields")]
    //[SerializeField] private TwoJawScript jawScript;
    [SerializeField] private float attackBuffer;
    [SerializeField] private HitBox basicHB_1;
    [SerializeField] private HitBox basicHB_2;
    [SerializeField] private HitBox basicHB_3;
    [SerializeField] private int basicDamage_1 = 1;
    [SerializeField] private int basicDamage_2 = 1;
    [SerializeField] private int basicDamage_3 = 1;
    private bool attackBuffering = false;
    private float bufferCount = 0f;

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

    
    private void BasicAttack()
    {
        //if (jawScript != null)
        //{ jawScript.Attack(); }
        //else
        //{
            if (attackBuffering)
            {
                bufferCount = attackBuffer;
            }
            else
            {
                StartCoroutine(attackingBuffer());
            }
        //}
    }

    IEnumerator attackingBuffer()
    {
        attackBuffering = true;
        bufferCount = attackBuffer;

        while (bufferCount > 0f)
        {
            yield return new WaitForSeconds(Time.deltaTime);
        }
        attackBuffering = false;
        bufferCount = attackBuffer;

        yield break;
    }
    
    private void SpecialAttack()
    {
        //Prepare special
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
            HUDController.Instance.SetGas((float) currentGas / (float)maxGas);
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
