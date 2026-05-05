using UnityEngine;

[CreateAssetMenu(fileName = "Edge Building", menuName = "Building Element/Edge Building")]
public class FloorEdgeObjectTypeSO : ScriptableObject
{
    public Transform prefab;
    public Transform ghostPrefab;
    public bool isStairs;

    [Header("Costes de Construcción")]
    public BuildRequirement[] requirements;
}
