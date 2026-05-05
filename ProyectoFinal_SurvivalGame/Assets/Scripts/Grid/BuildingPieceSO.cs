using NUnit.Framework.Constraints;
using UnityEngine;

[CreateAssetMenu(fileName = "Structure Building", menuName = "Building Element/Structure Building")]
public class BuildingPieceSO : ScriptableObject
{
    public string nameString; 
    public Transform prefab;
    public Transform ghostPrefab;

    public int whith = 1;
    public int hieght = 1;

    [Header("Costes de Construcción")]
    public BuildRequirement[] requirements;
}
