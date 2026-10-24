using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Stats")]
    public float fireRate = 10f; 
    public int magCapacity = 30;
    public float reloadTime = 2f;
    [Min(0)] public int reserveAmmo = 120;

    [Header("Advanced Camera Sync System")]
    public Camera mainCamera; 
    public LayerMask aimMask; 
    public float aimRotationOffset = 40f; 
    public float fireRotationOffset = 15f; 
    public float rotationSpeed = 15f; 
    public Transform spineBone; 
    public Vector3 spineBendAxis = new Vector3(1, 0, 0); 
    public float spineOffset = 0f;

    [Header("Weapon Setup")]
    public GameObject rifleIdle;
    public GameObject rifleAim;
    public Transform firePointAim;
    public Transform weaponAimRoot;
    public Vector3 weaponAimRotationOffset = Vector3.zero;
    [Min(0f)] public float weaponAimSmoothness = 25f;
    private Quaternion weaponAimRootInitialLocalRotation;
    private bool hasWeaponAimRootInitialRotation;

    [Header("VFX & Audio (Global Combat)")]
    public ParticleSystem muzzleFlash; 
    public AudioSource weaponAudio;        
    public AudioClip gunshotSound; 
    public AudioClip reloadSound; 
    
    [Tooltip("Ye wo blood particle hai jo dushman ko goli lagne par niklega")]
    public GameObject bloodHitEffectPrefab; 

    [Header("Tracer & Casing")]
    public Transform ejectionPort;        
    public GameObject bulletCasingPrefab; 
    public GameObject bulletTracerPrefab; 

    [Header("Network Hitscan Combat")]
    [Min(0f)] public float damagePerShot = 25f;
    [Min(1f)] public float hitRange = 200f;

    [Header("Recoil System")]
    public Transform recoilPivot; 
    public float recoilKickX = -2f; 
    public float recoilKickY = 1f;  
    public float recoilSnappiness = 20f; 
    public float recoilReturnSpeed = 5f; 
    
    private Vector3 currentRecoil;
    private Vector3 targetRecoil;
    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private bool isAiming = false;
    private bool isFiring = false;
    private float reloadTimer;

    public int CurrentAmmo => currentAmmo;
    public int MagazineCapacity => magCapacity;
    public bool IsReloading => isReloading;
    public bool IsAiming => isAiming;
    public bool IsFiring => isFiring;
    public float FireRate => fireRate;
    public int ReserveAmmo => reserveAmmo;
    public float ReloadProgress => isReloading ? 1f - Mathf.Clamp01(reloadTimer / Mathf.Max(0.01f, reloadTime)) : 0f;
    public float ReloadSecondsRemaining => isReloading ? Mathf.Max(0f, reloadTimer) : 0f;

    private Vector3 virtualAimPoint;
    private PhotonView rootPhotonView;

    [Header("References")]
    public Animator anim;
    public GameObject aimCamera;
    public InputActionReference fireActionRef;
    public InputActionReference reloadActionRef;
    public InputActionReference aimActionRef;
    public InputActionReference moveActionRef;

    private void Awake()
    {
        currentAmmo = magCapacity;
        rootPhotonView = GetComponentInParent<PhotonView>();
        
        if (rifleIdle != null) rifleIdle.SetActive(true);
        if (rifleAim != null) rifleAim.SetActive(false);
        if (mainCamera == null) mainCamera = Camera.main;
        if (weaponAudio == null) weaponAudio = GetComponent<AudioSource>();

        // JADOO 3: Gunshot Audio ko lamba sunai dene ke liye spatial blend adjust karna
        if (weaponAudio != null)
        {
            weaponAudio.spatialBlend = 0.5f; // 0 (2D - Har jagah sunai dega), 1 (3D - Sirf pass me). 0.5 balanced hai.
            weaponAudio.maxDistance = 500f; // Map ke end tak sound jayegi
            weaponAudio.rolloffMode = AudioRolloffMode.Linear;
        }

        if (weaponAimRoot != null)
        {
            weaponAimRootInitialLocalRotation = weaponAimRoot.localRotation;
            hasWeaponAimRootInitialRotation = true;
        }
    }

    private void OnEnable() 
    { 
        if (rootPhotonView != null && !rootPhotonView.IsMine) return;
        if(fireActionRef != null && fireActionRef.action != null) fireActionRef.action.Enable();
        if(reloadActionRef != null && reloadActionRef.action != null) reloadActionRef.action.Enable();
        if(aimActionRef != null && aimActionRef.action != null) aimActionRef.action.Enable();
        if(moveActionRef != null && moveActionRef.action != null) moveActionRef.action.Enable();
    }
    
    private void OnDisable() 
    { 
        if (rootPhotonView != null && !rootPhotonView.IsMine) return;
        if(fireActionRef != null && fireActionRef.action != null) fireActionRef.action.Disable();
        if(reloadActionRef != null && reloadActionRef.action != null) reloadActionRef.action.Disable();
        if(aimActionRef != null && aimActionRef.action != null) aimActionRef.action.Disable();
        if(moveActionRef != null && moveActionRef.action != null) moveActionRef.action.Disable();
    }

    private void Update()
    {
        if (rootPhotonView != null && !rootPhotonView.IsMine) return;

        if (isReloading) 
        {
            isAiming = false; isFiring = false;
            if (anim != null) { anim.SetBool("isAiming", false); anim.SetBool("isFiring", false); }
            if (aimCamera != null) aimCamera.SetActive(false);
            if (rifleIdle != null) rifleIdle.SetActive(true);
            if (rifleAim != null) rifleAim.SetActive(false);
            if (muzzleFlash != null && muzzleFlash.isPlaying) muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return; 
        }

        bool isReloadPressed = (reloadActionRef != null && reloadActionRef.action != null && reloadActionRef.action.WasPressedThisFrame());

        if (currentAmmo <= 0 || isReloadPressed)
        {
            if (currentAmmo < magCapacity && reserveAmmo > 0) RequestReload();
            if (currentAmmo <= 0)
            {
                isFiring = false;
                if (anim != null) anim.SetBool("isFiring", false);
                if (muzzleFlash != null && muzzleFlash.isPlaying) muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }
        }

        UpdateRecoil();

        bool actionAimHeld = aimActionRef != null && aimActionRef.action != null && aimActionRef.action.IsPressed();
        bool actionFireHeld = fireActionRef != null && fireActionRef.action != null && fireActionRef.action.IsPressed();

        isAiming = actionAimHeld;
        isFiring = actionFireHeld && currentAmmo > 0;
        
        if (anim != null) { anim.SetBool("isAiming", isAiming); anim.SetBool("isFiring", isFiring); }
        if (aimCamera != null) aimCamera.SetActive(isAiming);

        if (isAiming || isFiring)
        {
            if (rifleIdle != null) rifleIdle.SetActive(false);
            if (rifleAim != null) rifleAim.SetActive(true);
            
            if (mainCamera != null)
            {
                float cameraYaw = mainCamera.transform.eulerAngles.y;
                float activeOffset = isFiring ? fireRotationOffset : aimRotationOffset;
                Quaternion targetRotation = Quaternion.Euler(0f, cameraYaw + activeOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                CalculateAimPoint(); 
            }
        }
        else
        {
            if (rifleIdle != null) rifleIdle.SetActive(true);
            if (rifleAim != null) rifleAim.SetActive(false);
        }

        if (isFiring && Time.time >= nextFireTime && !isReloading) Shoot();
        else if (!isFiring && muzzleFlash != null && muzzleFlash.isPlaying) muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void LateUpdate()
    {
        if (rootPhotonView != null && !rootPhotonView.IsMine) return;
        if (isReloading || mainCamera == null) return;

        bool combatPoseActive = isAiming || isFiring;
        if (!combatPoseActive)
        {
            if (weaponAimRoot != null && hasWeaponAimRootInitialRotation)
                weaponAimRoot.localRotation = Quaternion.Slerp(weaponAimRoot.localRotation, weaponAimRootInitialLocalRotation, Mathf.Clamp01(weaponAimSmoothness * Time.deltaTime));
            return;
        }

        if (spineBone != null)
        {
            float cameraPitch = mainCamera.transform.eulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
            cameraPitch = Mathf.Clamp(cameraPitch, -60f, 70f);
            Vector3 bendAxis = spineBendAxis.sqrMagnitude > 0.0001f ? spineBendAxis.normalized : Vector3.right;
            spineBone.localRotation = spineBone.localRotation * Quaternion.AngleAxis(cameraPitch + spineOffset, bendAxis);
        }

        if (weaponAimRoot != null)
        {
            Vector3 aimDirection = virtualAimPoint - weaponAimRoot.position;
            if (aimDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetWeaponRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up) * Quaternion.Euler(weaponAimRotationOffset);
                weaponAimRoot.rotation = Quaternion.Slerp(weaponAimRoot.rotation, targetWeaponRotation, Mathf.Clamp01(weaponAimSmoothness * Time.deltaTime));
            }
        }
    }

    private void CalculateAimPoint()
    {
        if (mainCamera == null) return;
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimMask)) virtualAimPoint = hit.point;
        else virtualAimPoint = ray.GetPoint(1000f); 
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (rootPhotonView != null && !rootPhotonView.IsMine) return;
        if (anim != null && (isAiming || isFiring))
        {
            anim.SetLookAtWeight(1f, 1f, 1f, 1f, 0.5f);
            anim.SetLookAtPosition(virtualAimPoint);
        }
        else if (anim != null) anim.SetLookAtWeight(0f);
    }

    private void UpdateRecoil()
    {
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
        currentRecoil = Vector3.Slerp(currentRecoil, targetRecoil, recoilSnappiness * Time.deltaTime);
        if (recoilPivot != null) recoilPivot.localRotation = Quaternion.Euler(currentRecoil);
    }

    private void Shoot()
        {
            currentAmmo = Mathf.Max(0, currentAmmo - 1);
            nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
            
            ApplyNetworkHitscanDamage();

            if (recoilPivot != null) targetRecoil += new Vector3(recoilKickX, Random.Range(-recoilKickY, recoilKickY), 0f);

            // Apni screen par effect chalao
            PlayShootEffects(virtualAimPoint);

            // Firing ki khabar poore server (dusre players) ko do!
            if (rootPhotonView != null)
            {
                KERESNetworkCombat netCombat = rootPhotonView.GetComponent<KERESNetworkCombat>();
                if (netCombat != null)
                {
                    // JADOO: Target 'Others' rakha hai, yani baaki sabki screen par teri gun se aag niklegi
                    netCombat.photonView.RPC("RPC_PlayShootEffects", RpcTarget.Others, virtualAimPoint);
                }
            }
        }

        public void PlayShootEffects(Vector3 hitPoint)
            {
                if (weaponAudio != null && gunshotSound != null) weaponAudio.PlayOneShot(gunshotSound);
                if (muzzleFlash != null) 
                { 
                    muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 
                    muzzleFlash.Play(true); 
                }

                // Fix: Start point hamesha 'firePointAim' se hona chahiye taaki gun se nikalta hua lage
                Vector3 tracerStart = firePointAim != null ? firePointAim.position : (muzzleFlash != null ? muzzleFlash.transform.position : transform.position);
                
                if (bulletTracerPrefab != null) 
                {
                    StartCoroutine(SpawnTracer(tracerStart, hitPoint));
                }

                if (firePointAim != null && bulletCasingPrefab != null && ejectionPort != null)
                {
                    GameObject casing = Instantiate(bulletCasingPrefab, ejectionPort.position, ejectionPort.rotation);
                    Rigidbody rb = casing.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 ejectDirection = ejectionPort.right + ejectionPort.up * 0.5f; 
                        rb.AddForce(ejectDirection * Random.Range(3f, 6f), ForceMode.Impulse);
                        rb.AddTorque(new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f)), ForceMode.Impulse);
                    }
                    Destroy(casing, 5f); 
                }
            }

    // Enemy ko hit karne par wahan se blood particle nikalne ka logic
    public void PlayHitParticle(Vector3 position, Vector3 normal)
    {
        if (bloodHitEffectPrefab != null)
        {
            GameObject blood = Instantiate(bloodHitEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(blood, 2f); // 2 second baad clean up kar do
        }
    }

    private void ApplyNetworkHitscanDamage()
    {
        if (!PhotonNetwork.InRoom || mainCamera == null) return;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        RaycastHit[] hits = Physics.RaycastAll(ray, hitRange, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.root == transform.root) continue;
            
            PhotonView targetView = hit.collider.GetComponentInParent<PhotonView>();
            KERESNetworkCombat targetCombat = hit.collider.GetComponentInParent<KERESNetworkCombat>();

            if (targetView != null && targetCombat != null && !targetView.IsMine)
            {
                // 1. Dushman ko damage do
                targetCombat.photonView.RPC("RPC_RequestDamage", RpcTarget.MasterClient, damagePerShot);
                
                // 2. Apni screen par blood particle dikhao
                PlayHitParticle(hit.point, hit.normal);
                
                // 3. Network me sabko batao ki yaha par blood nikla hai
                targetCombat.photonView.RPC("RPC_PlayHitEffect", RpcTarget.Others, hit.point, hit.normal);

                return; 
            }
        }
    }

    private IEnumerator SpawnTracer(Vector3 startPoint, Vector3 hitPoint)
    {
        GameObject tracerObject = bulletTracerPrefab != null ? Instantiate(bulletTracerPrefab, startPoint, Quaternion.identity) : new GameObject("KERES_BulletTracer");
        tracerObject.name = "KERES_BulletTracer";
        LineRenderer line = tracerObject.GetComponentInChildren<LineRenderer>();
        if (line == null)
        {
            line = tracerObject.AddComponent<LineRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
            line.startWidth = 0.035f; line.endWidth = 0.008f;
            line.startColor = new Color(1f, 0.75f, 0.2f, 1f); line.endColor = new Color(1f, 0.2f, 0.05f, 0.8f);
        }
        line.enabled = true; line.useWorldSpace = true; line.positionCount = 2;
        line.SetPosition(0, startPoint); line.SetPosition(1, hitPoint);
        yield return new WaitForSeconds(0.07f);
        if (tracerObject != null) Destroy(tracerObject);
    }

    public void RequestReload()
    {
        if (!isReloading && currentAmmo < magCapacity && reserveAmmo > 0) StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        if (isReloading || currentAmmo >= magCapacity || reserveAmmo <= 0) yield break;
        isReloading = true;
        reloadTimer = Mathf.Max(0.01f, reloadTime);

        if (anim != null) { anim.SetBool("isFiring", false); anim.SetBool("isAiming", false); anim.SetTrigger("ReloadTrigger"); }
        isFiring = false; isAiming = false;
        if (aimCamera != null) aimCamera.SetActive(false);
        if (weaponAudio != null && reloadSound != null) weaponAudio.PlayOneShot(reloadSound);

        while (reloadTimer > 0f) { reloadTimer -= Time.deltaTime; yield return null; }

        int roundsNeeded = Mathf.Max(0, magCapacity - currentAmmo);
        int roundsToLoad = Mathf.Min(roundsNeeded, reserveAmmo);
        currentAmmo += roundsToLoad; reserveAmmo -= roundsToLoad;
        reloadTimer = 0f; isReloading = false;
    }
}