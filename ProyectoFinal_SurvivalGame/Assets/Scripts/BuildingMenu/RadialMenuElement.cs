using UnityEngine;

[CreateAssetMenu(fileName = "RadialElement", menuName = "Radial Menu/Menu Element")]
public class RadialMenuElement : ScriptableObject
{
    public string elementName;
    public Sprite icon;

    public RadialMenuSO nextMenu;

    [Header("Piezas de Construccion")]
    public FloorEdgeObjectTypeSO wallPieceToBuild;
    public BuildingPieceSO floorPieceToBuild;
    public LooseObjectSO loosePieceToBuild;

    public bool isWallType;
    public bool isStairs;
    public bool isLooseObject;

    public Sprite materialRequired;
}
