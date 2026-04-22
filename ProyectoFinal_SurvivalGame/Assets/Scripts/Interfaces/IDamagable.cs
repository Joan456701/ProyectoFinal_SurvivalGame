using UnityEngine;

public interface IDamagable
{
    void DamageRecived(int damage);
}

public interface IPickupable
{
    bool TryPickup(SceneInventoryController inventoryController);
    string GetPickupPrompt();
}
