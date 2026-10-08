using UnityEngine;
using Unity.Netcode;
using TMPro;

public class GameStartManager : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private GameCameraManager gameCameraManager;

    private void Start()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.GameStarted.OnValueChanged += OnGameStarted;
        }
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.GameStarted.OnValueChanged -= OnGameStarted;
        }
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            statusText.text = "Only the host can start the game!";
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "World",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    private void OnGameStarted(bool oldValue, bool newValue)
    {
        if (!newValue)
            return;

        gameCameraManager.EnterGame();

        if (startGameButton != null)
        {
            startGameButton.SetActive(false);
        }

        Debug.Log("Game started!");
    }
}