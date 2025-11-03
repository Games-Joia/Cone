using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyCollision : MonoBehaviour
{
    [Tooltip("Stress the enemy gains when stomped")]
    public float stompStress = 30f;

    [Tooltip("Stress added to player when enemy collides into the player")]
    public float collisionStressToPlayer = 15f;

    [Tooltip("Bounce velocity applied to player when stomping an enemy")]
    public float playerBounceVelocity = 8f;

    [Tooltip("Contact point tolerance (world units) used to consider a contact as a stomp.")]
    public float stompTolerance = 0.08f;

    [Tooltip("Player downward linearVelocity threshold to consider stomp (negative value).")]
    public float stompVelocityThreshold = -0.1f;

    [Tooltip("Fallback positional Y offset for stomps when no rigidbody (world units).")]
    public float stompPositionYOffset = 0.1f;
    
    [Tooltip("Minimum upward component of the contact normal required to consider a stomp (0..1).")]
    [Range(0f,1f)]
    public float stompNormalThreshold = 0.5f;

    [Tooltip("Fraction of the collider width to inset the stomp zone from the sides (0..0.5).")]
    [Range(0f,0.45f)]
    public float stompSideInsetFraction = 0.2f;

    [Tooltip("Stress threshold at which enemy will start having a chance to die")]
    public float deathAt = 100f;

    [Tooltip("Stress at which enemy is guaranteed to die")]
    public float guaranteedDeathAt = 110f;

    [Header("VFX")]
    [Tooltip("Optional particle prefab to spawn when this enemy dies (assign a ParticleSystem prefab).")]
    public GameObject deathEffect;

    [Tooltip("Interval in seconds between repeated collision damage while staying in contact")]
    public float collisionDamageInterval = 0.5f;

    private float stress = 0f;
    private Actor actor;
    private Collider2D enemyCollider;
    private Dictionary<Player, float> lastCollisionDamageTime = new Dictionary<Player, float>();

    void Awake()
    {
        actor = GetComponent<Actor>();
        enemyCollider = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null) return;

        var player = collision.collider.GetComponent<Player>();
        if (player != null)
        {
            bool stomped = IsStomp(collision);
            Rigidbody2D playerRigidbody = collision.rigidbody;

            if (stomped)
                HandleStomp(player, playerRigidbody);
            else
            {
                HandleCollisionWithPlayer(player);
                lastCollisionDamageTime[player] = Time.time;
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision == null) return;
        var player = collision.collider.GetComponent<Player>();
        if (player == null) return;

        if (IsStomp(collision)) return;

        float lastTime = 0f;
        lastCollisionDamageTime.TryGetValue(player, out lastTime);
        if (Time.time - lastTime >= collisionDamageInterval)
        {
            HandleCollisionWithPlayer(player);
            lastCollisionDamageTime[player] = Time.time;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision == null) return;
        var player = collision.collider.GetComponent<Player>();
        if (player == null) return;
        if (lastCollisionDamageTime.ContainsKey(player))
            lastCollisionDamageTime.Remove(player);
    }

    private bool IsStomp(Collision2D collision)
    {
        if (collision == null) return false;
        if (collision.contactCount > 0 && enemyCollider != null)
        {
            Bounds bounds = enemyCollider.bounds;
            float enemyTop = bounds.max.y;
            float inset = Mathf.Clamp01(stompSideInsetFraction) * bounds.size.x;
            float allowedMinX = bounds.min.x + inset;
            float allowedMaxX = bounds.max.x - inset;

            foreach (var contactPoint in collision.contacts)
            {
                float contactY = contactPoint.point.y;
                float contactX = contactPoint.point.x;
                float normalY = contactPoint.normal.y;

                bool withinTopBand = contactY > enemyTop - stompTolerance;
                bool normalUp = normalY >= stompNormalThreshold;
                bool withinX = contactX >= allowedMinX && contactX <= allowedMaxX;

                if (withinTopBand && normalUp && withinX)
                    return true;
            }
        }

        Rigidbody2D playerRigidbody = collision.rigidbody;
        if (playerRigidbody != null)
        {
            if (playerRigidbody.linearVelocity.y < stompVelocityThreshold)
                return true;
        }
        else
        {
            var player = collision.collider.GetComponent<Player>();
            if (player != null)
            {
                if (player.transform.position.y > transform.position.y + stompPositionYOffset)
                    return true;
            }
        }

        return false;
    }

    private void HandleStomp(Player player, Rigidbody2D playerRigidbody)
    {
        stress += stompStress;
        Debug.Log($"{name}: Stomped by Player -> stress={stress}");

        if (stress >= guaranteedDeathAt)
        {
            Die();
        }
        else if (stress >= deathAt)
        {
            if (Random.value < 0.2f)
                Die();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = new Vector2(playerRigidbody.linearVelocity.x, playerBounceVelocity);
        }
    }

    private void HandleCollisionWithPlayer(Player player)
    {
        Debug.Log($"{name}: Collided into player, adding {collisionStressToPlayer} stress to player");
        player.AddStress(collisionStressToPlayer);
    }

    private void Die()
    {
        Debug.Log($"{name}: Enemy died from stress.");
        if (deathEffect != null)
        {
            var instance = Instantiate(deathEffect, transform.position, Quaternion.identity);
            var particleSystemComponent = instance.GetComponent<ParticleSystem>();
            if (particleSystemComponent == null)
            {
                particleSystemComponent = instance.GetComponentInChildren<ParticleSystem>();
            }
            if (particleSystemComponent != null)
            {
                float lifetime = 0f;
                try
                {
                    var main = particleSystemComponent.main;
                    lifetime = main.duration;
                    lifetime += Mathf.Max(0.1f, main.startLifetime.constantMax);
                }
                catch
                {
                    lifetime = 2f;
                }
                Destroy(instance, lifetime + 0.25f);
            }
            else
            {
                Destroy(instance, 5f);
            }
        }
        if (actor != null)
        {
            actor.Death();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (stress >= deathAt && stress < guaranteedDeathAt)
        {
            if (Random.value < 0.01f)
            {
                Die();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        if (enemyCollider == null)
            return;

        Bounds bounds = enemyCollider.bounds;

    float inset = Mathf.Clamp01(stompSideInsetFraction) * bounds.size.x;
    float allowedMinX = bounds.min.x + inset;
    float allowedMaxX = bounds.max.x - inset;

    float stompHeight = Mathf.Max(0.001f, stompTolerance);
    Vector3 innerCenter = new Vector3((allowedMinX + allowedMaxX) * 0.5f, bounds.max.y - stompHeight * 0.5f, bounds.center.z);
    Vector3 innerSize = new Vector3(Mathf.Max(0.001f, allowedMaxX - allowedMinX), stompHeight, bounds.size.z);

    Color fillColor = new Color(1f, 0.9f, 0.0f, 0.18f);
    Color lineColor = Color.yellow;

    Gizmos.color = fillColor;
    Gizmos.DrawCube(innerCenter, innerSize);
    Gizmos.color = lineColor;
    Gizmos.DrawWireCube(innerCenter, innerSize);

    Gizmos.color = new Color(0f, 1f, 0f, 0.12f);
    Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
