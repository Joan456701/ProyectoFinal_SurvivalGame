using UnityEngine;

[DisallowMultipleComponent]
public class ScenePickupItem : MonoBehaviour, IPickupable
{
    [Header("Pickup Settings")]
    [SerializeField] private bool _autoConfigureFromObjectName;
    [SerializeField] private string _inventoryItemId = "stone";
    [SerializeField] private int _amount = 1;
    [SerializeField] private string _pickupName = "Piedra";

    private void Awake()
    {
        ApplyAutomaticConfiguration();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyAutomaticConfiguration();
    }
#endif

    public void Configure(string inventoryItemId, int amount, string pickupName)
    {
        _autoConfigureFromObjectName = false;
        _inventoryItemId = inventoryItemId;
        _amount = amount;
        _pickupName = pickupName;
    }

    public bool TryPickup(SceneInventoryController inventoryController)
    {
        if (inventoryController == null)
        {
            return false;
        }

        if (!inventoryController.TryAddItem(_inventoryItemId, _amount))
        {
            return false;
        }

        if (_inventoryItemId == "Bombona" && OxygenSystem.Instance != null)
        {
            OxygenSystem.Instance.GiveOxygenTank();
            Debug.Log("Has recogido la bombona de oxigeno. El tiempo de oxigeno ha comenzado!");
        }
        else
        {
            Debug.Log("Has recogido " + _amount + " " + _pickupName);
        }

        Destroy(gameObject);
        return true;
    }

    public string GetPickupPrompt()
    {
        return "Pulsar E para recoger";
    }

    private void ApplyAutomaticConfiguration()
    {
        if (!_autoConfigureFromObjectName)
        {
            return;
        }

        if (ScenePickupNameResolver.TryResolvePickup(gameObject.name, out string itemId, out string pickupName))
        {
            _inventoryItemId = itemId;
            _pickupName = pickupName;
            _amount = Mathf.Max(1, _amount);
        }
    }
}

internal static class ScenePickupNameResolver
{
    public static bool TryResolvePickup(string objectName, out string itemId, out string pickupName)
    {
        itemId = string.Empty;
        pickupName = string.Empty;

        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string trimmedName = objectName.Trim();

        if (trimmedName.StartsWith("Piedra", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "stone";
            pickupName = "Piedra";
            return true;
        }

        if (trimmedName.StartsWith("Madera", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "wood";
            pickupName = "Madera";
            return true;
        }

        if (trimmedName.StartsWith("Hoja", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "leaf";
            pickupName = "Hoja";
            return true;
        }

        if (trimmedName.StartsWith("Palo", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "stick";
            pickupName = "Palos";
            return true;
        }

        if (trimmedName.StartsWith("Bombona", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "Bombona";
            pickupName = "Bombona";
            return true;
        }

        if (trimmedName.StartsWith("Comida", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "Comida";
            pickupName = "Comida";
            return true;
        }

        if (trimmedName.StartsWith("Carbon", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "carbon";
            pickupName = "Carbon";
            return true;
        }


        if (trimmedName.StartsWith("Metal", System.StringComparison.OrdinalIgnoreCase))
        {
            itemId = "metal";
            pickupName = "Metal";
            return true;
        }

        return false;
    }
}
