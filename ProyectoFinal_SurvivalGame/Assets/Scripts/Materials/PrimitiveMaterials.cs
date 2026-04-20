using UnityEngine;

[System.Serializable]
public struct LootObject
{
    public string _itemName;
    public int minAmount;
    public int maxAmount;

    [Range(0, 100)]
    public int probability;
}
public class PrimitiveMaterials : MonoBehaviour, IDamagable
{
    [Header("Material Settings")]
    [SerializeField] private int _maxHealth;

    private int _currentHealth;
    private int _dropAmount;

    [Header("List of Materials")]

    [SerializeField] private LootObject[] lootObject;

    void Start()
    {
        _currentHealth = _maxHealth;
    }
    public void DamageRecived(int damge)
    {
        _currentHealth -= damge;

        foreach (LootObject item in lootObject)
        {
            int die = Random.Range(0, 100);

            if (die <= item.probability)
            {
                _dropAmount = Random.Range(item.minAmount, item.maxAmount + 1);

                //Añadir al inventario cuando este hecho
            }

            Debug.Log(item._itemName + " ha soltado " + _dropAmount);
        }

        if (_currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}
