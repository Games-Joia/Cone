using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public Actor actor { set; get; }
    private Vector2 velocity = Vector2.zero;

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
        if (actor.IsDashing)
            return;

        float activeSpeed =
            actor.IsRunning ? actor.runSpeed
            : actor.IsCrouching ? actor.crouchSpeed
            : actor.walkSpeed;

        if (input.x < 0)
            actor.actorSprite.flipX = true;
        else if (input.x > 0)
            actor.actorSprite.flipX = false;

        actor.Animator.SetFloat("Velocity", Mathf.Abs(input.x));

        Vector2 targetVelocity = new Vector2(
            input.x * activeSpeed * 10 * Time.fixedDeltaTime,
            actor.RigidBody.linearVelocity.y
        );

        actor.RigidBody.linearVelocity = Vector2.SmoothDamp(
            actor.RigidBody.linearVelocity,
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
