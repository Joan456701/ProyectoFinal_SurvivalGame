using System.Collections.Generic;
using UnityEngine;

public struct ExtractableResources
{
    public string itemId;
    public int amount;
    public string displayName;
}

public class ExtractorController : MonoBehaviour, IWorldInteractable
{
    [Header("Variables del funcionamiento")]
    [SerializeField] private float _extractorSpeed;

    [Header("Probabilidades de cada material")]
    [SerializeField] private int _stoneChance = 50;
    [SerializeField] private int _coalChance = 30;
    [SerializeField] private int _metalChance = 20;

    [Header("Probabilidad de la cantidad")]
    [SerializeField] private int _littleAmount = 80;
    [SerializeField] private int _mediumAmount = 18;
    [SerializeField] private int _maxAmount = 2;

    private List<ExtractableResources> _extractedItems = new List<ExtractableResources>();

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= _extractorSpeed)
        {
            _timer = 0;

            ExtractTheMaterial();
        }
    }

    private void ExtractTheMaterial()
    {
        string extractedItemID = RerorllMaterials();
        int materialAmount = RerollAmount();
        _extractedItems.Add(new ExtractableResources
        {
            itemId = extractedItemID,
            amount = materialAmount,
            displayName = extractedItemID
        });
    }

    private string RerorllMaterials()
    {
        int roll = Random.Range(0, 100);
        if (roll < _stoneChance) return "stone";
        if (roll < _stoneChance + _coalChance) return "carbon";
        else return "metal";
    }

    public bool TryInteract(SceneInventoryController inventory)
    {
        if (_extractedItems.Count == 0)
        {
            return false;
        }

        foreach (var item in _extractedItems)
        {
            inventory.TryAddItem(item.itemId, item.amount);
            Debug.Log("Recogido: " + item.amount + " " + item.displayName);
        }

        _extractedItems.Clear();
        return true;
    }

    private int RerollAmount()
    {
        int roll = Random.Range(0, 100);
        if (roll < _littleAmount) return 1;
        if (roll < _littleAmount + _mediumAmount) return 3;
        else return 5;
    }

    public string GetInteractionPrompt()
    {
        return _extractedItems.Count > 0
            ? $"Pulsar E para recoger ({_extractedItems.Count} recursos)"
            : "Sin recursos aún";
    }
}
