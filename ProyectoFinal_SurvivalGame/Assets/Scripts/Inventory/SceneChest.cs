using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneChest : MonoBehaviour, IWorldInteractable
{
    [Serializable]
    public class ChestSlotData
    {
        public string itemId;
        public int amount;

        public bool IsEmpty => string.IsNullOrEmpty(itemId) || amount <= 0;

        public void Clear()
        {
            itemId = string.Empty;
            amount = 0;
        }
    }

    [SerializeField] private int _slotCount = 12;
    [SerializeField] private List<ChestSlotData> _slots = new List<ChestSlotData>();

    public int SlotCount
    {
        get
        {
            EnsureInitialized();
            return _slots.Count;
        }
    }

    public bool TryInteract(SceneInventoryController inventoryController)
    {
        if (inventoryController == null)
        {
            return false;
        }

        EnsureInitialized();
        inventoryController.OpenChest(this);
        return true;
    }

    public string GetInteractionPrompt()
    {
        return "Pulsar E para abrir cofre";
    }

    public ChestSlotData GetSlot(int slotIndex)
    {
        EnsureInitialized();

        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return null;
        }

        return _slots[slotIndex];
    }

    public bool TryStoreItem(string itemId, int amount, int maxStack, out int remainingAmount)
    {
        EnsureInitialized();
        remainingAmount = amount;

        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            ChestSlotData slot = _slots[i];
            if (slot.IsEmpty || slot.itemId != itemId || slot.amount >= maxStack)
            {
                continue;
            }

            int storableAmount = Mathf.Min(maxStack - slot.amount, remainingAmount);
            slot.amount += storableAmount;
            remainingAmount -= storableAmount;

            if (remainingAmount <= 0)
            {
                return true;
            }
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            ChestSlotData slot = _slots[i];
            if (!slot.IsEmpty)
            {
                continue;
            }

            slot.itemId = itemId;
            slot.amount = Mathf.Min(maxStack, remainingAmount);
            remainingAmount -= slot.amount;

            if (remainingAmount <= 0)
            {
                return true;
            }
        }

        return remainingAmount < amount;
    }

    public bool TryTakeItem(int slotIndex, out string itemId, out int amount)
    {
        EnsureInitialized();
        itemId = string.Empty;
        amount = 0;

        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return false;
        }

        ChestSlotData slot = _slots[slotIndex];
        if (slot == null || slot.IsEmpty)
        {
            return false;
        }

        itemId = slot.itemId;
        amount = slot.amount;
        slot.Clear();
        return true;
    }

    public void EnsureInitialized()
    {
        if (_slotCount < 1)
        {
            _slotCount = 12;
        }

        while (_slots.Count < _slotCount)
        {
            _slots.Add(new ChestSlotData());
        }

        while (_slots.Count > _slotCount)
        {
            _slots.RemoveAt(_slots.Count - 1);
        }
    }
}
