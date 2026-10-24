using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class KERESNetworkPlayerSetup : MonoBehaviour
{
    [SerializeField] private bool logSetup = true;
    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        
        // JADOO: Setup ko ek frame delay karo taaki sab kuch (network state, objects) theek se load ho jaye.
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        yield return null; 

        if (photonView == null) yield break;

        ApplyOwnershipSetup();
    }

    private void ApplyOwnershipSetup()
    {
        bool isMine = photonView.IsMine;

        // Camera aur UI Setup
        SetChildActive("MainCamera", isMine);
        SetChildActive("PlayerFollowCamera", isMine);
        SetChildActive("PlayerAimCamera", isMine);
        SetChildActive("UI_EventSystem", isMine);
        SetChildActive("Canvas", isMine); 

        if (!isMine)
        {
            // JADOO: Scripts ko Destroy karne ki jagah sirf Disable kiya hai taaki RPC aur VFX theek se kaam karein
            var combat = GetComponentInChildren<PlayerCombat>(true);
            if (combat != null) combat.enabled = false;

            var playerInput = GetComponentInChildren<PlayerInput>(true);
            if (playerInput != null) playerInput.enabled = false;

            var starterInputs = GetComponentInChildren<StarterAssets.StarterAssetsInputs>(true);
            if (starterInputs != null) starterInputs.enabled = false;
            
            var tpc = GetComponentInChildren<StarterAssets.ThirdPersonController>(true);
            if (tpc != null) tpc.enabled = false;
            
            CharacterController cc = GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            foreach (Camera cam in GetComponentsInChildren<Camera>(true)) cam.enabled = false;
            foreach (AudioListener listener in GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;

            // ANIMATION EVENT ERROR KAA FIX (Dummy Receiver)
            Animator anim = GetComponentInChildren<Animator>(true);
            if(anim != null)
            {
                if(anim.gameObject.GetComponent<DummyAnimationEventReceiver>() == null)
                {
                    anim.gameObject.AddComponent<DummyAnimationEventReceiver>();
                }
            }
        }
        else 
        {
            // LOCAL PLAYER SETUP
            CharacterController cc = GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = true;

            EnforceSingleEventSystem(true);

            // JADOO 2: Multiple Audio Listeners ka fix, sirf local ka ON rahega
            AudioListener[] allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach(var l in allListeners) l.enabled = false;
            AudioListener myListener = GetComponentInChildren<AudioListener>(true);
            if (myListener != null) myListener.enabled = true;

            MobileHUDCustomizer customizer = FindFirstObjectByType<MobileHUDCustomizer>();
            if (customizer != null) customizer.RefreshInputBindings();

            KeresDevelopmentHUD hud = FindFirstObjectByType<KeresDevelopmentHUD>();
            if (hud != null) hud.RefreshPlayerReferences();
        }

        if (logSetup) Debug.Log($"KERES: {(isMine ? "LOCAL" : "REMOTE")} player setup applied for actor {photonView.OwnerActorNr}");
    }

    private void EnforceSingleEventSystem(bool isMine)
    {
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        EventSystem preferred = null;

        if (isMine)
        {
            Transform localEventSystem = FindDeepChild(transform, "UI_EventSystem");
            if (localEventSystem != null) preferred = localEventSystem.GetComponentInChildren<EventSystem>(true);
        }

        if (preferred == null)
        {
            foreach (EventSystem system in systems)
            {
                if (system != null && system.isActiveAndEnabled) { preferred = system; break; }
            }
        }

        foreach (EventSystem system in systems)
        {
            if (system != null) system.enabled = (system == preferred);
        }
    }

    private void SetChildActive(string childName, bool active)
    {
        Transform child = FindDeepChild(transform, childName);
        if (child != null) child.gameObject.SetActive(active);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}

public class DummyAnimationEventReceiver : MonoBehaviour 
{
    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }
}