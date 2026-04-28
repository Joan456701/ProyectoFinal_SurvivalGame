using UnityEngine;

public class SaveZone : MonoBehaviour
{
    [Header("Save Zone Settings")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private Collider _zoneCollider;

    private void Start()
    {
        if (_zoneCollider == null)
        {
            _zoneCollider = GetComponent<Collider>();
        }

        if (_zoneCollider != null)
        {
            _zoneCollider.isTrigger = true;
        }

        if (_showDebugInfo)
        {
            Debug.Log("SaveZone inicializado. Collider: " + (_zoneCollider != null ? _zoneCollider.name : "null"));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            if (OxygenSystem.Instance != null)
            {
                OxygenSystem.Instance.SetPaused(true);
                Debug.Log("=== ENTRADA A SAVEZONE === Oxigeno pausado");
            }
            else
            {
                Debug.LogWarning("OxygenSystem no encontrado");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            if (OxygenSystem.Instance != null)
            {
                OxygenSystem.Instance.SetPaused(false);
                Debug.Log("=== SALIDA DE SAVEZONE === Oxigeno resumido");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayer(other) && OxygenSystem.Instance != null && !OxygenSystem.Instance.IsPaused)
        {
            OxygenSystem.Instance.SetPaused(true);
            Debug.Log("=== SAVEZONE STAY === Oxigeno pausado");
        }
    }

    private bool IsPlayer(Collider other)
    {
        bool hasCharacterController = other.GetComponent<CharacterController>() != null;
        bool hasPlayerName = other.name.Contains("Player");
        bool isPlayerTag = other.CompareTag("Player");

        if (_showDebugInfo && (hasCharacterController || hasPlayerName))
        {
            Debug.Log("SaveZone detecto: " + other.name + " - CC: " + hasCharacterController + ", Name: " + hasPlayerName + ", Tag: " + isPlayerTag);
        }

        return hasCharacterController || hasPlayerName || isPlayerTag;
    }
}