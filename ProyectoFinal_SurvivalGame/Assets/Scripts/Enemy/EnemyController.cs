using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IDamagable
{
    [Header("Rango Deteccion")]
    [SerializeField] private float _listeningRange;
    [SerializeField] private float _vanishDistance;

    [Header("Variables de ataque")]
    [SerializeField] protected float _attackDistance = 4f;
    [SerializeField] protected float _cooldown = 2;
    [SerializeField] protected int _attackDamage = 1;

    private float _timeSinceLastAttack = 0;

    [Header("Tiempo de busqueda")]
    [SerializeField] private float _maxTime = 5f;

    [Header("Vida")]
    [SerializeField] protected int _maxHealth;
    private int _health;

    [Header("Inteligencia de Destrucción")]
    [SerializeField] private float _maxDetourMultiplier = 1.5f;

    protected BoxCollider _spaceshipCollider;
    protected Transform _spaceshipTarget;
    protected Transform _playerTarget;
    protected Transform _currentTarget;
    protected NavMeshAgent _navAgent;

    private float _timeWithoutSeeingPlayer = 0f;
    private bool isJumping = false;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();

        _navAgent.stoppingDistance = _attackDistance - 0.5f;
    }
    void Start()
    {
        SpaceshipIdentificator _spaceship = FindFirstObjectByType<SpaceshipIdentificator>();

        if (_spaceship != null)
        { 
            _spaceshipTarget = _spaceship.transform; 
            _spaceshipCollider = _spaceship.GetComponent<BoxCollider>();
        }
            
        _currentTarget = _spaceshipTarget.transform;
        UpdateDestination();

        _health = _maxHealth;
    }
    private void OnEnable(){FirstPersonController.OnPlayerAttackEvent += ListenThePlayer;}

    private void OnDisable(){FirstPersonController.OnPlayerAttackEvent -= ListenThePlayer;}

    protected virtual void Update()
    {
        if (_currentTarget == null || _spaceshipTarget == null)
            return;

        if (_currentTarget == _playerTarget)
        {
            HandlePlayerTarget();
        }
        else if (_currentTarget == _spaceshipTarget)
        {
            HandleSpaceshipTarget(); 
        }

        if (_navAgent.isOnOffMeshLink && !isJumping)
        {
            StartCoroutine(SmoothJump());
        }
    }

    protected virtual void HandlePlayerTarget()
    {
        UpdateDestination();

        float distance = Vector3.Distance(transform.position, _currentTarget.position);
        if (distance >= _vanishDistance)
            PlayerLost();
        else if (distance <= _attackDistance)
            AttackThePlayer();
        else
            _timeWithoutSeeingPlayer = 0;
    }

    private void UpdateDestination()
    {
        if (_currentTarget == null || _spaceshipTarget == null)
            return;

        if (_currentTarget != null)
        {
            Vector3 targetPos = _currentTarget.position;

            if (_currentTarget == _spaceshipTarget && _spaceshipCollider != null)
            {
                targetPos = _spaceshipCollider.ClosestPoint(transform.position);
            }

            if (Vector3.Distance(_navAgent.destination, targetPos) > 1f)
            {
                _navAgent.SetDestination(targetPos);
            }
        }
    }

    protected virtual void ListenThePlayer(Transform pPosition)
    {
        float distance = Vector3.Distance(transform.position, pPosition.position);

        if (distance <= _listeningRange)
        {
            _playerTarget = pPosition;
            _currentTarget = _playerTarget;
        }
    }

    protected virtual void HandleSpaceshipTarget()
    {
        Vector3 targetPos = _spaceshipTarget.position;
        if (_spaceshipCollider != null)
            targetPos = _spaceshipCollider.ClosestPoint(transform.position);

        NavMeshPath virtualPath = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, virtualPath);

        float straightDistance = Vector3.Distance(transform.position, targetPos);
        float walkingDistance = CalculatePathLength(virtualPath);

        bool isPathBlocked = virtualPath.status == NavMeshPathStatus.PathPartial;
        bool isDetourTooLong = walkingDistance > (straightDistance * _maxDetourMultiplier);

        Vector3 dirToSpaceship = (targetPos - transform.position).normalized;
        dirToSpaceship.y = 0;

        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        bool isWallInFront = Physics.Raycast(rayOrigin, dirToSpaceship, _attackDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (isPathBlocked || isDetourTooLong)
        {
            NavMeshHit navHit;
            if (NavMesh.Raycast(transform.position, targetPos, out navHit, NavMesh.AllAreas))
            {
                _navAgent.SetDestination(navHit.position);
            }

            if (dirToSpaceship != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToSpaceship), Time.deltaTime * 5f);

            if (isWallInFront)
            {
                _navAgent.isStopped = true;
                TryAttackObstacle();
            }
            else
            {
                _navAgent.isStopped = false;
            }
        }
        else
        {
            _navAgent.SetDestination(targetPos);

            if (straightDistance <= _attackDistance)
            {
                _navAgent.isStopped = true;
                TryAttackObstacle();
            }
            else
            {
                _navAgent.isStopped = false;
            }
        }
    }

    protected virtual void AttackThePlayer()
    {
        _timeWithoutSeeingPlayer = 0f;
        _timeSinceLastAttack += Time.deltaTime;

        if (_timeSinceLastAttack >= _cooldown)
        {
            IDamagable objDamagable = _currentTarget.GetComponent<IDamagable>();

            if (objDamagable != null)
            {
                objDamagable.DamageRecived(_attackDamage);
                _timeSinceLastAttack = 0f;
                Debug.Log("¡El enemigo atacó al jugador!");
            }
        }
    }

    private void PlayerLost()
    {
        _timeWithoutSeeingPlayer += Time.deltaTime;

        if (_timeWithoutSeeingPlayer >= _maxTime)
        {
            _currentTarget = _spaceshipTarget;
            _timeWithoutSeeingPlayer = 0;
            UpdateDestination();
        }
    }

    protected virtual void TryAttackObstacle()
    {
        _timeSinceLastAttack += Time.deltaTime;

        if (_timeSinceLastAttack >= _cooldown)
        {
            Vector3 targetPos = _currentTarget.position;

            if (_currentTarget == _spaceshipTarget && _spaceshipCollider != null)
                targetPos = _spaceshipCollider.ClosestPoint(transform.position);

            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3 dirToTarget = (targetPos - transform.position).normalized;
            dirToTarget.y = 0;

            if (Physics.Raycast(origin, dirToTarget, out RaycastHit hit, _attackDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                IDamagable obstacle = hit.collider.GetComponentInParent<IDamagable>();

                if (obstacle != null)
                {
                    obstacle.DamageRecived(_attackDamage);
                    _timeSinceLastAttack = 0;
                }
            }
        }
    }

    public void DamageRecived(int damage)
    {
        _health -= damage;

        if (_health <= 0)
            Destroy(gameObject);
    }

    private float CalculatePathLength(NavMeshPath path)
    {
        if (path == null || path.corners.Length < 2)
            return 0f;

        float totalDistance = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return totalDistance;
    }

    private System.Collections.IEnumerator SmoothJump()
    {
        isJumping = true;

        OffMeshLinkData data = _navAgent.currentOffMeshLinkData;
        Vector3 startPos = _navAgent.transform.position;

        Vector3 endPos = data.endPos + Vector3.up * _navAgent.baseOffset;

        float duration = 0.6f;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, time);

            currentPos.y += 1f * Mathf.Sin(time * Mathf.PI);

            _navAgent.transform.position = currentPos;

            yield return null;
        }

        _navAgent.CompleteOffMeshLink();
        isJumping = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _listeningRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _vanishDistance);
    }
}
