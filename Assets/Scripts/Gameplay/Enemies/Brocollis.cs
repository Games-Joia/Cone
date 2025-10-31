
using UnityEngine;
public class Brocollis : Actor
{
    public override void Death()
    {
        Debug.Log("Brocollis has died.");
        Destroy(gameObject);
    }
}
