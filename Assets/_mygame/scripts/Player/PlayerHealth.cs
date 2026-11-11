using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Photon.Pun;
using UnityEngine.UI; 

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isDead = false;

    public float CurrentHealth => Mathf.Clamp(currentHealth, 0f, maxHealth);
    public float MaxHealth => Mathf.Max(1f, maxHealth);

    [Header("Damage Feedback (Blood & Sound)")]
    public AudioSource healthAudio; 
    public AudioClip painSound; 
    public AudioClip deathSound; 
    public Image bloodScreenImage; 
    public float bloodFadeSpeed = 2f; 

    [Header("Components")]
    public Animator anim;

    private PlayerCombat combatScript;
    private Behaviour thirdPersonController;
    private Behaviour starterAssetsInputs;
    private PlayerInput playerInput;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private PhotonView rootPhotonView;
    private bool respawnScheduled;
    private CharacterController cc;
    private Coroutine bloodFadeCoroutine;

    private float lastDamageTime;
    private int lastKillerActorNumber = -1;

    private void Awake()
    {
        rootPhotonView = GetComponentInParent<PhotonView>();
        combatScript = GetComponentInChildren<PlayerCombat>(true);
        thirdPersonController = GetComponent("ThirdPersonController") as Behaviour;
        starterAssetsInputs = GetComponent("StarterAssetsInputs") as Behaviour;
        playerInput = GetComponent<PlayerInput>();
        cc = GetComponent<CharacterController>();

        if (anim == null) anim = GetComponentInChildren<Animator>(true);
        if (healthAudio == null) healthAudio = GetComponent<AudioSource>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);
        
        if (cc != null)
        {
            foreach (Collider col in ragdollColliders)
            {
                if (col != cc) Physics.IgnoreCollision(cc, col, true);
            }
        }
    }

    private void OnEnable()
    {
        isDead = false;
        respawnScheduled = false;
        currentHealth = maxHealth;
        lastKillerActorNumber = -1;
        SetRagdollState(false); 

        if (bloodScreenImage != null)
        {
            Color c = bloodScreenImage.color;
            c.a = 0f;
            bloodScreenImage.color = c;
        }

        if (combatScript != null && (rootPhotonView == null || rootPhotonView.IsMine)) combatScript.enabled = true;
        if (thirdPersonController != null && (rootPhotonView == null || rootPhotonView.IsMine)) thirdPersonController.enabled = true;
        if (starterAssetsInputs != null && (rootPhotonView == null || rootPhotonView.IsMine)) starterAssetsInputs.enabled = true;
        if (playerInput != null && (rootPhotonView == null || rootPhotonView.IsMine)) playerInput.enabled = true;
        
        if (anim != null) anim.enabled = true;
        if (cc != null && (rootPhotonView == null || rootPhotonView.IsMine)) cc.enabled = true;
    }

    public void TakeDamage(float damageAmount, int killerActorID = -1)
    {
        if (isDead || damageAmount <= 0f) return;

        if (Time.time - lastDamageTime < 0.05f) return; 
        lastDamageTime = Time.time;

        currentHealth = Mathf.Clamp(currentHealth - damageAmount, 0f, maxHealth);
        
        if (killerActorID != -1) lastKillerActorNumber = killerActorID;

        if (rootPhotonView != null && rootPhotonView.IsMine)
        {
            PlayPainFeedback();
        }

        if (currentHealth <= 0f) Die();
    }

    private void PlayPainFeedback()
    {
        if (healthAudio != null && painSound != null) healthAudio.PlayOneShot(painSound);

        if (bloodScreenImage != null)
        {
            if (bloodFadeCoroutine != null) StopCoroutine(bloodFadeCoroutine);
            bloodFadeCoroutine = StartCoroutine(FadeBloodScreen());
        }
    }

    // MISSING METHOD ADDED HERE: Ye raha FadeBloodScreen jo gayab tha!
    private IEnumerator FadeBloodScreen()
    {
        Color c = bloodScreenImage.color;
        c.a = 0.8f; 
        bloodScreenImage.color = c;

        while (c.a > 0.01f)
        {
            c.a = Mathf.Lerp(c.a, 0f, Time.deltaTime * bloodFadeSpeed);
            bloodScreenImage.color = c;
            yield return null;
        }
        
        c.a = 0f; 
        bloodScreenImage.color = c;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (healthAudio != null && deathSound != null) healthAudio.PlayOneShot(deathSound);

        if (combatScript != null) combatScript.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;
        if (starterAssetsInputs != null) starterAssetsInputs.enabled = false;
        if (playerInput != null) playerInput.enabled = false;
        if (anim != null) anim.enabled = false;
        if (cc != null) cc.enabled = false;

        SetRagdollState(true); 

        if (rootPhotonView != null && rootPhotonView.IsMine && KERES_MatchManager.Instance != null && KERES_MatchManager.Instance.photonView != null && lastKillerActorNumber != -1)
        {
            KERES_MatchManager.Instance.photonView.RPC("RPC_RegisterKill", RpcTarget.All, lastKillerActorNumber, rootPhotonView.Owner.ActorNumber);
        }

        if (rootPhotonView != null && rootPhotonView.IsMine && !respawnScheduled)
        {
            respawnScheduled = true;
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        KERESNetworkManager manager = FindFirstObjectByType<KERESNetworkManager>();
        if (manager == null) yield break;

        GameObject networkRoot = rootPhotonView != null ? rootPhotonView.gameObject : transform.root.gameObject;
        manager.RespawnLocalPlayer(networkRoot);
    }

    private void SetRagdollState(bool state)
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == null || rb.gameObject == gameObject) continue;
            rb.isKinematic = !state;
        }
        foreach (Collider col in ragdollColliders)
        {
            if (col == null || col.gameObject == gameObject || col is CharacterController) continue;
            col.isTrigger = !state; 
            col.enabled = true; 
        }
    }
}