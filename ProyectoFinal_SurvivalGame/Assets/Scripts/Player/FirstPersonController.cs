using UnityEngine;
using static UnityEngine.UI.Image;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movment Speeds")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 2f;

    [Header("Jump")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _gravityMultiplier = 1f;

    [Header("Look Parameters")]
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _upDownLookRange = 80f;

    [Header("References")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private PlayerInputHandler _pInputHandler;

    [Header("Interaction")]
    [SerializeField] private float _raycastDistance;
    private RaycastHit hitInfo;

    private Vector3 _currentMovement;
    private float _verticalRotation;
    private float _currentSpeed => _walkSpeed * (_pInputHandler.sprintTriggered ? _sprintMultiplier : 1f);
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //DrawRaycast();
        HandleMovement();
        HandleRotation();

        if (_pInputHandler.interactTriggered)
        {
            Vector3 origin = _mainCamera.transform.position;
            Vector3 direction = _mainCamera.transform.forward;
            Debug.Log("El jugador ha interactuado");

            if (Physics.Raycast(origin, direction, out hitInfo, _raycastDistance))
            {
               
            }
        }

        if (_pInputHandler.attackTiggered && !_pInputHandler.isBuildMode)
        {
            //Debug.Log("El jugador ha atacado");

            Vector3 origin = _mainCamera.transform.position;
            Vector3 direction = _mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out hitInfo, _raycastDistance))
            {
                IDamagable item = hitInfo.collider.GetComponent<IDamagable>();

                if (item != null)
                {
                    item.DamageRecived(1);
                }
            }
        }
    }

    private Vector3 CalculateWorldDircetion()
    { 
        Vector3 inputDirection = new Vector3(_pInputHandler.movementInput.x, 0, _pInputHandler.movementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    //Funcion para el salto
    private void HandleJumping()
    {
        if (_characterController.isGrounded)
        {
            _currentMovement.y = -0.5f;
            if (_pInputHandler.jumpTriggered)
            {
                _currentMovement.y = _jumpForce;
            }
        }
        else
        {
            _currentMovement.y += Physics.gravity.y * _gravityMultiplier * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDircetion();
        _currentMovement.x = worldDirection.x * _currentSpeed;
        _currentMovement.z = worldDirection.z * _currentSpeed;

        HandleJumping();
        _characterController.Move(_currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        _verticalRotation = Mathf.Clamp(_verticalRotation + rotationAmount, -_upDownLookRange, _upDownLookRange);
        _mainCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseX = _pInputHandler.rotationInput.x * _mouseSensitivity;
        float mouseY = _pInputHandler.rotationInput.y * _mouseSensitivity;
        ApplyHorizontalRotation(mouseX);
        ApplyVerticalRotation(-mouseY);
    }
    private void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
    }

    private void DrawRaycast()
    {
        Vector3 origin = _mainCamera.transform.position;
        Vector3 direction = _mainCamera.transform.forward;
        Debug.DrawRay(origin, direction * _raycastDistance, Color.red);
    }
}
