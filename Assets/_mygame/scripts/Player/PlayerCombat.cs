using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float damage = 25f;
    public float fireRate = 10f; 
    public int magCapacity = 30;
    public float reloadTime = 2f;

    [Header("Crosshair Tracking System")]
    public Camera mainCamera; 
    public Transform aimTarget; 
    public LayerMask aimMask; 
    public float bodyRotationOffset = 40f; 

    [Header("Dual Weapon Setup")]
    public GameObject rifleIdle; 
    public GameObject rifleAim;  
    public Transform firePointIdle; 
    public Transform firePointAim;  

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

    [Header("References")]
    public Animator anim;
    public GameObject aimCamera;

    [Header("Input Actions")]
    public InputAction fireAction;
    public InputAction reloadAction;
    public InputAction aimAction;

    private void Awake()
    {
        currentAmmo = magCapacity;
        if (fireAction.bindings.Count == 0) fireAction.AddBinding("<Mouse>/leftButton");
        if (reloadAction.bindings.Count == 0) reloadAction.AddBinding("<Keyboard>/r");
        if (aimAction.bindings.Count == 0) aimAction.AddBinding("<Mouse>/rightButton");
            
        rifleIdle.SetActive(true);
        rifleAim.SetActive(false);
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void OnEnable() { fireAction.Enable(); reloadAction.Enable(); aimAction.Enable(); }
    private void OnDisable() { fireAction.Disable(); reloadAction.Disable(); aimAction.Disable(); }

    private void Update()
    {
        if (isReloading) return;

        if (currentAmmo <= 0 || reloadAction.WasPressedThisFrame())
        {
            if (currentAmmo < magCapacity) StartCoroutine(ReloadRoutine());
            return;
        }

        // 1. RECOIL MATH
        targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
        currentRecoil = Vector3.Slerp(currentRecoil, targetRecoil, recoilSnappiness * Time.deltaTime);
        if (recoilPivot != null) recoilPivot.localRotation = Quaternion.Euler(currentRecoil);

        // 2. EXACT CROSSHAIR POINT TRACKING
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);
        Vector3 exactTargetPoint;
        
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, aimMask))
        {
            exactTargetPoint = hit.point;
        }
        else
        {
            exactTargetPoint = ray.GetPoint(100f); 
        }
        
        aimTarget.position = Vector3.Lerp(aimTarget.position, exactTargetPoint, Time.deltaTime * 40f);

        // 3. INPUTS
        isAiming = aimAction.IsPressed();
        bool isFiring = fireAction.IsPressed() && currentAmmo > 0;
        
        anim.SetBool("isAiming", isAiming);
        anim.SetBool("isFiring", isFiring);
        aimCamera.SetActive(isAiming);

        // 4. PHYSICALLY FACE THE TARGET
        if (isAiming || isFiring)
        {
            rifleIdle.SetActive(false);
            rifleAim.SetActive(true);
            
            Vector3 directionToTarget = aimTarget.position - transform.position;
            directionToTarget.y = 0f; 
            
            if (directionToTarget.sqrMagnitude > 0.05f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
                Quaternion finalRotation = lookRotation * Quaternion.Euler(0, bodyRotationOffset, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * 25f);
            }
        }
        else
        {
            rifleIdle.SetActive(true);
            rifleAim.SetActive(false);
        }

        if (isFiring && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    // 5. IK SE SPINE AUR GARDAN MODNA
    private void OnAnimatorIK(int layerIndex)
    {
        if (anim != null)
        {
            if (isAiming || anim.GetBool("isFiring"))
            {
                anim.SetLookAtWeight(1f, 1f, 1f, 1f, 0.5f);
                anim.SetLookAtPosition(aimTarget.position);
            }
            else
            {
                anim.SetLookAtWeight(0f);
            }
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        nextFireTime = Time.time + (1f / fireRate);

        if (recoilPivot != null)
        {
            targetRecoil += new Vector3(recoilKickX, Random.Range(-recoilKickY, recoilKickY), 0f);
        }

        Transform activeFirePoint = (isAiming || anim.GetBool("isFiring")) ? firePointAim : firePointIdle;
        Vector3 shootDirection = (aimTarget.position - activeFirePoint.position).normalized;

        if (Physics.Raycast(activeFirePoint.position, shootDirection, out RaycastHit hit, 999f, aimMask))
        {
            Debug.Log($"[Combat] Hit: {hit.collider.name}");
        }
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        anim.SetBool("isFiring", false); 
        anim.SetTrigger("ReloadTrigger");
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = magCapacity;
        isReloading = false;
    }
}