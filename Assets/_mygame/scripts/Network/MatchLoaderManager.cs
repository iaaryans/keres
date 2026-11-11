using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class MatchLoader : MonoBehaviour
{
    public Slider loadingSlider;
    public float fakeLoadTime = 3f; // 3 second ka cinematic load

    private void Start()
    {
        if (loadingSlider != null) loadingSlider.value = 0f;
        StartCoroutine(LoadMatchRoutine());
    }

    private IEnumerator LoadMatchRoutine()
    {
        // 1. Pehle loading bar ko smooth bharo
        float loadProgress = 0f;
        while (loadProgress < 1f)
        {
            loadProgress += Time.deltaTime / fakeLoadTime;
            if (loadingSlider != null) loadingSlider.value = loadProgress;
            yield return null;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SampleScene"); 
        }
    }
}