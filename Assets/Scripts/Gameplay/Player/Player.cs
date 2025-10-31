using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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

    [HideInInspector]
    public bool IsInHideZone = false;
    private Component currentHideZone = null;

    [Header("Hide Settings")]
    [SerializeField]
    [Tooltip("Tag to set on the player while hidden. Create this tag in the Tag manager (default: Hidden). If missing, falls back to Untagged.")]
    private string hideTag = "Hidden";

    [SerializeField]
    [Tooltip("Duration of the fade when entering/exiting hide.")]
    private float hideFadeDuration = 0.25f;

    [SerializeField]
    [Tooltip("Target alpha of the sprite when hidden.")]
    private float hiddenAlpha = 0.18f;

    private Coroutine hideCoroutine = null;
    private List<Collider2D> ignoredByAI = new List<Collider2D>();

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
    move.started += OnMoveStarted;
    move.performed += OnMoveStarted;
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

    move.started -= OnMoveStarted;
    move.performed -= OnMoveStarted;
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
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && IsInHideZone)
        {
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            {
                Debug.Log($"Player: Direct key detected W/Up while in hide zone. IsHidden={IsHidden}");
                if (!IsHidden) EnterHide(); else ExitHide();
            }
        }
    }

    private void OnMoveStarted(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        var v = ctx.ReadValue<Vector2>();
        Debug.Log($"Player: OnMoveStarted called. input={v}, IsInHideZone={IsInHideZone}, IsHidden={IsHidden}, phase={ctx.phase}");
        if (IsInHideZone && v.y > 0.5f)
        {
            if (!IsHidden) EnterHide(); else ExitHide();
        }
    }

    void FixedUpdate()
    {
        if (IsHidden)
        {
            Movement.Move(Vector2.zero);
        }
        else
        {
            Movement.Move(input);
        }

        if (JumpRequested)
        {
            StartCoroutine(Movement.Jump());
        }
    }

    public void OnEnterHideZone()
    {
        IsInHideZone = true;
        Debug.Log($"Player: Entered hide zone (IsInHideZone={IsInHideZone})");
    }

    public void OnExitHideZone()
    {
        IsInHideZone = false;
        Debug.Log($"Player: Exited hide zone (IsInHideZone={IsInHideZone})");
        if (IsHidden)
        {
            ExitHide();
        }
    }

    private void EnterHide()
    {
        if (IsHidden) return;
        IsHidden = true;
        SetCrouching(true);
        TryIgnoreAICollisions(true);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(FadeSpriteTo(hiddenAlpha, hideFadeDuration));
        try
        {
            gameObject.tag = hideTag;
        }
        catch
        {
            Debug.LogWarning($"Tag '{hideTag}' not found. Falling back to 'Untagged'. Please add the tag if you want AI to detect hidden state by tag.");
            gameObject.tag = "Untagged";
        }
    }

    private void ExitHide()
    {
        if (!IsHidden) return;
        IsHidden = false;
        SetCrouching(false);
        TryIgnoreAICollisions(false);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(FadeSpriteTo(1f, hideFadeDuration));
        gameObject.tag = "Player";
    }

    private void TryIgnoreAICollisions(bool ignore)
    {
        var playerCol = ActorCollider;
        if (playerCol == null) return;

        var ais = BaseAI.AllAIs;
        foreach (var ai in ais)
        {
            if (ai == null) continue;
            var aiActor = ai.GetComponent<Actor>();
            if (aiActor == null) continue;
            var aiCol = aiActor.ActorCollider;
            if (aiCol == null) continue;
            if (ignore)
            {
                if (!ignoredByAI.Contains(aiCol))
                {
                    Physics2D.IgnoreCollision(playerCol, aiCol, true);
                    ignoredByAI.Add(aiCol);
                    Debug.Log($"Player: Ignoring collision with AI '{ai.name}' while hidden");
                }
            }
            else
            {
                if (ignoredByAI.Contains(aiCol))
                {
                    Physics2D.IgnoreCollision(playerCol, aiCol, false);
                    Debug.Log($"Player: Restored collision with AI '{ai.name}' after unhide");
                }
            }
        }

        if (!ignore)
        {
            ignoredByAI.Clear();
        }
    }

    private IEnumerator FadeSpriteTo(float targetAlpha, float duration)
    {
        if (actorSprite == null) yield break;
        Color start = actorSprite.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(start.a, targetAlpha, t / duration);
            actorSprite.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }
        actorSprite.color = new Color(start.r, start.g, start.b, targetAlpha);
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
    public void AddStress(float amount)
    {
        Stress += Mathf.RoundToInt(amount);
        Debug.Log($"Player: AddStress({amount}) -> Stress={Stress}");

        if (Stress >= 110)
        {
            Debug.Log("Player: Stress >=110, guaranteed death");
            Death();
            return;
        }

        if (Stress > 100)
        {
            float chance = Mathf.Clamp01((Stress - 100f) / 10f);
            if (UnityEngine.Random.value < chance)
            {
                Debug.Log($"Player: Stress death roll succeeded (chance={chance})");
                Death();
            }
        }
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
            Destroy(this.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
