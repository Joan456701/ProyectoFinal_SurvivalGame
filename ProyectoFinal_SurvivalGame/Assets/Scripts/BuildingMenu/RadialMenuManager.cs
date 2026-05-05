using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class RadialMenuManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RadialMenuPiece _piecePrefab;
    [SerializeField] private FirstPersonBuilder _builder;
    [SerializeField] private Transform menuContainer;
    [SerializeField] private RadialMenuSO mainMenu;

    private PlayerInputHandler _pInputHandler;

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    public bool isMenuActive {get; private set;}

    private List<RadialMenuPiece> _spawnedPieces = new List<RadialMenuPiece>();
    private RadialMenuSO _currentMenu;
    private int _selectedIndex;

    [SerializeField] private Image _aimCursor;
    [SerializeField] private TMP_Text _centralCostText;
    [SerializeField] private Image _centralMaterialIcon;

    private void Start()
    {
        _pInputHandler = FindFirstObjectByType<PlayerInputHandler>();
        isMenuActive = false;

        _centralCostText.gameObject.SetActive(false);
        _centralMaterialIcon.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (isMenuActive)
        {
            CalculateMouseAngle(_pInputHandler.mousePosition);

            if (_pInputHandler.submitTriggered)
            {
                if (_selectedIndex != -1 && _currentMenu.elements.Length > 0)
                {
                    ExecuteAction(_currentMenu.elements[_selectedIndex]);

                    _pInputHandler.submitTriggered = false;
                }
            }

            if (_pInputHandler.cancelTriggered)
            {
                CloseMenu();
                _pInputHandler.cancelTriggered = false;
            }
        }
        else
        {
            if (_pInputHandler.buildingMenu && _pInputHandler.isBuildMode)
            {
                OpenMenu();
                _pInputHandler.buildingMenu = false;
            }
        }
    }

    public void OpenMenu()
    {
        isMenuActive = true;
        _aimCursor.gameObject.SetActive(false);

        menuContainer.gameObject.SetActive(true);
        _currentMenu = mainMenu;
        
        SpawnMenuElements();

        _pInputHandler.SwitchActionMap("UI");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        isMenuActive = false;
        _aimCursor.gameObject.SetActive(true);

        menuContainer.gameObject.SetActive (false);
        
        ClearMenuElements();

        _pInputHandler.SwitchActionMap("Player");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _selectedIndex = -1;
    }

    private void SpawnMenuElements()
    {
        ClearMenuElements();

        if (_currentMenu.elements.Length == 0)
            return;

        float stepLength = 360f / _currentMenu.elements.Length;
        float fillAmount = 1f / _currentMenu.elements.Length;

        for (int i = 0; i < _currentMenu.elements.Length; i++)
        {
            RadialMenuElement elementSO = _currentMenu.elements[i];
            RadialMenuPiece newPiece = Instantiate(_piecePrefab, menuContainer);
            _spawnedPieces.Add(newPiece);

            newPiece.backgroundImage.fillAmount = fillAmount;
            newPiece.backgroundImage.color = normalColor;

            newPiece.transform.localRotation = Quaternion.Euler(0, 0, (i * -stepLength) + (stepLength / 2f));
            newPiece.iconImage.sprite = elementSO.icon;

            float iconDistance = newPiece.iconImage.rectTransform.anchoredPosition.y;
            Vector3 centeredPosition = Quaternion.Euler(0, 0, -stepLength / 2f) * new Vector3(0, iconDistance, 0);

            newPiece.iconImage.rectTransform.anchoredPosition = centeredPosition;
            newPiece.iconImage.transform.rotation = Quaternion.identity;
        }

        ClearCentralCostUI();
    }

    private void UpdateCentralCostUI(RadialMenuElement elementSO)
    {
        if (_centralCostText == null || _centralMaterialIcon == null) return;

        BuildRequirement[] reqs = null;
        if (elementSO.isWallType && elementSO.wallPieceToBuild != null)
            reqs = elementSO.wallPieceToBuild.requirements;
        else if (elementSO.isLooseObject && elementSO.loosePieceToBuild != null)
            reqs = elementSO.loosePieceToBuild.requirements;
        else if (!elementSO.isWallType && !elementSO.isLooseObject && elementSO.floorPieceToBuild != null)
            reqs = elementSO.floorPieceToBuild.requirements;

        if (elementSO.nextMenu != null || reqs == null || reqs.Length == 0)
        {
            ClearCentralCostUI();
            return;
        }

        BuildRequirement mainReq = reqs[0];
        SceneInventoryController inventory = SceneInventoryController.Instance;

        _centralCostText.gameObject.SetActive(true);
        _centralMaterialIcon.gameObject.SetActive(true);

        if (inventory != null)
        {
            bool canAfford = inventory.HasItem(mainReq.itemId, mainReq.amount);
            _centralCostText.text = inventory.GetTotalItemAmount(mainReq.itemId).ToString() + " / " + mainReq.amount.ToString();
            _centralCostText.color = canAfford ? Color.white : Color.red;
        }

        _centralMaterialIcon.sprite = elementSO.materialRequired;
    }

    private void ClearCentralCostUI()
    {
        if (_centralCostText != null) _centralCostText.gameObject.SetActive(false);
        if (_centralMaterialIcon != null) _centralMaterialIcon.gameObject.SetActive(false);
    }
    private void ClearMenuElements()
    {
        foreach (var piece in _spawnedPieces)
        {
            Destroy(piece.gameObject);
        }
        _spawnedPieces.Clear();

        _selectedIndex = -1;
    }

    private void CalculateMouseAngle(Vector2 mouse)
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mousePosition = mouse;
        Vector2 direction = mousePosition - screenCenter;

        if (direction.magnitude < 20f)
        {
            ChangeHighlight(-1);
            return;
        }

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        if (angle < 0) 
            angle += 360f;

        float stepLength = 360f / _currentMenu.elements.Length;

        int newIndex = Mathf.FloorToInt((angle + (stepLength / 2)) / stepLength);
        if (newIndex >= _currentMenu.elements.Length) newIndex = 0;

        ChangeHighlight(newIndex);
    }

    private void ChangeHighlight(int newIndex)
    {
        if (_selectedIndex == newIndex)
            return;

        if (_selectedIndex != -1)
            _spawnedPieces[_selectedIndex].backgroundImage.color = normalColor;

        _selectedIndex = newIndex;

        if (_selectedIndex != -1)
        {
            _spawnedPieces[_selectedIndex].backgroundImage.color = highlightColor;

            UpdateCentralCostUI(_currentMenu.elements[_selectedIndex]);
        }
        else
        {
            ClearCentralCostUI();
        }
    }

    private void ExecuteAction(RadialMenuElement selectedElement)
    {
        if (selectedElement.nextMenu != null)
        {
            _currentMenu = selectedElement.nextMenu;
            SpawnMenuElements();
        }
        else
        {
            if (_builder != null)
                _builder.EquipPiece(selectedElement);

            CloseMenu();
        }
    }
}
