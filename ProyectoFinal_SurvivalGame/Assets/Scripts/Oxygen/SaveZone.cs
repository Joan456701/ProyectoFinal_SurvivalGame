using UnityEngine;

public class SaveZone : MonoBehaviour
{
    [Header("Save Zone Settings")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private Collider _zoneCollider;
    [SerializeField] private Transform _visualTransform;
    [SerializeField] private float _sizeIncreasePerLevel = 0.5f;

    private int _currentBaseLevel = 1;
    private bool _baseColliderSizeCached;
    private Vector3 _baseVisualScale;
    private float _baseSphereRadius;
    private Vector3 _baseBoxSize;
    private float _baseCapsuleRadius;
    private float _baseCapsuleHeight;

    public int CurrentBaseLevel => _currentBaseLevel;

    private void Start()
    {
        if (_zoneCollider == null)
        {
            _zoneCollider = GetComponent<Collider>();
        }

        if (_visualTransform == null)
        {
            _visualTransform = transform;
        }

        if (_zoneCollider != null)
        {
            _zoneCollider.isTrigger = true;
        }

        CacheBaseColliderSize();
        ApplyBaseLevel(_currentBaseLevel);

        if (_showDebugInfo)
        {
            Debug.Log("SaveZone inicializado. Collider: " + (_zoneCollider != null ? _zoneCollider.name : "null"));
        }
    }

    public void ApplyBaseLevel(int baseLevel)
    {
        _currentBaseLevel = Mathf.Max(1, baseLevel);

        if (_zoneCollider == null)
        {
            _zoneCollider = GetComponent<Collider>();
        }

        if (_visualTransform == null)
        {
            _visualTransform = transform;
        }

        if (_zoneCollider == null)
        {
            Debug.LogWarning("No se puede mejorar la SaveZone porque no tiene Collider.");
            return;
        }

        CacheBaseColliderSize();

        float sizeMultiplier = 1f + ((_currentBaseLevel - 1) * Mathf.Max(0f, _sizeIncreasePerLevel));

        if (_visualTransform != null)
        {
            _visualTransform.localScale = _baseVisualScale * sizeMultiplier;
        }

        bool visualScaleAlreadyAffectsCollider = _visualTransform != null &&
            (_zoneCollider.transform == _visualTransform || _zoneCollider.transform.IsChildOf(_visualTransform));
        float colliderMultiplier = visualScaleAlreadyAffectsCollider ? 1f : sizeMultiplier;

        if (_zoneCollider is SphereCollider sphereCollider)
        {
            sphereCollider.radius = _baseSphereRadius * colliderMultiplier;
        }
        else if (_zoneCollider is BoxCollider boxCollider)
        {
            boxCollider.size = new Vector3(_baseBoxSize.x * colliderMultiplier, _baseBoxSize.y, _baseBoxSize.z * colliderMultiplier);
        }
        else if (_zoneCollider is CapsuleCollider capsuleCollider)
        {
            capsuleCollider.radius = _baseCapsuleRadius * colliderMultiplier;
            capsuleCollider.height = _baseCapsuleHeight * colliderMultiplier;
        }

        if (_showDebugInfo)
        {
            Debug.Log("SaveZone actualizada a nivel " + _currentBaseLevel + ". Multiplicador: " + sizeMultiplier);
        }
    }

    private void CacheBaseColliderSize()
    {
        if (_baseColliderSizeCached || _zoneCollider == null)
        {
            return;
        }

        if (_visualTransform == null)
        {
            _visualTransform = transform;
        }

        _baseVisualScale = _visualTransform != null ? _visualTransform.localScale : Vector3.one;

        if (_zoneCollider is SphereCollider sphereCollider)
        {
            _baseSphereRadius = sphereCollider.radius;
        }
        else if (_zoneCollider is BoxCollider boxCollider)
        {
            _baseBoxSize = boxCollider.size;
        }
        else if (_zoneCollider is CapsuleCollider capsuleCollider)
        {
            _baseCapsuleRadius = capsuleCollider.radius;
            _baseCapsuleHeight = capsuleCollider.height;
        }

        _baseColliderSizeCached = true;
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
