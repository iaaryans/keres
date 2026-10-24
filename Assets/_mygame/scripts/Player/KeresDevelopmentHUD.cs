using System;
using System.Reflection;
using UnityEngine;
using Photon.Pun;

public class KeresDevelopmentHUD : MonoBehaviour
{
    [Header("Live References")]
    public PlayerCombat playerCombat;
    [Tooltip("Assigned automatically from the local player when multiplayer is active.")]
    public MonoBehaviour playerHealth;
    [Min(0)] public int testHealth = 100;

    [Header("HUD Settings")]
    public bool showCrosshair = true;
    public bool showFps = true;
    public bool showDevelopmentLabel = true;
    public string networkStatus = "LOCAL";
    public Color textColor = Color.white;
    public Color accentColor = new Color(0.25f, 0.95f, 0.78f, 1f);
    public Color panelColor = new Color(0.025f, 0.045f, 0.06f, 0.84f);
    public Color healthColor = new Color(0.25f, 0.9f, 0.48f, 1f);
    public Color lowHealthColor = new Color(1f, 0.25f, 0.22f, 1f);

    private GUIStyle panelStyle, titleStyle, smallStyle, valueStyle, accentStyle;
    private Texture2D panelTexture, whiteTexture;
    private int cachedWidth = -1, cachedHeight = -1;
    private float smoothedFps = 60f;
    private float referenceRefreshTimer;

    // NAYA: Sensitivity Variables
    private bool showSettings = false;
    private float sensX = 2.5f;
    private float sensY = 1.5f;

    private void Awake()
    {
        // Load settings
        sensX = PlayerPrefs.GetFloat("SensX", 2.5f);
        sensY = PlayerPrefs.GetFloat("SensY", 1.5f);
    }

    private void Update()
    {
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        smoothedFps = Mathf.Lerp(smoothedFps, 1f / dt, 1f - Mathf.Exp(-6f * dt));

        referenceRefreshTimer -= Time.unscaledDeltaTime;
        if (referenceRefreshTimer <= 0f)
        {
            referenceRefreshTimer = 0.2f;
            BindPlayerReferences();
        }

        if (PhotonNetwork.InRoom) networkStatus = "ONLINE / " + PhotonNetwork.CurrentRoom.Name;
        else if (PhotonNetwork.IsConnected) networkStatus = "CONNECTED / WAITING";
        else networkStatus = "OFFLINE / LOCAL";
    }

    public void RefreshPlayerReferences() { BindPlayerReferences(); }

