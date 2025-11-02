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

    [Tooltip("Stress threshold at which enemy will start having a chance to die")]
    public float deathAt = 100f;

    [Tooltip("Stress at which enemy is guaranteed to die")]
    public float guaranteedDeathAt = 110f;

    [Header("VFX")]
    [Tooltip("Optional particle prefab to spawn when this enemy dies (assign a ParticleSystem prefab).")]
    public GameObject deathEffect;

    private float stress = 0f;
    private Actor actor;

    void Awake()
    {
        actor = GetComponent<Actor>();
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col == null) return;

        var player = col.collider.GetComponent<Player>();
        if (player != null)
        {
            bool stomped = false;
            if (col.rigidbody != null)
            {
                float playerY = col.rigidbody.transform.position.y;
                float enemyY = transform.position.y;
                if (playerY > enemyY + 0.1f)
                    stomped = true;
            }
            else
            {
                if (player.transform.position.y > transform.position.y + 0.1f)
                    stomped = true;
            }

            if (stomped)
                HandleStomp(player, col.rigidbody);
            else
                HandleCollisionWithPlayer(player);
        }
    }

    private void HandleStomp(Player player, Rigidbody2D playerRb)
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

        if (playerRb != null)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, playerBounceVelocity);
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
            var inst = Instantiate(deathEffect, transform.position, Quaternion.identity);
            var ps = inst.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = inst.GetComponentInChildren<ParticleSystem>();
            }
            if (ps != null)
            {
                float lifetime = 0f;
                try
                {
                    var main = ps.main;
                    lifetime = main.duration;
                    lifetime += Mathf.Max(0.1f, main.startLifetime.constantMax);
                }
                catch
                {
                    lifetime = 2f;
                }
                Destroy(inst, lifetime + 0.25f);
            }
            else
            {
                Destroy(inst, 5f);
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
}
