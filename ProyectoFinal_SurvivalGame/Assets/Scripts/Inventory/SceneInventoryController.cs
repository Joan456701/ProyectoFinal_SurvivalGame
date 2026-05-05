using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Serializable]
    private class RecipeRequirement
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    private class CraftingRecipe
    {
        public string resultItemId;
        public int resultAmount;
        public List<RecipeRequirement> requirements = new List<RecipeRequirement>();
    }

    private sealed class SlotUI
    {
        public int Index;
        public Image Background;
        public Image Icon;
        public Text AmountLabel;
        public Text ShortcutLabel;
    }

    private sealed class CraftingRecipeUI
    {
        public CraftingRecipe Recipe;
        public Text NameLabel;
        public Text RequirementsLabel;
        public Text StatusLabel;
        public Button CreateButton;
        public Image ButtonImage;
    }

    private sealed class TransferSlotUI
    {
        public int Index;
        public Image Background;
        public Image Icon;
        public Text AmountLabel;
        public Text Label;
        public Button Button;
    }

    private enum ChestTransferContext
    {
        None,
        Inventory,
        Chest
    }

    private static SceneInventoryController _instance;
    public static SceneInventoryController Instance => _instance;

    private readonly Stack<InventoryState> _undoStack = new Stack<InventoryState>();
    private const int MaxUndoHistory = 50;

    [Header("Scene References")]
    [SerializeField] private PlayerInputHandler _playerInputHandler;
    [SerializeField] private FirstPersonController _firstPersonController;
    [SerializeField] private FirstPersonBuilder _firstPersonBuilder;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private GameObject _canvasPrefab;

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
    [SerializeField] private Color _craftReadyButtonColor = new Color(0.18f, 0.62f, 0.31f, 1f);
    [SerializeField] private Color _craftDisabledButtonColor = new Color(0.28f, 0.31f, 0.35f, 1f);

    private readonly List<InventorySlot> _slots = new List<InventorySlot>();
    private readonly List<SlotUI> _inventorySlotUIs = new List<SlotUI>();
    private readonly List<SlotUI> _hotbarSlotUIs = new List<SlotUI>();
    private readonly List<CraftingRecipe> _craftingRecipes = new List<CraftingRecipe>();
    private readonly List<CraftingRecipeUI> _craftingRecipeUIs = new List<CraftingRecipeUI>();
    private readonly List<TransferSlotUI> _chestStorageSlotUIs = new List<TransferSlotUI>();
    private readonly List<TransferSlotUI> _chestInventorySlotUIs = new List<TransferSlotUI>();
    private readonly Dictionary<string, InventoryItemDefinition> _itemCatalog = new Dictionary<string, InventoryItemDefinition>();

    private Font _defaultFont;
    private GameObject _inventoryPanel;
    private GameObject _hotbarPanel;
    private GameObject _craftingWindow;
    private GameObject _chestWindow;
    private GameObject _crosshair;
    private Text _detailTitle;
    private Text _detailAmount;
    private Text _detailDescription;
    private Text _pickupPromptText;
    private Text _craftingHintText;
    private Text _chestHintText;
    private GameObject _heldItemVisual;
    private GameObject _generatedUiRoot;
    private Image _dragIcon;
    private bool _inventoryOpen;
    private bool _craftingStationOpen;
    private bool _chestOpen;
    private int _selectedSlotIndex;
    private int _activeHotbarSlotIndex;
    private string _heldItemId = string.Empty;
    private int _draggedSlotIndex = -1;
    private ChestTransferContext _draggedChestTransferContext = ChestTransferContext.None;
    private int _draggedChestTransferSlotIndex = -1;
    private SceneChest _openedChest;
    private bool _isDuplicateController;

    private bool IsAnyMenuOpen()
    {
        return _inventoryOpen || _craftingStationOpen || _chestOpen;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            _isDuplicateController = true;
            enabled = false;
            return;
        }

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

    private void Start()
    {
        if (_isDuplicateController)
        {
            return;
        }

        _defaultFont = _uiFont != null ? _uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildCatalog();
        BuildCraftingRecipes();
        InitializeSlots();
        SeedInventory();

        bool controllerIsOnCanvas = GetComponent<Canvas>() != null && (transform as RectTransform) != null;

        if (controllerIsOnCanvas || _canvasPrefab == null)
        {
            InitializeInventoryUI();
        }
        else
        {
            // Usar prefab del Canvas solo cuando el controlador no vive ya dentro de un Canvas.
            GameObject canvasInstance = Instantiate(_canvasPrefab);
            canvasInstance.name = "Canvas";
            _generatedUiRoot = canvasInstance.transform.gameObject;

            SceneInventoryController clonedController = canvasInstance.GetComponent<SceneInventoryController>();
            if (clonedController != null && clonedController != this)
            {
                clonedController.enabled = false;
            }
        }

        SelectSlot(0);
        _activeHotbarSlotIndex = 0;
        SetInventoryOpen(false, true);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
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
        if (!_craftingStationOpen && !_chestOpen && _playerInputHandler != null && _playerInputHandler.undoTriggered)
        {
            Undo();
        }
    }

    private void HandleEatFoodShortcut()
    {
        if (!_craftingStationOpen && !_chestOpen && _playerInputHandler != null && _playerInputHandler.eatTriggered)
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

        _pickupPromptText.gameObject.SetActive(shouldShow && !IsAnyMenuOpen());
        if (shouldShow)
        {
            _pickupPromptText.text = promptText;
        }
    }

    public void OpenCraftingStation()
    {
        SetCraftingOpen(true, true);
    }

    public void OpenChest(SceneChest chest)
    {
        if (chest == null)
        {
            return;
        }

        chest.EnsureInitialized();
        _openedChest = chest;
        SetChestOpen(true, true);
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
        RegisterItem("wood", "Madera", "Bloque de madera basica. Materia prima para crear herramientas y componentes simples.", new Color(0.54f, 0.35f, 0.18f), 25, PrimitiveType.Cube, new Vector3(1f, 1f, 1f));
        RegisterItem("stick", "Palos", "Palo resistente y ligero. Util para mangos, lanzas y estructuras improvisadas.", new Color(0.64f, 0.49f, 0.24f), 25, PrimitiveType.Cylinder, new Vector3(0.22f, 1.1f, 0.22f));
        RegisterItem("leaf", "Hojas", "Hojas grandes del planeta. Sirven para fabricar cuerda y otros materiales vegetales.", new Color(0.33f, 0.74f, 0.31f), 30, PrimitiveType.Sphere, new Vector3(0.8f, 0.25f, 0.8f));
        RegisterItem("rope", "Cuerda", "Trenzado vegetal util para unir piezas y fabricar herramientas.", new Color(0.77f, 0.68f, 0.39f), 20, PrimitiveType.Capsule, new Vector3(0.35f, 0.75f, 0.35f));
        RegisterItem("pickaxe", "Pico", "Herramienta de recoleccion improvisada creada en la mesa de crafteo.", new Color(0.7f, 0.7f, 0.74f), 1, PrimitiveType.Cube, new Vector3(0.95f, 0.95f, 0.95f));
        RegisterItem("spear", "Lanza", "Arma sencilla de supervivencia montada con materiales del entorno.", new Color(0.82f, 0.74f, 0.61f), 1, PrimitiveType.Cylinder, new Vector3(0.18f, 1.35f, 0.18f));
        RegisterItem("axe", "Hacha", "Herramienta pesada para tala y combate cercano.", new Color(0.56f, 0.56f, 0.6f), 1, PrimitiveType.Cube, new Vector3(0.9f, 0.9f, 0.9f));
        RegisterItem("luminous_resin", "Resina luminosa", "Compuesto organico con brillo natural. Puede servir mas adelante para antorchas y adhesivos.", new Color(0.3f, 0.95f, 1f), 15, PrimitiveType.Sphere, new Vector3(0.55f, 0.55f, 0.55f));
        RegisterItem("purified_water", "Agua purificada", "Suministro basico de supervivencia. Conviene reservarla para expediciones largas.", new Color(0.45f, 0.7f, 1f), 10, PrimitiveType.Cylinder, new Vector3(0.45f, 0.6f, 0.45f));
        RegisterItem("Comida", "Comida", "Comida которую puedes consumir para recuperar hambre.", new Color(1f, 0.68f, 0.28f), 10, PrimitiveType.Sphere, new Vector3(0.5f, 0.5f, 0.5f));
        RegisterItem("Bombona", "Bombona de oxigeno", "Tanque de oxigeno. Se acabara el tiempo, moriras.", new Color(0.2f, 0.8f, 1f), 1, PrimitiveType.Capsule, new Vector3(0.3f, 0.6f, 0.3f));
        RegisterItem("carbon", "Carbon", "Material combustible. Se usa para rellenar la gasolina en la GasoilStation.", new Color(0.25f, 0.25f, 0.25f), 20, PrimitiveType.Cube, new Vector3(0.6f, 0.6f, 0.6f));
    }

    private void BuildCraftingRecipes()
    {
        _craftingRecipes.Clear();
        RegisterCraftingRecipe("stick", 2, ("wood", 1));
        RegisterCraftingRecipe("pickaxe", 1, ("wood", 3), ("rope", 2));
        RegisterCraftingRecipe("spear", 1, ("stick", 2), ("stone", 1), ("rope", 1));
        RegisterCraftingRecipe("axe", 1, ("stick", 2), ("stone", 3), ("rope", 1));
        RegisterCraftingRecipe("rope", 1, ("leaf", 2));
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
        return CanStoreItemAmount(definition, amount, _slots);
    }

    private bool CanStoreItemAmount(InventoryItemDefinition definition, int amount, List<InventorySlot> slotsToEvaluate)
    {
        int availableCapacity = 0;

        for (int i = 0; i < slotsToEvaluate.Count; i++)
        {
            InventorySlot slot = slotsToEvaluate[i];

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
        if (_playerInputHandler == null || !_playerInputHandler.ConsumeInventoryToggle())
        {
            return;
        }

        if (_craftingStationOpen)
        {
            SetCraftingOpen(false);
            return;
        }

        if (_chestOpen)
        {
            SetChestOpen(false);
            return;
        }

        bool newState = !_inventoryOpen;
        SetInventoryOpen(newState);
    }

    private void HandleHotbarShortcuts()
    {
        if (_playerInputHandler == null)
        {
            return;
        }

        if (_craftingStationOpen || _chestOpen)
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
        if (!_inventoryOpen || _craftingStationOpen || _chestOpen || _playerInputHandler == null || !_playerInputHandler.ConsumeDropTrigger())
        {
            return;
        }

        DropSelectedItem();
    }

    private void HandleEquipShortcut()
    {
        if (!_inventoryOpen || _craftingStationOpen || _chestOpen || _playerInputHandler == null || !_playerInputHandler.equipTriggered)
        {
            return;
        }

        EquipSelectedItemInHand();
    }

    private void HandleSplitShortcut()
    {
        if (!_inventoryOpen || _craftingStationOpen || _chestOpen || _playerInputHandler == null || !_playerInputHandler.subdivideTriggered)
        {
            return;
        }

        SplitSelectedStack();
    }

    private void HandleHeldItemDropShortcuts()
    {
        if (IsAnyMenuOpen() || _playerInputHandler == null)
        {
            return;
        }

        if (_playerInputHandler.ConsumeDropAllTrigger())
        {
            DropFromActiveHotbarStack(DropMode.FullStack);
        }

        if (_playerInputHandler.ConsumeDropHalfTrigger())
        {
            DropFromActiveHotbarStack(DropMode.HalfStack);
        }

        if (_playerInputHandler.ConsumeDropOneTrigger())
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
        if (_inventoryOpen)
        {
            _craftingStationOpen = false;
            _chestOpen = false;
            _openedChest = null;
        }

        ApplyMenuState(forceRefresh);
    }

    private void SetCraftingOpen(bool isOpen, bool forceRefresh = false)
    {
        if (!forceRefresh && _craftingStationOpen == isOpen)
        {
            return;
        }

        _craftingStationOpen = isOpen;
        if (_craftingStationOpen)
        {
            _inventoryOpen = false;
            _chestOpen = false;
            _openedChest = null;
        }

        ApplyMenuState(forceRefresh);
    }

    private void SetChestOpen(bool isOpen, bool forceRefresh = false)
    {
        if (!forceRefresh && _chestOpen == isOpen)
        {
            return;
        }

        _chestOpen = isOpen;
        if (_chestOpen)
        {
            _inventoryOpen = false;
            _craftingStationOpen = false;
        }
        else
        {
            _openedChest = null;
        }

        ApplyMenuState(forceRefresh);
    }

    private void ApplyMenuState(bool forceRefresh = false)
    {
        bool anyMenuOpen = IsAnyMenuOpen();

        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetActive(_inventoryOpen);
        }

        if (_craftingWindow != null)
        {
            _craftingWindow.SetActive(_craftingStationOpen);
        }

        if (_chestWindow != null)
        {
            _chestWindow.SetActive(_chestOpen);
        }

        if (_crosshair != null)
        {
            _crosshair.SetActive(!anyMenuOpen);
        }

        SetPickupPrompt(false, string.Empty);

        if (Application.isPlaying && _firstPersonController != null)
        {
            _firstPersonController.enabled = !anyMenuOpen;
        }

        if (Application.isPlaying && _firstPersonBuilder != null)
        {
            _firstPersonBuilder.enabled = !anyMenuOpen;
        }

        if (Application.isPlaying)
        {
            Cursor.lockState = anyMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = anyMenuOpen;
        }

        if (!anyMenuOpen && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (!anyMenuOpen)
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

    private void InitializeInventoryUI()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        _crosshair = transform.Find("Image") != null ? transform.Find("Image").gameObject : null;

        if (!TryBindExistingInventoryUI(canvasRect))
        {
            BuildInventoryUI();
        }
    }

    private bool TryBindExistingInventoryUI(RectTransform canvasRect)
    {
        Transform existingRoot = canvasRect.Find(GeneratedUiRootName);
        if (existingRoot == null || existingRoot.childCount == 0)
        {
            return false;
        }

        _generatedUiRoot = existingRoot.gameObject;
        _inventoryPanel = FindChildRecursive(existingRoot, "InventoryPanel")?.gameObject;
        _hotbarPanel = FindChildRecursive(existingRoot, "HotbarPanel")?.gameObject;
        _craftingWindow = FindChildRecursive(existingRoot, "CraftingWindow")?.gameObject;
        _chestWindow = FindChildRecursive(existingRoot, "ChestWindow")?.gameObject;

        if (_inventoryPanel == null || _hotbarPanel == null)
        {
            return false;
        }

        _pickupPromptText = FindChildRecursive(existingRoot, "PickupPrompt")?.GetComponent<Text>();
        _dragIcon = FindChildRecursive(existingRoot, "DragIcon")?.GetComponent<Image>();
        if (_dragIcon != null)
        {
            _dragIcon.raycastTarget = false;
            _dragIcon.enabled = false;
        }

        _detailTitle = FindChildRecursive(_inventoryPanel.transform, "SelectedItemTitle")?.GetComponent<Text>();
        _detailAmount = FindChildRecursive(_inventoryPanel.transform, "SelectedItemAmount")?.GetComponent<Text>();
        _detailDescription = FindChildRecursive(_inventoryPanel.transform, "SelectedItemDescription")?.GetComponent<Text>();
        _craftingHintText = FindChildRecursive(_craftingWindow != null ? _craftingWindow.transform : existingRoot, "Hint")?.GetComponent<Text>();
        _chestHintText = _chestWindow != null ? FindChildRecursive(_chestWindow.transform, "Hint")?.GetComponent<Text>() : null;

        BindExistingInventorySlots();
        BindExistingCraftingRecipes();
        BindExistingChestSlots();
        BindExistingHotbarSlots();

        return _inventorySlotUIs.Count == _slots.Count && _hotbarSlotUIs.Count == _hotbarSize;
    }

    private void BindExistingInventorySlots()
    {
        _inventorySlotUIs.Clear();

        Transform gridContainer = FindChildRecursive(_inventoryPanel.transform, "GridContainer");
        if (gridContainer == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            Transform slotTransform = FindDirectChild(gridContainer, "Slot_" + i);
            if (slotTransform != null && TryBindSlotUI(slotTransform, i, i < _hotbarSize ? (i + 1).ToString() : string.Empty, out SlotUI slotUI))
            {
                _inventorySlotUIs.Add(slotUI);
            }
        }
    }

    private void BindExistingHotbarSlots()
    {
        _hotbarSlotUIs.Clear();

        if (_hotbarPanel == null)
        {
            return;
        }

        for (int i = 0; i < _hotbarSize; i++)
        {
            Transform slotTransform = FindDirectChild(_hotbarPanel.transform, "Slot_" + i);
            if (slotTransform != null && TryBindSlotUI(slotTransform, i, (i + 1).ToString(), out SlotUI slotUI))
            {
                _hotbarSlotUIs.Add(slotUI);
            }
        }
    }

    private bool TryBindSlotUI(Transform slotTransform, int slotIndex, string shortcut, out SlotUI slotUI)
    {
        slotUI = null;

        Image background = slotTransform.GetComponent<Image>();
        Image icon = FindDirectChild(slotTransform, "Icon")?.GetComponent<Image>();
        Text amountLabel = FindDirectChild(slotTransform, "Amount")?.GetComponent<Text>();
        Text shortcutLabel = FindDirectChild(slotTransform, "Shortcut")?.GetComponent<Text>();

        if (background == null || icon == null || amountLabel == null || shortcutLabel == null)
        {
            return false;
        }

        Button button = slotTransform.GetComponent<Button>();
        if (button == null)
        {
            button = slotTransform.gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveAllListeners();
        int capturedIndex = slotIndex;
        button.onClick.AddListener(delegate { SelectSlot(capturedIndex); });

        InventorySlotDragHandler dragHandler = slotTransform.GetComponent<InventorySlotDragHandler>();
        if (dragHandler == null)
        {
            dragHandler = slotTransform.gameObject.AddComponent<InventorySlotDragHandler>();
        }

        dragHandler.Initialize(this, slotIndex);
        shortcutLabel.text = shortcut;

        slotUI = new SlotUI
        {
            Index = slotIndex,
            Background = background,
            Icon = icon,
            AmountLabel = amountLabel,
            ShortcutLabel = shortcutLabel
        };

        return true;
    }

    private void BindExistingCraftingRecipes()
    {
        _craftingRecipeUIs.Clear();

        if (_craftingWindow == null)
        {
            return;
        }

        Transform recipeContent = FindChildRecursive(_craftingWindow.transform, "RecipeContent");
        if (recipeContent == null)
        {
            return;
        }

        for (int i = 0; i < _craftingRecipes.Count; i++)
        {
            CraftingRecipe recipe = _craftingRecipes[i];
            Transform rowTransform = FindDirectChild(recipeContent, "Recipe_" + recipe.resultItemId);
            if (rowTransform == null)
            {
                continue;
            }

            Button createButton = FindChildRecursive(rowTransform, "CreateButton")?.GetComponent<Button>();
            if (createButton != null)
            {
                createButton.onClick.RemoveAllListeners();
                CraftingRecipe capturedRecipe = recipe;
                createButton.onClick.AddListener(delegate { TryCraftRecipe(capturedRecipe); });
            }

            _craftingRecipeUIs.Add(new CraftingRecipeUI
            {
                Recipe = recipe,
                NameLabel = FindDirectChild(rowTransform, "Name")?.GetComponent<Text>(),
                RequirementsLabel = FindDirectChild(rowTransform, "Requirements")?.GetComponent<Text>(),
                StatusLabel = FindDirectChild(rowTransform, "Status")?.GetComponent<Text>(),
                CreateButton = createButton,
                ButtonImage = createButton != null ? createButton.GetComponent<Image>() : null
            });
        }
    }

    private void BindExistingChestSlots()
    {
        _chestStorageSlotUIs.Clear();
        _chestInventorySlotUIs.Clear();

        if (_chestWindow == null)
        {
            return;
        }

        Transform storageGrid = FindChildRecursive(_chestWindow.transform, "ChestStorageGrid");
        Transform inventoryGrid = FindChildRecursive(_chestWindow.transform, "ChestInventoryGrid");

        for (int i = 0; i < 12; i++)
        {
            Transform slotTransform = storageGrid != null ? FindDirectChild(storageGrid, "TransferSlot_" + i) : null;
            if (slotTransform != null && TryBindTransferSlotUI(slotTransform, i, true, out TransferSlotUI slotUI))
            {
                _chestStorageSlotUIs.Add(slotUI);
            }
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            Transform slotTransform = inventoryGrid != null ? FindDirectChild(inventoryGrid, "TransferSlot_" + i) : null;
            if (slotTransform != null && TryBindTransferSlotUI(slotTransform, i, false, out TransferSlotUI slotUI))
            {
                _chestInventorySlotUIs.Add(slotUI);
            }
        }
    }

    private bool TryBindTransferSlotUI(Transform slotTransform, int slotIndex, bool isChestStorage, out TransferSlotUI slotUI)
    {
        slotUI = null;

        Image background = slotTransform.GetComponent<Image>();
        Image icon = FindDirectChild(slotTransform, "Icon")?.GetComponent<Image>();
        Text amountLabel = FindDirectChild(slotTransform, "Amount")?.GetComponent<Text>();
        Text label = FindDirectChild(slotTransform, "Label")?.GetComponent<Text>();
        Button button = slotTransform.GetComponent<Button>();

        if (background == null || icon == null || amountLabel == null || label == null || button == null)
        {
            return false;
        }

        ChestTransferSlotDragHandler dragHandler = slotTransform.GetComponent<ChestTransferSlotDragHandler>();
        if (dragHandler == null)
        {
            dragHandler = slotTransform.gameObject.AddComponent<ChestTransferSlotDragHandler>();
        }

        dragHandler.Initialize(this, slotIndex, isChestStorage);

        slotUI = new TransferSlotUI
        {
            Index = slotIndex,
            Background = background,
            Icon = icon,
            AmountLabel = amountLabel,
            Label = label,
            Button = button
        };

        return true;
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

        _craftingWindow = CreatePanel("CraftingWindow", _generatedUiRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(860f, 620f), _inventoryPanelColor);

        GameObject craftingHeader = CreateUIObject("CraftingHeader", _craftingWindow.transform);
        RectTransform craftingHeaderRect = craftingHeader.GetComponent<RectTransform>();
        StretchHorizontally(craftingHeaderRect, 24f, -24f, -24f, 78f);
        CreateText("Title", craftingHeader.transform, "Mesa de crafteo", 32, TextAnchor.MiddleLeft, FontStyle.Bold, _titleTextColor);
        _craftingHintText = CreateText("Hint", craftingHeader.transform, "Lista completa de objetos crafteables. El boton Crear solo se activa si tienes todos los materiales.", 18, TextAnchor.LowerLeft, FontStyle.Normal, _hintTextColor);

        RectTransform craftingTitleRect = craftingHeader.transform.GetChild(0).GetComponent<RectTransform>();
        craftingTitleRect.anchorMin = new Vector2(0f, 0.45f);
        craftingTitleRect.anchorMax = new Vector2(1f, 1f);
        craftingTitleRect.offsetMin = Vector2.zero;
        craftingTitleRect.offsetMax = Vector2.zero;

        RectTransform craftingHintRect = craftingHeader.transform.GetChild(1).GetComponent<RectTransform>();
        craftingHintRect.anchorMin = new Vector2(0f, 0f);
        craftingHintRect.anchorMax = new Vector2(1f, 0.45f);
        craftingHintRect.offsetMin = Vector2.zero;
        craftingHintRect.offsetMax = Vector2.zero;

        GameObject craftingBody = CreatePanel("CraftingBody", _craftingWindow.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, _sectionPanelColor);
        RectTransform craftingBodyRect = craftingBody.GetComponent<RectTransform>();
        craftingBodyRect.offsetMin = new Vector2(24f, 24f);
        craftingBodyRect.offsetMax = new Vector2(-24f, -112f);

        GameObject recipeViewport = CreateUIObject("RecipeViewport", craftingBody.transform);
        RectTransform recipeViewportRect = recipeViewport.GetComponent<RectTransform>();
        SetRect(recipeViewportRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 12f), new Vector2(-12f, -12f));
        Image recipeViewportImage = recipeViewport.AddComponent<Image>();
        recipeViewportImage.color = new Color(0f, 0f, 0f, 0.08f);
        Mask recipeMask = recipeViewport.AddComponent<Mask>();
        recipeMask.showMaskGraphic = false;

        GameObject recipeContent = CreateUIObject("RecipeContent", recipeViewport.transform);
        RectTransform recipeContentRect = recipeContent.GetComponent<RectTransform>();
        recipeContentRect.anchorMin = new Vector2(0f, 1f);
        recipeContentRect.anchorMax = new Vector2(1f, 1f);
        recipeContentRect.pivot = new Vector2(0.5f, 1f);
        recipeContentRect.offsetMin = new Vector2(0f, 0f);
        recipeContentRect.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup recipeLayout = recipeContent.AddComponent<VerticalLayoutGroup>();
        recipeLayout.padding = new RectOffset(2, 2, 2, 2);
        recipeLayout.spacing = 10f;
        recipeLayout.childAlignment = TextAnchor.UpperCenter;
        recipeLayout.childControlWidth = true;
        recipeLayout.childControlHeight = false;
        recipeLayout.childForceExpandWidth = true;
        recipeLayout.childForceExpandHeight = false;

        ContentSizeFitter recipeFitter = recipeContent.AddComponent<ContentSizeFitter>();
        recipeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect recipeScroll = _craftingWindow.AddComponent<ScrollRect>();
        recipeScroll.viewport = recipeViewportRect;
        recipeScroll.content = recipeContentRect;
        recipeScroll.horizontal = false;
        recipeScroll.vertical = true;
        recipeScroll.scrollSensitivity = 20f;

        _craftingRecipeUIs.Clear();
        for (int i = 0; i < _craftingRecipes.Count; i++)
        {
            _craftingRecipeUIs.Add(CreateCraftingRecipeUI(recipeContent.transform, _craftingRecipes[i]));
        }

        _chestWindow = CreatePanel("ChestWindow", _generatedUiRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(980f, 620f), _inventoryPanelColor);

        GameObject chestHeader = CreateUIObject("ChestHeader", _chestWindow.transform);
        RectTransform chestHeaderRect = chestHeader.GetComponent<RectTransform>();
        StretchHorizontally(chestHeaderRect, 24f, -24f, -24f, 78f);
        CreateText("Title", chestHeader.transform, "Cofre", 32, TextAnchor.MiddleLeft, FontStyle.Bold, _titleTextColor);
        _chestHintText = CreateText("Hint", chestHeader.transform, "Pulsa en un slot de tu inventario para guardarlo en el cofre. Pulsa en un slot del cofre para llevarlo a tu inventario.", 18, TextAnchor.LowerLeft, FontStyle.Normal, _hintTextColor);

        RectTransform chestTitleRect = chestHeader.transform.GetChild(0).GetComponent<RectTransform>();
        chestTitleRect.anchorMin = new Vector2(0f, 0.45f);
        chestTitleRect.anchorMax = new Vector2(1f, 1f);
        chestTitleRect.offsetMin = Vector2.zero;
        chestTitleRect.offsetMax = Vector2.zero;

        RectTransform chestHintRect = chestHeader.transform.GetChild(1).GetComponent<RectTransform>();
        chestHintRect.anchorMin = new Vector2(0f, 0f);
        chestHintRect.anchorMax = new Vector2(1f, 0.45f);
        chestHintRect.offsetMin = Vector2.zero;
        chestHintRect.offsetMax = Vector2.zero;

        GameObject chestBody = CreateUIObject("ChestBody", _chestWindow.transform);
        RectTransform chestBodyRect = chestBody.GetComponent<RectTransform>();
        chestBodyRect.anchorMin = new Vector2(0f, 0f);
        chestBodyRect.anchorMax = new Vector2(1f, 1f);
        chestBodyRect.offsetMin = new Vector2(24f, 24f);
        chestBodyRect.offsetMax = new Vector2(-24f, -112f);

        GameObject chestStoragePanel = CreatePanel("ChestStoragePanel", chestBody.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, _sectionPanelColor);
        RectTransform chestStorageRect = chestStoragePanel.GetComponent<RectTransform>();
        chestStorageRect.offsetMin = new Vector2(0f, 0f);
        chestStorageRect.offsetMax = new Vector2(-10f, 0f);
        CreateText("StorageTitle", chestStoragePanel.transform, "Contenido del cofre", 24, TextAnchor.UpperLeft, FontStyle.Bold, _titleTextColor);
        SetRect(chestStoragePanel.transform.GetChild(0).GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -44f), new Vector2(-18f, -10f));

        GameObject chestStorageGrid = CreateUIObject("ChestStorageGrid", chestStoragePanel.transform);
        RectTransform chestStorageGridRect = chestStorageGrid.GetComponent<RectTransform>();
        SetRect(chestStorageGridRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -62f));
        GridLayoutGroup chestStorageLayout = chestStorageGrid.AddComponent<GridLayoutGroup>();
        chestStorageLayout.cellSize = new Vector2(96f, 96f);
        chestStorageLayout.spacing = new Vector2(10f, 10f);
        chestStorageLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        chestStorageLayout.constraintCount = 4;
        chestStorageLayout.childAlignment = TextAnchor.UpperLeft;

        GameObject chestInventoryPanel = CreatePanel("ChestInventoryPanel", chestBody.transform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, _sectionPanelColor);
        RectTransform chestInventoryRect = chestInventoryPanel.GetComponent<RectTransform>();
        chestInventoryRect.offsetMin = new Vector2(10f, 0f);
        chestInventoryRect.offsetMax = new Vector2(0f, 0f);
        CreateText("InventoryTitle", chestInventoryPanel.transform, "Tu inventario", 24, TextAnchor.UpperLeft, FontStyle.Bold, _titleTextColor);
        SetRect(chestInventoryPanel.transform.GetChild(0).GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -44f), new Vector2(-18f, -10f));

        GameObject chestInventoryGrid = CreateUIObject("ChestInventoryGrid", chestInventoryPanel.transform);
        RectTransform chestInventoryGridRect = chestInventoryGrid.GetComponent<RectTransform>();
        SetRect(chestInventoryGridRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 18f), new Vector2(-18f, -62f));
        GridLayoutGroup chestInventoryLayout = chestInventoryGrid.AddComponent<GridLayoutGroup>();
        chestInventoryLayout.cellSize = new Vector2(96f, 96f);
        chestInventoryLayout.spacing = new Vector2(10f, 10f);
        chestInventoryLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        chestInventoryLayout.constraintCount = 4;
        chestInventoryLayout.childAlignment = TextAnchor.UpperLeft;

        _chestStorageSlotUIs.Clear();
        for (int i = 0; i < 12; i++)
        {
            _chestStorageSlotUIs.Add(CreateTransferSlotUI(chestStorageGrid.transform, i, true));
        }

        _chestInventorySlotUIs.Clear();
        for (int i = 0; i < _slots.Count; i++)
        {
            _chestInventorySlotUIs.Add(CreateTransferSlotUI(chestInventoryGrid.transform, i, false));
        }

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

    private TransferSlotUI CreateTransferSlotUI(Transform parent, int slotIndex, bool isChestStorage)
    {
        GameObject slotRoot = CreateUIObject("TransferSlot_" + slotIndex, parent);
        RectTransform slotRect = slotRoot.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(96f, 96f);

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

        ChestTransferSlotDragHandler dragHandler = slotRoot.AddComponent<ChestTransferSlotDragHandler>();
        dragHandler.Initialize(this, slotIndex, isChestStorage);

        GameObject iconObject = CreateUIObject("Icon", slotRoot.transform);
        Image icon = iconObject.AddComponent<Image>();
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = new Vector2(14f, 14f);
        iconRect.offsetMax = new Vector2(-14f, -26f);
        icon.enabled = false;

        Text amountLabel = CreateText("Amount", slotRoot.transform, string.Empty, 18, TextAnchor.LowerRight, FontStyle.Bold, _titleTextColor);
        SetRect(amountLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(8f, 4f), new Vector2(-8f, 24f));

        Text slotLabel = CreateText("Label", slotRoot.transform, (slotIndex + 1).ToString(), 14, TextAnchor.UpperLeft, FontStyle.Bold, _shortcutTextColor);
        SetRect(slotLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -22f), new Vector2(28f, -6f));

        return new TransferSlotUI
        {
            Index = slotIndex,
            Background = background,
            Icon = icon,
            AmountLabel = amountLabel,
            Label = slotLabel,
            Button = button
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

        RefreshCraftingPanel();
        RefreshChestPanel();
        RefreshDetailsPanel();
    }

    private void UpdateSlotVisual(SlotUI slotUI)
    {
        InventorySlot slot = _slots[slotUI.Index];
        bool isSelected = slotUI.Index == _selectedSlotIndex;
        bool isActiveHotbarSlot = slotUI.Index == _activeHotbarSlotIndex && slotUI.Index < _hotbarSize;

        if (slotUI.Background != null)
        {
            slotUI.Background.color = isSelected || isActiveHotbarSlot ? _selectedSlotColor : _slotColor;
        }

        if (slot.IsEmpty)
        {
            if (slotUI.Icon != null)
            {
                slotUI.Icon.enabled = false;
            }

            if (slotUI.AmountLabel != null)
            {
                slotUI.AmountLabel.text = string.Empty;
            }

            return;
        }

        if (slotUI.Icon != null)
        {
            slotUI.Icon.enabled = true;
            slotUI.Icon.color = slot.item.color;
        }

        if (slotUI.AmountLabel != null)
        {
            slotUI.AmountLabel.text = slot.amount > 1 ? slot.amount.ToString() : string.Empty;
        }
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
            GameObject go = childrenToDelete[i];

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform directChild = FindDirectChild(parent, childName);
        if (directChild != null)
        {
            return directChild;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindChildRecursive(parent.GetChild(i), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
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

    private void RegisterCraftingRecipe(string resultItemId, int resultAmount, params (string itemId, int amount)[] requirements)
    {
        CraftingRecipe recipe = new CraftingRecipe
        {
            resultItemId = resultItemId,
            resultAmount = resultAmount
        };

        for (int i = 0; i < requirements.Length; i++)
        {
            recipe.requirements.Add(new RecipeRequirement
            {
                itemId = requirements[i].itemId,
                amount = requirements[i].amount
            });
        }

        _craftingRecipes.Add(recipe);
    }

    private CraftingRecipeUI CreateCraftingRecipeUI(Transform parent, CraftingRecipe recipe)
    {
        GameObject rowRoot = CreatePanel("Recipe_" + recipe.resultItemId, parent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 118f), new Color(1f, 1f, 1f, 0.05f));
        LayoutElement rowLayout = rowRoot.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 118f;

        Text nameLabel = CreateText("Name", rowRoot.transform, GetItemDisplayName(recipe.resultItemId), 22, TextAnchor.UpperLeft, FontStyle.Bold, _titleTextColor);
        SetRect(nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -38f), new Vector2(-136f, -10f));

        Text requirementsLabel = CreateText("Requirements", rowRoot.transform, string.Empty, 16, TextAnchor.UpperLeft, FontStyle.Normal, _bodyTextColor);
        requirementsLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        requirementsLabel.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(requirementsLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(14f, 12f), new Vector2(-136f, -40f));

        Text statusLabel = CreateText("Status", rowRoot.transform, string.Empty, 15, TextAnchor.LowerLeft, FontStyle.Bold, _hintTextColor);
        SetRect(statusLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 10f), new Vector2(-136f, 30f));

        Button createButton = CreateButton("CreateButton", rowRoot.transform, "Crear");
        RectTransform buttonRect = createButton.GetComponent<RectTransform>();
        SetRect(buttonRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-108f, -24f), new Vector2(-14f, 24f));

        createButton.onClick.AddListener(delegate { TryCraftRecipe(recipe); });

        return new CraftingRecipeUI
        {
            Recipe = recipe,
            NameLabel = nameLabel,
            RequirementsLabel = requirementsLabel,
            StatusLabel = statusLabel,
            CreateButton = createButton,
            ButtonImage = createButton.GetComponent<Image>()
        };
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = _craftDisabledButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.disabledColor = Color.white;
        button.colors = colors;

        Text buttonLabel = CreateText("Label", buttonObject.transform, label, 18, TextAnchor.MiddleCenter, FontStyle.Bold, _titleTextColor);
        SetRect(buttonLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        return button;
    }

    private void RefreshCraftingPanel()
    {
        if (_craftingWindow == null)
        {
            return;
        }

        _craftingWindow.SetActive(_craftingStationOpen);

        if (_craftingHintText != null)
        {
            _craftingHintText.text = "Lista completa de objetos crafteables. I cierra la mesa de crafteo. Crear solo se activa si tienes todos los materiales.";
        }

        for (int i = 0; i < _craftingRecipeUIs.Count; i++)
        {
            CraftingRecipeUI recipeUI = _craftingRecipeUIs[i];
            bool canCraft = _craftingStationOpen && CanCraftRecipe(recipeUI.Recipe);

            if (recipeUI.RequirementsLabel != null)
            {
                recipeUI.RequirementsLabel.text = BuildRecipeRequirementText(recipeUI.Recipe);
            }

            if (recipeUI.StatusLabel != null)
            {
                recipeUI.StatusLabel.text = canCraft ? "Materiales completos" : "Te faltan materiales";
                recipeUI.StatusLabel.color = canCraft ? _craftReadyButtonColor : _hintTextColor;
            }

            if (recipeUI.CreateButton != null)
            {
                recipeUI.CreateButton.interactable = canCraft;
            }

            if (recipeUI.ButtonImage != null)
            {
                recipeUI.ButtonImage.color = canCraft ? _craftReadyButtonColor : _craftDisabledButtonColor;
            }
        }
    }

    private void RefreshChestPanel()
    {
        if (_chestWindow == null)
        {
            return;
        }

        _chestWindow.SetActive(_chestOpen);

        if (_chestHintText != null)
        {
            _chestHintText.text = _openedChest == null
                ? "No hay ningun cofre abierto."
                : "Pulsa en un slot de tu inventario para guardarlo en el cofre. Pulsa en un slot del cofre para recuperarlo.";
        }

        for (int i = 0; i < _chestInventorySlotUIs.Count; i++)
        {
            UpdateTransferSlotVisual(_chestInventorySlotUIs[i], i < _slots.Count ? _slots[i].item : null, i < _slots.Count ? _slots[i].amount : 0);
        }

        for (int i = 0; i < _chestStorageSlotUIs.Count; i++)
        {
            if (_openedChest == null || i >= _openedChest.SlotCount)
            {
                UpdateTransferSlotVisual(_chestStorageSlotUIs[i], null, 0);
                continue;
            }

            SceneChest.ChestSlotData chestSlot = _openedChest.GetSlot(i);
            if (chestSlot == null || chestSlot.IsEmpty || !_itemCatalog.TryGetValue(chestSlot.itemId, out InventoryItemDefinition definition))
            {
                UpdateTransferSlotVisual(_chestStorageSlotUIs[i], null, 0);
                continue;
            }

            UpdateTransferSlotVisual(_chestStorageSlotUIs[i], definition, chestSlot.amount);
        }
    }

    private void UpdateTransferSlotVisual(TransferSlotUI slotUI, InventoryItemDefinition definition, int amount)
    {
        if (slotUI == null)
        {
            return;
        }

        bool hasItem = definition != null && amount > 0;
        if (slotUI.Background != null)
        {
            slotUI.Background.color = hasItem ? _selectedSlotColor : _slotColor;
        }

        if (slotUI.Icon != null)
        {
            slotUI.Icon.enabled = hasItem;
        }

        if (slotUI.AmountLabel != null)
        {
            slotUI.AmountLabel.text = hasItem && amount > 1 ? amount.ToString() : string.Empty;
        }

        if (hasItem && slotUI.Icon != null)
        {
            slotUI.Icon.color = definition.color;
        }
    }

    public void BeginChestTransferDrag(bool fromChestStorage, int slotIndex)
    {
        if (!_chestOpen || _openedChest == null)
        {
            return;
        }

        InventoryItemDefinition definition = null;

        if (fromChestStorage)
        {
            if (slotIndex < 0 || slotIndex >= _openedChest.SlotCount)
            {
                return;
            }

            SceneChest.ChestSlotData chestSlot = _openedChest.GetSlot(slotIndex);
            if (chestSlot == null || chestSlot.IsEmpty || !_itemCatalog.TryGetValue(chestSlot.itemId, out definition))
            {
                return;
            }

            _draggedChestTransferContext = ChestTransferContext.Chest;
        }
        else
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                return;
            }

            InventorySlot inventorySlot = _slots[slotIndex];
            if (inventorySlot.IsEmpty)
            {
                return;
            }

            definition = inventorySlot.item;
            _draggedChestTransferContext = ChestTransferContext.Inventory;
        }

        _draggedChestTransferSlotIndex = slotIndex;

        if (_dragIcon != null && definition != null)
        {
            _dragIcon.enabled = true;
            _dragIcon.color = definition.color;
            _dragIcon.transform.SetAsLastSibling();
        }
    }

    public void HandleChestTransferDrop(bool targetIsChestStorage, int targetSlotIndex)
    {
        if (!_chestOpen || _openedChest == null || _draggedChestTransferContext == ChestTransferContext.None)
        {
            EndChestTransferDrag();
            return;
        }

        ChestTransferContext targetContext = targetIsChestStorage ? ChestTransferContext.Chest : ChestTransferContext.Inventory;

        if (_draggedChestTransferContext == targetContext && _draggedChestTransferSlotIndex == targetSlotIndex)
        {
            EndChestTransferDrag();
            return;
        }

        if (_draggedChestTransferContext == ChestTransferContext.Inventory && targetContext == ChestTransferContext.Inventory)
        {
            MergeStacks(_draggedChestTransferSlotIndex, targetSlotIndex);
        }
        else if (_draggedChestTransferContext == ChestTransferContext.Chest && targetContext == ChestTransferContext.Chest)
        {
            MergeChestStacks(_draggedChestTransferSlotIndex, targetSlotIndex);
        }
        else if (_draggedChestTransferContext == ChestTransferContext.Inventory && targetContext == ChestTransferContext.Chest)
        {
            MoveInventoryToChestTarget(_draggedChestTransferSlotIndex, targetSlotIndex);
        }
        else if (_draggedChestTransferContext == ChestTransferContext.Chest && targetContext == ChestTransferContext.Inventory)
        {
            MoveChestToInventoryTarget(_draggedChestTransferSlotIndex, targetSlotIndex);
        }

        EndChestTransferDrag();
    }

    public void EndChestTransferDrag()
    {
        _draggedChestTransferContext = ChestTransferContext.None;
        _draggedChestTransferSlotIndex = -1;

        if (_dragIcon != null)
        {
            _dragIcon.enabled = false;
        }
    }

    private void MoveInventoryToChestTarget(int inventorySlotIndex, int chestSlotIndex)
    {
        if (_openedChest == null || inventorySlotIndex < 0 || inventorySlotIndex >= _slots.Count || chestSlotIndex < 0 || chestSlotIndex >= _openedChest.SlotCount)
        {
            return;
        }

        InventorySlot inventorySlot = _slots[inventorySlotIndex];
        SceneChest.ChestSlotData chestSlot = _openedChest.GetSlot(chestSlotIndex);
        if (inventorySlot.IsEmpty || chestSlot == null)
        {
            return;
        }

        InventoryItemDefinition sourceDefinition = inventorySlot.item;

        if (chestSlot.IsEmpty)
        {
            SaveUndoState();
            chestSlot.itemId = sourceDefinition.itemId;
            chestSlot.amount = inventorySlot.amount;
            inventorySlot.Clear();
        }
        else if (chestSlot.itemId == sourceDefinition.itemId)
        {
            int movableAmount = Mathf.Min(inventorySlot.amount, sourceDefinition.maxStack - chestSlot.amount);
            if (movableAmount <= 0)
            {
                return;
            }

            SaveUndoState();
            chestSlot.amount += movableAmount;
            inventorySlot.amount -= movableAmount;
            if (inventorySlot.amount <= 0)
            {
                inventorySlot.Clear();
            }
        }
        else if (_itemCatalog.TryGetValue(chestSlot.itemId, out InventoryItemDefinition chestDefinition))
        {
            SaveUndoState();
            string previousItemId = chestSlot.itemId;
            int previousAmount = chestSlot.amount;

            chestSlot.itemId = sourceDefinition.itemId;
            chestSlot.amount = inventorySlot.amount;
            inventorySlot.item = chestDefinition;
            inventorySlot.amount = previousAmount;
        }

        ValidateHeldItem();
        RefreshHeldItemFromHotbarSelection();
        RefreshUI();
    }

    private void MoveChestToInventoryTarget(int chestSlotIndex, int inventorySlotIndex)
    {
        if (_openedChest == null || chestSlotIndex < 0 || chestSlotIndex >= _openedChest.SlotCount || inventorySlotIndex < 0 || inventorySlotIndex >= _slots.Count)
        {
            return;
        }

        SceneChest.ChestSlotData chestSlot = _openedChest.GetSlot(chestSlotIndex);
        InventorySlot inventorySlot = _slots[inventorySlotIndex];
        if (chestSlot == null || chestSlot.IsEmpty || !_itemCatalog.TryGetValue(chestSlot.itemId, out InventoryItemDefinition chestDefinition))
        {
            return;
        }

        if (inventorySlot.IsEmpty)
        {
            SaveUndoState();
            inventorySlot.item = chestDefinition;
            inventorySlot.amount = chestSlot.amount;
            chestSlot.Clear();
        }
        else if (inventorySlot.item.itemId == chestDefinition.itemId)
        {
            int movableAmount = Mathf.Min(chestSlot.amount, inventorySlot.item.maxStack - inventorySlot.amount);
            if (movableAmount <= 0)
            {
                return;
            }

            SaveUndoState();
            inventorySlot.amount += movableAmount;
            chestSlot.amount -= movableAmount;
            if (chestSlot.amount <= 0)
            {
                chestSlot.Clear();
            }
        }
        else
        {
            SaveUndoState();
            string previousChestItemId = chestSlot.itemId;
            int previousChestAmount = chestSlot.amount;

            chestSlot.itemId = inventorySlot.item.itemId;
            chestSlot.amount = inventorySlot.amount;
            inventorySlot.item = chestDefinition;
            inventorySlot.amount = previousChestAmount;
        }

        ValidateHeldItem();
        RefreshHeldItemFromHotbarSelection();
        RefreshUI();
    }

    private void MergeChestStacks(int sourceSlotIndex, int targetSlotIndex)
    {
        if (_openedChest == null || sourceSlotIndex < 0 || targetSlotIndex < 0 || sourceSlotIndex >= _openedChest.SlotCount || targetSlotIndex >= _openedChest.SlotCount)
        {
            return;
        }

        SceneChest.ChestSlotData sourceSlot = _openedChest.GetSlot(sourceSlotIndex);
        SceneChest.ChestSlotData targetSlot = _openedChest.GetSlot(targetSlotIndex);

        if (sourceSlot == null || targetSlot == null || sourceSlot.IsEmpty)
        {
            return;
        }

        if (targetSlot.IsEmpty)
        {
            SaveUndoState();
            targetSlot.itemId = sourceSlot.itemId;
            targetSlot.amount = sourceSlot.amount;
            sourceSlot.Clear();
            RefreshUI();
            return;
        }

        if (!_itemCatalog.TryGetValue(sourceSlot.itemId, out InventoryItemDefinition sourceDefinition))
        {
            return;
        }

        if (sourceSlot.itemId != targetSlot.itemId)
        {
            SaveUndoState();
            string previousItemId = targetSlot.itemId;
            int previousAmount = targetSlot.amount;
            targetSlot.itemId = sourceSlot.itemId;
            targetSlot.amount = sourceSlot.amount;
            sourceSlot.itemId = previousItemId;
            sourceSlot.amount = previousAmount;
            RefreshUI();
            return;
        }

        int movableAmount = Mathf.Min(sourceSlot.amount, sourceDefinition.maxStack - targetSlot.amount);
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

        RefreshUI();
    }

    private string BuildRecipeRequirementText(CraftingRecipe recipe)
    {
        List<string> parts = new List<string>();

        for (int i = 0; i < recipe.requirements.Count; i++)
        {
            RecipeRequirement requirement = recipe.requirements[i];
            int currentAmount = GetTotalItemAmount(requirement.itemId);
            parts.Add(GetItemDisplayName(requirement.itemId) + ": " + currentAmount + "/" + requirement.amount);
        }

        return string.Join("  |  ", parts);
    }

    private bool CanCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || !_itemCatalog.TryGetValue(recipe.resultItemId, out InventoryItemDefinition resultDefinition))
        {
            return false;
        }

        List<InventorySlot> simulatedSlots = CloneSlots();

        for (int i = 0; i < recipe.requirements.Count; i++)
        {
            RecipeRequirement requirement = recipe.requirements[i];
            int remainingAmount = requirement.amount;

            for (int slotIndex = 0; slotIndex < simulatedSlots.Count; slotIndex++)
            {
                InventorySlot slot = simulatedSlots[slotIndex];
                if (slot.IsEmpty || slot.item.itemId != requirement.itemId)
                {
                    continue;
                }

                int amountToRemove = Mathf.Min(slot.amount, remainingAmount);
                slot.amount -= amountToRemove;
                remainingAmount -= amountToRemove;

                if (slot.amount <= 0)
                {
                    slot.Clear();
                }

                if (remainingAmount <= 0)
                {
                    break;
                }
            }

            if (remainingAmount > 0)
            {
                return false;
            }
        }

        return CanStoreItemAmount(resultDefinition, recipe.resultAmount, simulatedSlots);
    }

    private void TryCraftRecipe(CraftingRecipe recipe)
    {
        if (!_craftingStationOpen || recipe == null || !CanCraftRecipe(recipe))
        {
            return;
        }

        SaveUndoState();

        for (int i = 0; i < recipe.requirements.Count; i++)
        {
            RemoveItemAmount(recipe.requirements[i].itemId, recipe.requirements[i].amount);
        }

        ValidateHeldItem();
        TryAddItem(recipe.resultItemId, recipe.resultAmount, false);
        Debug.Log("Has creado " + GetItemDisplayName(recipe.resultItemId));
        RefreshHeldItemFromHotbarSelection();
        RefreshUI();
    }

    private bool RemoveItemAmount(string itemId, int amount)
    {
        int remainingAmount = amount;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            if (slot.IsEmpty || slot.item.itemId != itemId)
            {
                continue;
            }

            int amountToRemove = Mathf.Min(slot.amount, remainingAmount);
            slot.amount -= amountToRemove;
            remainingAmount -= amountToRemove;

            if (slot.amount <= 0)
            {
                slot.Clear();
            }

            if (remainingAmount <= 0)
            {
                return true;
            }
        }

        return false;
    }

    public int GetTotalItemAmount(string itemId)
    {
        int totalAmount = 0;

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            if (!slot.IsEmpty && slot.item.itemId == itemId)
            {
                totalAmount += slot.amount;
            }
        }

        return totalAmount;
    }

    private string GetItemDisplayName(string itemId)
    {
        return _itemCatalog.TryGetValue(itemId, out InventoryItemDefinition definition) ? definition.displayName : itemId;
    }

    public bool HasItem(string itemId, int amount)
    {
        return GetTotalItemAmount(itemId) >= amount;
    }

    public bool ConsumeItem(string itemId, int amount)
    {
        return ConsumeItems((itemId, amount));
    }

    public bool ConsumeItems(params (string itemId, int amount)[] items)
    {
        if (items == null || items.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(items[i].itemId) || items[i].amount <= 0 || GetTotalItemAmount(items[i].itemId) < items[i].amount)
            {
                return false;
            }
        }

        SaveUndoState();

        for (int i = 0; i < items.Length; i++)
        {
            RemoveItemAmount(items[i].itemId, items[i].amount);
        }

        ValidateHeldItem();
        RefreshHeldItemFromHotbarSelection();
        RefreshUI();
        return true;
    }

    private List<InventorySlot> CloneSlots()
    {
        List<InventorySlot> simulatedSlots = new List<InventorySlot>(_slots.Count);

        for (int i = 0; i < _slots.Count; i++)
        {
            InventorySlot slot = _slots[i];
            simulatedSlots.Add(new InventorySlot
            {
                item = slot.item,
                amount = slot.amount
            });
        }

        return simulatedSlots;
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

public class ChestTransferSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private SceneInventoryController _inventoryController;
    private int _slotIndex;
    private bool _isChestStorage;

    public void Initialize(SceneInventoryController inventoryController, int slotIndex, bool isChestStorage)
    {
        _inventoryController = inventoryController;
        _slotIndex = slotIndex;
        _isChestStorage = isChestStorage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _inventoryController?.BeginChestTransferDrag(_isChestStorage, _slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _inventoryController?.UpdateSlotDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _inventoryController?.EndChestTransferDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        _inventoryController?.HandleChestTransferDrop(_isChestStorage, _slotIndex);
    }
}
