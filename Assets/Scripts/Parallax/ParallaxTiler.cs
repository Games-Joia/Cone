using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxTiler : MonoBehaviour
{
    SpriteRenderer sr;
    Transform cam;
    Vector3 startPos;
    float spriteWidth;
    public float ParallaxFactor { get; private set; } = 0.5f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        UpdateSpriteWidth();
    }

    public void UpdateSpriteWidth()
    {
        if (sr != null)
            spriteWidth = sr.bounds.size.x;
    }

    public void Initialize(Transform cameraTransform, float parallaxFactor)
    {
        cam = cameraTransform;
        ParallaxFactor = parallaxFactor;
        startPos = transform.position;
        UpdateSpriteWidth();
    }

    public void UpdateTiler(Vector3 camPos)
    {
        if (cam == null)
            return;

        Vector3 target = startPos + new Vector3(camPos.x * (1 - ParallaxFactor), camPos.y * (1 - ParallaxFactor), 0f);
        transform.position = new Vector3(target.x, target.y, transform.position.z);

        if (spriteWidth <= 0f) UpdateSpriteWidth();

        float diff = camPos.x - transform.position.x;
        if (Mathf.Abs(diff) >= spriteWidth && spriteWidth > 0f)
        {
            float offset = Mathf.Sign(diff) * spriteWidth * Mathf.FloorToInt(Mathf.Abs(diff) / spriteWidth);
            transform.position += new Vector3(offset, 0f, 0f);
            startPos += new Vector3(offset, 0f, 0f);
        }
    }

    public float Width => spriteWidth;
}