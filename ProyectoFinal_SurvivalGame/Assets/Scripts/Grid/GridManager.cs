using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Instancia global para acceder a este script desde cualquier lado sin buscarlo
    public static GridManager Instance { get; private set; }

    [Header("Ajustes del Grid")]
    [SerializeField] private int _width = 10;
    [SerializeField] private int _height = 10;
    [SerializeField] private float _cellSize = 3f;

    // Lista que almacena todos los pisos del mundo
    private List<Grid<GridObject>> _gridList;
    [SerializeField] private int _gridVerticalCoutn = 1; // Cantidad total de pisos
    [SerializeField] private float _gridVerticalSize = 2; // Altura de cada piso

    private void Awake()
    {
        Instance = this;
        _gridList = new List<Grid<GridObject>>();
        
        //Bucle para generar los pisos
        for (int i = 0; i < _gridVerticalCoutn; i++)
        {
            // Calcula la altura de cada piso
            Vector3 worldOrigin = new Vector3(0, i * _gridVerticalSize, 0);

            // Genera la cuadricula en la posicion y la añade a la lista
            Grid<GridObject> newGrid = new Grid<GridObject>(_width, _height, _cellSize, worldOrigin, (Grid<GridObject> g, int x, int z) => new GridObject(g, x, z));
        
            _gridList.Add(newGrid);
        }
    }

    //Devuelve el piso correspondiente segun la posicion
    public Grid<GridObject> GetGrid(Vector3 worldPosition)
    {
        //Calcula el indice del piso dividiendo la altura entre el tamaño del piso
        int gridIndex = Mathf.RoundToInt(worldPosition.y / _gridVerticalSize);

        //Evita que el indice se salga de los limites
        gridIndex = Mathf.Clamp(gridIndex, 0, _gridList.Count -1);

        return _gridList[gridIndex];
    }

    private void OnDrawGizmos()
    {
        // Ponemos el color de las líneas (amarillo destaca bien, pero puedes cambiarlo)
        Gizmos.color = Color.yellow;

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                // Calculamos la posición de esta celda
                Vector3 currentPos = new Vector3(x, 0, z) * _cellSize;

                // Dibujamos la línea hacia arriba
                Gizmos.DrawLine(currentPos, new Vector3(x, 0, z + 1) * _cellSize);

                // Dibujamos la línea hacia la derecha
                Gizmos.DrawLine(currentPos, new Vector3(x + 1, 0, z) * _cellSize);
            }
        }

        // Dibujar los bordes de cierre exteriores (Arriba y Derecha)
        Gizmos.DrawLine(new Vector3(0, 0, _height) * _cellSize, new Vector3(_width, 0, _height) * _cellSize);
        Gizmos.DrawLine(new Vector3(_width, 0, 0) * _cellSize, new Vector3(_width, 0, _height) * _cellSize);
    }
}

