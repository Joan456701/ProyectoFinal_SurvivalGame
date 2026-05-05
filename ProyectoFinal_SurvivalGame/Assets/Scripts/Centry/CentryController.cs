using System.Collections.Generic;
using UnityEngine;

public class CentryController : MonoBehaviour, IDamagable
{
    [Header("Referencias")]
    [SerializeField] private GameObject _proyectile;
    [SerializeField] private Transform _centryRotation;
    [SerializeField] private Transform _bulletsPivot;
    [SerializeField] private Transform _raycastPivot;
    private BoxCollider _centryCollider;

    [Header("Disparo")]
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _bulletSpeed = 100f;
    private float _timeSinceLastShot = 0f;

    [SerializeField] private float _targetSearchRate = 0.25f;
    private float _timeSinceLastSearch = 0f;

    [Header("Vida de la torreta")]
    [SerializeField] private int _maxHealth;
    private int _currentHealth;

    private GameObject actualObjective;

    private List<GameObject> _enemiesList = new List<GameObject>();

    private void Start()
    {
        _centryCollider = GetComponent<BoxCollider>();

        _currentHealth = _maxHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && !_enemiesList.Contains(other.gameObject))
        {
            _enemiesList.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            if (actualObjective == enemy.gameObject)
                actualObjective = null;

            _enemiesList.Remove(enemy.gameObject);
        }
    }

    private void Update()
    {
        _timeSinceLastSearch += Time.deltaTime;
        if (_timeSinceLastSearch >= _targetSearchRate)
        {
            FindBestTarget();
            _timeSinceLastSearch = 0f;
        }

        if (actualObjective != null)
        {
            PointAndShoot(actualObjective);
        }
    }

    private void FindBestTarget()
    {
        for (int i = _enemiesList.Count - 1; i >= 0; i--)
        {
            if (_enemiesList[i] == null || !_enemiesList[i].activeInHierarchy)
            {
                if (actualObjective == _enemiesList[i]) actualObjective = null;
                _enemiesList.RemoveAt(i);
            }
        }

        if (_enemiesList.Count == 0) return;

        if (actualObjective != null)
        {
            Vector3 rayOrigin = _raycastPivot.position;
            Vector3 rayDirection = actualObjective.transform.position - rayOrigin;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit))
            {
                if (hit.collider.gameObject != actualObjective)
                {
                    actualObjective = null;
                }
                else
                {
                    return;
                }
            }
        }

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        Vector3 origin = _raycastPivot.position;

        for (int i = 0; i < _enemiesList.Count; i++)
        {
            Vector3 enemyPosition = _enemiesList[i].transform.position;
            float distance = Vector3.Distance(origin, enemyPosition);

            if (distance < closestDistance)
            {
                Vector3 direction = enemyPosition - origin;
                if (Physics.Raycast(origin, direction, out RaycastHit hit))
                {
                    if (hit.collider.gameObject == _enemiesList[i])
                    {
                        closestDistance = distance;
                        closestEnemy = _enemiesList[i];
                    }
                }
            }
        }

        actualObjective = closestEnemy;
    }

    private void PointAndShoot(GameObject objective)
    {
        Vector3 enemyDirection = objective.transform.position - _centryRotation.position;
        enemyDirection.y = 0;

        if (enemyDirection != Vector3.zero)
        {
            Quaternion finalRotation = Quaternion.LookRotation(enemyDirection);
            _centryRotation.rotation = Quaternion.Slerp(_centryRotation.rotation, finalRotation, Time.deltaTime * 5f);
        }

        _timeSinceLastShot += Time.deltaTime;
        if (_timeSinceLastShot >= _fireRate)
        {
            GameObject newBullet = Instantiate(_proyectile, _bulletsPivot.position, _bulletsPivot.rotation);

            Collider bulletCollider = newBullet.GetComponent<Collider>();
            if (bulletCollider != null && _centryCollider != null)
            {
                Physics.IgnoreCollision(bulletCollider, _centryCollider);
            }

            Rigidbody bulletRb = newBullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
                bulletRb.linearVelocity = _bulletsPivot.forward * _bulletSpeed;

            Destroy(newBullet, 3f);
            _timeSinceLastShot = 0f;
        }
    }

    public void DamageRecived(int damage)
    {
        _currentHealth -= damage;

        if (_currentHealth <= 0)
            Destroy(gameObject);
    }
}
