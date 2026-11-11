using UnityEngine;
using TMPro;

public class KeresFPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        if (fpsText != null)
        {
            float fps = 1.0f / deltaTime;
            fpsText.text = $"FPS: {Mathf.Ceil(fps)}";

            if (fps < 30) fpsText.color = Color.red;
            else if (fps < 50) fpsText.color = Color.yellow;
            else fpsText.color = Color.green;
        }
    }
}