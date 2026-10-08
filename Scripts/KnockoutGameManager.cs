using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class KnockoutGameManager : NetworkBehaviour
{
    public static KnockoutGameManager Instance { get; private set; }

    [Header("Results UI")]
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private Image resultBG;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private CanvasGroup winnerCanvasGroup;
    [SerializeField] private Button returnButton;

    [Header("Game")]
    [SerializeField] private string minigameName = "Knockout Arena";

    [Header("Timer")]
    [SerializeField] private TMP_Text gameTime;
    [SerializeField] private float maxGameTime = 15f;

    private float timer;

    private readonly List<ulong> eliminationOrder =
        new List<ulong>();

    private bool gameFinished = false;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        timer -= Time.deltaTime;

        if (timer < 0f)
            timer = 0f;

        UpdateTimerClientRpc(
            Mathf.CeilToInt(timer)
        );

        if (timer <= 0f)
        {
            FinishGameOnTime();
        }
    }

    private void FinishGameOnTime()
    {
        if (!IsServer || gameFinished)
            return;

        gameFinished = true;

        List<ulong> activePlayers =
            new List<ulong>();

        foreach (ulong clientId in
            NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!eliminationOrder.Contains(clientId))
            {
                activePlayers.Add(clientId);
            }
        }

        foreach (ulong clientId in activePlayers)
        {
            if (MinigameManager.Instance != null)
            {
                MinigameManager.Instance.GivePoints(
                    clientId,
                    minigameName,
                    4
                );
            }
        }

        string results =
            "TIME'S UP!\n\n";

        foreach (ulong clientId in activePlayers)
        {
            int points = 0;

            if (MinigameManager.Instance != null)
            {
                points =
                    MinigameManager.Instance.GetPointsForPlace(
                        minigameName,
                        4
                    );
            }

            results +=
                "4th: Player " +
                (clientId + 1) +
                "  +" +
                points +
                " POINTS\n";
        }

        ShowResultsClientRpc(
            results,
            "No Winner"
        );
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(
    int time)
    {
        if (gameTime != null)
        {
            gameTime.text = time.ToString();
        }
    }

    private ulong GetLastActivePlayer()
    {
        foreach (ulong clientId
            in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!eliminationOrder.Contains(clientId))
            {
                return clientId;
            }
        }

        return NetworkManager.Singleton.ConnectedClientsIds[0];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            timer = maxGameTime;
        }


        if (resultsText != null)
            resultsText.gameObject.SetActive(false);

        if (resultBG != null)
            resultBG.gameObject.SetActive(false);

        if (winnerText != null)
            winnerText.gameObject.SetActive(false);

        if (winnerCanvasGroup != null)
            winnerCanvasGroup.alpha = 0f;

        if (returnButton != null)
            returnButton.gameObject.SetActive(false);
    }

   
    public void PlayerEliminated(
        ulong clientId)
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        if (eliminationOrder.Contains(clientId))
            return;

        eliminationOrder.Add(clientId);

        

        CheckForWinner();
    }

    private void CheckForWinner()
    {
        if (!IsServer)
            return;

        List<ulong> activePlayers =
            new List<ulong>();

        foreach (ulong clientId
            in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!eliminationOrder.Contains(clientId))
            {
                activePlayers.Add(clientId);
            }
        }

       

        if (activePlayers.Count > 1)
            return;

        if (activePlayers.Count == 1)
        {
            FinishGame(
                activePlayers[0]
            );

            return;
        }

       
        FinishGame(
            NetworkManager.Singleton.ConnectedClientsIds[0]
        );
    }

    private void FinishGame(
        ulong winnerClientId)
    {
        if (gameFinished)
            return;

        gameFinished = true;


        List<ulong> orderedPlayers =
            new List<ulong>();

       
        orderedPlayers.Add(
            winnerClientId
        );

        
        for (int i =
            eliminationOrder.Count - 1;
            i >= 0;
            i--)
        {
            ulong clientId =
                eliminationOrder[i];

            if (clientId == winnerClientId)
                continue;

            orderedPlayers.Add(
                clientId
            );
        }

        GivePoints(
            orderedPlayers
        );

        string results =
            BuildResultsText(
                orderedPlayers
            );

        string winnerName =
            "Player " +
            (winnerClientId + 1);

        ShowResultsClientRpc(
            results,
            winnerName
        );
    }
    private void GivePoints(
        List<ulong> orderedPlayers)
    {
        if (!IsServer)
            return;

        if (MinigameManager.Instance == null)
        {

            return;
        }

        for (int i = 0;
            i < orderedPlayers.Count;
            i++)
        {
            int place = i + 1;

            ulong clientId =
                orderedPlayers[i];

            int points =
                MinigameManager.Instance
                .GetPointsForPlace(
                    minigameName,
                    place
                );

            MinigameManager.Instance.GivePoints(
                clientId,
                minigameName,
                place
            );

            
        }
    }

    private string BuildResultsText(
        List<ulong> orderedPlayers)
    {
        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine("RESULTS");
        builder.AppendLine();

        for (int i = 0;
            i < orderedPlayers.Count;
            i++)
        {
            int place = i + 1;

            ulong clientId =
                orderedPlayers[i];

            int points = 0;

            if (MinigameManager.Instance != null)
            {
                points =
                    MinigameManager.Instance
                    .GetPointsForPlace(
                        minigameName,
                        place
                    );
            }

            builder.AppendLine(
                GetPlaceText(place) +
                ": Player " +
                (clientId + 1) +
                "  +" +
                points +
                " POINTS"
            );
        }

        return builder.ToString();
    }

    private string GetPlaceText(
        int place)
    {
        if (place == 1)
            return "1st";

        if (place == 2)
            return "2nd";

        if (place == 3)
            return "3rd";

        return place + "th";
    }
    [ClientRpc]
    private void ShowResultsClientRpc(
        string results,
        string winnerName)
    {
        if (resultsText != null)
        {
            resultsText.text = results;
            resultsText.gameObject.SetActive(true);
        }

        if (resultBG != null)
        {
            resultBG.gameObject.SetActive(true);
        }

        if (winnerText != null)
        {
            winnerText.text =
                "WINNER!\n" +
                winnerName;

            winnerText.gameObject.SetActive(true);
        }

        if (winnerCanvasGroup != null)
        {
            winnerCanvasGroup.alpha = 1f;
        }

        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(true);
        }
    }
    public void ReturnToBoard()
    {
        if (!IsServer)
            return;

       

        if (returnButton != null)
        {
            returnButton.interactable = false;
        }

        foreach (ulong clientId
            in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!NetworkManager.Singleton.ConnectedClients
                .TryGetValue(
                    clientId,
                    out NetworkClient client))
            {
                continue;
            }

            if (client.PlayerObject != null)
            {
                NetworkObject playerObject =
                    client.PlayerObject;

                playerObject.Despawn(
                    false
                );
            }
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "World",
            LoadSceneMode.Single
        );
    }
}