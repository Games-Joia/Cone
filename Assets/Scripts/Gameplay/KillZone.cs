using UnityEngine;
using UnityEngine.SceneManagement;
public class KillZone : MonoBehaviour
{

     private void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(other.gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
