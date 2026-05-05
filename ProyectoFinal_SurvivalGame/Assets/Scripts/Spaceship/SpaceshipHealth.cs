using UnityEngine;

public class SpaceshipHealth : MonoBehaviour, IDamagable
{
    [Header("Variables de la vida")]
    [SerializeField] private int _maxHealth;
    [SerializeField] GameObject _spaceship;
    private int _currentHealth;

    void Start()
    {
        _currentHealth = _maxHealth;    
    }

    void Update()
    {
        //Debug.Log(_currentHealth);
    }

    public void DamageRecived(int damage)
    {
        _currentHealth -= damage;

        if (_currentHealth <= 0)
        {
            GameStateManager.Instance.ChangeGameState(GameState.StateType.OVER);
            Destroy(_spaceship);
        }
    }

    
}
