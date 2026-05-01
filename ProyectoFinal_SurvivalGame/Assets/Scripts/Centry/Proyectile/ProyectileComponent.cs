using UnityEngine;

public class ProyectileComponent : MonoBehaviour
{
    [SerializeField] private int _bulletDamage = 1;

    private void Start()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("La bala ha colisionado contra: " + collision.gameObject.name);

        EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();

        if (enemy != null)
        {
            enemy.DamageRecived(_bulletDamage);
        }

        Destroy(gameObject);
    }
}
