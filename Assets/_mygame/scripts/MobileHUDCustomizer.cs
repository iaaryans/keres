using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileHUDCustomizer : MonoBehaviour
{
    private readonly List<HUDDragItem> items = new();
    private HUDDragItem selected;
    private bool editing;

    private Canvas canvas;
    private Button editButton, smallerButton, biggerButton, doneButton;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("MobileHUDCustomizer needs a Canvas.");
            enabled = false;
            return;
        }

        string[] controlNames =
        {
            "Fixed Joystick",
            "ShootBtn",
            "ReloadBtn",
            "AimBtn",
            "JumpBtn"
        };

        foreach (string controlName in controlNames)
        {
            // NAYA FIX: Ab ye direct bacchon ke alawa andar ke folders me bhi dhoondhega
            Transform control = FindDeepChild(canvas.transform, controlName);

            if (control == null)
            {
                Debug.LogWarning($"[KERES] HUD Item '{controlName}' nahi mila! Naam spelling check karo.");
                continue;
            }

            HUDDragItem item = control.GetComponent<HUDDragItem>();
            if (item == null) item = control.gameObject.AddComponent<HUDDragItem>();

            item.Initialize(this, canvas);
            items.Add(item);
            item.LoadLayout();

            if (selected == null) selected = item;
        }

        CreateToolbar();
        SetEditing(false);
    }

    // Deep search function
    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform result = FindDeepChild(child, childName);
            if (result != null) return result;
        }
        return null;
    }

    public void RefreshInputBindings()
    {
        if (editing) return;
        foreach (HUDDragItem item in items)
        {
            if (item != null) item.RefreshInputBindings();
        }
    }

    void CreateToolbar()
    {
        editButton = CreateButton("EDIT HUD", -165);
        smallerButton = CreateButton("SIZE -", -55);
        biggerButton = CreateButton("SIZE +", 55);
        doneButton = CreateButton("DONE", 165);

        editButton.onClick.AddListener(() => SetEditing(!editing));
        smallerButton.onClick.AddListener(() => ResizeSelected(0.9f));
        biggerButton.onClick.AddListener(() => ResizeSelected(1.1f));
        doneButton.onClick.AddListener(() =>
        {
            foreach (var item in items) item.SaveLayout();
            SetEditing(false);
        });
    }

    Button CreateButton(string label, float x)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = new Vector2(x, -20);
        rt.sizeDelta = new Vector2(100, 48);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGO.transform.SetParent(go.transform, false);

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;

        Text text = textGO.GetComponent<Text>();
        text.text = label; text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 15; text.fontStyle = FontStyle.Bold; text.raycastTarget = false;

        return button;
    }

    public void Select(HUDDragItem item) { selected = item; }

    void ResizeSelected(float factor)
    {
        if (!editing || selected == null) return;
        RectTransform rt = selected.GetComponent<RectTransform>();
        Vector2 size = rt.sizeDelta * factor;
        rt.sizeDelta = new Vector2(Mathf.Clamp(size.x, 40f, 500f), Mathf.Clamp(size.y, 40f, 500f));
        selected.SaveLayout();
    }

    void SetEditing(bool value)
    {
        editing = value;
        foreach (var item in items) item.SetEditing(value);
        smallerButton.interactable = value; biggerButton.interactable = value;
        doneButton.gameObject.SetActive(value);
        Text label = editButton.GetComponentInChildren<Text>();
        label.text = value ? "EDITING..." : "EDIT HUD";
    }
}

public class HUDDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private MobileHUDCustomizer owner;
    private Canvas canvas;
    private RectTransform rt;
    private bool editing;
    private Vector2 dragOffset;

    private Behaviour[] inputComponents;
    private bool[] originalStates;

    public void Initialize(MobileHUDCustomizer controller, Canvas parentCanvas)
    {
        owner = controller; canvas = parentCanvas; rt = GetComponent<RectTransform>();
        var components = new List<Behaviour>();
        foreach (Behaviour component in GetComponents<Behaviour>())
        {
            if (component != this && (component.GetType().Name == "OnScreenButton" || component.GetType().Name == "OnScreenStick"))
                components.Add(component);
        }
        inputComponents = components.ToArray();
        originalStates = new bool[inputComponents.Length];
        for (int i = 0; i < inputComponents.Length; i++) originalStates[i] = inputComponents[i].enabled;
    }

    public void SetEditing(bool value)
    {
        editing = value;
        for (int i = 0; i < inputComponents.Length; i++) inputComponents[i].enabled = value ? false : originalStates[i];
    }

    public void RefreshInputBindings()
    {
        if (editing || inputComponents == null) return;
        for (int i = 0; i < inputComponents.Length; i++)
        {
            Behaviour component = inputComponents[i];
            if (component == null) continue;
            bool shouldBeEnabled = originalStates[i];
            component.enabled = false; component.enabled = shouldBeEnabled;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!editing) return;
        owner.Select(this);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 pointerPosition);
        dragOffset = rt.anchoredPosition - pointerPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!editing) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 pointerPosition))
        {
            rt.anchoredPosition = pointerPosition + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData) { if (editing) SaveLayout(); }

    public void SaveLayout()
    {
        PlayerPrefs.SetFloat(name + "_X", rt.anchoredPosition.x);
        PlayerPrefs.SetFloat(name + "_Y", rt.anchoredPosition.y);
        PlayerPrefs.SetFloat(name + "_W", rt.sizeDelta.x);
        PlayerPrefs.SetFloat(name + "_H", rt.sizeDelta.y);
        PlayerPrefs.Save();
    }

    public void LoadLayout()
    {
        if (PlayerPrefs.HasKey(name + "_X"))
        {
            rt.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(name + "_X"), PlayerPrefs.GetFloat(name + "_Y"));
            rt.sizeDelta = new Vector2(PlayerPrefs.GetFloat(name + "_W", rt.sizeDelta.x), PlayerPrefs.GetFloat(name + "_H", rt.sizeDelta.y));
        }
    }
}