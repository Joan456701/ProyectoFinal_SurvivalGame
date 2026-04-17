using UnityEngine;

public class GridObject
{
    Grid<GridObject> _grid;
    private int _x;
    private int _z;

    private Transform _placedObject;
    public GridObject(Grid<GridObject> grid, int x, int z)
    {
        this._grid = grid;
        this._x = x;
        this._z = z;
    }

    public bool CanBuild()
    {
        return _placedObject == null;
    }

    public void SetPlacedObject(Transform placedObject)
    {
        this._placedObject = placedObject;
    }

    public Transform GetPlacedObject()
    {
        return _placedObject;
    }

    public override string ToString()
    {
        return _x + ", " + _z;
    }
}
