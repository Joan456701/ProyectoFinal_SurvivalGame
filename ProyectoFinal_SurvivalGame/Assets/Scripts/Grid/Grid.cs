using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class Grid <TGridObject>
{
    private int _width;
    private int _height;
    private float _cellSize;
    private TGridObject[,] _gridArray;

    public Grid(int width, int height, float cellSize, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this._width = width;
        this._height = height;
        this._cellSize = cellSize;

        _gridArray = new TGridObject[_width, _height];

        for (int x = 0; x < _gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < _gridArray.GetLength(1); z++)
            {
                _gridArray[x, z] = createGridObject(this, x, z);

                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);
            }
            Debug.DrawLine(GetWorldPosition(0, _height), GetWorldPosition(_width, _height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(_width, 0), GetWorldPosition(_width, _height), Color.white, 100f);
        }
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * _cellSize;
    }

    // Coge la posición del láser y la pasa a posición de la cuadricula
    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt(worldPosition.x / _cellSize);
        z = Mathf.FloorToInt(worldPosition.z / _cellSize);
    }

    // Le damos una X y una Z, y nos devuelve la celda que hay en ese hueco
    public TGridObject GetGridObject(int x, int z)
    {
        // Para comprobar que no estamos fuera del grid
        if (x >= 0 && z >= 0 && x < _width && z < _height)
            return _gridArray[x, z];
        else
            return default(TGridObject);
    }
}
