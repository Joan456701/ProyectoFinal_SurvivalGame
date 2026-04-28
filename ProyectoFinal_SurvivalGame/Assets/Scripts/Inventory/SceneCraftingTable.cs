using UnityEngine;

public class SceneCraftingTable : MonoBehaviour, IWorldInteractable
{
    public bool TryInteract(SceneInventoryController inventoryController)
    {
        if (inventoryController == null)
        {
            return false;
        }

        inventoryController.OpenCraftingStation();
        return true;
    }

    public string GetInteractionPrompt()
    {
        return "Pulsar E para usar mesa de crafteo";
    }
}
