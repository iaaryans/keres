using UnityEngine;
using UnityEngine.EventSystems;
using StarterAssets;

public class MobileTouchLook : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("PUBG Pro Sensitivity")]
    public float sensitivityX = 2.5f; 
    public float sensitivityY = 1.5f; 

    [Header("Axis Settings")]
    public bool invertY = true; 
    
    [Header("Smoothness (Buttery Feel)")]
    [Range(10f, 50f)] public float smoothing = 25f; 

    private StarterAssetsInputs starterInputs;
    private int activePointerId = -999;
    
    private Vector2 targetDelta;
    private Vector2 currentDelta;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (starterInputs == null) GetInput();
        if (activePointerId == -999) 
        {
            activePointerId = eventData.pointerId;
            targetDelta = Vector2.zero;

            // JADOO: Latest sensitivity setting PlayerPrefs se read karo
            sensitivityX = PlayerPrefs.GetFloat("SensX", 2.5f);
            sensitivityY = PlayerPrefs.GetFloat("SensY", 1.5f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;

        float scale = 1000f / Screen.height;

        float calcY = eventData.delta.y * sensitivityY * scale;
        if (invertY) calcY = -calcY; 

        targetDelta = new Vector2(
            eventData.delta.x * sensitivityX * scale,
            calcY
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            activePointerId = -999;
            targetDelta = Vector2.zero;
        }
    }

    private void Update()
    {
        if (starterInputs == null) return;

        if (activePointerId != -999)
        {
            currentDelta = Vector2.Lerp(currentDelta, targetDelta, Time.deltaTime * smoothing);
            targetDelta = Vector2.Lerp(targetDelta, Vector2.zero, Time.deltaTime * (smoothing * 0.5f));
        }
        else
        {
            currentDelta = Vector2.Lerp(currentDelta, Vector2.zero, Time.deltaTime * smoothing);
        }

        starterInputs.LookInput(currentDelta);
    }

    private void GetInput()
    {
        StarterAssetsInputs[] inputs = FindObjectsByType<StarterAssetsInputs>(FindObjectsSortMode.None);
        foreach (var input in inputs)
        {
            if (input.isActiveAndEnabled)
            {
                starterInputs = input;
                break;
            }
        }
    }
}