using UnityEngine;

public abstract class Actor : MonoBehaviour
{
    [SerializeField]
    private int stress;
    public int Stress
    {
        get { return stress; }
        set { stress = Mathf.Max(0, value); }
    }
    [SerializeField]
    private int damage;
    public int Damage
    {
        get { return damage; }
        set { damage = Mathf.Max(0, value); }
    }

    [SerializeField]
    private Movement _movement;
    public Movement Movement
    {
        get
        {
            if (_movement == null)
            {
                _movement = GetComponent<Movement>();
            }
            return _movement;
        }
        set => _movement = value;
    }

    [SerializeField]
    public SpriteRenderer actorSprite { get; set; }

    [SerializeField]
    public bool IsJumping { get; set; } = false;

    [SerializeField]
    public bool IsCrouching { get; set; } = false;

    [SerializeField]
    public bool IsRunning { get; set; } = false;

    [SerializeField]
    public bool IsDashing { get; set; } = false;

    [SerializeField]
    public bool IsWallGrabbing { get; set; } = false;

    [SerializeField]
    public bool IsHanging { get; set; } = false;

    [SerializeField]
    public bool IsHidden { get; set; } = false;

    [Header("Movement Parameters")]
    [SerializeField]
    public float jumpForce = 400f;
    public float crouchSpeed = 7.5f;
    public float walkSpeed = 15.0f;
    public float runSpeed = 30.0f;

    [SerializeField]
    [Range(0.01f, 0.3f)]
    public float smoothing = 0.05f;

    private bool jumpRequested = false;
    public bool JumpRequested
    {
        get { return jumpRequested; }
        set { jumpRequested = value; }
    }

    [SerializeField]
    private Animator anim;
    public Animator Animator
    {
        get
        {
            if (anim == null)
            {
                anim = GetComponent<Animator>();
            }
            return anim;
        }
    }
    private Rigidbody2D rigidBody;
    public Rigidbody2D RigidBody
    {
        get
        {
            if (rigidBody == null)
            {
                rigidBody = GetComponent<Rigidbody2D>();
            }
            return rigidBody;
        }
    }
    public Collider2D ActorCollider
    {
        get
        {
            return GetComponent<Collider2D>();
        }
    }
    public bool grounded { get; set; } = false;

    public abstract void Death();
}
