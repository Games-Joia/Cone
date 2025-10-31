using UnityEngine;

public class Onion : Actor
{
    public override void Death()
    {
        Debug.Log("Onion has died.");
        Destroy(gameObject);
    }
}