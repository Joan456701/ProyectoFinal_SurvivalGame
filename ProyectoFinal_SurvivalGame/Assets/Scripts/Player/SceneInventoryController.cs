using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SceneInventoryController : MonoBehaviour
{
    [Serializable]
    private class InventoryItemDefinition
    {
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Color color = Color.white;
        [Min(1)] public int maxStack = 20;
    }

    [Serializable]
    private class InventorySlot
    {
        public InventoryItemDefinition item;
        public int amount;

        public bool IsEmpty => item == null || amount <= 0;

        public void Clear()
        {
            item = null;
            amount = 0;
        }
    }

    private sealed class SlotUI
    {
        public int Index;
        public Image Background;
        public Image Icon;
        public Text AmountLabel;
        public Text ShortcutLabel;
    }

    [Header("Scene References")]
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private FirstPersonController _firstPersonController;
    [SerializeField] private FirstPersonBuilder _firstPersonBuilder;

    [Header("Inventory Settings")]
    [SerializeField] private int _inventorySize = 20;
    [SerializeField] private int _hotbarSize = 5;

    private readonly List<InventorySlot> _slots = new List<InventorySlot>();
    private readonly List<SlotUI> _inventorySlotUIs = new List<SlotUI>();
    private readonly List<SlotUI> _hotbarSlotUIs = new List<SlotUI>();
    private readonly Dictionary<string, InventoryItemDefinition> _itemCatalog = new Dictionary<string, InventoryItemDefinition>();

    private Font _defaultFont;
    private GameObject _inventoryPanel;
    private GameObject _hotbarPanel;
    private GameObject _crosshair;
    private Text _detailTitle;
    private Text _detailAmount;
    private Text _detailDescription;
    private bool _inventoryOpen;
    private int _selectedSlotIndex;

    private void Awake()
    {
        if (_playerInputHandler == null)
        {
            _playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
        }

        if (_firstPersonController == null)
        {
            _firstPersonController = FindFirstObjectByType<FirstPersonController>();
        }

        if (_firstPersonBuilder == null)
        {
            _firstPersonBuilder = FindFirstObjectByType<FirstPersonBuilder>();
        }
    }

    private void Start()
    {
        _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        InitializeSlots();
        BuildCatalog();
        SeedInventory();
        BuildInventoryUI();
        SelectSlot(0);
        SetInventoryOpen(false, true);
    }

    private void Update()
    {
        HandleInventoryToggle();
        HandleHotbarShortcuts();
    }

    public bool TryAddItem(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !_itemCatalog.TryGetValue(itemId, out InventoryItemDefinition definition))
        {
            return false;
        }

        int remainingAmount = amount;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            if (slot.IsEmpty || slot.item.itemId != definition.itemId || slot.amount >= slot.item.maxStack)
            {
                continue;
            }

            int availableSpace = slot.item.maxStack - slot.amount;
            int amountToStore = Mathf.Min(availableSpace, remainingAmount);

            slot.amount += amountToStore;
            remainingAmount -= amountToStore;

            if (remainingAmount <= 0)
            {
                RefreshUI();
                return true;
            }
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            if (!slot.IsEmpty)
            {
                continue;
            }

            slot.item = definition;
            slot.amount = Mathf.Min(definition.maxStack, remainingAmount);
            remainingAmount -= slot.amount;

            if (remainingAmount <= 0)
            {
                RefreshUI();
                return true;
            }
        }

        RefreshUI();
        return false;
    }

    public bool TryRemoveFromSlot(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count || amount <= 0)
        {
            return false;
        }

        InventorySlot slot = _slots[slotIndex];
        if (slot.IsEmpty || slot.amount < amount)
        {
            return false;
        }

        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            slot.Clear();
        }

        RefreshUI();
        return true;
    }

    public string GetSelectedItemId()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slots.Count)
        {
            return string.Empty;
        }

        return _slots[_selectedSlotIndex].IsEmpty ? string.Empty : _slots[_selectedSlotIndex].item.itemId;
    }

    private void InitializeSlots()
    {
        _slots.Clear();

        for (int i = 0; i < _inventorySize; i++)
        {
            _slots.Add(new InventorySlot());
        }
    }

    private void BuildCatalog()
    {
        _itemCatalog.Clear();
        RegisterItem("alien_fiber", "Fibra alienigena", "Material vegetal flexible. Ideal para futuras recetas de cuerda, vendas y piezas blandas.", new Color(0.45f, 0.9f, 0.55f), 25);
        RegisterItem("ferrite_stone", "Piedra ferrita", "Roca densa y resistente. Base perfecta para construccion y herramientas primitivas.", new Color(0.72f, 0.75f, 0.8f), 30);
        RegisterItem("luminous_resin", "Resina luminosa", "Compuesto organico con brillo natural. Puede servir mas adelante para antorchas y adhesivos.", new Color(0.3f, 0.95f, 1f), 15);
        RegisterItem("purified_water", "Agua purificada", "Suministro basico de supervivencia. Conviene reservarla para expediciones largas.", new Color(0.45f, 0.7f, 1f), 10);
        RegisterItem("dehydrated_food", "Comida deshidratada", "Racion compacta y duradera. Buena para mantener recursos siempre a mano.", new Color(1f, 0.68f, 0.28f), 10);
    }

    private void RegisterItem(string itemId, string displayName, string description, Color color, int maxStack)
    {
        _itemCatalog[itemId] = new InventoryItemDefinition
        {
            itemId = itemId,
            displayName = displayName,
            description = description,
            color = color,
            maxStack = maxStack
        };
    }

    private void SeedInventory()
    {
        TryAddItem("alien_fiber", 12);
        TryAddItem("ferrite_stone", 18);
        TryAddItem("luminous_resin", 7);
        TryAddItem("purified_water", 3);
        TryAddItem("dehydrated_food", 5);
    }

    private void HandleInventoryToggle()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        bool shouldToggle = Keyboard.current.escapeKey.wasPressedThisFrame;

        if (!shouldToggle)
        {
            return;
        }

        SetInventoryOpen(!_inventoryOpen);
    }

    private void HandleHotbarShortcuts()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (_hotbarSize > 1 && Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (_hotbarSize > 2 && Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        if (_hotbarSize > 3 && Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        if (_hotbarSize > 4 && Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
    }

    private void SetInventoryOpen(bool isOpen, bool forceRefresh = false)
    {
        if (!forceRefresh && _inventoryOpen == isOpen)
        {
            return;
        }

        _inventoryOpen = isOpen;

        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(_inventoryOpen);
        }

        if (_crosshair != null)
        {
            _crosshair.SetActive(!_inventoryOpen);
        }

        if (_firstPersonController != null)
        {
            _firstPersonController.enabled = !_inventoryOpen;
        }

        if (_firstPersonBuilder != null)
        {
            _firstPersonBuilder.enabled = !_inventoryOpen;
        }

        Cursor.lockState = _inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _inventoryOpen;

        if (!_inventoryOpen && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        RefreshUI();
    }

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return;
        }

        _selectedSlotIndex = slotIndex;
        RefreshUI();
    }

    private void BuildInventoryUI()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        _crosshair = transform.Find("Image") != null ? transform.Find("Image").gameObject : null;
        _inventorySlotUIs.Clear();
        _hotbarSlotUIs.Clear();

        _inventoryPanel = CreatePanel("InventoryPanel", canvasRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(980f, 620f), new Color(0.05f, 0.08f, 0.11f, 0.94f));

        GameObject header = CreateUIObject("Header", _inventoryPanel.transform);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        StretchHorizontally(headerRect, 24f, -24f, -24f, 70f);
        CreateText("Title", header.transform, "Inventario de supervivencia", 32, TextAnchor.MiddleLeft, FontStyle.Bold, Color.white);
        CreateText("Hint", header.transform, "Esc abre/cierra, 1-5 seleccion rapida", 18, TextAnchor.LowerLeft, FontStyle.Normal, new Color(0.74f, 0.82f, 0.88f));

        RectTransform titleRect = header.transform.GetChild(0).GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.45f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        RectTransform hintRect = header.transform.GetChild(1).GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0.45f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        GameObject content = CreateUIObject("Content", _inventoryPanel.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.offsetMin = new Vector2(24f, 24f);
        contentRect.offsetMax = new Vector2(-24f, -104f);

        GameObject gridContainer = CreatePanel("GridContainer", content.transform, new Vector2(0f, 0f), new Vector2(0.64f, 1f), new Vector2(0f, 0f), Vector2.zero, new Color(0.09f, 0.13f, 0.17f, 0.95f));
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();
        gridRect.offsetMin = new Vector2(0f, 0f);
        gridRect.offsetMax = new Vector2(-12f, 0f);

        GridLayoutGroup gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(110f, 110f);
        gridLayout.spacing = new Vector2(12f, 12f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 5;
        gridLayout.padding = new RectOffset(18, 18, 18, 18);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        for (int i = 0; i < _slots.Count; i++)
        {
            _inventorySlotUIs.Add(CreateSlotUI(gridContainer.transform, i, i < _hotbarSize ? (i + 1).ToString() : string.Empty));
        }

        GameObject detailPanel = CreatePanel("DetailPanel", content.transform, new Vector2(0.64f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), Vector2.zero, new Color(0.09f, 0.13f, 0.17f, 0.95f));
        RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.offsetMin = new Vector2(12f, 0f);
        detailRect.offsetMax = Vector2.zero;

        _detailTitle = CreateText("SelectedItemTitle", detailPanel.transform, string.Empty, 30, TextAnchor.UpperLeft, FontStyle.Bold, Color.white);
        SetRect(_detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -92f), new Vector2(-18f, -18f));

        _detailAmount = CreateText("SelectedItemAmount", detailPanel.transform, string.Empty, 20, TextAnchor.UpperLeft, FontStyle.Bold, new Color(0.39f, 0.85f, 1f));
        SetRect(_detailAmount.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -138f), new Vector2(-18f, -58f));

        _detailDescription = CreateText("SelectedItemDescription", detailPanel.transform, string.Empty, 18, TextAnchor.UpperLeft, FontStyle.Normal, new Color(0.84f, 0.88f, 0.92f));
        _detailDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailDescription.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(_detailDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -168f));

        _hotbarPanel = CreatePanel("HotbarPanel", canvasRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(620f, 112f), new Color(0.04f, 0.06f, 0.08f, 0.82f));
        HorizontalLayoutGroup hotbarLayout = _hotbarPanel.AddComponent<HorizontalLayoutGroup>();
        hotbarLayout.padding = new RectOffset(16, 16, 16, 16);
        hotbarLayout.spacing = 12f;
        hotbarLayout.childAlignment = TextAnchor.MiddleCenter;
        hotbarLayout.childControlWidth = false;
        hotbarLayout.childControlHeight = false;
        hotbarLayout.childForceExpandWidth = false;
        hotbarLayout.childForceExpandHeight = false;

        for (int i = 0; i < _hotbarSize; i++)
        {
            _hotbarSlotUIs.Add(CreateSlotUI(_hotbarPanel.transform, i, (i + 1).ToString()));
        }
    }

    private SlotUI CreateSlotUI(Transform parent, int slotIndex, string shortcut)
    {
        GameObject slotRoot = CreateUIObject("Slot_" + slotIndex, parent);
        RectTransform slotRect = slotRoot.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(110f, 110f);

        Image background = slotRoot.AddComponent<Image>();
        background.color = new Color(0.14f, 0.18f, 0.22f, 0.95f);

        Button button = slotRoot.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        button.colors = colors;

        int capturedIndex = slotIndex;
        button.onClick.AddListener(delegate { SelectSlot(capturedIndex); });

        GameObject iconObject = CreateUIObject("Icon", slotRoot.transform);
        Image icon = iconObject.AddComponent<Image>();
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = new Vector2(14f, 14f);
        iconRect.offsetMax = new Vector2(-14f, -30f);
        icon.enabled = false;

        Text amountLabel = CreateText("Amount", slotRoot.transform, string.Empty, 20, TextAnchor.LowerRight, FontStyle.Bold, Color.white);
        SetRect(amountLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 4f), new Vector2(-10f, 28f));

        Text shortcutLabel = CreateText("Shortcut", slotRoot.transform, shortcut, 18, TextAnchor.UpperLeft, FontStyle.Bold, new Color(0.75f, 0.86f, 0.96f));
        SetRect(shortcutLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -28f), new Vector2(34f, -8f));

        return new SlotUI
        {
            Index = slotIndex,
            Background = background,
            Icon = icon,
            AmountLabel = amountLabel,
            ShortcutLabel = shortcutLabel
        };
    }

    private void RefreshUI()
    {
        for (int i = 0; i < _inventorySlotUIs.Count; i++)
        {
            UpdateSlotVisual(_inventorySlotUIs[i]);
        }

        for (int i = 0; i < _hotbarSlotUIs.Count; i++)
        {
            UpdateSlotVisual(_hotbarSlotUIs[i]);
        }

        RefreshDetailsPanel();
    }

    private void UpdateSlotVisual(SlotUI slotUI)
    {
        InventorySlot slot = _slots[slotUI.Index];
        bool isSelected = slotUI.Index == _selectedSlotIndex;

        slotUI.Background.color = isSelected
            ? new Color(0.23f, 0.47f, 0.58f, 1f)
            : new Color(0.14f, 0.18f, 0.22f, 0.95f);

        if (slot.IsEmpty)
        {
            slotUI.Icon.enabled = false;
            slotUI.AmountLabel.text = string.Empty;
            return;
        }

        slotUI.Icon.enabled = true;
        slotUI.Icon.color = slot.item.color;
        slotUI.AmountLabel.text = slot.amount > 1 ? slot.amount.ToString() : string.Empty;
    }

    private void RefreshDetailsPanel()
    {
        if (_detailTitle == null || _selectedSlotIndex < 0 || _selectedSlotIndex >= _slots.Count)
        {
            return;
        }

        InventorySlot selectedSlot = _slots[_selectedSlotIndex];

        if (selectedSlot.IsEmpty)
        {
            _detailTitle.text = "Slot vacio";
            _detailAmount.text = "Sin objeto seleccionado";
            _detailDescription.text = "Selecciona un slot ocupado para ver sus detalles. Esta base ya admite apilar objetos y servira como punto de partida para crafteo y construccion mas adelante.";
            return;
        }

        _detailTitle.text = selectedSlot.item.displayName;
        _detailAmount.text = "Cantidad: " + selectedSlot.amount + " / " + selectedSlot.item.maxStack;
        _detailDescription.text = selectedSlot.item.description;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject panel = CreateUIObject(name, parent);
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, FontStyle style, Color color)
    {
        GameObject textObject = CreateUIObject(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = _defaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = style;
        text.color = color;
        text.supportRichText = false;
        return text;
    }

    private static void StretchHorizontally(RectTransform rectTransform, float left, float right, float top, float height)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.offsetMin = new Vector2(left, -height - top);
        rectTransform.offsetMax = new Vector2(right, -top);
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }
}
