using System.Collections.Generic;
using UnityEngine;

public class CentryController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject _proyectile;
    [SerializeField] private Transform _centryRotation;
    [SerializeField] private Transform _bulletsPivot;
    [SerializeField] private Transform _raycasrPivot;
    private BoxCollider _centryCollider;

    [Header("Disparo")]
    [SerializeField] private float _fireRate = 0.5f;
    [SerializeField] private float _bulletSpeed = 100f;
    private float _timeSinceLastShot = 0f;

    private GameObject actualObjective;

    private List<GameObject> _enemiesList = new List<GameObject>();

    private void Start()
    {
        _centryCollider = GetComponent<BoxCollider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
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
        _enemiesList.RemoveAll(e => e == null);

        if (_enemiesList.Count > 0)
        {
            if (actualObjective != null)
            {
                Vector3 rayOrigin = _raycasrPivot.transform.position;
                Vector3 rayDirection = actualObjective.transform.position - rayOrigin;

                if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit))
                {
                    if (hit.collider.gameObject != actualObjective)
                        actualObjective = null;
                }
            }
            if (actualObjective == null && _enemiesList.Count > 0)
            {
                GameObject closestEnemy = null;
                float closestDistance = Mathf.Infinity;

                for (int i = 0; i < _enemiesList.Count; i++)
                {
                    Vector3 origin = _raycasrPivot.transform.position;
                    Vector3 enemyPosition = _enemiesList[i].transform.position;
                    Vector3 direction = enemyPosition - origin;

                    if (Physics.Raycast(origin, direction, out RaycastHit hit))
                    {
                        if (hit.collider.gameObject == _enemiesList[i])
                        {
                            float distance = Vector3.Distance(origin, enemyPosition);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestEnemy = _enemiesList[i];
                            }
                        }   
                    }
                }
                actualObjective = closestEnemy;
            }
            if (actualObjective != null)
                PointAndShoot(actualObjective);
        }
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
            if (bulletCollider != null)
            {
                if (_centryCollider != null)
                    Physics.IgnoreCollision(bulletCollider, _centryCollider);
            }

            Rigidbody bulletRb = newBullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
                bulletRb.linearVelocity = _bulletsPivot.forward * _bulletSpeed;

            Destroy(newBullet, 3f);
            _timeSinceLastShot = 0f;
        }
    }
}
