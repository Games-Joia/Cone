using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAI : MonoBehaviour
{
    [SerializeField]
    protected Actor actor;

    [SerializeField]
    protected List<Transform> patrolPoints;

    [SerializeField]
    protected bool pingPong = false;
    protected float arriveDistance = 0.1f;
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
        bool handled = CustomMovement();
        Debug.Log($"Handled: {handled}");
        if (!handled && patrolPoints != null && patrolPoints.Count > 0)
        {
            FollowPatrolPoints();
        }
    }

    private void FollowPatrolPoints()
    {
        Transform targetPoint = patrolPoints[currentPointIndex];
        Vector2 direction = (targetPoint.position - actor.transform.position).normalized;
        Debug.Log($"Patrolling towards point {currentPointIndex} at {targetPoint.position}");
        Debug.Log($"Direction: {direction}");
        Debug.Log($"Actor: {actor}");
        Debug.Log($"Actor Movement: {actor.Movement}");
        actor.Movement.Move(direction);

        float arriveSqr = arriveDistance * arriveDistance;
        if (Vector2.SqrMagnitude(targetPoint.position - actor.transform.position) < arriveSqr)
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Count;
        }
    }

    protected abstract bool CustomMovement();
}