
using UnityEngine;
public class Brocollis : Actor
{
    public override void Death()
    {
        // Implement death behavior for Brocollis here
        Debug.Log("Brocollis has died.");
        Destroy(gameObject);
    }
}
