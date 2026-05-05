using System.Collections.Generic;
using UnityEngine;

public class DayNightSpawnManager : MonoBehaviour
{
    public enum DayCycleState
    {
        Day,
        Night
    }

    [Header("Control iluminacion del sol")]
    [SerializeField] private Transform _sunTransform;
    [SerializeField] private WeatherProfileSO _normalWeather;
    [SerializeField] private WeatherProfileSO _bloodMoonWeather;
    [SerializeField] private Light _sunLight;

    private WeatherProfileSO _currentWeather;

    [Header("Duracion del dia y la noche")]
    [SerializeField] private float _dayDuration;
    [SerializeField] private float _nightDuration;

    [Header("Numero de rondas")]
    [SerializeField] private int _maxNightsSurvive;
    private DayCycleState _currentState;
    private int _nightsCount = -1;
    private float _timer;

    [Header("Variables de spawn y enemigos")]
    [SerializeField] private GameObject _enemyPrefab;
    private List<GameObject> _enemyInScene = new List<GameObject>();
    private List<Transform> _spawnPoints = new List<Transform>();

    [Header("Dificultad de spawneo")]
    [SerializeField] private int _baseMaxEnemies = 5;
    [SerializeField] private int _extraEnemiesNight = 2;
    [SerializeField] private float _baseSpawnRate = 6f;
    [SerializeField] private float _spawnRateDecrease = 0.5f;
    [SerializeField] private float _minSpawnRate = 1.5f;
    [SerializeField] private int _nightsToIncreaseSpeed = 3;

    private float _spawnTimer = 0;
    private void Start()
    {
        SpawnIdentificator[] identificators = FindObjectsByType<SpawnIdentificator>(FindObjectsSortMode.None);

        foreach (SpawnIdentificator identificator in identificators)
            _spawnPoints.Add(identificator.transform);

        StartDay();
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        UpdateVisuals();

        if (_currentState == DayCycleState.Night)
            HandleNightSpawning();

        if (_timer <= 0)
        {
            if (_currentState == DayCycleState.Day)
                StartNight();
            else if (_currentState == DayCycleState.Night)
            {
                if ((_nightsCount + 1) >= _maxNightsSurvive)
                {
                    GameStateManager.Instance.ChangeGameState(GameState.StateType.WIN);
                    return;
                }

                StartDay();
            }
        }
    }

    private void StartDay()
    {
        _currentState = DayCycleState.Day;
        _timer = _dayDuration;

        _currentWeather = _normalWeather;

        CleanRemainingEnemies();
        Debug.Log("Esta saliendo el soool");
    }

    private void StartNight()
    {
        _currentState = DayCycleState.Night;
        _timer = _nightDuration;
        _nightsCount++;
        
        if ((_nightsCount + 1) % _nightsToIncreaseSpeed == 0)
        {
            _currentWeather = _bloodMoonWeather;
            Debug.Log("Aumento de nivel");
        }
        else
        {
            _currentWeather = _normalWeather;
            Debug.Log("A mimir");
        }
    }

    private void HandleNightSpawning()
    {
        _spawnTimer += Time.deltaTime;

        int maxEnemiesThisNight = _baseMaxEnemies + (_extraEnemiesNight * _nightsCount);

        int difficultyTier = (_nightsCount + 1) / _nightsToIncreaseSpeed;

        float currentSpawnRate = _baseSpawnRate - (_spawnRateDecrease * difficultyTier);
        currentSpawnRate = Mathf.Max(currentSpawnRate, _minSpawnRate);

        CleanDeadEnemies();

        if (_spawnTimer >= currentSpawnRate && _enemyInScene.Count < maxEnemiesThisNight)
        {
            _spawnTimer = 0;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (_spawnPoints.Count == 0) 
            return;

        int randomIndex = Random.Range(0, _spawnPoints.Count);
        Transform selectedPoint = _spawnPoints[randomIndex];

        GameObject newEnemy = Instantiate(_enemyPrefab, selectedPoint.position, selectedPoint.rotation);

        _enemyInScene.Add(newEnemy);
    }

    private void CleanDeadEnemies()
    {
        for (int i = _enemyInScene.Count - 1; i >= 0; i--)
        {
            if ( _enemyInScene[i] == null )
                _enemyInScene.RemoveAt(i);
        }
    }

    private void CleanRemainingEnemies()
    {
        CleanDeadEnemies();

        for (int i = 0; i < _enemyInScene.Count; i++)
        {
            if (_enemyInScene[i] != null)
                Destroy(_enemyInScene[i]);
        }

        CleanDeadEnemies();
    }

    private void UpdateVisuals()
    {
        if (_sunTransform == null || _sunLight == null || _currentWeather == null) return;

        float progress;
        float currentAngle;

        if (_currentState == DayCycleState.Day)
        {
            progress = 1f - (_timer / _dayDuration);
            currentAngle = Mathf.Lerp(0f, 180f, progress);

            _sunLight.color = _currentWeather.dayDirectionalColor.Evaluate(progress);
            RenderSettings.ambientLight = _currentWeather.dayAmbientColor.Evaluate(progress);
            RenderSettings.fogColor = _currentWeather.dayFogColor.Evaluate(progress);

            RenderSettings.skybox.SetColor("_SkyGradientTop", _currentWeather.daySkyTop.Evaluate(progress));
            RenderSettings.skybox.SetColor("_SkyGradientBottom", _currentWeather.daySkyBottom.Evaluate(progress));
            RenderSettings.skybox.SetColor("_HorizonLineColor", _currentWeather.dayHorizon.Evaluate(progress));
        }
        else
        {
            progress = 1f - (_timer / _nightDuration);
            currentAngle = Mathf.Lerp(180f, 360f, progress);

            _sunLight.color = _currentWeather.nightDirectionalColor.Evaluate(progress);
            RenderSettings.ambientLight = _currentWeather.nightAmbientColor.Evaluate(progress);
            RenderSettings.fogColor = _currentWeather.nightFogColor.Evaluate(progress);

            RenderSettings.skybox.SetColor("_SkyGradientTop", _currentWeather.nightSkyTop.Evaluate(progress));
            RenderSettings.skybox.SetColor("_SkyGradientBottom", _currentWeather.nightSkyBottom.Evaluate(progress));
            RenderSettings.skybox.SetColor("_HorizonLineColor", _currentWeather.nightHorizon.Evaluate(progress));
        }

        _sunTransform.rotation = Quaternion.Euler(currentAngle, -30f, 0f);
    }
}
