using UnityEngine;
using UnityEngine.InputSystem;

public class OxygenRecharger : MonoBehaviour
{
    [Header("Recharger Settings")]
    [SerializeField] private float _interactionDistance = 5f;
    [SerializeField] private GameObject _rechargeEffect;
    [SerializeField] private AudioSource _rechargeSound;
    [SerializeField] private bool _showDebugInfo = true;

    [Header("Visual Feedback")]
    [SerializeField] private Color _readyColor = Color.green;
    [SerializeField] private Color _notReadyColor = Color.red;
    [SerializeField] private Renderer _indicatorLight;

    private GameObject _player;
    private PlayerInputHandler _playerInputHandler;
    private bool _canInteract;
    private bool _playerFound;

    private void Start()
    {
        FindPlayer();

        if (_indicatorLight != null)
        {
            _indicatorLight.material.color = _notReadyColor;
        }
    }

    private void FindPlayer()
    {
        _player = GameObject.Find("Player");
        if (_player == null)
        {
            _player = FindFirstObjectByType<CharacterController>()?.gameObject;
        }

        if (_player != null)
        {
            _playerInputHandler = _player.GetComponent<PlayerInputHandler>();
            if (_playerInputHandler == null)
            {
                _playerInputHandler = FindFirstObjectByType<PlayerInputHandler>();
            }
        }

        _playerFound = _player != null;

        if (_showDebugInfo && !_playerFound)
        {
            Debug.LogWarning("OxygenRecharger: No se encontró el jugador llamado 'Player'");
        }
    }

    private void Update()
    {
        CheckPlayerProximity();

        if (_canInteract && _playerInputHandler != null && _playerInputHandler.interactTriggered)
        {
            Interact();
        }
    }

    private void CheckPlayerProximity()
    {
        if (!_playerFound || _player == null)
        {
            FindPlayer();
            _canInteract = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, _player.transform.position);
        _canInteract = distance <= _interactionDistance;

        if (_indicatorLight != null)
        {
            _indicatorLight.material.color = _canInteract ? _readyColor : _notReadyColor;
        }
    }

    private void Interact()
    {
        if (OxygenSystem.Instance == null)
        {
            Debug.LogWarning("OxygenSystem no encontrado. Asegurate de que esta en el jugador.");
            return;
        }

        if (OxygenSystem.Instance.HasOxygenTank)
        {
            OxygenSystem.Instance.RefillOxygen();

            if (_rechargeEffect != null)
            {
                Instantiate(_rechargeEffect, transform.position, Quaternion.identity);
            }

            if (_rechargeSound != null)
            {
                _rechargeSound.Play();
            }

            Debug.Log("Oxigeno recargado en la estacion de recarga");
        }
        else
        {
            Debug.Log("No tienes una bombona para recargar");
        }
    }

    private void OnGUI()
    {
        if (_canInteract)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (screenPos.z > 0)
            {
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 30, 200, 30), "Presiona E para recargar", style);
            }
        }
    }
}
