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

            var layerRoot = new GameObject($"ParallaxLayer_{i}");
            layerRoot.transform.SetParent(transform, false);
            layerRoot.transform.position = transform.position;

            Vector3 templateWorldPos = settings.template != null ? settings.template.transform.position : transform.position;

            var sr = settings.template.GetComponent<SpriteRenderer>();
            float width = sr != null ? sr.bounds.size.x : 10f;
            int poolCount = Mathf.Max(1, settings.poolSize);
            int centerIndex = poolCount / 2;

            for (int j = 0; j < poolCount; j++)
            {
                var inst = Instantiate(settings.template);
                inst.name = settings.template.name + "_tile_" + j;

                float xPos = templateWorldPos.x + (j - centerIndex) * width;
                Vector3 worldPos = new Vector3(xPos, templateWorldPos.y + settings.verticalOffset, templateWorldPos.z);
                inst.transform.position = worldPos;
                inst.transform.SetParent(layerRoot.transform, true);

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

    [ContextMenu("Rebuild Pools")]
    public void RebuildPools()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != null) DestroyImmediate(c.gameObject);
        }
        InitializePools();
    }

}
