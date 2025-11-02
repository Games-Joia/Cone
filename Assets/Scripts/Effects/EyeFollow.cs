using UnityEngine;

public class EyeFollow : MonoBehaviour
{
    [Tooltip("World target to follow. If null, will find by TargetTag.")]
    public Transform Target;
    public string TargetTag = "Player";

    [Tooltip("Local ellipse radii (x = horizontal, y = vertical).")]
    public Vector2 Radius = new Vector2(0.06f, 0.03f);

    [Tooltip("Allowed angle range in degrees (relative to local +X). For lower half use -180 to 0).")]
    public float MinAngle = -180f;
    public float MaxAngle = 0f;

    [Tooltip("Smoothing speed (0 = immediate).")]
    public float SmoothSpeed = 10f;

    Vector3 targetLocalPos;
    Vector3 velocity;

    void Start()
    {
        if (Target == null && !string.IsNullOrEmpty(TargetTag))
        {
            var go = GameObject.FindWithTag(TargetTag);
            if (go) Target = go.transform;
        }
    }

    void LateUpdate()
    {
        if (Target == null) return;

        Transform reference = transform.parent != null ? transform.parent : transform;
        Vector3 localTargetPos = reference.InverseTransformPoint(Target.position);
        Vector3 localEyePos = reference.InverseTransformPoint(transform.position);

        Vector2 dir = new Vector2(localTargetPos.x - localEyePos.x, localTargetPos.y - localEyePos.y);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right; 

        float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float clampedAngle = Mathf.Clamp(angleDeg, MinAngle, MaxAngle);
        float rad = clampedAngle * Mathf.Deg2Rad;

        Vector2 desiredLocal = new Vector2(Mathf.Cos(rad) * Radius.x, Mathf.Sin(rad) * Radius.y);

        Vector3 targetLocal = new Vector3(desiredLocal.x, desiredLocal.y, transform.localPosition.z);

        if (SmoothSpeed <= 0f)
            transform.localPosition = targetLocal;
        else
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetLocal, ref velocity, 1f / SmoothSpeed);
    }
}