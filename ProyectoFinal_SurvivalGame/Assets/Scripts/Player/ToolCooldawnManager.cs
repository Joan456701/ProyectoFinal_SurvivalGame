using System;
using UnityEngine;

public class ToolCooldawnManager : MonoBehaviour
{
    [SerializeField] private PlayerInputHandler _pInputHandler;
    [SerializeField] private float _maxCooldownTime;
    
    private float _cooldownTime;

    public event Action OnActionFired;
    public bool _youCanAttack {get;  private set;}

    private void Start()
    {
        _cooldownTime = _maxCooldownTime; 
        _youCanAttack = true;
    }

    void Update()
    {
        if (_cooldownTime < _maxCooldownTime)
        {
            _cooldownTime += Time.deltaTime;

            if (_cooldownTime >= _maxCooldownTime)
            {
                _youCanAttack = true;
            }
        }

        if (_pInputHandler.attackTiggered && _youCanAttack)
        {
            OnActionFired?.Invoke();

            _youCanAttack = false;
            _cooldownTime = 0;
        }
    }
}
