using UnityEngine;

public class FirstPersonBuilder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private PlayerInputHandler _pInputHandler;
    [SerializeField] private ToolCooldawnManager _cooldawnManager;

    [Header("Pieces Settings")]
    [SerializeField] private BuildingPieceSO _currentBuilding;
    [SerializeField] private FloorEdgeObjectTypeSO _currentWallBuilding;

    [Header("Contsrtuction Settings")]
    [SerializeField] private float _raycastDistance;
    [SerializeField] private LayerMask _edgeLayer;

    private bool _hasBuiltThisPress = false;
    public bool _wallMode = false;

    private float _currentRotation = 0;
    private Transform _ghostObject;

    private void Start()
    {
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

        if (_pInputHandler.rotateTriggered && _pInputHandler.isBuildMode)
        {
                _currentRotation += 90;

                if (_currentRotation >= 360)
                    _currentRotation = 0;
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

                //Si detecta el borde y esta libre permite construir
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

            Debug.Log("1. Bot�n pulsado. Disparando l�ser...");

            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance))
            {
                Debug.Log("2. El l�ser ha chocado contra: " + hitInfo.collider.name + " en la pos: " + hitInfo.point);

                //Pide al gridManger que le diga que piso es
                Grid<GridObject> currentGrid = GridManager.Instance.GetGrid(hitInfo.point);
                currentGrid.GetXZ(hitInfo.point, out int x, out int z);
                GridObject gridObject = currentGrid.GetGridObject(x, z);

                //Si la casilla esta libre contruye el suelo u lo guarda en su memoria
                if (gridObject.CanBuild())
                {
                    Vector3 buildPosition = currentGrid.GetWorldPosition(x, z);
                    Transform builtObject = Instantiate(_currentBuilding.prefab, buildPosition, Quaternion.Euler(0, _currentRotation, 0));
                    gridObject.SetPlacedObject(builtObject);
                }
                else
                    Debug.Log("Esta casilla ya esta ocupada");
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
                Grid<GridObject> currentGrid = GridManager.Instance.GetGrid(hitInfo.point);

                currentGrid.GetXZ(hitInfo.point, out int x, out int z);
                GridObject gridObject = currentGrid.GetGridObject(x, z);

                if (gridObject != null)
                {
                    if (gridObject.CanBuild())
                    {
                        _ghostObject.gameObject.SetActive(true);

                        Vector3 targetPosition = currentGrid.GetWorldPosition(x, z);

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

