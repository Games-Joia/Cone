using UnityEngine;
using TMPro;

public class InteractionPrompt : MonoBehaviour
{
    public GameObject promptUI;

    void Awake()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (promptUI != null) promptUI.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
    }
}