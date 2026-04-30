using UnityEngine;

[DisallowMultipleComponent]
public class BaseLevelUpStation : MonoBehaviour, IWorldInteractable
{
    [Header("References")]
    [SerializeField] private SaveZone _saveZone;

    [Header("Level Settings")]
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private int _maxLevel = 3;

    [Header("Upgrade Cost")]
    [SerializeField] private string _stickItemId = "stick";
    [SerializeField] private string _stoneItemId = "stone";
    [SerializeField] private int _baseStickCost = 2;
    [SerializeField] private int _baseStoneCost = 3;

    public int CurrentLevel => _currentLevel;

    private void Awake()
    {
        if (_saveZone == null)
        {
            _saveZone = FindFirstObjectByType<SaveZone>();
        }

        ApplyCurrentLevelToSaveZone();
    }

    public bool TryInteract(SceneInventoryController inventoryController)
    {
        if (inventoryController == null)
        {
            return false;
        }

        if (_currentLevel >= _maxLevel)
        {
            Debug.Log("La base ya esta al nivel maximo.");
            return false;
        }

        int stickCost = GetStickCostForNextLevel();
        int stoneCost = GetStoneCostForNextLevel();

        if (!inventoryController.HasItem(_stickItemId, stickCost) || !inventoryController.HasItem(_stoneItemId, stoneCost))
        {
            Debug.Log("Faltan materiales para mejorar la base. Necesitas " + stickCost + " palos y " + stoneCost + " piedras.");
            return false;
        }

        if (!inventoryController.ConsumeItems((_stickItemId, stickCost), (_stoneItemId, stoneCost)))
        {
            Debug.Log("No se pudieron consumir los materiales de mejora.");
            return false;
        }

        _currentLevel++;
        ApplyCurrentLevelToSaveZone();

        Debug.Log("Base mejorada al nivel " + _currentLevel + ".");
        return true;
    }

    public string GetInteractionPrompt()
    {
        if (_currentLevel >= _maxLevel)
        {
            return "Base al nivel maximo";
        }

        int nextLevel = _currentLevel + 1;
        int stickCost = GetStickCostForNextLevel();
        int stoneCost = GetStoneCostForNextLevel();

        SceneInventoryController inventoryController = SceneInventoryController.Instance;
        if (inventoryController != null &&
            (!inventoryController.HasItem(_stickItemId, stickCost) || !inventoryController.HasItem(_stoneItemId, stoneCost)))
        {
            return "Nivel " + nextLevel + ": necesitas " + stickCost + " palos y " + stoneCost + " piedras";
        }

        return "Pulsar E para mejorar base a nivel " + nextLevel;
    }

    private int GetStickCostForNextLevel()
    {
        return GetScaledCost(_baseStickCost);
    }

    private int GetStoneCostForNextLevel()
    {
        return GetScaledCost(_baseStoneCost);
    }

    private int GetScaledCost(int baseCost)
    {
        int upgradesAlreadyDone = Mathf.Max(0, _currentLevel - 1);
        return Mathf.Max(1, baseCost * (int)Mathf.Pow(2, upgradesAlreadyDone));
    }

    private void ApplyCurrentLevelToSaveZone()
    {
        if (_saveZone == null)
        {
            return;
        }

        _currentLevel = Mathf.Clamp(_currentLevel, 1, Mathf.Max(1, _maxLevel));
        _saveZone.ApplyBaseLevel(_currentLevel);
    }
}
