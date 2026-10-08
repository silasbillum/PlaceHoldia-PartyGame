using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BoardPointsUI : MonoBehaviour
{
    [Header("Player Names")]
    [SerializeField] private TMP_Text player1Name;
    [SerializeField] private TMP_Text player2Name;
    [SerializeField] private TMP_Text player3Name;
    [SerializeField] private TMP_Text player4Name;

    [Header("Player Points")]
    [SerializeField] private TMP_Text player1Points;
    [SerializeField] private TMP_Text player2Points;
    [SerializeField] private TMP_Text player3Points;
    [SerializeField] private TMP_Text player4Points;

    [Header("Player UI Panels")]
    [SerializeField] private GameObject player1Panel;
    [SerializeField] private GameObject player2Panel;
    [SerializeField] private GameObject player3Panel;
    [SerializeField] private GameObject player4Panel;

    private void Update()
    {
        if (BoardGameState.Instance == null)
            return;

        UpdatePlayer(0, player1Name, player1Points, player1Panel);
        UpdatePlayer(1, player2Name, player2Points, player2Panel);
        UpdatePlayer(2, player3Name, player3Points, player3Panel);
        UpdatePlayer(3, player4Name, player4Points, player4Panel);
    }

    private void UpdatePlayer(
        ulong clientId,
        TMP_Text nameText,
        TMP_Text pointsText,
        GameObject playerPanel)
    {
        if (nameText == null || pointsText == null || playerPanel == null)
            return;

        // Check if this player is actually connected
        bool isConnected =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClientsIds.Contains(clientId);

        // Show/hide the whole player UI
        playerPanel.SetActive(isConnected);

        if (!isConnected)
            return;

        // Get points
        int points = 0;

        if (BoardGameState.Instance.HasPlayerPoints(clientId))
        {
            points =
                BoardGameState.Instance.GetPlayerPoints(clientId);
        }

        nameText.text = "Player " + (clientId + 1);
        pointsText.text = points + " Points";
    }
}
