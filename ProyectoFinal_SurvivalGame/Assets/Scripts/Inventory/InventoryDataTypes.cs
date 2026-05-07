using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
internal sealed class InventoryItemDefinition
{
    public string itemId;
    public string displayName;
    [TextArea] public string description;
    public Color color = Color.white;
    [Min(1)] public int maxStack = 20;
    public PrimitiveType worldPrimitiveType = PrimitiveType.Cube;
    public Vector3 worldScale = Vector3.one;
}

[Serializable]
internal sealed class InventorySlot
{
    public InventoryItemDefinition item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}

[Serializable]
internal sealed class InventoryState
{
    public string[] itemIds;
    public int[] amounts;
    public int activeHotbarSlotIndex;
}

[Serializable]
internal sealed class RecipeRequirement
{
    public string itemId;
    public int amount;
}

[Serializable]
internal sealed class CraftingRecipe
{
    public string resultItemId;
    public int resultAmount;
    public List<RecipeRequirement> requirements = new List<RecipeRequirement>();
}

internal sealed class SlotUI
{
    public int Index;
    public Image Background;
    public Image Icon;
    public Text AmountLabel;
    public Text ShortcutLabel;
}

internal sealed class CraftingRecipeUI
{
    public CraftingRecipe Recipe;
    public Text NameLabel;
    public Text RequirementsLabel;
    public Text StatusLabel;
    public Button CreateButton;
    public Image ButtonImage;
}

internal sealed class TransferSlotUI
{
    public int Index;
    public Image Background;
    public Image Icon;
    public Text AmountLabel;
    public Text Label;
    public Button Button;
}

internal enum ChestTransferContext
{
    None,
    Inventory,
    Chest
}

internal enum DropMode
{
    FullStack,
    HalfStack,
    SingleUnit
}
