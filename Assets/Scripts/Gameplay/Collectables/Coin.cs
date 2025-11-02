public class Coin : Collectible
{
    CollectibleType collectibleType = CollectibleType.Coin;

    protected override void Collect(Player player)
    {
        player.AddCollectible(this);
        Destroy(gameObject);
    }

}