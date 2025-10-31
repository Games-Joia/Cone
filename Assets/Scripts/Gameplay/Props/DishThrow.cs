using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class DishThrow : MonoBehaviour
{
    public string throwTrigger = "Throw";
    public string breakTrigger = "Break";
    public float launchSpeed = 8f;
    public bool autoLaunchOnStart = true;
    public string breakTag = "DishBreak";
    public Collider2D breakCollider;
    public float destroyDelayAfterBreak = 0.5f;
    public bool applySpin = true;
    public float spinTorque = 80f;
    public Transform target;
    public float stressToPlayerOnHit = 120f;

    Rigidbody2D rb;
    Animator animator;
    Collider2D myCollider;
    bool broken = false;
    bool launched = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        myCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (autoLaunchOnStart && !launched)
        {
            Vector2 dir = target != null ? (target.position - transform.position).normalized : transform.right;
            Launch(dir);
        }
    }

    public void Launch(Vector2 direction)
    {
        if (launched || broken) return;
        launched = true;
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = direction.normalized * launchSpeed;
            if (applySpin) rb.AddTorque(spinTorque * Mathf.Sign(direction.x));
        }
        if (animator != null && !string.IsNullOrEmpty(throwTrigger)) animator.SetTrigger(throwTrigger);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (broken) return;
        if (col == null) return;
        var player = col.collider.GetComponent<Player>();
        if (player != null)
        {
            player.AddStress(stressToPlayerOnHit);
            Break();
            return;
        }
        if (breakCollider != null)
        {
            if (col.collider == breakCollider) { Break(); return; }
        }
        else
        {
            if (!string.IsNullOrEmpty(breakTag) && col.collider.CompareTag(breakTag)) { Break(); return; }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (broken) return;
        if (other == null) return;
        var player = other.GetComponent<Player>();
        if (player != null)
        {
            player.AddStress(stressToPlayerOnHit);
            Break();
            return;
        }
        if (breakCollider != null)
        {
            if (other == breakCollider) { Break(); return; }
        }
        else
        {
            if (!string.IsNullOrEmpty(breakTag) && other.CompareTag(breakTag)) { Break(); return; }
        }
    }

    public void Break()
    {
        if (broken) return;
        broken = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
        if (myCollider != null) myCollider.enabled = false;
        if (animator != null && !string.IsNullOrEmpty(breakTrigger)) animator.SetTrigger(breakTrigger);
        StartCoroutine(FadeAndDestroy(destroyDelayAfterBreak));
    }

    IEnumerator FadeAndDestroy(float duration)
    {
        var srs = GetComponentsInChildren<SpriteRenderer>();

        Color[] starts = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++)
        {
            starts[i] = srs[i].color;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - (t / Mathf.Max(0.0001f, duration)));
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                var c = starts[i];
                srs[i].color = new Color(c.r, c.g, c.b, c.a * a);
            }
            yield return null;
        }

        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] == null) continue;
            var c = starts[i];
            srs[i].color = new Color(c.r, c.g, c.b, 0f);
        }

        Destroy(gameObject);
    }

    public void LaunchAtCollider(Collider2D targetCollider)
    {
        if (targetCollider == null || myCollider == null) return;
        Vector2 dir = (targetCollider.bounds.center - myCollider.bounds.center).normalized;
        Launch(dir);
    }
}
