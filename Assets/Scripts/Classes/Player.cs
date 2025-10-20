using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Actor
{
    [field: SerializeField]
    public SpriteRenderer PlayerSprite { get; private set; }

    private Animator anim;

    [SerializeField]
    private PlayerState currentState = PlayerState.Idle;

    private InputSystem_Actions playerInput;

    private InputAction move;
    private InputAction jump;

    private InputAction run;
    private InputAction crouch;

    private InputAction dash;

    [SerializeField]
    private LayerMask groundLayer;

    [SerializeField]
    private Transform groundSensorSpawn;

    public static Player Instance;

    public Rigidbody2D RigidBody { get; private set; }

    private HashSet<PowerType> unlockedPowers = new HashSet<PowerType>();
    private Dictionary<PowerType, IPlayerPower> powerInstances =
        new Dictionary<PowerType, IPlayerPower>();

    private bool jumpRequested = true;

    void Awake()
    {
        this.anim = GetComponent<Animator>();
        this.playerInput = new InputSystem_Actions();
        actorSprite = GetComponent<SpriteRenderer>();
        movement = GetComponent<Movement>();
        movement.actor = this;

        powerInstances[PowerType.Dash] = new Dash(this);
        Instance = this;
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

    void Update()
    {
        Collider2D col = Physics2D.OverlapCircle(groundSensorSpawn.position, 0.1f, groundLayer);

        if (col != null)
        {
            grounded = true;
            // anim.SetBool("Grounded", true);
            // anim.SetBool("Airborne", false);
        }
        else
        {
            grounded = false;
            // anim.SetBool("Grounded", false);
            // anim.SetBool("Airborne", true);
        }

        if (jump.WasPressedThisFrame() && grounded)
        {
            Debug.Log("Jump Pressed");
            // anim.SetBool("Jumping", true);
            jumpRequested = true;
        }
        if (dash.WasPressedThisFrame() && HasPower(PowerType.Dash))
        {
            powerInstances[PowerType.Dash].ActivatePower();
        }
    }

    public void SetRunning(bool isHeld)
    {
        if (isHeld && IsCrouching)
            return;
        IsRunning = isHeld;
        // anim.SetBool("Running", IsRunning);
        if (IsRunning)
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
        if (isHeld && IsRunning)
            return;

        IsCrouching = isHeld;
        // anim.SetBool("Crouching", IsCrouching);

        if (IsCrouching)
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

    public void UnlockPower(PowerType power)
    {
        unlockedPowers.Add(power);
    }

    public bool HasPower(PowerType power)
    {
        return unlockedPowers.Contains(power);
    }
}
