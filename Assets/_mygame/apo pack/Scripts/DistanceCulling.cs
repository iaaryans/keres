using UnityEngine;

public class DistanceCulling : MonoBehaviour
{
    [Min(1f)]
    public float visibleDistance = 80f; // Thoda distance badha diya taaki achanak se na aayen

    private Transform targetCamera;
    private Renderer[] renderers;
    private float checkInterval = 0.5f; // Interval thoda aur bada kar diya (har aadhe second mein check)
    private float nextCheckTime;

    private void Awake()
    {
        // Awake mein sirf ek baar renderers nikal rahe hain, har baar nahi.
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

        float distanceSqr = (transform.position - targetCamera.position).sqrMagnitude;
        bool shouldBeVisible = distanceSqr <= visibleDistance * visibleDistance;

        // JADOO FIX: Har renderer ko alag se enable/disable mat karo, poore GameObject ko kar do
        if (gameObject.activeSelf != shouldBeVisible)
        {
            gameObject.SetActive(shouldBeVisible);
        }
    }
}