using UnityEngine;

public class GameCameraManager : MonoBehaviour
{
    [SerializeField] private GameObject lobbyCanvas;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera gameCamera;

    public void EnterGame()
    {
        // Hide lobby
        lobbyCanvas.SetActive(false);

        // Turn off lobby camera
        mainCamera.enabled = false;

        // Turn on game camera
        gameCamera.enabled = true;
    }
}