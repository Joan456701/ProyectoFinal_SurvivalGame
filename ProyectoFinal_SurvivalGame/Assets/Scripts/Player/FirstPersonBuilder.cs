using UnityEngine;

public class FirstPersonBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private PlayerInputHandler _pInputHandler;
    [SerializeField] private BuildingPieceSO _currentBuilding;

    [Header("Contsrtuction Settings")]
    [SerializeField] private float _raycastDistance;

    private Grid<GridObject> _grid;
    private bool hasBuiltThisPress = false;

    private void Start()
    {
        _grid = new Grid<GridObject>(10, 10, 3, (Grid<GridObject> g, int x, int z) => new GridObject(g, x, z));
    }

    private void Update()
    {
        // 1. Vigilamos si estás pulsando el botón (ej: clic o la E)
        if (_pInputHandler.attackTiggered && _pInputHandler.isBuildMode)
        {
            if (!hasBuiltThisPress)
            {
                TryBuild();
                hasBuiltThisPress = true;
            }
        }
        else
        {
            hasBuiltThisPress = false;
        }
    }

    private void TryBuild()
    {
        Vector3 origin = _mainCamera.transform.position;
        Vector3 direction = _mainCamera.transform.forward;

        Debug.Log("1. Botón pulsado. Disparando láser...");

        if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance))
        {
            Debug.Log("2. El láser ha chocado contra: " + hitInfo.collider.name + " en la pos: " + hitInfo.point);

            _grid.GetXZ(hitInfo.point, out int x, out int z);

            GridObject gridObject = _grid.GetGridObject(x, z);

            if (gridObject.CanBuild())
            {
                Vector3 buildPosition = _grid.GetWorldPosition(x, z);

                Transform builtObject = Instantiate(_currentBuilding.prefab, buildPosition, Quaternion.identity);

                gridObject.SetPlacedObject(builtObject);

                _grid = new Grid<GridObject>(10, 10, 3, (Grid<GridObject> g, int x, int z) => new GridObject(g, x, z));

            }
        }
    }
}
