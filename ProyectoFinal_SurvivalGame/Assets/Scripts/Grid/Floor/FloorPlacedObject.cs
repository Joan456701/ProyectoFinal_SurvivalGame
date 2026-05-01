using System;
using System.Runtime.CompilerServices;
using Unity.AI.Navigation;
using UnityEngine;

public class FloorPlacedObject : MonoBehaviour, IDamagable
{
    //Las 4 direcciones
    public enum Edge
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Posiciones de los Bordes")]
    [SerializeField] private FloorEdgePosition _upFloorEdgePosition;
    [SerializeField] private FloorEdgePosition _downFloorEdgePosition;
    [SerializeField] private FloorEdgePosition _leftFloorEdgePosition;
    [SerializeField] private FloorEdgePosition _rightFloorEdgePosition;

    [Header("Vida Estructura")]
    [SerializeField] private int _maxHealth;
    private int _health;

    // Memoria interna guarda el muro real que esta construido en cada lado
    private FloorEdgePlacedObject _upEdgeObject;
    private FloorEdgePlacedObject _downEdgeObject;
    private FloorEdgePlacedObject _leftEdgeObject;
    private FloorEdgePlacedObject _rightEdgeObject;

    private NavMeshSurface _navMeshSurface;

    private void Start()
    {
        // Al nacer, el Suelo comprueba sus 4 lados automáticamente
        CheckNeighborForWall(Edge.Up);
        CheckNeighborForWall(Edge.Down);
        CheckNeighborForWall(Edge.Left);
        CheckNeighborForWall(Edge.Right);

        _health = _maxHealth;

        _navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
        if (_navMeshSurface != null)
        {
            RebuildNavMesh();
        }
    }
    private void RebuildNavMesh()
    {
        if (_navMeshSurface == null) return;

        // Guarda la configuracion original
        LayerMask originalMask = _navMeshSurface.layerMask;

        // Excluye el layer de los pivotes (cambia "NavMeshIgnore" por el nombre de tu layer)
        _navMeshSurface.layerMask = originalMask & ~LayerMask.GetMask("NavMeshIgnore");

        _navMeshSurface.BuildNavMesh();

        // Restaura
        _navMeshSurface.layerMask = originalMask;
    }

    // Función para mirar si el vecin ya ha colocado el muro
    private void CheckNeighborForWall(Edge edge)
    {
        // Mira si hay un vecindo en esa dirección
        FloorPlacedObject neighbor = GetNeighbor(edge);

        if (neighbor != null)
        {
            // En el caso de que tenga vecimos, calculamos cual es el lado de la forntera
            Edge oppositeEdge = GetOppositeEdge(edge);

            // Después le pedimos que nos diga que es lo que tiene en ese lado de la frontera
            FloorEdgePlacedObject existingWall = neighbor.GetEdgeObject(oppositeEdge);

            // Si resulta que ya hay un muro ahí nos lo guardamos en nuestra memoria
            if (existingWall != null)
                SetFloorPlacedObjects(edge, existingWall);
        }
    }

    public void DamageRecived(int damage)
    {
        _health -= damage;

        if (_health <= 0)
        {
            gameObject.SetActive(false);

            Grid<GridObject> grid = GridManager.Instance.GetGrid(transform.position);
            grid.GetXZ(transform.position, out int x, out int z);
            grid.GetGridObject(x, z)?.SetPlacedObject(null);

            RebuildNavMesh();

            Destroy(gameObject);
        }
    }

    // Construye un muro nuevo en un borde y avisa al vecino
    public void PlaceEdge(Edge edge, FloorEdgeObjectTypeSO floorEdgeObjectTypeSO)
    {
        FloorEdgePosition floorEdgePosition = GetFloorEdgePosition(edge);

        // Instancia el muro fisico en la posicion correcta y lo hace hijo de este Suelo
        Transform floorEdgeObjectTransform = Instantiate(floorEdgeObjectTypeSO.prefab, 
            floorEdgePosition.transform.position, 
            floorEdgePosition.transform.rotation, transform);

        FloorEdgePlacedObject floorEdgePlacedObject = floorEdgeObjectTransform.GetComponent<FloorEdgePlacedObject>();
        
        //Lo guarda en la memoria
        SetFloorPlacedObjects(edge, floorEdgePlacedObject);

        //Si tenemos vencino le avisamos para que lo anote
        FloorPlacedObject neighbor = GetNeighbor(edge);
        if (neighbor != null)
        {
            Edge opositeEdge = GetOppositeEdge(edge);
            neighbor.SetFloorPlacedObjects(opositeEdge, floorEdgePlacedObject);
        }

        if (floorEdgeObjectTypeSO.isStairs && _navMeshSurface != null)
        {
            _navMeshSurface.BuildNavMesh();
        }
    }

