using NUnit.Framework.Constraints;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/BuildingPiece")]
public class BuildingPieceSO : ScriptableObject
{
    public string nameString; 
    public Transform prefab;
    public Transform ghostPrefab;

    public int whith = 1;
    public int hieght = 1;
}
