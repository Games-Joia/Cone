using UnityEngine;

public class HideZone : MonoBehaviour
{
    [Tooltip("Optional: target alpha when hidden (player's own settings may override)")]
    public float hiddenAlpha = 0.18f;

    [Tooltip("Optional: fade duration in seconds")]
    public float fadeDuration = 0.25f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<Player>();
        if (player != null)
        {
            Debug.Log($"HideZone: Player entered hide zone '{name}'");
            player.OnEnterHideZone();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<Player>();
        if (player != null)
        {
            Debug.Log($"HideZone: Player exited hide zone '{name}'");
            player.OnExitHideZone();
        }
    }
}
