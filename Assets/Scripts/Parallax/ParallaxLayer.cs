using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("0 = static, 1 = moves with camera")]
    public float parallaxFactor = 0.5f;

    Transform cam;
    Vector3 lastCamPos;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cam.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor, 0f);
        lastCamPos = cam.position;
    }
}