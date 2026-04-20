using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class FloorPlacedObject : MonoBehaviour
{
    public enum Edge
    {
        Up,
        Down,
        Left,
        Right
    }

    [SerializeField] private FloorEdgePosition _upFloorEdgePosition;
    [SerializeField] private FloorEdgePosition _downFloorEdgePosition;
    [SerializeField] private FloorEdgePosition _leftFloorEdgePosition;
    [SerializeField] private FloorEdgePosition _rightFloorEdgePosition;

    private FloorEdgePlacedObject _upEdgeObject;
    private FloorEdgePlacedObject _downEdgeObject;
    private FloorEdgePlacedObject _leftEdgeObject;
    private FloorEdgePlacedObject _rightEdgeObject;

    public void PlaceEdge(Edge edge, FloorEdgeObjectTypeSO floorEdgeObjectTypeSO)
    {
        FloorEdgePosition floorEdgePosition = GetFloorEdgePosition(edge);

        Transform floorEdgeObjectTransform = Instantiate(floorEdgeObjectTypeSO.prefab, floorEdgePosition.transform.position, floorEdgePosition.transform.rotation);

        FloorEdgePlacedObject floorEdgePlacedObject = floorEdgeObjectTransform.GetComponent<FloorEdgePlacedObject>();
        SetFloorPlacedObjects(edge, floorEdgePlacedObject);
    }

    private FloorEdgePosition GetFloorEdgePosition(Edge edge)
    {
        switch (edge)
        {
            default:
                case Edge.Up: return _upFloorEdgePosition;
                case Edge.Down: return _downFloorEdgePosition;
                case Edge.Left: return _leftFloorEdgePosition;
                case Edge.Right: return _rightFloorEdgePosition;
        }
    }

    private void SetFloorPlacedObjects(Edge edge, FloorEdgePlacedObject floorEdgePlacedObject)
    {
        switch (edge)
        {
            case Edge.Up: _upEdgeObject = floorEdgePlacedObject; break;
            case Edge.Down: _downEdgeObject = floorEdgePlacedObject; break;
            case Edge.Left: _leftEdgeObject = floorEdgePlacedObject; break;
            case Edge.Right: _rightEdgeObject = floorEdgePlacedObject; break;
        }
    }

    public bool HasEdgeObject(Edge edge)
    {
        switch (edge)
        {
            default:
            case Edge.Up: return _upEdgeObject != null;
            case Edge.Down: return _downEdgeObject != null;
            case Edge.Left: return _leftEdgeObject != null;
            case Edge.Right: return _rightEdgeObject != null;
        }
    }
}
