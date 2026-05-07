using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ShipStatusSystem : MonoBehaviour, IDamagable
{
    private const string ShipScreenObjectName = "PantallaNave";

    [Header("Ship Health")]
    [SerializeField] private float _maxShipHealth = 100f;
    [SerializeField] private float _currentShipHealth = 100f;

    [Header("Ship Energy")]
    [SerializeField] private float _maxShipEnergy = 100f;
    [SerializeField] private float _currentShipEnergy = 100f;
    [SerializeField] private float _fullEnergyDurationSeconds = 100f;
    [SerializeField] private bool _drainEnergyOverTime = true;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = true;

    private static ShipStatusSystem _instance;
    private bool _energyEmptyLogSent;

    public static ShipStatusSystem Instance => _instance;

    public float CurrentShipHealth => _currentShipHealth;
    public float MaxShipHealth => _maxShipHealth;
    public float CurrentShipEnergy => _currentShipEnergy;
    public float MaxShipEnergy => _maxShipEnergy;
    public bool HasEnergy => _currentShipEnergy > 0f;

    public static ShipStatusSystem GetOrCreate()
    {
        if (_instance != null)
        {
            return _instance;
        }

        ShipStatusSystem existingSystem = FindFirstObjectByType<ShipStatusSystem>();
        if (existingSystem != null)
        {
            _instance = existingSystem;
            return _instance;
        }

        GameObject shipScreenObject = GameObject.Find(ShipScreenObjectName);
        if (shipScreenObject != null)
        {
            _instance = shipScreenObject.AddComponent<ShipStatusSystem>();
            return _instance;
        }

        GameObject fallbackObject = new GameObject("ShipStatusSystem");
        _instance = fallbackObject.AddComponent<ShipStatusSystem>();
        Debug.LogWarning("No se encontro PantallaNave. Se ha creado ShipStatusSystem sin pantalla de nave.");
        return _instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            enabled = false;
            return;
        }

        ClampValues();
        EnsureScreenUI();
    }

    private void Start()
    {
        ClampValues();
        EnsureScreenUI();
    }

    private void Update()
    {
        if (!Application.isPlaying || !_drainEnergyOverTime || _currentShipEnergy <= 0f)
        {
            return;
        }

        float energyConsumedPerSecond = _maxShipEnergy / Mathf.Max(1f, _fullEnergyDurationSeconds);
        _currentShipEnergy = Mathf.Max(0f, _currentShipEnergy - energyConsumedPerSecond * Time.deltaTime);

        if (_currentShipEnergy <= 0f && !_energyEmptyLogSent)
        {
            _energyEmptyLogSent = true;
            if (_showDebugLogs)
            {
                Debug.Log("La nave se ha quedado sin energia");
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ClampValues();
        EnsureScreenUI();
    }
#endif

    public void DamageRecived(int damage)
    {
        DamageShip(damage);
    }

    public void DamageShip(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentShipHealth = Mathf.Max(0f, _currentShipHealth - amount);
        if (_showDebugLogs)
        {
            Debug.Log("Daño recibido en la nave: -" + Mathf.RoundToInt(amount) + " de vida. Vida nave actual: " + Mathf.RoundToInt(_currentShipHealth) + "/" + Mathf.RoundToInt(_maxShipHealth));
        }

        if (_currentShipHealth <= 0f && _showDebugLogs)
        {
            Debug.Log("La nave ha quedado destruida");
        }
    }

    public void RepairShip(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentShipHealth = Mathf.Min(_maxShipHealth, _currentShipHealth + amount);
        if (_showDebugLogs)
        {
            Debug.Log("Vida de la nave reparada: " + Mathf.RoundToInt(_currentShipHealth) + "/" + Mathf.RoundToInt(_maxShipHealth));
        }
    }

    public void ConsumeEnergy(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentShipEnergy = Mathf.Max(0f, _currentShipEnergy - amount);
        _energyEmptyLogSent = _currentShipEnergy <= 0f;
        if (_showDebugLogs)
        {
            Debug.Log("Energia de la nave consumida: " + Mathf.RoundToInt(_currentShipEnergy) + "/" + Mathf.RoundToInt(_maxShipEnergy));
        }
    }

    public void RefillEnergy()
    {
        _currentShipEnergy = _maxShipEnergy;
        _energyEmptyLogSent = false;
        if (_showDebugLogs)
        {
            Debug.Log("Energia de la nave restaurada: " + Mathf.RoundToInt(_currentShipEnergy) + "/" + Mathf.RoundToInt(_maxShipEnergy));
        }
    }

    private void ClampValues()
    {
        _maxShipHealth = Mathf.Max(1f, _maxShipHealth);
        _maxShipEnergy = Mathf.Max(1f, _maxShipEnergy);
        _fullEnergyDurationSeconds = Mathf.Max(1f, _fullEnergyDurationSeconds);
        _currentShipHealth = Mathf.Clamp(_currentShipHealth, 0f, _maxShipHealth);
        _currentShipEnergy = Mathf.Clamp(_currentShipEnergy, 0f, _maxShipEnergy);
    }

    private void EnsureScreenUI()
    {
        GameObject screenObject = GameObject.Find(ShipScreenObjectName);
        if (screenObject == null)
        {
            return;
        }

        ShipScreenUI screenUI = screenObject.GetComponent<ShipScreenUI>();
        if (screenUI == null)
        {
            screenUI = screenObject.AddComponent<ShipScreenUI>();
        }

        screenUI.Initialize(this);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
