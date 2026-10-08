using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private GameManager gameManager;

    public void JoinGame()
    {
        gameManager.JoinGame(joinCodeInput.text);
    }
}
