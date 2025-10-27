using UnityEngine;

public class SightBasedAI : BaseAI
{
    [SerializeField]
    private float sightRange = 5f;

    protected override bool CustomMovement()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(actor.transform.position, sightRange);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector2 directionToPlayer = (hit.transform.position - actor.transform.position).normalized;
                Debug.Log(actor);
                actor.movement.Move(directionToPlayer);
                return true;
            }
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(actor.transform.position, sightRange);
    }
}