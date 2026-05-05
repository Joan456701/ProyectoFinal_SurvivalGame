using UnityEngine;

[CreateAssetMenu(fileName = "GameStateGameplay", menuName = "GameStates/GSGameplay", order = 1)]
public class GSGameplay : GameState
{
    public override void OnEnter()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UIGamePlay gameplayUI = FindObjectOfType<UIGamePlay>(true);
        if (gameplayUI != null)
            gameplayUI.gameObject.SetActive(true);
    }

    public override void OnUpdate()
    {
        PlayerInputHandler inputHandler = FindFirstObjectByType<PlayerInputHandler>();

        if (inputHandler != null && inputHandler.pauseTriggered)
        {     
            inputHandler.pauseTriggered = false;
            GameStateManager.Instance.ChangeGameState(StateType.PAUSE);
        }
        
    }

    public override void OnExit()
    {
        UIGamePlay gameplayUI = FindObjectOfType<UIGamePlay>(true);
        if (gameplayUI != null)
            gameplayUI.gameObject.SetActive(false);
    }
}
