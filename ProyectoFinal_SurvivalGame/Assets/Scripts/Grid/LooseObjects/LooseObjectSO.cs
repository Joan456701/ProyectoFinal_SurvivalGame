using UnityEngine;

[CreateAssetMenu(fileName = "Loose Stucture", menuName = "Building Element/Loose Stucture")]
public class LooseObjectSO : ScriptableObject
{
    public string objectName;
    public Transform prefab;
    public Transform ghostPrefab;

    [Header("Ajustes de Colisión")]
    public Vector3 clearanceSize = new Vector3(1f, 1f, 1f);
}
