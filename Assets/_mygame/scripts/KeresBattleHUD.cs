using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class KeresBattleHUD : MonoBehaviour
{
    [Header("Scoreboard")]
    public TextMeshProUGUI blueScoreText;
    public TextMeshProUGUI redScoreText;

    [Header("Health Bar")]
    public Slider healthBar;
    public Image healthBarFill;
    public Color highHealthColor = new Color(0.1f, 1f, 0.2f);
    public Color lowHealthColor = new Color(1f, 0.1f, 0.1f);

    [Header("Ammo & Reload UI")]
    public TextMeshProUGUI ammoText;

    [Header("Kill Feed")]
    public Transform killFeedPanel;
    public GameObject killFeedTextPrefab; 

    [Header("Respawn")]
    public GameObject respawnPanel;
    public TextMeshProUGUI respawnText;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public GameObject openSettingsBtn;
    public GameObject closeSettingsBtn;
    public Slider sensXSlider;
    public Slider sensYSlider;
    public GameObject editHudBtn;

    [Header("Live Player References")]
    public PlayerHealth localHealth;
    public PlayerCombat localCombat;
    
    private MobileHUDCustomizer hudCustomizer;

    private void Start()
    {
        if (respawnPanel != null) respawnPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        SetupButton(openSettingsBtn, ToggleSettings);
        SetupButton(closeSettingsBtn, ToggleSettings);

        if (sensXSlider != null) 
        {
            sensXSlider.value = PlayerPrefs.GetFloat("SensX", 2.5f);
            sensXSlider.onValueChanged.AddListener((val) => { PlayerPrefs.SetFloat("SensX", val); PlayerPrefs.Save(); });
        }
        if (sensYSlider != null) 
        {
            sensYSlider.value = PlayerPrefs.GetFloat("SensY", 1.5f);
            sensYSlider.onValueChanged.AddListener((val) => { PlayerPrefs.SetFloat("SensY", val); PlayerPrefs.Save(); });
        }

        hudCustomizer = FindFirstObjectByType<MobileHUDCustomizer>();
        if (editHudBtn != null)
        {
            SetupButton(editHudBtn, () => { 
                if (hudCustomizer != null) hudCustomizer.StartEditing(); 
                if (settingsPanel != null) settingsPanel.SetActive(false); 
            });
        }

        InvokeRepeating(nameof(FindLocalPlayer), 0.5f, 0.5f); 
    }

    private void SetupButton(GameObject obj, UnityEngine.Events.UnityAction action)
    {
        if (obj == null) return;
        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(action);
    }

    private void ToggleSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void FindLocalPlayer()
    {
        if (localHealth != null && localCombat != null) return;
        
        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        foreach (PhotonView view in views)
        {
            if (view.IsMine)
            {
                if (localHealth == null) localHealth = view.GetComponentInChildren<PlayerHealth>(true);
                if (localCombat == null) localCombat = view.GetComponentInChildren<PlayerCombat>(true);
                if (localHealth != null && localCombat != null) break;
            }
        }
    }

    private void Update()
    {
        // Health Bar Update
        if (localHealth != null && healthBar != null)
        {
            float targetValue = localHealth.CurrentHealth / localHealth.MaxHealth;
            healthBar.value = Mathf.Lerp(healthBar.value, targetValue, Time.deltaTime * 15f);

            if (healthBarFill != null)
            {
                healthBarFill.color = Color.Lerp(lowHealthColor, highHealthColor, healthBar.value);
            }
        }

        // Ammo Text Update
        if (localCombat != null && ammoText != null)
        {
            if (localCombat.IsReloading)
            {
                ammoText.text = "<color=red>RELOADING...</color>";
            }
            else
            {
                ammoText.text = $"{localCombat.CurrentAmmo} / {localCombat.ReserveAmmo}";
            }
        }
    }

    public void UpdateScore(int blue, int red)
    {
        if (blueScoreText != null) blueScoreText.text = blue.ToString();
        if (redScoreText != null) redScoreText.text = red.ToString();
    }

    public void ShowKill(string killer, string victim)
    {
        if (killFeedPanel == null || killFeedTextPrefab == null) return;

        GameObject feed = Instantiate(killFeedTextPrefab, killFeedPanel);
        TextMeshProUGUI text = feed.GetComponent<TextMeshProUGUI>();
        text.text = $"{killer}  <color=yellow>-></color>  {victim}"; 
        Destroy(feed, 4f); 
    }

    public void StartRespawnCountdown(float time)
    {
        StartCoroutine(RespawnRoutine(time));
    }

    private IEnumerator RespawnRoutine(float time)
    {
        if (respawnPanel != null) respawnPanel.SetActive(true);
        while (time > 0)
        {
            if (respawnText != null) respawnText.text = $"RESPAWNING IN {Mathf.CeilToInt(time)}...";
            time -= Time.deltaTime;
            yield return null;
        }
        if (respawnPanel != null) respawnPanel.SetActive(false);
    }
}