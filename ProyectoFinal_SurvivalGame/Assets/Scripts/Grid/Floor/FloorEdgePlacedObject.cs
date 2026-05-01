using UnityEngine;
using Unity.AI.Navigation;

public class FloorEdgePlacedObject : MonoBehaviour, IDamagable
{
    [Header("Vida Estructura")]
    [SerializeField] private int _maxHealth;
    private int _health;

    [SerializeField] private FloorEdgeObjectTypeSO _floorEdgeObjectTypeSO;

    private NavMeshSurface _navMeshSurface;
    void Start()
    {
        _health = _maxHealth;

        if (_floorEdgeObjectTypeSO.isStairs && _navMeshSurface != null)
        {
            Physics.SyncTransforms();

            _navMeshSurface.BuildNavMesh();
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

            NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
            if (surface != null && _floorEdgeObjectTypeSO.isStairs)
            {
                surface.BuildNavMesh();
            }

            Destroy(gameObject);
        }
    }
    public FloorEdgeObjectTypeSO GetSO() => _floorEdgeObjectTypeSO;
}
