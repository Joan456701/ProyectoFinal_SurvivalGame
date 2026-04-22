using System;
using UnityEngine;

public class Grid <TGridObject>
{
    private int _width; //Número de casillas en el eje x
    private int _height; //Número de casillas en el eje z
    private float _cellSize; //Tamaño de las casillas
    private Vector3 _originPosition; 

    //Matriz de 2 dimensiones para guradar nuestras casillas
    private TGridObject[,] _gridArray; 

    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this._width = width;
        this._height = height;
        this._cellSize = cellSize;
        this._originPosition = originPosition;

        _gridArray = new TGridObject[_width, _height];

        //Recorre el tablero para ver los huecos usando 2 bucles
        for (int x = 0; x < _gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < _gridArray.GetLength(1); z++)
            {
                //Llenamos los huecos con createGridObject
                _gridArray[x, z] = createGridObject(this, x, z);

                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);
            }
            Debug.DrawLine(GetWorldPosition(0, _height), GetWorldPosition(_width, _height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(_width, 0), GetWorldPosition(_width, _height), Color.white, 100f);
        }
    }

    //COnvierte las cordenadas del tablero en cordenadas de unity
    public Vector3 GetWorldPosition(int x, int z)
    {
        return (new Vector3(x, 0, z) * _cellSize) + _originPosition;
    }

    // Coge la posición del láser y la pasa a posición de la cuadricula
    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition.x - _originPosition.x) / _cellSize);
        z = Mathf.FloorToInt((worldPosition.z - _originPosition.z) / _cellSize);
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
