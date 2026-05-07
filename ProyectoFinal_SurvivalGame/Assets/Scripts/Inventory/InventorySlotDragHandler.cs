using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private SceneInventoryController _inventoryController;
    private int _slotIndex;

    public void Initialize(SceneInventoryController inventoryController, int slotIndex)
    {
        _inventoryController = inventoryController;
        _slotIndex = slotIndex;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _inventoryController?.BeginSlotDrag(_slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _inventoryController?.UpdateSlotDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _inventoryController?.EndSlotDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        _inventoryController?.HandleSlotDrop(_slotIndex);
    }
}
