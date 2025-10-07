using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputAction move;
    private InputAction jump;

    private InputAction run;

    private InputAction crouch;

    private Animator anim;
    private bool lastInput = false;
    private bool isJumping = false;
    [SerializeField] float jumpForce = 230;

    private SpriteRenderer playerSprite;
    public bool isCrouching = false;
    public bool isRunning = false;
    private bool grounded = false;
    private Vector2 velocity = Vector2.zero;
    [SerializeField]
    [Range(0.05f, 0.3f)]
    public float smoothing = 0.1f;
    [SerializeField]
    public float crouchSpeed = 7.5f;
    public float walkSpeed = 15.0f;
    public float runSpeed = 30.0f;
    [SerializeField]
    private LayerMask groundLayer; 

    [SerializeField]
    private Transform groundSensorSpawn;

    public static PlayerController Instance;
    private Rigidbody2D rb;

    void Awake()
    {
        this.anim = GetComponent<Animator>();
        this.playerInput = new InputSystem_Actions();
        this.playerSprite = GetComponent<SpriteRenderer>();
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
    }
    public void Move()
    {
        Vector2 _move = move.ReadValue<Vector2>();

        float activeSpeed = isRunning ? runSpeed : isCrouching ? crouchSpeed : walkSpeed;  

        if (_move.x < 0)
        {
            playerSprite.flipX = true;
            lastInput = true;
        }
        if (_move.x == 0)
        {
            playerSprite.flipX = lastInput;
        }
        if (_move.x > 0)
        {
            playerSprite.flipX = false;
            lastInput = false;
        }
        anim.SetFloat("Velocity", Mathf.Abs(_move.x));
        Vector2 targetVelocity = new Vector2(_move.x * activeSpeed * 10 * Time.fixedDeltaTime,rb.linearVelocity.y);

        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, smoothing);
    }
    public IEnumerator Jump()
    {
        if (isJumping)
        {
            crouch.Disable();
            run.Disable();
            grounded = false;
            rb.AddForceY(jumpForce);
            isJumping = false;
            yield return new WaitForSeconds(0.5f);
            anim.SetBool("Jumping", false);
        }
        else
        {
            crouch.Enable();
            run.Enable();
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
        }
        else
        {
            grounded = false;
            anim.SetBool("Grounded", false);
        }

        if (jump.WasPressedThisFrame() && grounded)
        {
            anim.SetBool("Jumping", true);
            isJumping = true;
        }

    }
    void FixedUpdate()
    {
        Move();
        StartCoroutine(Jump());
    }
}
