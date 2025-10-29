using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Actor
{
    [field: SerializeField]
    public SpriteRenderer PlayerSprite { get; private set; }

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
    public Vector2 input;
    private HashSet<PowerType> unlockedPowers = new HashSet<PowerType>();
    private Dictionary<PowerType, IPlayerPower> powerInstances =
        new Dictionary<PowerType, IPlayerPower>();

    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    void Awake()
    {
        this.playerInput = new InputSystem_Actions();
        actorSprite = GetComponent<SpriteRenderer>();
        Movement = GetComponent<Movement>();
        Movement.actor = this;

        if (ActorCollider is BoxCollider2D box)
        {
            originalColliderSize = box.size;
            originalColliderOffset = box.offset;
        }

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
        input = move.ReadValue<Vector2>();
        Collider2D col = Physics2D.OverlapCircle(groundSensorSpawn.position, 0.1f, groundLayer);

        if (col != null)
        {
            grounded = true;
            Animator.SetBool("Grounded", true);
            Animator.SetBool("Airborne", false);
        }
        else
        {
            grounded = false;
            Animator.SetBool("Grounded", false);
            Animator.SetBool("Airborne", true);
        }

        if (jump.WasPressedThisFrame() && grounded)
        {
            Debug.Log("Jump Pressed");
            Animator.SetBool("Jumping", true);
            JumpRequested = true;
        }
        if (dash.WasPressedThisFrame() && HasPower(PowerType.Dash))
        {
            powerInstances[PowerType.Dash].ActivatePower();
        }
    }

    void FixedUpdate()
    {
        Movement.Move(input);

        if (JumpRequested)
        {
            StartCoroutine(Movement.Jump());
        }
    }

    public void SetRunning(bool isHeld)
    {
        if (isHeld && IsCrouching)
            return;
        IsRunning = isHeld;
        Animator.SetBool("Running", IsRunning);
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

        if (!isHeld && IsCrouching)
        {
            if (!CanStand())
            {
                Debug.Log("Cannot stand up, something is overhead!");
                return;
            }
        }
        IsCrouching = isHeld;
        Animator.SetBool("Crouching", IsCrouching);
        if (IsCrouching)
        {
            Vector2 newSize = originalColliderSize;
            newSize.y = originalColliderSize.y * 0.5f;

            float delta = originalColliderSize.y - newSize.y;
            float newOffsetY = originalColliderOffset.y - (delta * 0.5f);

            if (ActorCollider is BoxCollider2D box)
            {
                box.size = newSize;
                box.offset = new Vector2(originalColliderOffset.x, newOffsetY);
            }
        }

        else
        {
            if (ActorCollider is BoxCollider2D box)
            {
                box.size = originalColliderSize;
                box.offset = originalColliderOffset;
            }
        }
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
    private bool CanStand()
    {
        if (!(ActorCollider is BoxCollider2D box)) return true;

        int rayCount = Mathf.Clamp(Mathf.CeilToInt(2f), 3, 12);
        float step = 2f;
        for (int i = 0; i < rayCount; i++)
        {
            float x = 2 + i * step;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up, originalColliderSize.y - box.size.y, groundLayer);
            if (hit.collider == null) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.collider == ActorCollider) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            Debug.Log($"CanStand blocked by: {hit.collider.name} at ray x={x}");
            return false;
        }
        return true;
    }
    public override void Death()
    {
        if (Stress > 100)
        {
            
        }
        if (Stress >= 110)
        {
            Debug.Log("Player has died due to stress!");
        }
    }
}
