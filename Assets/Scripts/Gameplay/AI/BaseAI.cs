using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class BaseAI : MonoBehaviour
{
    // registry of AI instances to allow other systems to find AIs without expensive scene searches
    public static readonly System.Collections.Generic.List<BaseAI> AllAIs = new System.Collections.Generic.List<BaseAI>();
    [SerializeField]
    protected Actor actor;

    [SerializeField]
    protected List<Transform> patrolPoints;

    [SerializeField]
    protected bool pingPong = false;
    protected float arriveDistance = 0.1f;
    protected int currentPointIndex = 0;
    protected int patrolDirection = 1;

    protected Movement cachedMovement;
    protected Rigidbody2D rb2d;
    [Header("Animator / Sprite")]
    [SerializeField]
    protected string animatorVelocityParam = "Velocity";

    [SerializeField]
    protected float fallbackSpeed = 2f;
    private Vector2 pendingMove = Vector2.zero;
    private bool hasPendingMove = false;
    
    /// <summary>
    /// Request a movement direction for this AI. Movement requests are queued and applied in FixedUpdate
    /// which ensures proper physics behavior for Rigidbody2D-driven actors. Subclasses should call this
    /// instead of directly calling Movement.Move when making AI-driven moves.
    /// </summary>
    /// <param name="direction">Normalized direction vector (use Mathf.Sign for X)</param>
    protected void RequestMove(Vector2 direction)
    {
        pendingMove = direction;
        hasPendingMove = true;
    }
    [Header("AI Bounds")]
    [Tooltip("Optional area (Collider2D) the AI is allowed to walk inside. Leave null for no restriction.")]
    [SerializeField]
    protected Collider2D walkableArea;

     protected virtual void Awake()
    {
        AllAIs.Add(this);
        if (actor == null) actor = GetComponent<Actor>();
        rb2d = GetComponent<Rigidbody2D>();
        cachedMovement = actor != null ? actor.Movement : GetComponent<Movement>();
    }

    protected virtual void OnDestroy()
    {
        AllAIs.Remove(this);
    }

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
        // diagnostic info to help track null refs at runtime
        Debug.Log($"[{name}] Awake summary - actor={(actor!=null?actor.name:"null")}, cachedMovement={(cachedMovement!=null?cachedMovement.GetType().Name:"null")}, rb2d={(rb2d!=null?"yes":"no")}");
    }

    protected virtual void Update()
    {
        bool handled = false;
        try
        {
            handled = CustomMovement();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{name}: CustomMovement threw: {ex.Message}");
            handled = false;
        }

        Debug.Log($"[{name}] Handled: {handled}");
        if (!handled && patrolPoints != null && patrolPoints.Count > 0)
        {
            QueuePatrolMovement();
        }
    }

    void FixedUpdate()
    {
        if (hasPendingMove)
        {
            ApplyPendingMove();
            hasPendingMove = false;
            pendingMove = Vector2.zero;
        }
    }

    private void QueuePatrolMovement()
    {
        if (actor == null) return;
        if (patrolPoints == null || patrolPoints.Count == 0) return;

        Transform targetPoint = patrolPoints[currentPointIndex];
        float dx = targetPoint.position.x - actor.transform.position.x;
        float arriveAbs = arriveDistance;
        if (Mathf.Abs(dx) < arriveAbs)
        {
            currentPointIndex = GetNextIndex(currentPointIndex);
            hasPendingMove = false;
            return;
        }

        pendingMove = new Vector2(Mathf.Sign(dx), 0f);
        hasPendingMove = true;
    }

    private void ApplyPendingMove()
    {
        if (!hasPendingMove) return;
        Vector2 direction = pendingMove;

        float evalVelocity = 0f;

        if (cachedMovement != null)
        {
            cachedMovement.Move(direction);
            evalVelocity = Mathf.Abs(direction.x) * fallbackSpeed;
        }
        else if (actor != null && actor.Movement != null)
        {
            actor.Movement.Move(direction);
            evalVelocity = Mathf.Abs(direction.x) * fallbackSpeed;
        }
        else if (rb2d != null)
        {
            rb2d.linearVelocity = new Vector2(direction.x * fallbackSpeed, rb2d.linearVelocity.y);
            evalVelocity = Mathf.Abs(rb2d.linearVelocity.x);
        }
        else if (actor != null && actor.RigidBody != null)
        {
            actor.RigidBody.linearVelocity = new Vector2(direction.x * fallbackSpeed, actor.RigidBody.linearVelocity.y);
            evalVelocity = Mathf.Abs(actor.RigidBody.linearVelocity.x);
        }
        else
        {
            Debug.LogWarning($"{name}: No Movement or Rigidbody available to apply AI movement.");
            evalVelocity = 0f;
        }

        ApplyAnimatorAndFlip(direction.x, evalVelocity);
    }

    private void FollowPatrolPoints()
    {
        if (actor == null)
        {
            Debug.LogWarning($"{name}: actor is null, cannot patrol.");
            return;
        }

        if (patrolPoints == null || patrolPoints.Count == 0) return;

    Transform targetPoint = patrolPoints[currentPointIndex];
    float dx = targetPoint.position.x - actor.transform.position.x;
    Vector2 direction = new Vector2(Mathf.Sign(dx), 0f);

        Debug.Log($"[{name}] Patrolling towards point {currentPointIndex} at {targetPoint.position}");
        Debug.Log($"[{name}] Direction: {direction}");
        Debug.Log($"[{name}] Actor: {actor}");
        Debug.Log($"[{name}] Actor Movement: {cachedMovement}");

        if (cachedMovement != null)
        {
            cachedMovement.Move(direction);
        }
        else if (actor != null && actor.Movement != null)
        {
            actor.Movement.Move(direction);
        }
        else if (rb2d != null)
        {
            rb2d.linearVelocity = new Vector2(direction.x * fallbackSpeed, rb2d.linearVelocity.y);
        }
        else
        {
            Debug.LogWarning($"{name}: No Movement or Rigidbody2D available to move the AI.");
            return;
        }

        float evalVelocity = 0f;
        if (rb2d != null)
            evalVelocity = Mathf.Abs(rb2d.linearVelocity.x);
        else if (actor != null && actor.RigidBody != null)
            evalVelocity = Mathf.Abs(actor.RigidBody.linearVelocity.x);
        else
            evalVelocity = Mathf.Abs(direction.x) * fallbackSpeed;

        ApplyAnimatorAndFlip(direction.x, evalVelocity);

        if (Mathf.Abs(dx) < arriveDistance)
        {
            currentPointIndex = GetNextIndex(currentPointIndex);
        }
    }

    private void ApplyAnimatorAndFlip(float dirX, float velocity)
    {
        if (actor == null) return;

        var anim = actor.Animator;
        if (anim != null && !string.IsNullOrEmpty(animatorVelocityParam))
        {
            anim.SetFloat(animatorVelocityParam, velocity);
        }

        if (actor.actorSprite != null)
        {
            if (dirX < -0.01f) actor.actorSprite.flipX = true;
            else if (dirX > 0.01f) actor.actorSprite.flipX = false;
        }
        else
        {
            if (Mathf.Abs(dirX) > 0.01f)
            {
                var s = actor.transform.localScale;
                s.x = Mathf.Sign(dirX) * Mathf.Abs(s.x);
                actor.transform.localScale = s;
            }
        }
    }

    private int GetNextIndex(int from)
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return from;
        int next = from + patrolDirection;
        if (pingPong)
        {
            if (next >= patrolPoints.Count || next < 0)
            {
                patrolDirection = -patrolDirection;
                next = from + patrolDirection;
                next = Mathf.Clamp(next, 0, patrolPoints.Count - 1);
            }
        }
        else
        {
            if (next >= patrolPoints.Count) next = 0;
            else if (next < 0) next = patrolPoints.Count - 1;
        }
        return next;
    }

    protected abstract bool CustomMovement();

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.SceneView.RepaintAll();
    }

    void OnDrawGizmos()
    {
        if (walkableArea != null)
        {
            Gizmos.color = Color.cyan;
            var b = walkableArea.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        for (int i = 0; i < patrolPoints.Count; i++)
        {
            var p = patrolPoints[i];
            if (p == null) continue;
            Gizmos.color = (i == currentPointIndex) ? Color.green : Color.yellow;
            Gizmos.DrawSphere(p.position, 0.12f);
            UnityEditor.Handles.Label(p.position + Vector3.up * 0.12f, i.ToString());
            int next = (i + 1) % patrolPoints.Count;
            if (patrolPoints[next] != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(p.position, patrolPoints[next].position);
            }
        }

        if (actor != null && currentPointIndex >= 0 && currentPointIndex < patrolPoints.Count && patrolPoints[currentPointIndex] != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(actor.transform.position, patrolPoints[currentPointIndex].position);
        }
    }
#endif
}