    private void BindPlayerReferences()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
            foreach (PhotonView view in views)
            {
                if (view == null || !view.IsMine) continue;
                PlayerCombat ownedCombat = view.GetComponentInChildren<PlayerCombat>(true);
                if (ownedCombat != null)
                {
                    playerCombat = ownedCombat;
                    playerHealth = FindHealthComponent(view.gameObject);
                    return;
                }
            }
            playerCombat = null; playerHealth = null; return;
        }

        if (playerCombat == null) playerCombat = FindFirstObjectByType<PlayerCombat>();
        if (playerHealth == null && playerCombat != null) playerHealth = FindHealthComponent(playerCombat.transform.root.gameObject);
    }

    private MonoBehaviour FindHealthComponent(GameObject root)
    {
        if (root == null) return null;
        MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour component in components)
        {
            if (component != null && component.GetType().Name == "PlayerHealth") return component;
        }
        return null;
    }

    private void OnGUI()
    {
        if (Screen.width < 1 || Screen.height < 1) return;
        if (cachedWidth != Screen.width || cachedHeight != Screen.height || panelStyle == null) BuildStyles();

        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1600f, Screen.height / 900f), 0.55f, 1.2f);
        float margin = Mathf.Clamp(16f * scale, 8f, 20f);
        float panelH = 78f * scale;
        float healthW = Mathf.Min(250f * scale, Screen.width * 0.42f);
        float ammoW = Mathf.Min(285f * scale, Screen.width * 0.46f);

        if (showDevelopmentLabel)
            GUI.Label(new Rect(margin, margin * 0.7f, Mathf.Min(390f * scale, Screen.width * 0.55f), 28f * scale), "KERES  /  DEVELOPMENT BUILD", titleStyle);

        if (showFps)
        {
            float statusW = Mathf.Min(300f * scale, Screen.width * 0.48f);
            GUI.Label(new Rect(Screen.width - statusW - margin, margin * 0.7f, statusW, 45f * scale), $"FPS  {Mathf.RoundToInt(smoothedFps)}\nNET  {networkStatus}", smallStyle);
        }

        // SETTINGS BUTTON & PANEL
        Rect setBtnRect = new Rect(margin, margin * 0.7f + 35f * scale, 120f * scale, 30f * scale);
        if (GUI.Button(setBtnRect, showSettings ? "CLOSE SETTINGS" : "SETTINGS"))
        {
            showSettings = !showSettings;
        }

        if (showSettings)
        {
            float settingsW = 300f * scale;
            Rect panelRect = new Rect(margin, margin * 0.7f + 70f * scale, settingsW, 110f * scale);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            
            GUI.Label(new Rect(panelRect.x + 10 * scale, panelRect.y + 10 * scale, 200 * scale, 20 * scale), $"SENSITIVITY X: {sensX:F1}", accentStyle);
            float newX = GUI.HorizontalSlider(new Rect(panelRect.x + 10 * scale, panelRect.y + 35 * scale, settingsW - 20 * scale, 20 * scale), sensX, 0.5f, 15f);
            
            GUI.Label(new Rect(panelRect.x + 10 * scale, panelRect.y + 60 * scale, 200 * scale, 20 * scale), $"SENSITIVITY Y: {sensY:F1}", accentStyle);
            float newY = GUI.HorizontalSlider(new Rect(panelRect.x + 10 * scale, panelRect.y + 85 * scale, settingsW - 20 * scale, 20 * scale), sensY, 0.5f, 15f);
            
            if (newX != sensX || newY != sensY)
            {
                sensX = newX; sensY = newY;
                PlayerPrefs.SetFloat("SensX", sensX);
                PlayerPrefs.SetFloat("SensY", sensY);
                PlayerPrefs.Save();
            }
        }

        Rect hp = new Rect(margin, Screen.height - margin - panelH, healthW, panelH);
        GUI.Box(hp, GUIContent.none, panelStyle);
        int maxHealth; int currentHealth = ReadHealth(out maxHealth);
        float healthRatio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

        GUI.Label(new Rect(hp.x + 12 * scale, hp.y + 5 * scale, healthW - 24 * scale, 18 * scale), "VITALS", accentStyle);
        GUI.Label(new Rect(hp.x + 12 * scale, hp.y + 22 * scale, healthW - 24 * scale, 30 * scale), $"HP   {currentHealth:000} / {maxHealth:000}", valueStyle);

        Rect bar = new Rect(hp.x + 12 * scale, hp.y + 57 * scale, healthW - 24 * scale, 7 * scale);
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(bar, whiteTexture);
        GUI.color = currentHealth <= maxHealth * 0.25f ? lowHealthColor : healthColor;
        bar.width *= healthRatio;
        GUI.DrawTexture(bar, whiteTexture);
        GUI.color = Color.white;

        Rect ammo = new Rect(Screen.width - margin - ammoW, Screen.height - margin - panelH, ammoW, panelH);
        GUI.Box(ammo, GUIContent.none, panelStyle);

        if (playerCombat != null)
        {
            GUI.Label(new Rect(ammo.x + 12 * scale, ammo.y + 5 * scale, ammoW - 24 * scale, 18 * scale), "AMMUNITION", accentStyle);
            string ammoText = $"{playerCombat.CurrentAmmo:00}  /  {playerCombat.ReserveAmmo:000}";
            GUI.Label(new Rect(ammo.x + 12 * scale, ammo.y + 21 * scale, ammoW - 24 * scale, 31 * scale), ammoText, valueStyle);

            string state = playerCombat.IsReloading ? $"RELOADING  {Mathf.RoundToInt(playerCombat.ReloadSecondsRemaining):0}s" : playerCombat.CurrentAmmo <= 0 && playerCombat.ReserveAmmo <= 0 ? "OUT OF AMMO" : $"MAG {playerCombat.MagazineCapacity:00}   {playerCombat.FireRate:0.#} ROUNDS/SEC";
            GUI.Label(new Rect(ammo.x + 12 * scale, ammo.y + 52 * scale, ammoW - 24 * scale, 18 * scale), state, smallStyle);

            if (playerCombat.IsReloading)
            {
                Rect reloadBar = new Rect(ammo.x + 12 * scale, ammo.y + 71 * scale, ammoW - 24 * scale, 3 * scale);
                reloadBar.width *= playerCombat.ReloadProgress;
                GUI.color = accentColor;
                GUI.DrawTexture(reloadBar, whiteTexture);
                GUI.color = Color.white;
            }
        }
        else
        {
            GUI.Label(new Rect(ammo.x + 12 * scale, ammo.y + 20 * scale, ammoW - 24 * scale, 40f * scale), PhotonNetwork.InRoom ? "WAITING FOR LOCAL PLAYER" : "PLAYER COMBAT NOT FOUND", valueStyle);
        }

        if (showCrosshair) DrawCrosshair(Screen.width * 0.5f, Screen.height * 0.5f, 8f * scale, 2f * scale);
    }

    private int ReadHealth(out int maxHealth)
    {
        maxHealth = 100;
        if (playerHealth == null) return Mathf.Clamp(testHealth, 0, maxHealth);

        float maxValue = ReadFloatMember(playerHealth, "MaxHealth", "maxHealth", "maxHP", "maxHp");
        float currentValue = ReadFloatMember(playerHealth, "CurrentHealth", "currentHealth", "currentHP", "currentHp", "Health", "health", "current");

        if (maxValue > 0f) maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxValue));
        if (currentValue >= 0f) return Mathf.Clamp(Mathf.RoundToInt(currentValue), 0, maxHealth);
        return Mathf.Clamp(testHealth, 0, maxHealth);
    }

    private float ReadFloatMember(object target, params string[] names)
    {
        Type type = target.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (string memberName in names)
        {
            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null && property.GetIndexParameters().Length == 0) { try { return Convert.ToSingle(property.GetValue(target)); } catch { } }

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null) { try { return Convert.ToSingle(field.GetValue(target)); } catch { } }
        }
        return -1f;
    }

    private void DrawCrosshair(float cx, float cy, float arm, float thickness)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - arm - thickness, cy - thickness * 0.5f, arm, thickness), whiteTexture);
        GUI.DrawTexture(new Rect(cx + thickness, cy - thickness * 0.5f, arm, thickness), whiteTexture);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - arm - thickness, thickness, arm), whiteTexture);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + thickness, thickness, arm), whiteTexture);
        GUI.DrawTexture(new Rect(cx - thickness, cy - thickness, thickness * 2f, thickness * 2f), whiteTexture);
        GUI.color = Color.white;
    }

    private void BuildStyles()
    {
        cachedWidth = Screen.width; cachedHeight = Screen.height;
        if (panelTexture != null) Destroy(panelTexture);
        if (whiteTexture != null) Destroy(whiteTexture);

        panelTexture = MakeTexture(panelColor);
        whiteTexture = MakeTexture(Color.white);

        panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panelTexture }, border = new RectOffset(0, 0, 0, 0), padding = new RectOffset(0, 0, 0, 0) };

        titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.022f, 12f, 22f)), clipping = TextClipping.Clip };
        titleStyle.normal.textColor = accentColor;

        smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.015f, 9f, 15f)), clipping = TextClipping.Clip, wordWrap = false };
        smallStyle.normal.textColor = accentColor;

        valueStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.028f, 16f, 28f)), clipping = TextClipping.Clip, wordWrap = false };
        valueStyle.normal.textColor = textColor;

        accentStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.014f, 9f, 14f)), clipping = TextClipping.Clip, wordWrap = false };
        accentStyle.normal.textColor = accentColor;
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave; texture.SetPixel(0, 0, color); texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (panelTexture != null) Destroy(panelTexture);
        if (whiteTexture != null) Destroy(whiteTexture);
    }
}