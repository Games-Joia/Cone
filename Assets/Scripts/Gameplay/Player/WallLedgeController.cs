using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Actor))]
public class WallLedgeController : MonoBehaviour
{
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.2f;
    public float grabHoldTime = 0.25f;
    public float releaseCooldown = 0.2f;
    public float wallJumpHorizontal = 200f;
    public float wallJumpVertical = 350f;
    public Vector2 hangOffset = new Vector2(0.4f, -0.3f);

    [Header("Hold settings")]
    [Tooltip("Seconds the player must hold Up/JUMP while hanging to climb")]
    public float climbHoldTime = 0.25f;

    private float climbHoldTimer = 0f;
    [Header("Climb settings")]
    [Tooltip("How long the climb movement takes (seconds)")]
    public float climbDuration = 0.18f;
    [Tooltip("Vertical offset applied when the player finishes climbing onto the ledge")]
    public float climbOffsetY = 0.6f;

    private Vector3 currentHangPoint;

    private Actor actor;
    private Rigidbody2D rb;
    private Collider2D col;

    private bool canGrab = true;
    private float grabTimer = 0f;
    private float releaseTimer = 0f;
    private float originalGravity;

    private int lastWallDir = 0;
    private InputSystem_Actions inputActions;
    private bool isEnteringHang = false;
    void Awake()
    {
        actor = GetComponent<Actor>();
        rb = actor.RigidBody;
        col = actor.ActorCollider;
        originalGravity = rb.gravityScale;
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
    }

    void FixedUpdate()
    {
        if (actor.grounded)
        {
            if (actor.IsWallGrabbing || actor.IsHanging)
            {
                ReleaseAll();
            }
            canGrab = true;
            return;
        }

        if (!canGrab)
        {
            releaseTimer -= Time.fixedDeltaTime;
            if (releaseTimer <= 0f) canGrab = true;
        }

        if (actor.IsHanging)
        {
            HandleHangingInput();
            return;
        }

        if (actor.IsWallGrabbing)
        {
            grabTimer -= Time.fixedDeltaTime;

            bool wallJumpRequested = false;
            if (inputActions != null)
            {
                wallJumpRequested = inputActions.Player.Jump.triggered;
            }
            else
            {
                wallJumpRequested = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            }

            if (wallJumpRequested)
            {
                DoWallJump(lastWallDir);
                return;
            }

            if (grabTimer <= 0f)
            {
                ReleaseAllWithCooldown();
            }
            else
            {
                TryEnterLedgeHang(lastWallDir);
            }

            return;
        }

        Vector2 pos = transform.position;
        bool leftWall = Physics2D.Raycast(pos, Vector2.left, wallCheckDistance, wallLayer);
        bool rightWall = Physics2D.Raycast(pos, Vector2.right, wallCheckDistance, wallLayer);

        float inputX = 0f;
        if (inputActions != null)
        {
            inputX = inputActions.Player.Move.ReadValue<Vector2>().x;
        }
        else if (Player.Instance != null)
            inputX = Player.Instance.input.x;
        else
            inputX = Input.GetAxisRaw("Horizontal");

        if (canGrab && rb.linearVelocity.y < 0f)
        {
            if (leftWall && inputX < -0.1f)
            {
                StartWallGrab(-1);
                return;
            }
            else if (rightWall && inputX > 0.1f)
            {
                StartWallGrab(1);
                return;
            }
        }
    }

    private void StartWallGrab(int dir)
    {
        lastWallDir = dir;
        actor.IsWallGrabbing = true;
        actor.IsHanging = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        grabTimer = grabHoldTime;
        if (actor.Animator != null)
        {
            actor.Animator.SetBool("WallGrab", true);
            actor.Animator.SetBool("LedgeHang", false);
        }
    }

    private void ReleaseAll()
    {
        actor.IsWallGrabbing = false;
        actor.IsHanging = false;
        rb.gravityScale = originalGravity;
    }

    private void ReleaseAllWithCooldown()
    {
        ReleaseAll();
        canGrab = false;
        releaseTimer = releaseCooldown;
    }

    private void DoWallJump(int wallDir)
    {
        ReleaseAll();
        int away = -wallDir;
        rb.AddForce(new Vector2(away * wallJumpHorizontal, wallJumpVertical));
        rb.position += new Vector2(away * 0.05f, 0);
        canGrab = false;
        releaseTimer = releaseCooldown;
    }

