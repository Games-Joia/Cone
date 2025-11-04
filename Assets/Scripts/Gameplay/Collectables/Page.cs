public class Page : Collectible
{
    CollectibleType collectibleType = CollectibleType.Page;
    protected override void Collect(Player player)
    {
        player.AddCollectible(this);
        Destroy(gameObject);
    }

    

}