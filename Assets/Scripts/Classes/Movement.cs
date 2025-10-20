using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Actor actor { set; get; }

    private Vector2 velocity = Vector2.zero;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator anim;

    private bool jumpRequested = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 input, bool isRunning, bool isCrouching, bool isDashing)
    {
        if (isDashing)
            return;

        float activeSpeed =
            isRunning ? actor.runSpeed
            : isCrouching ? actor.crouchSpeed
            : actor.walkSpeed;

        if (input.x < 0)
            sprite.flipX = false;
        else if (input.x > 0)
            sprite.flipX = true;

        anim.SetFloat("Velocity", Mathf.Abs(input.x));

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
        if (jumpRequested)
        {
            actor.IsJumping = true;
            actor.grounded = false;
            actor.RigidBody.AddForceY(actor.jumpForce);
            jumpRequested = false;
            yield return new WaitForSeconds(0.5f);
            actor.IsJumping = false;
            anim.SetBool("Jumping", false);
        }
    }
}
