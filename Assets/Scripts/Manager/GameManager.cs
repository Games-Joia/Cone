using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager instance;
    public static Player player;

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    } 
    
}