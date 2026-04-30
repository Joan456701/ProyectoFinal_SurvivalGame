using UnityEngine;

[DisallowMultipleComponent]
public class GasoilStation : MonoBehaviour, IWorldInteractable
{
    [Header("Settings")]
    [SerializeField] private int _carbonCost = 1;

    public bool TryInteract(SceneInventoryController inventoryController)
    {
        if (inventoryController == null)
            return false;

        if (!inventoryController.HasItem("carbon", _carbonCost))
        {
            Debug.Log("Necesitas carbon para rellenar");
            return false;
        }

        if (OxygenSystem.Instance == null)
        {
            Debug.Log("Sistema de oxigeno no encontrado");
            return false;
        }

        OxygenSystem.Instance.RefillOxygen();
        inventoryController.ConsumeItem("carbon", _carbonCost);
        Debug.Log("Has rellenado la gasolina usando carbon");
        return true;
    }

    public string GetInteractionPrompt()
    {
        if (OxygenSystem.Instance != null && 
            OxygenSystem.Instance.HasOxygenTank && 
            Mathf.Approximately(OxygenSystem.Instance.CurrentOxygenTime, OxygenSystem.Instance.MaxOxygenTime))
        {
            return "La gasolina ya esta llena";
        }

        SceneInventoryController inv = SceneInventoryController.Instance;
        if (inv == null)
            return "Necesitas carbon para rellenar";

        if (!inv.HasItem("carbon", _carbonCost))
            return "Necesitas carbon para rellenar";

        return "Pulsar E para rellenar gasolina";
    }
}
