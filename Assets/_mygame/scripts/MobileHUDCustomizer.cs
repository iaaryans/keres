using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileHUDCustomizer : MonoBehaviour
{
    private readonly List<HUDDragItem> items = new();
    private HUDDragItem selected;
    private bool isEditing;
    private Canvas canvas;

    [Header("Editing UI")]
    public GameObject editModeToolbar; 
    
    // NAYA FIX: Ab ye GameObject accept karega (tu seedha Image drag kar sakta hai)
    public GameObject smallerButton;
    public GameObject biggerButton;
    public GameObject doneButton;

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

        if (editModeToolbar != null) editModeToolbar.SetActive(false);

        string[] controlNames = { "Fixed Joystick", "ShootBtn", "ReloadBtn", "AimBtn", "JumpBtn" };

        foreach (string controlName in controlNames)
        {
            Transform control = FindDeepChild(canvas.transform, controlName);
            if (control == null) continue;

            HUDDragItem item = control.GetComponent<HUDDragItem>();
            if (item == null) item = control.gameObject.AddComponent<HUDDragItem>();

            item.Initialize(this, canvas);
            items.Add(item);
            item.LoadLayout();

            if (selected == null) selected = item;
        }

        // Setup Toolbar Buttons (Jadoo function call)
        SetupButton(smallerButton, () => ResizeSelected(0.9f));
        SetupButton(biggerButton, () => ResizeSelected(1.1f));
        SetupButton(doneButton, StopEditing);

        SetEditingState(false);
    }

    // NAYA JADOO FUNCTION: Agar object pe Button nahi hai, toh ye khud laga dega!
    private void SetupButton(GameObject obj, UnityEngine.Events.UnityAction action)
    {
        if (obj == null) return;
        
        Button btn = obj.GetComponent<Button>();
        if (btn == null) 
        {
            btn = obj.AddComponent<Button>(); // Auto-add Button component
        }
        btn.onClick.AddListener(action);
    }

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
        if (isEditing) return;
        foreach (HUDDragItem item in items)
        {
            if (item != null) item.RefreshInputBindings();
        }
    }

    public void StartEditing()
    {
        SetEditingState(true);
    }

    public void StopEditing()
    {
        foreach (var item in items) item.SaveLayout();
        SetEditingState(false);
    }

    public void Select(HUDDragItem item) { selected = item; }

    private void ResizeSelected(float factor)
    {
        if (!isEditing || selected == null) return;
        RectTransform rt = selected.GetComponent<RectTransform>();
        Vector2 size = rt.sizeDelta * factor;
        rt.sizeDelta = new Vector2(Mathf.Clamp(size.x, 40f, 500f), Mathf.Clamp(size.y, 40f, 500f));
        selected.SaveLayout();
    }

    private void SetEditingState(bool value)
    {
        isEditing = value;
        foreach (var item in items) item.SetEditing(value);
        
        if (editModeToolbar != null) editModeToolbar.SetActive(value);
    }
}

// Baki HUDDragItem class waisi ki waisi hi hai
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