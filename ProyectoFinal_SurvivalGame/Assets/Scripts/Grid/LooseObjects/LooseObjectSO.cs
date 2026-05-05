using JetBrains.Annotations;
using UnityEngine;

[CreateAssetMenu(fileName = "Loose Stucture", menuName = "Building Element/Loose Stucture")]
public class LooseObjectSO : ScriptableObject
{
    public string objectName;
    public Transform prefab;
    public Transform ghostPrefab;

    [Header("Ajustes de Colision")]
    public Vector3 clearanceSize = new Vector3(1f, 1f, 1f);

    [Header("Costes de Construcción")]
    public BuildRequirement[] requirements;
}
