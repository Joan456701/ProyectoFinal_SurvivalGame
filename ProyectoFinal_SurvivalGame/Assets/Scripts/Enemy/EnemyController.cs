using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private NavMeshAgent _navAgent;
    void Start()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        
    }
}
