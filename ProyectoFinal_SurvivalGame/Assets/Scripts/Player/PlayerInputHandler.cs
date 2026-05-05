using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _playerControls;
    private bool _controlsInitialized;
    private bool _interactConsumed;
    private bool _dropConsumed;
    private bool _dropAllConsumed;
    private bool _dropOneConsumed;
    private bool _dropHalfConsumed;

    public Vector2 movementInput { get; private set; }
    public Vector2 rotationInput { get; private set; }
    public bool jumpTriggered { get; private set; }
    public bool sprintTriggered { get; private set; }
    public bool interactTriggered { get; private set; }
    public bool attackTiggered { get; private set; }
    public bool isBuildMode {  get; private set; } = false;
    public bool destroyTriggered { get; private set; }
    public bool slot1Triggered { get; private set; }
    public bool slot2Triggered { get; private set; }
    public bool slot3Triggered { get; private set; }
    public bool slot4Triggered { get; private set; }
    public bool slot5Triggered { get; private set; }
    public bool subdivideTriggered;
    public bool dropTriggered;
    public bool dropAllTriggered { get; private set; }
    public bool dropOneTriggered;
    public bool dropHalfTriggered;
    public bool eatTriggered { get; private set; }
    public bool equipTriggered;
    public bool inventoryTriggered;
    public bool undoTriggered;

    public bool rotateTriggered;
    public bool buildingMenu;


    //Variables de la interfaz
    public bool submitTriggered;
    public bool cancelTriggered;
    public Vector2 mousePosition;
    public bool pauseTriggered;
    public bool ConsumeInventoryToggle()
    {
        if (!inventoryTriggered)
        {
            return false;
        }

        inventoryTriggered = false;
        return true;
    }

    public bool ConsumeInteractTrigger()
    {
        if (!interactTriggered || _interactConsumed)
        {
            return false;
        }

        _interactConsumed = true;
        return true;
    }

    public bool ConsumeDropTrigger()
    {
        return ConsumeHeldTrigger(dropTriggered, ref _dropConsumed);
    }

    public bool ConsumeDropAllTrigger()
    {
        return ConsumeHeldTrigger(dropAllTriggered, ref _dropAllConsumed);
    }

    public bool ConsumeDropOneTrigger()
    {
        return ConsumeHeldTrigger(dropOneTriggered, ref _dropOneConsumed);
    }

    public bool ConsumeDropHalfTrigger()
    {
        return ConsumeHeldTrigger(dropHalfTriggered, ref _dropHalfConsumed);
    }

    private bool ConsumeHeldTrigger(bool isTriggered, ref bool consumed)
    {
        if (!isTriggered || consumed)
        {
            return false;
        }

        consumed = true;
        return true;
    }

    private void Awake()
    {
        InitializeControls();
    }

    public void SwitchActionMap(string mapName)
    {
        _playerControls.Disable();

        if (mapName == "UI")
            _playerControls.PlayerUI.Enable();
        else
            _playerControls.Player.Enable();
    }

    private void InitializeControls()
    {
        if (_controlsInitialized)
        {
            return;
        }

        // Inicializacion de los controles
        _playerControls = new PlayerInput();

        // --- Movimiento ---
        _playerControls.Player.Movement.performed += inputInfo => movementInput = inputInfo.ReadValue<Vector2>();
        _playerControls.Player.Movement.canceled += inputInfo => movementInput = Vector2.zero;

        // --- Rotacion de la camara ---
        _playerControls.Player.Rotation.performed += inputInfo => rotationInput = inputInfo.ReadValue<Vector2>();
        _playerControls.Player.Rotation.canceled += inputInfo => rotationInput = Vector2.zero;

        // --- Salto ---
        _playerControls.Player.Jump.performed += inputInfo => jumpTriggered = true;
        _playerControls.Player.Jump.canceled += inputInfo => jumpTriggered = false;

        // --- Correr ---
        _playerControls.Player.Sprint.performed += inputInfo => sprintTriggered = true;
        _playerControls.Player.Sprint.canceled += inputInfo => sprintTriggered = false;

        // --- Interactuar ---
        _playerControls.Player.Interact.performed += inputInfo =>
        {
            interactTriggered = true;
            _interactConsumed = false;
        };
        _playerControls.Player.Interact.canceled += inputInfo =>
        {
            interactTriggered = false;
            _interactConsumed = false;
        };

        // --- Attack ---
        _playerControls.Player.Attack.performed += inputInfo => attackTiggered = true;
        _playerControls.Player.Attack.canceled += inputInfo => attackTiggered = false;

        // --- Build ---
        _playerControls.Player.Build.performed += inputInfo => isBuildMode = !isBuildMode;

        // --- Provisional Destroy ---
        _playerControls.Player.DestroyProbisional.performed += inputInfo => destroyTriggered = true;
        _playerControls.Player.DestroyProbisional.canceled += inputInfo => destroyTriggered = false;

        // --- Rotate Structures ---
        _playerControls.Player.Rotate.performed += inputInfo => rotateTriggered = true;
        _playerControls.Player.Rotate.canceled += inputInfo => rotateTriggered = false;

        // --- Slot1 Objects ---
        _playerControls.Player.Slot1.performed += inputInfo => slot1Triggered = true;
        _playerControls.Player.Slot1.canceled += inputInfo => slot1Triggered = false;

        // --- Slot2 Objects ---
        _playerControls.Player.Slot2.performed += inputInfo => slot2Triggered = true;
        _playerControls.Player.Slot2.canceled += inputInfo => slot2Triggered = false;

        // --- slot3 Objects ---
        _playerControls.Player.Slot3.performed += inputInfo => slot3Triggered = true;
        _playerControls.Player.Slot3.canceled += inputInfo => slot3Triggered = false;

        // --- Slot4 Objects ---
        _playerControls.Player.Slot4.performed += inputInfo => slot4Triggered = true;
        _playerControls.Player.Slot4.canceled += inputInfo => slot4Triggered = false;

        // --- Slot5 Objects ---
        _playerControls.Player.Slot5.performed += inputInfo => slot5Triggered = true;
        _playerControls.Player.Slot5.canceled += inputInfo => slot5Triggered = false;

        // --- Subdivide Objects ---
        _playerControls.Player.Subdivide.performed += inputInfo => subdivideTriggered = true;
        _playerControls.Player.Subdivide.canceled += inputInfo => subdivideTriggered = false;

        // --- Drop Objects ---
        _playerControls.Player.Drop.performed += inputInfo =>
        {
            dropTriggered = true;
            _dropConsumed = false;
        };
        _playerControls.Player.Drop.canceled += inputInfo =>
        {
            dropTriggered = false;
            _dropConsumed = false;
        };

        // --- DropAll Objects ---
        _playerControls.Player.DropAll.performed += inputInfo =>
        {
            dropAllTriggered = true;
            _dropAllConsumed = false;
        };
        _playerControls.Player.DropAll.canceled += inputInfo =>
        {
            dropAllTriggered = false;
            _dropAllConsumed = false;
        };

        // --- DropOne Objects ---
        _playerControls.Player.DropOne.performed += inputInfo =>
        {
            dropOneTriggered = true;
            _dropOneConsumed = false;
        };
        _playerControls.Player.DropOne.canceled += inputInfo =>
        {
            dropOneTriggered = false;
            _dropOneConsumed = false;
        };

        // --- DropHalf Objects ---
        _playerControls.Player.DropHalf.performed += inputInfo =>
        {
            dropHalfTriggered = true;
            _dropHalfConsumed = false;
        };
        _playerControls.Player.DropHalf.canceled += inputInfo =>
        {
            dropHalfTriggered = false;
            _dropHalfConsumed = false;
        };

        // --- Eat Objects ---
        _playerControls.Player.Eat.performed += inputInfo => eatTriggered = true;
        _playerControls.Player.Eat.canceled += inputInfo => eatTriggered = false;

        // --- Open Building Menu ---
        _playerControls.Player.BuildingMenu.performed += inputInfo => buildingMenu = true;
        _playerControls.Player.BuildingMenu.canceled += inputInfo => buildingMenu = false;

        // --- Open Inventory ---
        _playerControls.Player.OpenInventory.performed += inputInfo => inventoryTriggered = true;
        _playerControls.Player.OpenInventory.canceled += inputInfo => inventoryTriggered = false;

        // --- Undo ---
        _playerControls.Player.Undo.performed += inputInfo => undoTriggered = true;
        _playerControls.Player.Undo.canceled += inputInfo => undoTriggered = false;

        // --- Equip ---
        _playerControls.Player.Equip.performed += inputInfo => equipTriggered = true;
        _playerControls.Player.Equip.canceled += inputInfo => equipTriggered = false;

        // --- UI MAP ---
        // --- Player interact UI ---
        _playerControls.PlayerUI.InteractUI.performed += inputInfo => submitTriggered = true;
        _playerControls.PlayerUI.InteractUI.canceled += inputInfo => submitTriggered = false;

        // --- Player cancel UI ---
        _playerControls.PlayerUI.CancelInteract.performed += inputInfo => cancelTriggered = true;
        _playerControls.PlayerUI.CancelInteract.canceled += inputInfo => cancelTriggered = false;

        // --- Mouse position ---
        _playerControls.PlayerUI.Point.performed += inputInfo => mousePosition = inputInfo.ReadValue<Vector2>();
        _playerControls.PlayerUI.Point.canceled += inputInfo => mousePosition = Vector2.zero;

        // --- Pause Button ---
        _playerControls.PlayerUI.Pause.performed += inputInfo => pauseTriggered = true;
        _playerControls.PlayerUI.Pause.canceled += inputInfo => pauseTriggered = false;
    
        _controlsInitialized = true;
    }

    private void OnEnable()
    {
        InitializeControls();

        if (_playerControls == null)
        {
            return;
        }

        _playerControls.Enable();
    }

    private void OnDisable()
    {
        if (_playerControls == null)
        {
            return;
        }

        _playerControls.Disable();
    }
}
