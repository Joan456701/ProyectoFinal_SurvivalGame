using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GSOver", menuName = "GameStates/GSOver", order = 1)]
public class GSOver : GameState
{
    public override void OnEnter()
    {
        Time.timeScale = 0.0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerInputHandler inputHandler = FindFirstObjectByType<PlayerInputHandler>();
        if (inputHandler != null) inputHandler.SwitchActionMap("UI");

        UIOver pause = FindObjectOfType<UIOver>(true);
        pause.gameObject.SetActive(true);
    }

    public override void OnUpdate()
    {

    }

    public override void OnExit()
    {
        Time.timeScale = 1.0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerInputHandler inputHandler = FindFirstObjectByType<PlayerInputHandler>();
        if (inputHandler != null) inputHandler.SwitchActionMap("UI");

        UIOver pause = FindObjectOfType<UIOver>();
        pause.gameObject.SetActive(false);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene("CampoDePruebas_Joan");
        GameStateManager.Instance.ChangeGameState(GameState.StateType.GAMEPLAY);
    }

    public void ReturnMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
        GameStateManager.Instance.ChangeGameState(GameState.StateType.MAINMENU);
    }
}