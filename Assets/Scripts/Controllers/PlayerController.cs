using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSprite;
    public SpriteRenderer PlayerSprite { get { return playerSprite; } }
    
    [SerializeField] private PlayerState currentState = PlayerState.Idle;

    private InputSystem_Actions playerInput;
    private InputAction move;
    private InputAction jump;

    private InputAction run;
    private InputAction crouch;
 
    private InputAction dash;

    private Animator anim;
    private bool lastInput = false;

    private bool grounded = false;
    private Vector2 velocity = Vector2.zero;

    [SerializeField]
    [Range(0.05f, 0.3f)]
    public float smoothing = 0.1f;

    [Header("Movement Parameters")]
    [SerializeField]
    public float jumpForce = 400f;
    public float crouchSpeed = 7.5f;
    public float walkSpeed = 15.0f;
    public float runSpeed = 30.0f;
    [SerializeField] private bool canMove = true;

    private bool jumpRequested = false;
    [SerializeField] private bool isJumping = false;
    public bool IsJumping { get { return isJumping; } }

    [SerializeField] private bool isCrouching = false;
    public bool IsCrouching { get { return isCrouching; } }

    [SerializeField] private bool isRunning = false;
    public bool IsRunning { get { return isRunning; } }

    [SerializeField] private bool isDashing = false;
    public bool IsDashing { get { return isDashing; } set { isDashing = value; } }
    
    [SerializeField]
    private LayerMask groundLayer; 

    [SerializeField]
    private Transform groundSensorSpawn;

    public static PlayerController Instance;
    private Rigidbody2D rb;
    public Rigidbody2D RigidBody { get { return rb; } }

    private HashSet<PowerType> unlockedPowers = new HashSet<PowerType>();
    private Dictionary<PowerType, IPlayerPower> powerInstances = new Dictionary<PowerType, IPlayerPower>();

    void Awake()
    {
        this.anim = GetComponent<Animator>();
        this.playerInput = new InputSystem_Actions();
        this.playerSprite = GetComponent<SpriteRenderer>();

        // Initialize power instances , will make this better later.
        powerInstances[PowerType.Dash] = new Dash(this);

        Instance = this;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void OnEnable()
    {

        run = playerInput.Player.Run;
        jump = playerInput.Player.Jump;
        crouch = playerInput.Player.Crouch;

        run.started += ctx => SetRunning(true);
        run.canceled += ctx => SetRunning(false);

        crouch.started += ctx => SetCrouching(true);
        crouch.canceled += ctx => SetCrouching(false);

        move = playerInput.Player.Move;
        move.Enable();

        run.Enable();
        jump.Enable();
        crouch.Enable();

        dash = playerInput.Player.Dash;
        dash.Enable();

    }

    void OnDisable()
    {
        run.started -= ctx => SetRunning(true);
        run.canceled -= ctx => SetRunning(false);

        crouch.started -= ctx => SetCrouching(true);
        crouch.canceled -= ctx => SetCrouching(false);

        move.Disable();

        run.Disable();
        jump.Disable();
        crouch.Disable();
        dash.Disable();
    }

    public void UnlockPower(PowerType power)
    {
        unlockedPowers.Add(power);
    }
    public bool HasPower(PowerType power)
    {
        return unlockedPowers.Contains(power);
    }

    public void Move()
    {
        if (!canMove || isDashing) return;
        Vector2 _move = move.ReadValue<Vector2>();

        float activeSpeed = isRunning ? runSpeed : isCrouching ? crouchSpeed : walkSpeed;  

        if (_move.x < 0)
        {
            playerSprite.flipX = false;
            lastInput = false;
        }
        if (_move.x == 0)
        {
            playerSprite.flipX = lastInput;
        }
        if (_move.x > 0)
        {
            playerSprite.flipX = true;
            lastInput = true;
        }
        anim.SetFloat("Velocity", Mathf.Abs(_move.x));

        Vector2 targetVelocity = new Vector2(_move.x * activeSpeed * 10 * Time.fixedDeltaTime,rb.linearVelocity.y);

        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, smoothing);
    }
    public IEnumerator Jump()
    {
        if (jumpRequested)
        {
            isJumping = true;
            grounded = false;
            rb.AddForceY(jumpForce);
            jumpRequested = false;
            yield return new WaitForSeconds(0.5f);
            isJumping = false;
            anim.SetBool("Jumping", false);
        }

    }

    public void SetRunning(bool isHeld)
    {
        if (isHeld && isCrouching) return; 
        isRunning = isHeld;
        anim.SetBool("Running", isRunning);
        if (isRunning)
        {
            crouch.Disable();
            jump.Disable();
        }
        else
        {
            crouch.Enable();
            jump.Enable();
        }
    }
    public void SetCrouching(bool isHeld)
    {
        if (isHeld && isRunning) return;

        isCrouching = isHeld;
        anim.SetBool("Crouching", isCrouching);

        if (isCrouching)
        {
            run.Disable();
            jump.Disable();
        }
        else
        {
            run.Enable();
            jump.Enable();
        }
    }
    void Update()
    {
        Collider2D col = Physics2D.OverlapCircle(groundSensorSpawn.position, 0.1f, groundLayer);

        if (col != null)
        {
            grounded = true;
            anim.SetBool("Grounded", true);
            anim.SetBool("Airborne", false);
        }
        else
        {
            grounded = false;
            anim.SetBool("Grounded", false);
            anim.SetBool("Airborne", true);
        }

        if (jump.WasPressedThisFrame() && grounded)
        {
            Debug.Log("Jump Pressed");
            anim.SetBool("Jumping", true);
            jumpRequested = true;
        }
        if (dash.WasPressedThisFrame() && HasPower(PowerType.Dash))
        {
            powerInstances[PowerType.Dash].ActivatePower();
        }

    }
    void FixedUpdate()
    {
        Move();
        if(jumpRequested)
        {
            Debug.Log("Jump Requested");
            StartCoroutine(Jump());
        }
    }
}