    // Devuelve el punto 3D exacto donde debe ir el muro según la dirección
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

    // Guarda el muro recién creado en la variable correspondiente de nuestra memoria
    public void SetFloorPlacedObjects(Edge edge, FloorEdgePlacedObject floorEdgePlacedObject)
    {
        switch (edge)
        {
            case Edge.Up: _upEdgeObject = floorEdgePlacedObject; break;
            case Edge.Down: _downEdgeObject = floorEdgePlacedObject; break;
            case Edge.Left: _leftEdgeObject = floorEdgePlacedObject; break;
            case Edge.Right: _rightEdgeObject = floorEdgePlacedObject; break;
        }
    }

    // Comprueba si ya hay un muro fisico construido en esta direccion
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

    // Devuelve la referencia al muro fisico construido en esta direccion
    public FloorEdgePlacedObject GetEdgeObject(Edge edge)
    {
        switch (edge)
        {
            default:
            case Edge.Up: return _upEdgeObject;
            case Edge.Down: return _downEdgeObject;
            case Edge.Left: return _leftEdgeObject;
            case Edge.Right: return _rightEdgeObject;
        }
    }

    //Sirve para ver que cordenadas (X,Z) hay que mirar para encontrar su vecino
    private Vector2Int GetEdgeGridPositionOffset(Edge edge)
    {
        switch (edge)
        {
            default:
            case Edge.Up: return new Vector2Int(0, 1);
            case Edge.Down: return new Vector2Int(0, -1);
            case Edge.Left: return new Vector2Int(-1, 0);
            case Edge.Right: return new Vector2Int(1, 0);
        }
    }

    //Invierte la direccion (si miro a la izquierda el vecino a la derecha)
    private static Edge GetOppositeEdge(Edge edge)
    {
        switch (edge)
        {
            default:
            case Edge.Up: return Edge.Down;
            case Edge.Down: return Edge.Up;
            case Edge.Left: return Edge.Right;
            case Edge.Right: return Edge.Left;
        }
    }
    
    //Busca el GridManager si hay otro suelo construido al lado
    private FloorPlacedObject GetNeighbor(Edge edge)
    {
        //Pide el mapa del piso actual usando su propia altura
        Grid<GridObject> grid = GridManager.Instance.GetGrid(transform.position);

        // Calcula qué casilla matemática ocupa este suelo
        grid.GetXZ(transform.position, out int x, out int z);

        // Suma el Offset para mirar la casilla de al lado
        Vector2Int offset = GetEdgeGridPositionOffset(edge);
        int neighborX = x + offset.x;
        int neighborZ = z + offset.y;

        // Pregunta al mapa si hay algo en esa casilla contigua
        GridObject neighborGridObject = grid.GetGridObject(neighborX, neighborZ);

        // Si hay algo, y ese algo es un Suelo, nos lo devuelve
        if (neighborGridObject != null && neighborGridObject.GetPlacedObject() != null)
        {
            return neighborGridObject.GetPlacedObject().GetComponent<FloorPlacedObject>();
        }
        return null;
    }

    // Booleano para saber si el suelo tien una escalera ya puesta
    public bool HasAnyStairs()
    {
        return IsStairsAtEdge(Edge.Up) || IsStairsAtEdge(Edge.Down) ||
               IsStairsAtEdge(Edge.Left) || IsStairsAtEdge(Edge.Right);
    }

    public bool IsStairsAtEdge(Edge edge)
    {
        FloorEdgePlacedObject edgeObj = GetEdgeObject(edge);

        if (edgeObj == null)
            return false;

        return edgeObj.GetSO().isStairs;
    }
}
