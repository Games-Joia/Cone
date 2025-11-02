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

    private List<Collectible> collectibles = new List<Collectible>();
    public List<Collectible> Collectibles => collectibles;
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
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float a = Mathf.Lerp(start.a, targetAlpha, time / duration);
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
        if (actorSprite == null) actorSprite = GetComponent<SpriteRenderer>();
        if (actorSprite != null)
        {
            float redIntensity = Mathf.Clamp01(Stress / 100f);
            Color baseColor = new Color(1f, 1f - redIntensity, 1f - redIntensity);
            actorSprite.color = baseColor;
            if (waveActive && waveMaterialInstance != null)
            {
                waveMaterialInstance.SetColor("_Color", baseColor);
            }
        }

        if (actorSprite != null)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HitFlashCoroutine());
        }

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

    private IEnumerator HitFlashCoroutine()
    {
        if (actorSprite == null) yield break;

        // Determine base color (prefer shader material color if wave active)
        Color baseColor = actorSprite.color;
        if (waveActive && waveMaterialInstance != null)
        {
            try { baseColor = waveMaterialInstance.GetColor("_Color"); } catch { baseColor = actorSprite.color; }
        }

        // Apply flash color immediately to sprite and shader
        actorSprite.color = hitFlashColor;
        if (waveActive && waveMaterialInstance != null)
        {
            waveMaterialInstance.SetColor("_Color", hitFlashColor);
        }

        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hitFlashDuration);
            Color c = Color.Lerp(hitFlashColor, baseColor, t);
            if (actorSprite != null)
            {
                actorSprite.color = c;
            }
            if (waveActive && waveMaterialInstance != null)
            {
                waveMaterialInstance.SetColor("_Color", c);
            }
            yield return null;
        }

        if (actorSprite != null)
        {
            actorSprite.color = baseColor;
        }
        if (waveActive && waveMaterialInstance != null)
        {
            waveMaterialInstance.SetColor("_Color", baseColor);
        }

        flashRoutine = null;
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
    public void AddCollectible(Collectible collectible)
    {
        collectibles.Add(collectible);
        Debug.Log($"Player: Collected {collectible.getCollectibleType()}. Total collectibles: {collectibles.Count}");
    }
    public override void Death()
    {
        
        Debug.Log("Player has died!");
        Destroy(this.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    [Header("Hit & Wave Effect")]
    [Tooltip("Duration of the red flash when taking stress/hit (seconds)")]
    public float hitFlashDuration = 0.12f;
    [Tooltip("Color to flash when hit")]
    public Color hitFlashColor = Color.red;

    [Tooltip("Wave amplitude (units) applied by the wave shader when stress >= wiggle threshold")]
    public float shaderWaveAmplitude = 0.05f;
    [Tooltip("Wave frequency (how many waves vertically)")]
    public float shaderWaveFrequency = 10f;
    [Tooltip("Wave speed multiplier")]
    public float shaderWaveSpeed = 2f;
        [Tooltip("Maximum stress value used to scale the shader effect (e.g. 110)")]
        public float shaderMaxStress = 110f;
    [Tooltip("Stress threshold where the shader wave should start (e.g. 100)")]
    public float waveThreshold = 100f;

    private Coroutine flashRoutine = null;
    private Material originalMaterial = null;
    private Material waveMaterialInstance = null;
    private bool waveActive = false;

    void LateUpdate()
    {
            if (Stress > waveThreshold && !waveActive)
        {
            EnableWaveMaterial();
        }
            else if (Stress <= waveThreshold && waveActive)
        {
            DisableWaveMaterial();
        }

        if (waveActive && waveMaterialInstance != null)
        {
            float t = 0f;
            if (shaderMaxStress > waveThreshold)
                t = Mathf.Clamp01((Stress - waveThreshold) / (shaderMaxStress - waveThreshold));
            float amp = Mathf.Lerp(shaderWaveAmplitude * 0.5f, shaderWaveAmplitude * 2f, t);
            float freq = shaderWaveFrequency;
            float spd = shaderWaveSpeed;
            waveMaterialInstance.SetFloat("_Amplitude", amp);
            waveMaterialInstance.SetFloat("_Frequency", freq);
            waveMaterialInstance.SetFloat("_Speed", spd);
        }
    }

    void EnableWaveMaterial()
    {
        if (actorSprite == null) actorSprite = GetComponent<SpriteRenderer>();
        if (actorSprite == null) return;
        if (waveActive) return;

        originalMaterial = actorSprite.material;

        Shader s = Shader.Find("Custom/WaveySprite");
        if (s == null)
        {
            Debug.LogWarning("Wavey shader not found (Custom/WaveySprite). Install shader or restart Unity to compile it.");
            return;
        }

        waveMaterialInstance = new Material(s);

        if (actorSprite.sprite != null)
        {
            waveMaterialInstance.SetTexture("_MainTex", actorSprite.sprite.texture);
        }
        waveMaterialInstance.SetColor("_Color", actorSprite.color);
        waveMaterialInstance.SetFloat("_Amplitude", shaderWaveAmplitude);
        waveMaterialInstance.SetFloat("_Frequency", shaderWaveFrequency);
        waveMaterialInstance.SetFloat("_Speed", shaderWaveSpeed);

        actorSprite.material = waveMaterialInstance;
        waveActive = true;
    }

    void DisableWaveMaterial()
    {
        if (!waveActive) return;
        if (actorSprite == null) actorSprite = GetComponent<SpriteRenderer>();

        if (actorSprite != null && originalMaterial != null)
        {
            actorSprite.material = originalMaterial;
        }

        if (waveMaterialInstance != null)
        {
            Destroy(waveMaterialInstance);
            waveMaterialInstance = null;
        }
        originalMaterial = null;
        waveActive = false;
    }
}
