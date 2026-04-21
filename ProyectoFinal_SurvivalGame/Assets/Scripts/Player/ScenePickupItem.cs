using UnityEngine;

public class ScenePickupItem : MonoBehaviour, IPickupable
{
    [Header("Pickup Settings")]
    [SerializeField] private string _inventoryItemId = "stone";
    [SerializeField] private int _amount = 1;
    [SerializeField] private string _pickupName = "Piedra";

    public void Configure(string inventoryItemId, int amount, string pickupName)
    {
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

        Debug.Log("Has recogido " + _amount + " " + _pickupName);
        Destroy(gameObject);
        return true;
    }

    public string GetPickupPrompt()
    {
        return "Pulsar E para recoger";
    }
}
