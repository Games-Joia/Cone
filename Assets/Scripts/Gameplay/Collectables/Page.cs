public class Page : Collectible
{
    protected override void Collect(Player player)
    {
        player.AddCollectible(this);
        Destroy(gameObject);
    }

}