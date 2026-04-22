using UnityEngine;

public class FloorEdgePlacedObject : MonoBehaviour, IDamagable
{
    [Header("Vida Estructura")]
    [SerializeField] private int _maxHealth;
    private int _health;
    void Start()
    {
        _health = _maxHealth;
    }

    public void DamageRecived(int damage)
    {
        _health -= damage;

        if (_health <= 0)
        {
            Grid<GridObject> grid = GridManager.Instance.GetGrid(transform.position);
            grid.GetXZ(transform.position, out int x, out int z);
            grid.GetGridObject(x, z)?.SetPlacedObject(null);

            Destroy(gameObject);
        }
    }
}
