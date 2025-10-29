using UnityEngine;

public class Onion : Actor
{
    public override void Death()
    {
        // Implement death behavior for Onion here
        Debug.Log("Onion has died.");
        Destroy(gameObject);
    }
}