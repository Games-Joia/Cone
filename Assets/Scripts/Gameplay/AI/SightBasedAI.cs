using UnityEngine;

public class SightBasedAI : BaseAI
{
    [SerializeField]
    private float sightRange = 5f;
    [Tooltip("Optional tag to filter targets. Leave empty to target any Actor.")]
    [Tag]
    public string targetTag = "";

    protected override bool CustomMovement()
    {
        if (actor == null) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(actor.transform.position, sightRange);

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!string.IsNullOrEmpty(targetTag) && !hit.CompareTag(targetTag)) continue;
            var targetActor = hit.GetComponent<Actor>();
            if (targetActor == null) continue;
            if (targetActor == actor) continue;
            if (targetActor.IsHidden) continue;

            float dx = hit.transform.position.x - actor.transform.position.x;
            Vector2 axisInput = new Vector2(Mathf.Sign(dx), 0f);
            RequestMove(axisInput);
            float evalVelocity = 0f;
            if (rb2d != null)
                evalVelocity = Mathf.Abs(rb2d.linearVelocity.x);
            else if (actor != null && actor.RigidBody != null)
                evalVelocity = Mathf.Abs(actor.RigidBody.linearVelocity.x);
            else
                evalVelocity = Mathf.Abs(axisInput.x) * fallbackSpeed;

            if (actor != null && actor.Animator != null && !string.IsNullOrEmpty(animatorVelocityParam))
            {
                actor.Animator.SetFloat(animatorVelocityParam, evalVelocity);
            }

            if (actor != null && actor.actorSprite != null)
            {
                if (dx < -0.01f) actor.actorSprite.flipX = true;
                else if (dx > 0.01f) actor.actorSprite.flipX = false;
            }
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(actor.transform.position, sightRange);
    }
}