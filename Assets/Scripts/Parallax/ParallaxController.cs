using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ParallaxController : MonoBehaviour
{
    [System.Serializable]
    public class LayerSettings
    {
        [Tooltip("A template GameObject with a SpriteRenderer and ParallaxTiler (or at least a SpriteRenderer). The controller will instantiate copies of this template.")]
        public GameObject template;
        [Range(0f, 1f)]
        public float parallaxFactor = 0.5f;
        [Tooltip("Number of pooled tiles (recommended 3)")]
        public int poolSize = 3;
        [Tooltip("Vertical offset applied to each spawned tile relative to template")]
        public float verticalOffset = 0f;
    }

    public LayerSettings[] layers = new LayerSettings[0];

    [Tooltip("Camera to follow. If null, Camera.main will be used.")]
    public Camera targetCamera;

    private readonly List<List<ParallaxTiler>> pools = new List<List<ParallaxTiler>>();

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        InitializePools();
    }

    void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (targetCamera == null) targetCamera = Camera.main;
    }

    void InitializePools()
    {
        // clear existing
        pools.Clear();

        for (int i = 0; i < layers.Length; i++)
        {
            var settings = layers[i];
            var list = new List<ParallaxTiler>();
            if (settings.template == null)
            {
                pools.Add(list);
                continue;
            }

            // Create a container for this layer and position it at controller's position
            var layerRoot = new GameObject($"ParallaxLayer_{i}");
            layerRoot.transform.SetParent(transform, false);
            layerRoot.transform.position = transform.position;

            // Determine base position from the template (use controller position if template is a prefab asset)
            Vector3 templateWorldPos = settings.template != null ? settings.template.transform.position : transform.position;

            // Instantiate poolSize copies and position them horizontally to tile
            // Use the template's sprite width to space them
            var sr = settings.template.GetComponent<SpriteRenderer>();
            float width = sr != null ? sr.bounds.size.x : 10f;
            int poolCount = Mathf.Max(1, settings.poolSize);
            int centerIndex = poolCount / 2;

            for (int j = 0; j < poolCount; j++)
            {
                // instantiate without parent first to avoid local position confusion, then parent under layerRoot
                var inst = Instantiate(settings.template);
                inst.name = settings.template.name + "_tile_" + j;

                // compute world position so tiles are lined up left-to-right around the template position
                float xPos = templateWorldPos.x + (j - centerIndex) * width;
                Vector3 worldPos = new Vector3(xPos, templateWorldPos.y + settings.verticalOffset, templateWorldPos.z);
                inst.transform.position = worldPos;
                inst.transform.SetParent(layerRoot.transform, true);

                // ensure it has ParallaxTiler
                var tiler = inst.GetComponent<ParallaxTiler>();
                if (tiler == null)
                {
                    tiler = inst.AddComponent<ParallaxTiler>();
                }
                tiler.Initialize(targetCamera != null ? targetCamera.transform : null, settings.parallaxFactor);
                list.Add(tiler);
            }

            pools.Add(list);
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;
        var camPos = targetCamera.transform.position;

        for (int i = 0; i < pools.Count; i++)
        {
            var pool = pools[i];
            if (pool == null) continue;
            foreach (var tiler in pool)
            {
                if (tiler == null) continue;
                tiler.UpdateTiler(camPos);
            }
        }
    }

    // Editor helper to (re)create pools from inspector
    [ContextMenu("Rebuild Pools")]
    public void RebuildPools()
    {
        // remove existing children created earlier
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null) DestroyImmediate(c.gameObject);
        }
        InitializePools();
    }

}
