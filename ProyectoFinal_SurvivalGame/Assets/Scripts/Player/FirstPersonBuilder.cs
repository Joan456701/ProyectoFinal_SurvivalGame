using UnityEngine;

public class FirstPersonBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private PlayerInputHandler _pInputHandler;

    [Header("Pieces Settings")]
    [SerializeField] private BuildingPieceSO _currentBuilding;
    [SerializeField] private FloorEdgeObjectTypeSO _currentWallBuilding;

    [Header("Contsrtuction Settings")]
    [SerializeField] private float _raycastDistance;
    [SerializeField] private LayerMask _edgeLayer;

    private Grid<GridObject> _grid;
    private bool _hasBuiltThisPress = false;
    private bool _hasDestroyedThisPress = false;
    public bool _wallMode = false;

    private float _currentRotation = 0;
    private Transform _ghostObject;

    private void Start()
    {
        _grid = GridManager.Instance.GetGrid();

        RefreshGhost();

        _ghostObject.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateGhost();

        if (_pInputHandler.attackTiggered && _pInputHandler.isBuildMode)
        {
            if (!_hasBuiltThisPress)
            {
                TryBuild();
                _hasBuiltThisPress = true;
            }
        }
        else
        {
            _hasBuiltThisPress = false;
        }

        if (_pInputHandler.destroyTriggered && _pInputHandler.isBuildMode)
        {
            if (!_hasDestroyedThisPress)
            {
                TryDestroy();
                _hasDestroyedThisPress = true;
            }
            else
                _hasDestroyedThisPress = false;
        }
        
        if (_pInputHandler.rotateTriggered && _pInputHandler.isBuildMode)
        {
                _currentRotation += 90;

                if (_currentRotation >= 360)
                    _currentRotation = 0;

            _pInputHandler.rotateTriggered = false;
        }
    }

    private void TryBuild()
    {
        if (_wallMode)
        { 
            FloorEdgePosition pointedEdge = GetMouseFloorEdgePosition();

            if (pointedEdge != null)
            {
                FloorPlacedObject fatherWall = pointedEdge.GetComponentInParent<FloorPlacedObject>();

                if (fatherWall != null && _currentWallBuilding != null)
                {
                    if (!fatherWall.HasEdgeObject(pointedEdge.edge))
                        fatherWall.PlaceEdge(pointedEdge.edge, _currentWallBuilding);
                    else
                        Debug.Log("Este hueco ya esta ocupado");
                }
            }
        }
        else
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

                    Transform builtObject = Instantiate(_currentBuilding.prefab, buildPosition, Quaternion.Euler(0, _currentRotation, 0));

                    gridObject.SetPlacedObject(builtObject);
                }
                else
                    Debug.Log("Esta casilla ya esta ocupada");
            }
        }
    }

    private void TryDestroy()
    {
        Vector3 origin = _mainCamera.transform.position;
        Vector3 direction = _mainCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance))
        {
            _grid.GetXZ(hitInfo.point, out int x, out int z);
            GridObject gridObject = _grid.GetGridObject(x, z);

            if (gridObject != null)
            {
                Transform objectToDestroy = gridObject.GetPlacedObject();

                if (objectToDestroy != null)
                {
                    Destroy(objectToDestroy.gameObject);
                    gridObject.SetPlacedObject(null);
                }
            }
        }
    }

    private void UpdateGhost()
    {
        if (!_pInputHandler.isBuildMode)
        { 
            _ghostObject.gameObject.SetActive(false);
            return;
        }

        if(_wallMode)
        {
            FloorEdgePosition pointedEdge = GetMouseFloorEdgePosition();

            if (pointedEdge != null)
            {
                FloorPlacedObject fatherWall = pointedEdge.GetComponentInParent<FloorPlacedObject>();
                if (fatherWall != null && !fatherWall.HasEdgeObject(pointedEdge.edge))
                {    
                    _ghostObject.gameObject.SetActive(true);
                    _ghostObject.position = pointedEdge.transform.position;
                    _ghostObject.rotation = pointedEdge.transform.rotation;
                }
                else
                {
                    _ghostObject.gameObject.SetActive(false);
                }
            }
            else
                _ghostObject.gameObject.SetActive(false);
        }
        else
        {
            Vector3 origin = _mainCamera.transform.position;
            Vector3 direction = _mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance))
            {
                _grid.GetXZ(hitInfo.point, out int x, out int z);
                GridObject gridObject = _grid.GetGridObject(x, z);

                if (gridObject != null)
                {
                    if (gridObject.CanBuild())
                    {
                        _ghostObject.gameObject.SetActive(true);

                        Vector3 targetPosition = _grid.GetWorldPosition(x, z);

                        _ghostObject.position = targetPosition;
                        _ghostObject.rotation = Quaternion.Euler(0, _currentRotation, 0);
                    }
                    else
                    {
                        _ghostObject.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _ghostObject.gameObject.SetActive(false);
                }
            }
            else
                _ghostObject.gameObject.SetActive(false);
        }
    }

    private FloorEdgePosition GetMouseFloorEdgePosition()
    {
        Vector3 origin = _mainCamera.transform.position;
        Vector3 direction = _mainCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance, _edgeLayer))
        {
            if(hitInfo.collider.TryGetComponent(out FloorEdgePosition floorEdgePosition))
                return floorEdgePosition;
        }
        return null;
    }

    private void RefreshGhost()
    {
        if (_ghostObject != null)
            Destroy(_ghostObject.gameObject);

        if (_wallMode)
        {
            if (_currentWallBuilding != null)
                _ghostObject = Instantiate(_currentWallBuilding.ghostPrefab);
        }
        else
        {
            if (_currentBuilding != null)
                _ghostObject = Instantiate(_currentBuilding.ghostPrefab);
        }

        if (_ghostObject != null)
            _ghostObject.gameObject.SetActive(false);
    }
}
