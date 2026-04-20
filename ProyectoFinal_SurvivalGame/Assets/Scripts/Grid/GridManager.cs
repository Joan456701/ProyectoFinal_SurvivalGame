using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Ajustes del Grid")]
    [SerializeField] private int _width = 10;
    [SerializeField] private int _height = 10;
    [SerializeField] private float _cellSize = 3f;

    private Grid<GridObject> _grid;
    private void Awake()
    {
        Instance = this;

        _grid = new Grid<GridObject>(_width, _height, _cellSize, (Grid<GridObject> g, int x, int z) => new GridObject(g, x, z));
    }

    public Grid<GridObject> GetGrid()
    {
        return _grid;
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

