using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SceneInventoryController : MonoBehaviour
{
    private const string GeneratedUiRootName = "GeneratedInventoryUI";

    [Serializable]
    private class InventoryItemDefinition
    {
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Color color = Color.white;
        [Min(1)] public int maxStack = 20;
        public PrimitiveType worldPrimitiveType = PrimitiveType.Cube;
        public Vector3 worldScale = Vector3.one;
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

    [Serializable]
    private class InventoryState
    {
        public string[] itemIds;
        public int[] amounts;
        public int activeHotbarSlotIndex;
    }

    private sealed class SlotUI
    {
        public int Index;
        public Image Background;
        public Image Icon;
        public Text AmountLabel;
        public Text ShortcutLabel;
    }

    private readonly Stack<InventoryState> _undoStack = new Stack<InventoryState>();
    private const int MaxUndoHistory = 50;

    [Header("Scene References")]
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private FirstPersonController _firstPersonController;
    [SerializeField] private FirstPersonBuilder _firstPersonBuilder;
    [SerializeField] private Camera _playerCamera;

    [Header("Inventory Settings")]
    [SerializeField] private int _inventorySize = 20;
    [SerializeField] private int _hotbarSize = 5;

    [Header("UI Style")]
    [SerializeField] private Font _uiFont;
    [SerializeField] private Color _inventoryPanelColor = new Color(0.05f, 0.08f, 0.11f, 0.94f);
    [SerializeField] private Color _sectionPanelColor = new Color(0.09f, 0.13f, 0.17f, 0.95f);
    [SerializeField] private Color _hotbarPanelColor = new Color(0.04f, 0.06f, 0.08f, 0.82f);
    [SerializeField] private Color _slotColor = new Color(0.14f, 0.18f, 0.22f, 0.95f);
    [SerializeField] private Color _selectedSlotColor = new Color(0.23f, 0.47f, 0.58f, 1f);
    [SerializeField] private Color _titleTextColor = Color.white;
    [SerializeField] private Color _hintTextColor = new Color(0.74f, 0.82f, 0.88f);
    [SerializeField] private Color _detailAmountColor = new Color(0.39f, 0.85f, 1f);
    [SerializeField] private Color _bodyTextColor = new Color(0.84f, 0.88f, 0.92f);
    [SerializeField] private Color _shortcutTextColor = new Color(0.75f, 0.86f, 0.96f);
    [SerializeField] private Color _pickupPromptColor = Color.white;

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
    private Text _pickupPromptText;
    private GameObject _heldItemVisual;
    private GameObject _generatedUiRoot;
    private Image _dragIcon;
    private bool _inventoryOpen;
    private int _selectedSlotIndex;
    private int _activeHotbarSlotIndex;
    private string _heldItemId = string.Empty;
    private int _draggedSlotIndex = -1;
    private bool _editorPreviewRefreshQueued;

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

        if (_playerCamera == null)
        {
            _playerCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            QueueEditorPreviewRefresh();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            QueueEditorPreviewRefresh();
        }
    }

    private void Start()
    {
        _defaultFont = _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        InitializeSlots();
        BuildCatalog();
        SeedInventory();
        BuildInventoryUI();
        SelectSlot(0);
        _activeHotbarSlotIndex = 0;
        SetInventoryOpen(false, true);
    }

    private void Update()
    {
        HandleInventoryToggle();
        HandleHotbarShortcuts();
        HandleDropShortcut();
        HandleEquipShortcut();
        HandleSplitShortcut();
        HandleHeldItemDropShortcuts();
        HandleUndoShortcut();
        HandleEatFoodShortcut();
    }

    private void SaveUndoState()
    {
        InventoryState state = new InventoryState();
        state.itemIds = new string[_slots.Count];
        state.amounts = new int[_slots.Count];
        state.activeHotbarSlotIndex = _activeHotbarSlotIndex;

        for (int i = 0; i < _slots.Count; i++)
        {
            state.itemIds[i] = _slots[i].item?.itemId ?? string.Empty;
            state.amounts[i] = _slots[i].amount;
        }

        _undoStack.Push(state);

        if (_undoStack.Count > MaxUndoHistory)
        {
            InventoryState[] tempArray = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 1; i < tempArray.Length; i++)
            {
                _undoStack.Push(tempArray[i]);
            }
        }
    }

    private void Undo()
    {
        if (_undoStack.Count == 0)
        {
            Debug.Log("No hay acciones para deshacer");
            return;
        }

        InventoryState state = _undoStack.Pop();

        for (int i = 0; i < _slots.Count && i < state.itemIds.Length; i++)
        {
            if (string.IsNullOrEmpty(state.itemIds[i]))
            {
                _slots[i].Clear();
            }
            else if (_itemCatalog.TryGetValue(state.itemIds[i], out InventoryItemDefinition definition))
            {
                _slots[i].item = definition;
                _slots[i].amount = state.amounts[i];
            }
        }

        _activeHotbarSlotIndex = state.activeHotbarSlotIndex;
        _selectedSlotIndex = Mathf.Clamp(_selectedSlotIndex, 0, _slots.Count - 1);

        ClearHeldItemVisual();
        RefreshHeldItemFromHotbarSelection();
        RefreshUI();

        Debug.Log("Accion deshecha. Historial restante: " + _undoStack.Count);
    }

    private void HandleUndoShortcut()
    {
        if (_playerInputHandler != null && _playerInputHandler.undoTriggered)
        {
            Undo();
        }
    }

    private void HandleEatFoodShortcut()
    {
        if (_playerInputHandler != null && _playerInputHandler.eatTriggered)
        {
            int slotIndexToEat = -1;

            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slots.Count && 
                !_slots[_selectedSlotIndex].IsEmpty && _slots[_selectedSlotIndex].item.itemId == "Comida")
            {
                slotIndexToEat = _selectedSlotIndex;
            }
            else if (_activeHotbarSlotIndex >= 0 && _activeHotbarSlotIndex < _slots.Count &&
                    !_slots[_activeHotbarSlotIndex].IsEmpty && _slots[_activeHotbarSlotIndex].item.itemId == "Comida")
            {
                slotIndexToEat = _activeHotbarSlotIndex;
            }

            if (slotIndexToEat >= 0)
            {
                FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
                if (player != null)
                {
                    player.HealPlayer(20);
                    Debug.Log("Has consumido comida. Vida regenerada: " + Mathf.RoundToInt(player.GetHealth()) + "/" + player.GetMaxHealth());
                }
                TryRemoveFromSlot(slotIndexToEat, 1, true);
                RefreshUI();
            }
        }
    }

    public bool TryAddItem(string itemId, int amount, bool saveUndo = true)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !_itemCatalog.TryGetValue(itemId, out InventoryItemDefinition definition))
        {
            return false;
        }

        if (!CanStoreItemAmount(definition, amount))
        {
            Debug.Log("No hay sitio en el inventario");
            return false;
        }

        if (saveUndo)
        {
            SaveUndoState();
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

    public bool TryRemoveFromSlot(int slotIndex, int amount, bool saveUndo = false)
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

        if (saveUndo)
        {
            SaveUndoState();
        }

        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            slot.Clear();
        }

        ValidateHeldItem();
        RefreshHeldItemFromHotbarSelection();
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

    public void SetPickupPrompt(bool shouldShow, string promptText)
    {
        if (_pickupPromptText == null)
        {
            return;
        }

        _pickupPromptText.gameObject.SetActive(shouldShow && !_inventoryOpen);
        if (shouldShow)
        {
            _pickupPromptText.text = promptText;
        }
    }

    public void BeginSlotDrag(int slotIndex)
    {
        if (!_inventoryOpen || slotIndex < 0 || slotIndex >= _slots.Count || _slots[slotIndex].IsEmpty)
        {
            return;
        }

        _draggedSlotIndex = slotIndex;

        if (_dragIcon != null)
        {
            _dragIcon.enabled = true;
            _dragIcon.color = _slots[slotIndex].item.color;
            _dragIcon.transform.SetAsLastSibling();
        }
    }

    public void UpdateSlotDrag(Vector2 screenPosition)
    {
        if (_draggedSlotIndex < 0 || _dragIcon == null)
        {
            return;
        }

        RectTransform dragRect = _dragIcon.rectTransform;
        dragRect.position = screenPosition;
    }

    public void EndSlotDrag()
    {
        _draggedSlotIndex = -1;

        if (_dragIcon != null)
        {
            _dragIcon.enabled = false;
        }
    }

    public void EndSlotDrag(PointerEventData eventData)
    {
        if (_draggedSlotIndex >= 0 && eventData != null && WasDroppedOutsideInventory(eventData))
        {
            DropInventorySlotAmount(_draggedSlotIndex, _slots[_draggedSlotIndex].amount);
        }

        EndSlotDrag();
    }

    public void HandleSlotDrop(int targetSlotIndex)
    {
        if (_draggedSlotIndex < 0 || targetSlotIndex < 0 || targetSlotIndex >= _slots.Count)
        {
            EndSlotDrag();
            return;
        }

        if (_draggedSlotIndex == targetSlotIndex)
        {
            EndSlotDrag();
            return;
        }

        MergeStacks(_draggedSlotIndex, targetSlotIndex);
        EndSlotDrag();
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
        RegisterItem("alien_fiber", "Fibra alienigena", "Material vegetal flexible. Ideal para futuras recetas de cuerda, vendas y piezas blandas.", new Color(0.45f, 0.9f, 0.55f), 25, PrimitiveType.Capsule, new Vector3(0.45f, 0.45f, 0.45f));
        RegisterItem("ferrite_stone", "Piedra ferrita", "Roca densa y resistente. Base perfecta para construccion y herramientas primitivas.", new Color(0.72f, 0.75f, 0.8f), 30, PrimitiveType.Cube, new Vector3(0.7f, 0.7f, 0.7f));
        RegisterItem("stone", "Piedra", "Fragmento mineral recogido del entorno. Puede utilizarse como recurso basico para supervivencia y construccion.", new Color(0.62f, 0.64f, 0.68f), 30, PrimitiveType.Cube, new Vector3(1f, 1f, 1f));
        RegisterItem("luminous_resin", "Resina luminosa", "Compuesto organico con brillo natural. Puede servir mas adelante para antorchas y adhesivos.", new Color(0.3f, 0.95f, 1f), 15, PrimitiveType.Sphere, new Vector3(0.55f, 0.55f, 0.55f));
        RegisterItem("purified_water", "Agua purificada", "Suministro basico de supervivencia. Conviene reservarla para expediciones largas.", new Color(0.45f, 0.7f, 1f), 10, PrimitiveType.Cylinder, new Vector3(0.45f, 0.6f, 0.45f));
        RegisterItem("Comida", "Comida", "Comida которую puedes consumir para recuperar hambre.", new Color(1f, 0.68f, 0.28f), 10, PrimitiveType.Sphere, new Vector3(0.5f, 0.5f, 0.5f));
        RegisterItem("Bombona", "Bombona de oxigeno", "Tanque de oxigeno. Se acabara el tiempo, moriras.", new Color(0.2f, 0.8f, 1f), 1, PrimitiveType.Capsule, new Vector3(0.3f, 0.6f, 0.3f));
    }

    private void RegisterItem(string itemId, string displayName, string description, Color color, int maxStack, PrimitiveType worldPrimitiveType, Vector3 worldScale)
    {
        _itemCatalog[itemId] = new InventoryItemDefinition
        {
            itemId = itemId,
            displayName = displayName,
            description = description,
            color = color,
            maxStack = maxStack,
            worldPrimitiveType = worldPrimitiveType,
            worldScale = worldScale
        };
    }

    private void SeedInventory()
    {
        TryAddItem("alien_fiber", 12, false);
        TryAddItem("ferrite_stone", 18, false);
        TryAddItem("luminous_resin", 7, false);
        TryAddItem("purified_water", 3, false);
        TryAddItem("dehydrated_food", 5, false);
    }

    private bool CanStoreItemAmount(InventoryItemDefinition definition, int amount)
    {
        int availableCapacity = 0;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];

            if (slot.IsEmpty)
            {
                availableCapacity += definition.maxStack;
                continue;
            }

            if (slot.item.itemId == definition.itemId)
            {
                availableCapacity += definition.maxStack - slot.amount;
            }
        }

        return availableCapacity >= amount;
    }

    private void HandleInventoryToggle()
    {
        if (_playerInputHandler == null || !_playerInputHandler.inventoryTriggered)
        {
            return;
        }

        SetInventoryOpen(!_inventoryOpen);
    }

    private void HandleHotbarShortcuts()
    {
        if (_playerInputHandler == null)
        {
            return;
        }

        if (_playerInputHandler.slot1Triggered) HandleHotbarKeyPressed(0);
        if (_hotbarSize > 1 && _playerInputHandler.slot2Triggered) HandleHotbarKeyPressed(1);
        if (_hotbarSize > 2 && _playerInputHandler.slot3Triggered) HandleHotbarKeyPressed(2);
        if (_hotbarSize > 3 && _playerInputHandler.slot4Triggered) HandleHotbarKeyPressed(3);
        if (_hotbarSize > 4 && _playerInputHandler.slot5Triggered) HandleHotbarKeyPressed(4);
    }

    private void HandleDropShortcut()
    {
        if (!_inventoryOpen || _playerInputHandler == null || !_playerInputHandler.dropTriggered)
        {
            return;
        }

        DropSelectedItem();
    }

    private void HandleEquipShortcut()
    {
        if (!_inventoryOpen || _playerInputHandler == null || !_playerInputHandler.equipTriggered)
        {
            return;
        }

        EquipSelectedItemInHand();
    }

    private void HandleSplitShortcut()
    {
        if (!_inventoryOpen || _playerInputHandler == null || !_playerInputHandler.subdivideTriggered)
        {
            return;
        }

        SplitSelectedStack();
    }

    private void HandleHeldItemDropShortcuts()
    {
        if (_inventoryOpen || _playerInputHandler == null)
        {
            return;
        }

        if (_playerInputHandler.dropAllTriggered)
        {
            DropFromActiveHotbarStack(DropMode.FullStack);
        }

        if (_playerInputHandler.dropHalfTriggered)
        {
            DropFromActiveHotbarStack(DropMode.HalfStack);
        }

        if (_playerInputHandler.dropOneTriggered)
        {
            DropFromActiveHotbarStack(DropMode.SingleUnit);
        }
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

        SetPickupPrompt(false, string.Empty);

        if (Application.isPlaying && _firstPersonController != null)
        {
            _firstPersonController.enabled = !_inventoryOpen;
        }

        if (Application.isPlaying && _firstPersonBuilder != null)
        {
            _firstPersonBuilder.enabled = !_inventoryOpen;
        }

        if (Application.isPlaying)
        {
            Cursor.lockState = _inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _inventoryOpen;
        }

        if (!_inventoryOpen && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (!_inventoryOpen)
        {
            RefreshHeldItemFromHotbarSelection();
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

    private void HandleHotbarKeyPressed(int hotbarSlotIndex)
    {
        if (_inventoryOpen)
        {
            MoveSelectedStackToHotbarSlot(hotbarSlotIndex);
            return;
        }

        _activeHotbarSlotIndex = hotbarSlotIndex;
        SelectSlot(hotbarSlotIndex);
        RefreshHeldItemFromHotbarSelection();
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
        EnsureGeneratedUiRoot(canvasRect);
        ClearGeneratedUiRootChildren();

        _inventoryPanel = CreatePanel("InventoryPanel", _generatedUiRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(980f, 620f), _inventoryPanelColor);

        _pickupPromptText = CreateText("PickupPrompt", _generatedUiRoot.transform, "Pulsar E para recoger", 24, TextAnchor.MiddleCenter, FontStyle.Bold, _pickupPromptColor);
        SetRect(_pickupPromptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 140f), new Vector2(190f, 180f));
        _pickupPromptText.gameObject.SetActive(false);

        GameObject dragIconObject = CreateUIObject("DragIcon", _generatedUiRoot.transform);
        _dragIcon = dragIconObject.AddComponent<Image>();
        _dragIcon.raycastTarget = false;
        _dragIcon.enabled = false;
        RectTransform dragIconRect = _dragIcon.rectTransform;
        dragIconRect.sizeDelta = new Vector2(72f, 72f);

        GameObject header = CreateUIObject("Header", _inventoryPanel.transform);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        StretchHorizontally(headerRect, 24f, -24f, -24f, 70f);
        CreateText("Title", header.transform, "Inventario de supervivencia", 32, TextAnchor.MiddleLeft, FontStyle.Bold, _titleTextColor);
        CreateText("Hint", header.transform, "I abre/cierra, 1-5 hotbar, R suelta 1, T pone en mano, X divide, arrastrar fuera tira stack, Ctrl+Z undo", 18, TextAnchor.LowerLeft, FontStyle.Normal, _hintTextColor);

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

        GameObject gridContainer = CreatePanel("GridContainer", content.transform, new Vector2(0f, 0f), new Vector2(0.64f, 1f), new Vector2(0f, 0f), Vector2.zero, _sectionPanelColor);
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

        GameObject detailPanel = CreatePanel("DetailPanel", content.transform, new Vector2(0.64f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), Vector2.zero, _sectionPanelColor);
        RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
        detailRect.offsetMin = new Vector2(12f, 0f);
        detailRect.offsetMax = Vector2.zero;

        _detailTitle = CreateText("SelectedItemTitle", detailPanel.transform, string.Empty, 30, TextAnchor.UpperLeft, FontStyle.Bold, _titleTextColor);
        SetRect(_detailTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -92f), new Vector2(-18f, -18f));

        _detailAmount = CreateText("SelectedItemAmount", detailPanel.transform, string.Empty, 20, TextAnchor.UpperLeft, FontStyle.Bold, _detailAmountColor);
        SetRect(_detailAmount.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -138f), new Vector2(-18f, -58f));

        _detailDescription = CreateText("SelectedItemDescription", detailPanel.transform, string.Empty, 18, TextAnchor.UpperLeft, FontStyle.Normal, _bodyTextColor);
        _detailDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailDescription.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(_detailDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -168f));

        _hotbarPanel = CreatePanel("HotbarPanel", _generatedUiRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(620f, 112f), _hotbarPanelColor);
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
        background.color = _slotColor;

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

        InventorySlotDragHandler dragHandler = slotRoot.AddComponent<InventorySlotDragHandler>();
        dragHandler.Initialize(this, slotIndex);

        GameObject iconObject = CreateUIObject("Icon", slotRoot.transform);
        Image icon = iconObject.AddComponent<Image>();
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = new Vector2(14f, 14f);
        iconRect.offsetMax = new Vector2(-14f, -30f);
        icon.enabled = false;

        Text amountLabel = CreateText("Amount", slotRoot.transform, string.Empty, 20, TextAnchor.LowerRight, FontStyle.Bold, _titleTextColor);
        SetRect(amountLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, 4f), new Vector2(-10f, 28f));

        Text shortcutLabel = CreateText("Shortcut", slotRoot.transform, shortcut, 18, TextAnchor.UpperLeft, FontStyle.Bold, _shortcutTextColor);
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
        bool isActiveHotbarSlot = slotUI.Index == _activeHotbarSlotIndex && slotUI.Index < _hotbarSize;

        slotUI.Background.color = isSelected || isActiveHotbarSlot ? _selectedSlotColor : _slotColor;

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
            _detailDescription.text = "Selecciona un slot ocupado para ver sus detalles. Dentro del inventario puedes pulsar T para poner el objeto en la mano, R para soltar una unidad, X para dividir un stack par y arrastrar fuera del inventario para tirar el stack completo.";
            return;
        }

        _detailTitle.text = selectedSlot.item.displayName;
        _detailAmount.text = "Cantidad: " + selectedSlot.amount + " / " + selectedSlot.item.maxStack;
        _detailDescription.text = selectedSlot.item.description + "\n\n1-5: mover o intercambiar con la hotbar\nT: poner en la mano\nR: soltar una unidad\nX: dividir stack entre 2\nArrastrar fuera: tirar stack completo\nCtrl+Z: deshacer ultimo cambio\nFuera del inventario: G stack entero, H mitad, J una unidad";
    }

    private void DropSelectedItem()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slots.Count)
        {
            return;
        }

        InventorySlot selectedSlot = _slots[_selectedSlotIndex];
        if (selectedSlot.IsEmpty || _playerCamera == null)
        {
            return;
        }

        DropInventorySlotAmount(_selectedSlotIndex, 1);
    }

    private void EquipSelectedItemInHand()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slots.Count || _playerCamera == null)
        {
            return;
        }

        InventorySlot selectedSlot = _slots[_selectedSlotIndex];
        if (selectedSlot.IsEmpty)
        {
            return;
        }

        if (_heldItemId == selectedSlot.item.itemId)
        {
            if (selectedSlot.item.itemId == "Bombona")
            {
                OxygenSystem.Instance?.SetPaused(true);
            }
            ClearHeldItemVisual();
            Debug.Log("Has quitado " + selectedSlot.item.displayName + " de la mano");
            RefreshUI();
            return;
        }

        if (selectedSlot.item.itemId == "Bombona")
        {
            OxygenSystem.Instance?.GiveOxygenTank();
        }

        SaveUndoState();
        CreateHeldItemVisual(selectedSlot.item);
        Debug.Log("Has puesto " + selectedSlot.item.displayName + " en la mano");
        RefreshUI();
    }

    private void RefreshHeldItemFromHotbarSelection()
    {
        ClearHeldItemVisual();

        if (_inventoryOpen || _activeHotbarSlotIndex < 0 || _activeHotbarSlotIndex >= _hotbarSize)
        {
            return;
        }

        InventorySlot activeSlot = _slots[_activeHotbarSlotIndex];
        if (activeSlot.IsEmpty)
        {
            RefreshUI();
            return;
        }

        CreateHeldItemVisual(activeSlot.item);
        RefreshUI();
    }

    private enum DropMode
    {
        FullStack,
        HalfStack,
        SingleUnit
    }

    private void DropFromActiveHotbarStack(DropMode dropMode)
    {
        if (_activeHotbarSlotIndex < 0 || _activeHotbarSlotIndex >= _hotbarSize)
        {
            return;
        }

        InventorySlot activeSlot = _slots[_activeHotbarSlotIndex];
        if (activeSlot.IsEmpty)
        {
            return;
        }

        int amountToDrop = dropMode switch
        {
            DropMode.FullStack => activeSlot.amount,
            DropMode.HalfStack => activeSlot.amount % 2 == 0 ? activeSlot.amount / 2 : 0,
            DropMode.SingleUnit => 1,
            _ => 0
        };

        if (amountToDrop <= 0)
        {
            Debug.Log("No se puede tirar la mitad porque la cantidad es impar");
            return;
        }

        DropInventorySlotAmount(_activeHotbarSlotIndex, amountToDrop);
    }

    private void CreateDroppedPickup(InventoryItemDefinition definition, Vector3 spawnPosition, int amount)
    {
        GameObject droppedObject = GameObject.CreatePrimitive(definition.worldPrimitiveType);
        droppedObject.name = definition.displayName;
        droppedObject.transform.position = spawnPosition;
        droppedObject.transform.localScale = definition.worldScale;

        Renderer rendererComponent = droppedObject.GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = definition.color;
            rendererComponent.material = material;
        }

        Rigidbody rigidbodyComponent = droppedObject.AddComponent<Rigidbody>();
        rigidbodyComponent.mass = 1f;

        ScenePickupItem pickupItem = droppedObject.AddComponent<ScenePickupItem>();
        pickupItem.Configure(definition.itemId, amount, definition.displayName);
    }

    private void CreateHeldItemVisual(InventoryItemDefinition definition)
    {
        ClearHeldItemVisual();

        _heldItemVisual = GameObject.CreatePrimitive(definition.worldPrimitiveType);
        _heldItemVisual.name = definition.displayName + "_InHand";
        _heldItemVisual.transform.SetParent(_playerCamera.transform, false);
        _heldItemVisual.transform.localPosition = new Vector3(0.32f, -0.22f, 0.65f);
        _heldItemVisual.transform.localRotation = Quaternion.Euler(18f, -28f, -12f);
        _heldItemVisual.transform.localScale = definition.worldScale * 0.28f;

        Collider heldCollider = _heldItemVisual.GetComponent<Collider>();
        if (heldCollider != null)
        {
            DestroyImmediate(heldCollider);
        }

        Renderer rendererComponent = _heldItemVisual.GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = definition.color;
            rendererComponent.material = material;
        }

        _heldItemId = definition.itemId;
    }

    private void ValidateHeldItem()
    {
        if (string.IsNullOrEmpty(_heldItemId))
        {
            return;
        }

        if (!InventoryContainsItem(_heldItemId))
        {
            ClearHeldItemVisual();
        }
    }

    private void MoveSelectedStackToHotbarSlot(int hotbarSlotIndex)
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slots.Count || hotbarSlotIndex < 0 || hotbarSlotIndex >= _hotbarSize)
        {
            return;
        }

        if (_selectedSlotIndex == hotbarSlotIndex)
        {
            _activeHotbarSlotIndex = hotbarSlotIndex;
            RefreshUI();
            return;
        }

        SaveUndoState();

        InventorySlot sourceSlot = _slots[_selectedSlotIndex];
        InventorySlot targetSlot = _slots[hotbarSlotIndex];

        _slots[_selectedSlotIndex] = targetSlot;
        _slots[hotbarSlotIndex] = sourceSlot;

        if (_activeHotbarSlotIndex == hotbarSlotIndex)
        {
            RefreshHeldItemFromHotbarSelection();
        }
        else if (_activeHotbarSlotIndex == _selectedSlotIndex && _selectedSlotIndex < _hotbarSize)
        {
            _activeHotbarSlotIndex = hotbarSlotIndex;
            RefreshHeldItemFromHotbarSelection();
        }

        _selectedSlotIndex = hotbarSlotIndex;
        RefreshUI();
    }

    private bool InventoryContainsItem(string itemId)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            if (!slot.IsEmpty && slot.item.itemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearHeldItemVisual()
    {
        if (_playerCamera != null)
        {
            List<GameObject> childrenToDestroy = new List<GameObject>();
            for (int i = 0; i < _playerCamera.transform.childCount; i++)
            {
                Transform child = _playerCamera.transform.GetChild(i);
                if (child.name.EndsWith("_InHand"))
                {
                    childrenToDestroy.Add(child.gameObject);
                }
            }

            foreach (GameObject childObj in childrenToDestroy)
            {
                Debug.Log("Destruyendo: " + childObj.name);
                if (Application.isPlaying)
                {
                    Destroy(childObj);
                }
                else
                {
                    DestroyImmediate(childObj);
                }
            }
        }

        _heldItemVisual = null;
        _heldItemId = string.Empty;
    }

    private void SplitSelectedStack()
    {
        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slots.Count)
        {
            return;
        }

        InventorySlot selectedSlot = _slots[_selectedSlotIndex];
        if (selectedSlot.IsEmpty || selectedSlot.amount <= 1)
        {
            return;
        }

        if (selectedSlot.amount % 2 != 0)
        {
            Debug.Log("No se puede dividir el objeto porque la cantidad es impar");
            return;
        }

        int emptySlotIndex = FindFirstEmptySlotIndex();
        if (emptySlotIndex < 0)
        {
            Debug.Log("No hay sitio en el inventario");
            return;
        }

        SaveUndoState();

        int halfAmount = selectedSlot.amount / 2;
        InventorySlot emptySlot = _slots[emptySlotIndex];
        emptySlot.item = selectedSlot.item;
        emptySlot.amount = halfAmount;
        selectedSlot.amount = halfAmount;

        RefreshUI();
    }

    private void DropInventorySlotAmount(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count || amount <= 0 || _playerCamera == null)
        {
            return;
        }

        InventorySlot slot = _slots[slotIndex];
        if (slot.IsEmpty)
        {
            return;
        }

        SaveUndoState();

        int amountToDrop = Mathf.Min(amount, slot.amount);
        InventoryItemDefinition definition = slot.item;
        Vector3 spawnPosition = _playerCamera.transform.position + _playerCamera.transform.forward * 1.5f;
        spawnPosition.y = Mathf.Max(spawnPosition.y, 0.5f);

        CreateDroppedPickup(definition, spawnPosition, amountToDrop);
        TryRemoveFromSlot(slotIndex, amountToDrop);
        Debug.Log("Has soltado " + amountToDrop + " " + definition.displayName);
    }

    private void MergeStacks(int sourceSlotIndex, int targetSlotIndex)
    {
        InventorySlot sourceSlot = _slots[sourceSlotIndex];
        InventorySlot targetSlot = _slots[targetSlotIndex];

        if (sourceSlot.IsEmpty || targetSlot.IsEmpty)
        {
            return;
        }

        if (sourceSlot.item.itemId != targetSlot.item.itemId)
        {
            SwapSlots(sourceSlotIndex, targetSlotIndex);
            return;
        }

        if (targetSlot.amount >= targetSlot.item.maxStack)
        {
            return;
        }

        int movableAmount = Mathf.Min(sourceSlot.amount, targetSlot.item.maxStack - targetSlot.amount);
        if (movableAmount <= 0)
        {
            return;
        }

        SaveUndoState();

        targetSlot.amount += movableAmount;
        sourceSlot.amount -= movableAmount;

        if (sourceSlot.amount <= 0)
        {
            sourceSlot.Clear();
        }

        ValidateHeldItem();
        RefreshUI();
    }

    private void SwapSlots(int firstSlotIndex, int secondSlotIndex)
    {
        SaveUndoState();

        InventorySlot firstSlot = _slots[firstSlotIndex];
        InventorySlot secondSlot = _slots[secondSlotIndex];

        _slots[firstSlotIndex] = secondSlot;
        _slots[secondSlotIndex] = firstSlot;

        if (_activeHotbarSlotIndex == firstSlotIndex)
        {
            _activeHotbarSlotIndex = secondSlotIndex;
        }
        else if (_activeHotbarSlotIndex == secondSlotIndex)
        {
            _activeHotbarSlotIndex = firstSlotIndex;
        }

        if (_selectedSlotIndex == firstSlotIndex)
        {
            _selectedSlotIndex = secondSlotIndex;
        }
        else if (_selectedSlotIndex == secondSlotIndex)
        {
            _selectedSlotIndex = firstSlotIndex;
        }

        RefreshHeldItemFromHotbarSelection();
        RefreshUI();
    }

    private int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    private bool WasDroppedOutsideInventory(PointerEventData eventData)
    {
        if (_inventoryPanel == null)
        {
            return false;
        }

        RectTransform inventoryRect = _inventoryPanel.GetComponent<RectTransform>();
        return !RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, eventData.position, eventData.pressEventCamera);
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

    private void EnsureEditorPreviewUI()
    {
        _defaultFont = _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        InitializeSlots();
        BuildCatalog();
        BuildInventoryUI();
        _selectedSlotIndex = Mathf.Clamp(_selectedSlotIndex, 0, Mathf.Max(0, _inventorySize - 1));
        _activeHotbarSlotIndex = Mathf.Clamp(_activeHotbarSlotIndex, 0, Mathf.Max(0, _hotbarSize - 1));
        SetInventoryOpen(false, true);
    }

    private void QueueEditorPreviewRefresh()
    {
#if UNITY_EDITOR
        if (_editorPreviewRefreshQueued)
        {
            return;
        }

        _editorPreviewRefreshQueued = true;
        EditorApplication.delayCall += RefreshEditorPreviewIfAlive;
#endif
    }

