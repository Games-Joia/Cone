using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    private CollectibleType collectibleType;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Collect(other.GetComponent<Player>());
        }
    }

    public CollectibleType getCollectibleType()
    {
        return collectibleType;
    }
    protected abstract void Collect(Player player);
}