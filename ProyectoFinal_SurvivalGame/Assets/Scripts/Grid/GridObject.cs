using UnityEngine;

//Representa las casillas en el grid
public class GridObject
{
    //Referencia al grid que pertenece
    Grid<GridObject> _grid;

    private int _x; //Cordenada x en el tablero
    private int _z; //Cordenada z en el tablero

    private Transform _placedObject;

    //Se ejecuta cuando el GridManager fabrica esta casilla por primera vez
    public GridObject(Grid<GridObject> grid, int x, int z)
    {
        this._grid = grid;
        this._x = x;
        this._z = z;
    }

    //Comprueba si el hueco esta libre para poder construir en el
    public bool CanBuild()
    {
        return _placedObject == null;
    }

    //Registra en la casilla el objeto que colocas
    public void SetPlacedObject(Transform placedObject)
    {
        this._placedObject = placedObject;
    }

    //Devuelve el objeto que hemos guardado en la casilla
    public Transform GetPlacedObject()
    {
        return _placedObject;
    }

    //Debug
    public override string ToString()
    {
        return _x + ", " + _z;
    }
}
