using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Actor actor { set; get; }
    private Vector2 velocity = Vector2.zero;
    [Header("Wall detection")]
    [Tooltip("Layer mask used to detect walls for blocking horizontal movement while airborne.")]
    public LayerMask wallLayer;
    [Tooltip("Horizontal ray distance to check for walls when airborne")]
    public float wallCheckDistance = 0.2f;

    private void Awake() { }

    private void Start()
    {
        if (actor == null)
        {
            actor = GetComponent<Actor>();
        }
    }   

    public void Move(Vector2 input)
    {
        // ensure actor reference is available (can be null if component order differs)
        if (actor == null)
        {
            actor = GetComponent<Actor>();
            if (actor == null)
            {
                Debug.LogWarning($"{name}: Movement.Move called but no Actor found on the GameObject.");
                return;
            }
        }

        if (actor.IsDashing)
            return;

        if (actor.IsHanging)
        {
            var _rb = actor.RigidBody;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            return;
        }

        if (actor.IsWallGrabbing)
        {
            var _rb = actor.RigidBody;
            if (_rb == null)
            {
                return;
            }
            var v = _rb.linearVelocity;
            float slideSpeed = -2f;
            v.x = 0f;
            // allow gentle slide down: ensure we take the more negative (downwards) value
            v.y = Mathf.Min(v.y, slideSpeed);
            _rb.linearVelocity = v;
            return;
        }

        if (!actor.grounded && input.x != 0f && wallLayer != 0)
        {
            Vector2 origin = transform.position;
            Vector2 dir = input.x > 0f ? Vector2.right : Vector2.left;
            bool wallNearby = Physics2D.Raycast(origin, dir, wallCheckDistance, wallLayer);
            var _rb = actor.RigidBody;
            if (wallNearby && _rb != null && _rb.linearVelocity.y < 0f)
            {
                input.x = 0f;
            }
        }

        float activeSpeed =
            actor.IsRunning ? actor.runSpeed
            : actor.IsCrouching ? actor.crouchSpeed
            : actor.walkSpeed;

        // sprite flip
        if (actor.actorSprite != null)
        {
            if (input.x < 0) actor.actorSprite.flipX = true;
            else if (input.x > 0) actor.actorSprite.flipX = false;
        }

        // animator (optional)
        if (actor.Animator != null)
        {
            actor.Animator.SetFloat("Velocity", Mathf.Abs(input.x));
        }

        // finally, apply velocity via Rigidbody (must be present)
        var rb = actor.RigidBody;
        if (rb == null)
        {
            Debug.LogWarning($"{name}: Movement.Move cannot apply velocity because Actor has no Rigidbody2D.");
            return;
        }

        Vector2 targetVelocity = new Vector2(
            input.x * activeSpeed * 10 * Time.fixedDeltaTime,
            rb.linearVelocity.y
        );

        rb.linearVelocity = Vector2.SmoothDamp(
            rb.linearVelocity,
            targetVelocity,
            ref velocity,
            actor.smoothing
        );
    }

    public IEnumerator Jump()
    {
        if (actor.JumpRequested)
        {
            Debug.Log("JumpingInsideMov");
            actor.IsJumping = true;
            actor.grounded = false;
            actor.RigidBody.AddForceY(actor.jumpForce);
            actor.JumpRequested = false;
            yield return new WaitForSeconds(0.5f);
            actor.IsJumping = false;
            actor.Animator.SetBool("Jumping", false);
        }
    }
}