#if UNITY_EDITOR
    private void RefreshEditorPreviewIfAlive()
    {
        _editorPreviewRefreshQueued = false;

        if (this == null || gameObject == null || Application.isPlaying)
        {
            return;
        }

        EnsureEditorPreviewUI();
    }
#endif

    private void EnsureGeneratedUiRoot(RectTransform canvasRect)
    {
        if (_generatedUiRoot == null)
        {
            Transform existingRoot = canvasRect.Find(GeneratedUiRootName);
            _generatedUiRoot = existingRoot != null ? existingRoot.gameObject : null;
        }

        if (_generatedUiRoot != null)
        {
            return;
        }

        _generatedUiRoot = new GameObject(GeneratedUiRootName, typeof(RectTransform));
        _generatedUiRoot.transform.SetParent(canvasRect, false);
        RectTransform rootRect = _generatedUiRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
    }

    private void ClearGeneratedUiRootChildren()
    {
        if (_generatedUiRoot == null)
        {
            return;
        }

        List<GameObject> childrenToDelete = new List<GameObject>();
        for (int i = 0; i < _generatedUiRoot.transform.childCount; i++)
        {
            childrenToDelete.Add(_generatedUiRoot.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < childrenToDelete.Count; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(childrenToDelete[i]);
            }
            else
            {
                DestroyImmediate(childrenToDelete[i]);
            }
        }
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

public class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private SceneInventoryController _inventoryController;
    private int _slotIndex;

    public void Initialize(SceneInventoryController inventoryController, int slotIndex)
    {
        _inventoryController = inventoryController;
        _slotIndex = slotIndex;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _inventoryController?.BeginSlotDrag(_slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _inventoryController?.UpdateSlotDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _inventoryController?.EndSlotDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        _inventoryController?.HandleSlotDrop(_slotIndex);
    }
}
