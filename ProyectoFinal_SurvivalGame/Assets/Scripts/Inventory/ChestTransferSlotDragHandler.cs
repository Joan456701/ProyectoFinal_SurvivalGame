using UnityEngine;
using UnityEngine.EventSystems;

public class ChestTransferSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private SceneInventoryController _inventoryController;
    private int _slotIndex;
    private bool _isChestStorage;

    public void Initialize(SceneInventoryController inventoryController, int slotIndex, bool isChestStorage)
    {
        _inventoryController = inventoryController;
        _slotIndex = slotIndex;
        _isChestStorage = isChestStorage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _inventoryController?.BeginChestTransferDrag(_isChestStorage, _slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _inventoryController?.UpdateSlotDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _inventoryController?.EndChestTransferDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        _inventoryController?.HandleChestTransferDrop(_isChestStorage, _slotIndex);
    }
}
