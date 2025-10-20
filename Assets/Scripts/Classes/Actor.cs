using UnityEngine;

public class Actor : MonoBehaviour
{
    [SerializeField]
    private int stress;

    [field: SerializeField]
    public Movement movement { get; set; }

    [field: SerializeField]
    public SpriteRenderer actorSprite { get; set; }

    [field: SerializeField]
    public bool IsJumping { get; set; } = false;

    [field: SerializeField]
    public bool IsCrouching { get; set; } = false;

    [field: SerializeField]
    public bool IsRunning { get; set; } = false;

    [field: SerializeField]
    public bool IsDashing { get; set; } = false;

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

    public bool grounded { get; set; } = false;
}
