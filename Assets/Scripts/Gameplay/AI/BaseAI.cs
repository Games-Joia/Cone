using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAI : MonoBehaviour
{
    [SerializeField]
    protected Actor actor;

    [SerializeField]
    protected List<Transform> patrolPoints;

    protected int currentPointIndex = 0;

    protected virtual void Start()
    {
        if (actor == null)
        {
            actor = GetComponent<Actor>();
        }
        if (patrolPoints == null || patrolPoints.Count == 0)
        {
            Debug.LogWarning("No patrol points set for AI.");
        }
    }

    protected virtual void Update()
    {
        if (!CustomMovement() && patrolPoints != null && patrolPoints.Count > 0)
        {
            FollowPatrolPoints();
        }

        CustomMovement();
    }

    private void FollowPatrolPoints()
    {
        Transform targetPoint = patrolPoints[currentPointIndex];
        Vector2 direction = (targetPoint.position - actor.transform.position).normalized;
        actor.movement.Move(direction);


        if (Vector2.Distance(actor.transform.position, targetPoint.position) < 0.1f)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Count;
        }
    }

    protected abstract bool CustomMovement();
}