    private void TryEnterLedgeHang(int wallDir)
    {
        if (isEnteringHang) return;

        Bounds b = col.bounds;
        float cornerX = b.center.x + wallDir * (b.extents.x + 0.05f);
        float topY = b.center.y + b.extents.y + 0.05f;

        Vector2 probeOrigin = new Vector2(cornerX, topY + 0.6f);
        float probeDistance = 1.0f;
        RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, probeDistance, wallLayer);
        if (hit.collider != null)
        {
            if (hit.point.y > topY + 0.05f)
            {
                Vector3 hangPos = new Vector3(hit.point.x - wallDir * hangOffset.x, hit.point.y + hangOffset.y, transform.position.z);
                Vector2 checkSize = new Vector2(col.bounds.size.x * 0.8f, col.bounds.size.y * 0.6f);
                Vector2 checkCenter = (Vector2)hangPos + new Vector2(0f, checkSize.y * 0.5f);
                Collider2D overlap = Physics2D.OverlapBox(checkCenter, checkSize, 0f, wallLayer);
                if (overlap != null)
                {
                    return;
                }

                isEnteringHang = true;
                actor.IsWallGrabbing = false;
                actor.IsHanging = true;
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;

                rb.MovePosition(hangPos);
                currentHangPoint = hangPos;

                if (actor.Animator != null)
                {
                    actor.Animator.SetBool("LedgeHang", true);
                    actor.Animator.SetBool("WallGrab", false);
                }

                StartCoroutine(ClearEnteringHangNextFrame());
            }
        }
    }

    private IEnumerator ClearEnteringHangNextFrame()
    {
        yield return new WaitForFixedUpdate();
        isEnteringHang = false;
    }

    private void HandleHangingInput()
    {
        float jumpVal = 0f;
        float crouchVal = 0f;
        if (inputActions != null)
        {
            jumpVal = inputActions.Player.Jump.ReadValue<float>();
            crouchVal = inputActions.Player.Crouch.ReadValue<float>();
        }

        bool jumpHeld = jumpVal > 0.5f;
        bool crouchHeld = crouchVal > 0.5f;

        if (crouchHeld)
        {
            ReleaseAllWithCooldown();
            climbHoldTimer = 0f;
            return;
        }

        if (jumpHeld)
        {
            climbHoldTimer += Time.fixedDeltaTime;
            if (climbHoldTimer >= climbHoldTime)
            {
                Vector3 climbTarget = currentHangPoint + new Vector3(0f, climbOffsetY, 0f);
                StartCoroutine(ClimbLedgeRoutine(climbTarget));
                climbHoldTimer = 0f;
            }
        }
        else
        {
            if (climbHoldTimer > 0f) climbHoldTimer = 0f;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.SceneView.RepaintAll();
    }

    void OnDrawGizmos()
    {
        if (actor == null)
        {
            actor = GetComponent<Actor>();
            if (actor == null) return;
        }

        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos, pos + Vector3.left * wallCheckDistance);
        Gizmos.DrawLine(pos, pos + Vector3.right * wallCheckDistance);

        if (col != null)
        {
            Bounds b = col.bounds;
            Vector3 leftOrigin = new Vector3(b.center.x - (b.extents.x + 0.05f), b.center.y + b.extents.y + 0.05f, b.center.z);
            Vector3 rightOrigin = new Vector3(b.center.x + (b.extents.x + 0.05f), b.center.y + b.extents.y + 0.05f, b.center.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(leftOrigin, 0.03f);
            Gizmos.DrawSphere(rightOrigin, 0.03f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftOrigin, leftOrigin + Vector3.up * 0.5f);
            Gizmos.DrawLine(rightOrigin, rightOrigin + Vector3.up * 0.5f);
        }
    }
#endif

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private IEnumerator ClimbLedgeRoutine(Vector3 target)
    {
        if (actor.Animator != null)
        {
            actor.Animator.SetTrigger("LedgeClimb");
        }

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        Vector3 start = transform.position;
        float t = 0f;
        while (t < climbDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / climbDuration);
            float s = Mathf.SmoothStep(0f, 1f, p);
            transform.position = Vector3.Lerp(start, target, s);
            yield return null;
        }

        transform.position = target;

        rb.gravityScale = originalGravity;
        actor.IsHanging = false;
        actor.IsWallGrabbing = false;
        canGrab = false;
        releaseTimer = releaseCooldown;

        if (actor.Animator != null)
        {
            actor.Animator.SetBool("LedgeHang", false);
        }
    }
}
