using UnityEngine;

[DisallowMultipleComponent]
public class GasoilStation : MonoBehaviour, IWorldInteractable
{
    [Header("Settings")]
    [SerializeField] private int _carbonCost = 1;

    private void Awake()
    {
        ShipStatusSystem.GetOrCreate();
    }

    public bool TryInteract(SceneInventoryController inventoryController)
    {
        if (inventoryController == null)
            return false;

        if (!inventoryController.HasItem("carbon", _carbonCost))
        {
            Debug.Log("Necesitas carbon para rellenar");
            return false;
        }

        ShipStatusSystem shipStatusSystem = ShipStatusSystem.GetOrCreate();
        if (Mathf.Approximately(shipStatusSystem.CurrentShipEnergy, shipStatusSystem.MaxShipEnergy))
        {
            Debug.Log("La energia de la nave ya esta llena");
            return false;
        }

        if (!inventoryController.ConsumeItem("carbon", _carbonCost))
        {
            Debug.Log("No se pudo consumir el carbon para rellenar la energia");
            return false;
        }

        shipStatusSystem.RefillEnergy();
        Debug.Log("Has rellenado la energia de la nave usando carbon");
        return true;
    }

    public string GetInteractionPrompt()
    {
        SceneInventoryController inv = SceneInventoryController.Instance;
        if (inv == null)
            return "Necesitas carbon para rellenar";

        if (!inv.HasItem("carbon", _carbonCost))
            return "Necesitas carbon para rellenar";

        ShipStatusSystem shipStatusSystem = ShipStatusSystem.Instance;
        if (shipStatusSystem != null && Mathf.Approximately(shipStatusSystem.CurrentShipEnergy, shipStatusSystem.MaxShipEnergy))
        {
            return "La energia de la nave ya esta llena";
        }

        return "Pulsar E para rellenar energia de la nave";
    }
}
