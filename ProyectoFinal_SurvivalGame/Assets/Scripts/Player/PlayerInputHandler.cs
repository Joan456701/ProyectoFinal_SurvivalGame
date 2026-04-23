using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _playerControls;
    private bool _controlsInitialized;

    public Vector2 movementInput { get; private set; }
    public Vector2 rotationInput { get; private set; }
    public bool jumpTriggered { get; private set; }
    public bool sprintTriggered { get; private set; }
    public bool interactTriggered { get; private set; }
    public bool attackTiggered { get; private set; }
    public bool isBuildMode {  get; private set; } = false;
    public bool destroyTriggered { get; private set; }
    public bool rotateTriggered { get; private set; }
    public bool slot1Triggered { get; private set; }
    public bool slot2Triggered { get; private set; }
    public bool slot3Triggered { get; private set; }
    public bool slot4Triggered { get; private set; }
    public bool slot5Triggered { get; private set; }
    public bool subdivideTriggered { get; private set; }
    public bool dropTriggered { get; private set; }
    public bool dropAllTriggered { get; private set; }
    public bool dropOneTriggered { get; private set; }
    public bool dropHalfTriggered { get; private set; }
    public bool eatTriggered { get; private set; }
    public bool equipTriggered { get; private set; }
    public bool inventoryTriggered { get; private set; }
    public bool undoTriggered { get; private set; }

    private void Awake()
    {
        InitializeControls();
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
        _playerControls.Player.Interact.canceled += inputInfo => interactTriggered = false;

        // --- Attack ---
        _playerControls.Player.Attack.canceled += inputInfo => attackTiggered = false;

        // --- Build ---
        _playerControls.Player.Build.performed += inputInfo => isBuildMode = !isBuildMode;

        // --- Provisional Destroy ---
        _playerControls.Player.DestroyProbisional.performed += inputInfo => destroyTriggered = true;
        _playerControls.Player.DestroyProbisional.canceled += inputInfo => destroyTriggered = false;

        // --- Rotate Objects ---
        _playerControls.Player.Rotate.canceled += inputInfo => rotateTriggered = false;

        // --- Slot1 Objects ---
        _playerControls.Player.Slot1.canceled += inputInfo => slot1Triggered = false;

        // --- Slot2 Objects ---
        _playerControls.Player.Slot2.canceled += inputInfo => slot2Triggered = false;

        // --- slot3 Objects ---
        _playerControls.Player.Slot3.canceled += inputInfo => slot3Triggered = false;

        // --- Slot4 Objects ---
        _playerControls.Player.Slot4.canceled += inputInfo => slot4Triggered = false;

        // --- Slot5 Objects ---
        _playerControls.Player.Slot5.canceled += inputInfo => slot5Triggered = false;

        // --- Subdivide Objects ---
        _playerControls.Player.Subdivide.canceled += inputInfo => subdivideTriggered = false;

        // --- Drop Objects ---
        _playerControls.Player.Drop.canceled += inputInfo => dropTriggered = false;

        // --- DropAll Objects ---
        _playerControls.Player.DropAll.canceled += inputInfo => dropAllTriggered = false;

        // --- DropOne Objects ---
        _playerControls.Player.DropOne.canceled += inputInfo => dropOneTriggered = false;

        // --- DropHalf Objects ---
        _playerControls.Player.DropHalf.canceled += inputInfo => dropHalfTriggered = false;

        // --- Eat Objects ---
        _playerControls.Player.Eat.canceled += inputInfo => eatTriggered = false;

        _controlsInitialized = true;
    }

    private void Update()
    {
        if (_playerControls != null)
        {
            interactTriggered = _playerControls.Player.Interact.WasPressedThisFrame();
            attackTiggered = _playerControls.Player.Attack.WasPressedThisFrame();
            rotateTriggered = _playerControls.Player.Rotate.WasPressedThisFrame();
            slot1Triggered = _playerControls.Player.Slot1.WasPressedThisFrame();
            slot2Triggered = _playerControls.Player.Slot2.WasPressedThisFrame();
            slot3Triggered = _playerControls.Player.Slot3.WasPressedThisFrame();
            slot4Triggered = _playerControls.Player.Slot4.WasPressedThisFrame();
            slot5Triggered = _playerControls.Player.Slot5.WasPressedThisFrame();
            subdivideTriggered = _playerControls.Player.Subdivide.WasPressedThisFrame();
            dropTriggered = _playerControls.Player.Drop.WasPressedThisFrame();
            dropAllTriggered = _playerControls.Player.DropAll.WasPressedThisFrame();
            dropOneTriggered = _playerControls.Player.DropOne.WasPressedThisFrame();
            dropHalfTriggered = _playerControls.Player.DropHalf.WasPressedThisFrame();
            eatTriggered = _playerControls.Player.Eat.WasPressedThisFrame();
        }

        if (Keyboard.current == null)
        {
            inventoryTriggered = false;
            undoTriggered = false;
            equipTriggered = false;
            return;
        }

        inventoryTriggered = Keyboard.current.iKey.wasPressedThisFrame;
        undoTriggered = (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) && Keyboard.current.zKey.wasPressedThisFrame;
        equipTriggered = Keyboard.current.tKey.wasPressedThisFrame;
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
