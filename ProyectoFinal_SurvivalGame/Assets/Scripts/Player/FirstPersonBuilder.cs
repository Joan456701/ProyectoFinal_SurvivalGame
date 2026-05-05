using UnityEngine;

public class FirstPersonBuilder : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private PlayerInputHandler _pInputHandler;
    [SerializeField] private ToolCooldawnManager _cooldawnManager;

    [Header("Ajustes de construccion")]
    [SerializeField] private float _looseObjectRotationAmount;
    [SerializeField] private float _raycastDistance;
    [SerializeField] private LayerMask _edgeLayer;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private Vector3 _cellSizeDimension;

    [SerializeField] private BuildingPieceSO _currentBuilding;
    private FloorEdgeObjectTypeSO _currentWallBuilding;
    private LooseObjectSO _currentLooseBuilding;

    private float _looseObjectRotation = 0;
    private bool _hasBuiltThisPress = false;
    private bool _stairsMode = false;
    public bool _looseMode = false;
    public bool _wallMode = false;

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

        if (_pInputHandler.rotateTriggered && _looseMode)
        {
            _looseObjectRotation += _looseObjectRotationAmount * Time.deltaTime;  
        }
    }

    private void TryBuild()
    {
        if (_wallMode)
        { 
            FloorEdgePosition pointedEdge = GetMouseFloorEdgePosition();

            if (pointedEdge != null && pointedEdge.transform.parent != null)
            {
                FloorPlacedObject fatherFloor = pointedEdge.GetComponentInParent<FloorPlacedObject>();

                if (fatherFloor != null && _currentWallBuilding != null)
                {   
                    FloorPlacedObject.Edge currentEdge = pointedEdge.edge;
                    FloorPlacedObject.Edge oppositeEdge = GetOppositeEdge(currentEdge);

                    if (_stairsMode && fatherFloor.HasAnyStairs()) 
                        return;

                    if (!_stairsMode)
                    {
                        if (fatherFloor.IsStairsAtEdge(currentEdge) || fatherFloor.IsStairsAtEdge(oppositeEdge))
                            return;
                    }
                    else
                    {
                        if (fatherFloor.HasEdgeObject(oppositeEdge))
                            return;
                    }

                    if (!fatherFloor.HasEdgeObject(currentEdge))
                    {
                        if (CheckAndConsumeRequirements(_currentWallBuilding.requirements))
                        {
                            fatherFloor.PlaceEdge(currentEdge, _currentWallBuilding);
                        }
                    }
                }
            }
        }
        else if (_looseMode)
        {
            Vector3 origin = _mainCamera.transform.position;
            Vector3 direction = _mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 checkCenter = hitInfo.point + new Vector3(0, _currentLooseBuilding.clearanceSize.y / 2f, 0);
                
                bool isSpaceOccupied = Physics.CheckBox(checkCenter, _currentLooseBuilding.clearanceSize / 2f,
                    _ghostObject.rotation, _obstacleLayer);

                if (!isSpaceOccupied)
                {
                    if (CheckAndConsumeRequirements(_currentLooseBuilding.requirements))
                    {
                        Instantiate(_currentLooseBuilding.prefab, hitInfo.point, Quaternion.Euler(0, _looseObjectRotation, 0));
                    }
                }
            }
        }
        else
        {
            Vector3 origin = _mainCamera.transform.position;
            Vector3 direction = _mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                //Pide al gridManger que le diga que piso es
                Grid<GridObject> currentGrid = GridManager.Instance.GetGrid(hitInfo.point);
                currentGrid.GetXZ(hitInfo.point, out int x, out int z);
                GridObject gridObject = currentGrid.GetGridObject(x, z);

                //Si la casilla esta libre contruye el suelo y lo guarda en su memoria
                if (gridObject.CanBuild())
                {
                    Vector3 buildPosition = currentGrid.GetWorldPosition(x, z);
                    Vector3 centerPosition = buildPosition + (_cellSizeDimension/2);

                    //Comprobamos si hay algun objeto en esa casilla que nos bloquee la construccion
                    if (!Physics.CheckBox(centerPosition, _cellSizeDimension / 2, Quaternion.identity, _obstacleLayer))
                    {
                        // Comprobamos y cobramos los materiales del Suelo
                        if (CheckAndConsumeRequirements(_currentBuilding.requirements))
                        {
                            Transform builtObject = Instantiate(_currentBuilding.prefab, buildPosition, Quaternion.identity);
                            gridObject.SetPlacedObject(builtObject);
                        }
                    }
                    else
                        Debug.Log("Hay un obstaculo en la casilla");
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
        else if (_looseMode)
        {
            Vector3 origin = _mainCamera.transform.position;
            Vector3 direction = _mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _raycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 checkCenter = hitInfo.point + new Vector3(0, _currentLooseBuilding.clearanceSize.y / 2f, 0);

                bool isSpaceOccupied = Physics.CheckBox(checkCenter, _currentLooseBuilding.clearanceSize / 2f,
                    _ghostObject.rotation, _obstacleLayer);

                if (!isSpaceOccupied)
                {
                    _ghostObject.gameObject.SetActive(true);
                    _ghostObject.position = hitInfo.point;
                    _ghostObject.rotation = Quaternion.Euler(0, _looseObjectRotation, 0); 
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
                        Vector3 targetPosition = currentGrid.GetWorldPosition(x, z);
                        Vector3 centerPosition = targetPosition + (_cellSizeDimension / 2);

                        if (!Physics.CheckBox(centerPosition, _cellSizeDimension / 2, Quaternion.identity, _obstacleLayer))
                        {
                            _ghostObject.gameObject.SetActive(true);
                            _ghostObject.position = targetPosition;
                            _ghostObject.rotation = Quaternion.identity;
                        }
                        else
                            _ghostObject.gameObject.SetActive(false);
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
        else if (_looseMode)
        {
            if (_currentLooseBuilding != null)
                _ghostObject = Instantiate(_currentLooseBuilding.ghostPrefab);
        }
        else
        {
            if (_currentBuilding != null)
                _ghostObject = Instantiate(_currentBuilding.ghostPrefab);
        }

        if (_ghostObject != null)
            _ghostObject.gameObject.SetActive(false);
    }

    public void EquipPiece(RadialMenuElement newPiece)
    {
        if (_ghostObject != null)
        {
            Destroy(_ghostObject.gameObject);
        }

        _wallMode = newPiece.isWallType;
        _stairsMode = newPiece.isStairs;
        _looseMode = newPiece.isLooseObject;

        if (_wallMode)
            _currentWallBuilding = newPiece.wallPieceToBuild;
        else if (_looseMode)
            _currentLooseBuilding = newPiece.loosePieceToBuild;
        else
            _currentBuilding = newPiece.floorPieceToBuild;

        RefreshGhost();
    }

    private FloorPlacedObject.Edge GetOppositeEdge(FloorPlacedObject.Edge edge)
    {
        switch (edge)
        {
            case FloorPlacedObject.Edge.Up: return FloorPlacedObject.Edge.Down;
            case FloorPlacedObject.Edge.Down: return FloorPlacedObject.Edge.Up;
            case FloorPlacedObject.Edge.Left: return FloorPlacedObject.Edge.Right;
            case FloorPlacedObject.Edge.Right: return FloorPlacedObject.Edge.Left;
            default: return edge;
        }
    }

    private bool CheckAndConsumeRequirements(BuildRequirement[] requirements)
    {
        if (requirements == null || requirements.Length == 0)
            return true;

        SceneInventoryController inventory = SceneInventoryController.Instance;
        if (inventory == null)
            return false;

        for (int i = 0; i < requirements.Length; i++)
        {
            if (!inventory.HasItem(requirements[i].itemId, requirements[i].amount))
            {
                Debug.Log("Faltan materiales. Necesitas más: " + requirements[i].itemId);
                return false;
            }
        }

        var itemsToConsume = new (string itemID, int amount)[requirements.Length];
        for (int i = 0; i < requirements.Length; i++)
        {
            itemsToConsume[i] = (requirements[i].itemId, requirements[i].amount);
        }

        return inventory.ConsumeItems(itemsToConsume);
    }
    private void OnDrawGizmos()
    {
        if (_looseMode && _ghostObject != null && _currentLooseBuilding != null && _ghostObject.gameObject.activeInHierarchy)
        {
            Vector3 center = _ghostObject.position + new Vector3(0, _currentLooseBuilding.clearanceSize.y / 2f, 0);
            Gizmos.matrix = Matrix4x4.TRS(center, _ghostObject.rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawCube(Vector3.zero, _currentLooseBuilding.clearanceSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, _currentLooseBuilding.clearanceSize);
        }
    }
}