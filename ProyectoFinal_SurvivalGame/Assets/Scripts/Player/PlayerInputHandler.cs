using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // Referencia a la clase que Unity acaba de generar por ti
    private PlayerInput _playerControls;
    private bool _controlsInitialized;

    // Estas son las variables publicas que el creador del tutorial crea en el minuto 06:55
    // Las dejamos exactamente igual para que su FirstPersonController pueda leerlas
    public Vector2 movementInput { get; private set; }
    public Vector2 rotationInput { get; private set; }
    public bool jumpTriggered { get; private set; }
    public bool sprintTriggered { get; private set; }
    public bool interactTriggered { get; private set; }
    public bool attackTiggered { get; private set; }
    public bool isBuildMode { get; private set; } = false;

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
        _playerControls.Player.Interact.performed += inputInfo => interactTriggered = true;
        _playerControls.Player.Interact.canceled += inputInfo => interactTriggered = false;

        // --- Attack ---
        _playerControls.Player.Attack.performed += inputInfo => attackTiggered = true;
        _playerControls.Player.Attack.canceled += inputInfo => attackTiggered = false;

        // --- Build ---
        _playerControls.Player.Build.performed += inputInfo => isBuildMode = !isBuildMode;

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
