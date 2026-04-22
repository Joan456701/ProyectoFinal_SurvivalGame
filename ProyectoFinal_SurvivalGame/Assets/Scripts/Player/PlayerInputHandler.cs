using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _playerControls;
    public Vector2 movementInput { get; private set; }
    public Vector2 rotationInput { get; private set; }
    public bool jumpTriggered { get; private set; }
    public bool sprintTriggered { get; private set; }
    public bool interactTriggered { get; private set; }
    public bool attackTiggered;
    public bool isBuildMode {  get; private set; } = false;
    public bool destroyTriggered { get; private set; }
    
    public bool rotateTriggered;

    private void Awake()
    {
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
        _playerControls.Player.Interact.performed += inputInfo => interactTriggered = true;
        _playerControls.Player.Interact.canceled += inputInfo => interactTriggered = false;

        // --- Atack ---
        _playerControls.Player.Attack.performed += inputInfo => attackTiggered = true;
        _playerControls.Player.Attack.canceled += inputInfo => attackTiggered = false;

        // --- Build ---
        _playerControls.Player.Build.performed += inputInfo => isBuildMode = !isBuildMode;

        // --- Provisional Destroy ---
        _playerControls.Player.DestroyProbisional.performed += inputInfo => destroyTriggered = true;
        _playerControls.Player.DestroyProbisional.canceled += inputInfo => destroyTriggered = false;

        // --- Rotate Objects ---
        _playerControls.Player.Rotate.performed += inputInfo => rotateTriggered = true;
        _playerControls.Player.Rotate.canceled += inputInfo => rotateTriggered = false;
    }

    private void OnEnable()
    {
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        _playerControls.Disable();
    }
}