
using UnityEngine;

public class DistanceCulling : MonoBehaviour
{
    [Min(1f)]
    public float visibleDistance = 45f;

    private Transform targetCamera;
    private Renderer[] renderers;
    private float checkInterval = 0.25f;
    private float nextCheckTime;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        Camera cam = Camera.main;
        if (cam != null)
            targetCamera = cam.transform;
    }

    private void Update()
    {
        if (targetCamera == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            targetCamera = cam.transform;
        }

        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        float distanceSqr =
            (transform.position - targetCamera.position).sqrMagnitude;

        bool shouldBeVisible =
            distanceSqr <= visibleDistance * visibleDistance;

        foreach (Renderer rend in renderers)
        {
            if (rend != null && rend.enabled != shouldBeVisible)
                rend.enabled = shouldBeVisible;
        }
    }
}
