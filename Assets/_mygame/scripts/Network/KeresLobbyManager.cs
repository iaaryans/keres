using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KeresLobbyManager : MonoBehaviour
{
    [Header("Screens (Canvas Groups)")]
    public CanvasGroup splashScreen;
    public CanvasGroup loaderScreen;

    [Header("Loading UI")]
    public Slider loadingSlider;

    [Header("Timings")]
    public float splashDuration = 2.5f; 
    public float fadeSpeed = 1.5f;      
    public float fakeLoadTime = 4f;     

    // Awake sabse pehle chalta hai, isse start hote hi flicker nahi hoga
    private void Awake()
    {
        if (splashScreen != null)
        {
            splashScreen.alpha = 1f;
            splashScreen.gameObject.SetActive(true);
        }
        
        if (loaderScreen != null)
        {
            loaderScreen.alpha = 0f;
            loaderScreen.gameObject.SetActive(true); 
        }

        if (loadingSlider != null) 
        {
            loadingSlider.value = 0f;
        }
    }

    private void Start()
    {
        StartCoroutine(LobbyFlowRoutine());
    }

    private IEnumerator LobbyFlowRoutine()
    {
        // 1. Splash Screen Logo dikhao
        yield return new WaitForSeconds(splashDuration);

        // 2. Smooth CrossFade (Logo fade out aur Poster fade in ek sath honge)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            
            // Mathf.Lerp dono ko ek dum smooth control karta hai
            splashScreen.alpha = Mathf.Lerp(1f, 0f, t);
            loaderScreen.alpha = Mathf.Lerp(0f, 1f, t);
            
            yield return null;
        }
        
        splashScreen.gameObject.SetActive(false);
        splashScreen.alpha = 0f;
        loaderScreen.alpha = 1f;

        // 3. Loading Bar Animation
        float loadProgress = 0f;
        while (loadProgress < 1f)
        {
            loadProgress += Time.deltaTime / fakeLoadTime;
            if (loadingSlider != null) 
            {
                loadingSlider.value = loadProgress;
            }
            yield return null;
        }

        Debug.Log("[KERES] Loading Complete! Ready for Main Menu.");
    }
}