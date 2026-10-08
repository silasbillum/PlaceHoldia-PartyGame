using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BoardResultsUI : MonoBehaviour
{
    public static BoardResultsUI Instance;

    [Header("Results Panel")]
    [SerializeField] private GameObject resultsPanel;

    [Header("Winner")]
    [SerializeField] private TMP_Text winnerText;

    [Header("Restart")]
    [SerializeField] private Button restartButton;

    [Header("Player 1")]
    [SerializeField] private GameObject player1Row;
    [SerializeField] private TMP_Text player1Place;
    [SerializeField] private TMP_Text player1Name;
    [SerializeField] private TMP_Text player1Points;

    [Header("Player 2")]
    [SerializeField] private GameObject player2Row;
    [SerializeField] private TMP_Text player2Place;
    [SerializeField] private TMP_Text player2Name;
    [SerializeField] private TMP_Text player2Points;

    [Header("Player 3")]
    [SerializeField] private GameObject player3Row;
    [SerializeField] private TMP_Text player3Place;
    [SerializeField] private TMP_Text player3Name;
    [SerializeField] private TMP_Text player3Points;

    [Header("Player 4")]
    [SerializeField] private GameObject player4Row;
    [SerializeField] private TMP_Text player4Place;
    [SerializeField] private TMP_Text player4Name;
    [SerializeField] private TMP_Text player4Points;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(
                RestartGame
            );
        }
    }


    public void ShowResults(
        ulong[] finishingOrder,
        int[] points)
    {
        if (finishingOrder == null ||
            points == null)
        {
         

            return;
        }

        if (finishingOrder.Length != points.Length)
        {
         

            return;
        }


        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }


        HideAllRows();


        List<PlayerResult> results =
            new List<PlayerResult>();


        for (int i = 0;
             i < finishingOrder.Length;
             i++)
        {
            PlayerResult result =
                new PlayerResult();

            result.clientId =
                finishingOrder[i];

            result.finishPosition =
                i + 1;

            result.points =
                points[i];

            results.Add(result);
        }


        
        results.Sort(
            (a, b) =>
                b.points.CompareTo(a.points)
        );


       
        if (results.Count > 0)
        {
            PlayerResult winner =
                results[0];

            if (winnerText != null)
            {
                winnerText.text =
                    "🏆 WINNER: Player " +
                    (winner.clientId + 1) +
                    " - " +
                    winner.points +
                    " Points";
            }
        }


        
        for (int i = 0;
             i < results.Count && i < 4;
             i++)
        {
            ShowPlayer(
                i,
                results[i]
            );
        }


        

        for (int i = 0;
             i < results.Count;
             i++)
        {
           
        }
    }


    private void ShowPlayer(
        int rowIndex,
        PlayerResult result)
    {
        GameObject row = null;

        TMP_Text placeText = null;
        TMP_Text nameText = null;
        TMP_Text pointsText = null;


        switch (rowIndex)
        {
            case 0:

                row = player1Row;

                placeText =
                    player1Place;

                nameText =
                    player1Name;

                pointsText =
                    player1Points;

                break;


            case 1:

                row = player2Row;

                placeText =
                    player2Place;

                nameText =
                    player2Name;

                pointsText =
                    player2Points;

                break;


            case 2:

                row = player3Row;

                placeText =
                    player3Place;

                nameText =
                    player3Name;

                pointsText =
                    player3Points;

                break;


            case 3:

                row = player4Row;

                placeText =
                    player4Place;

                nameText =
                    player4Name;

                pointsText =
                    player4Points;

                break;
        }


        if (row != null)
        {
            row.SetActive(true);
        }


        if (placeText != null)
        {
            placeText.text =
                GetPlaceText(
                    rowIndex + 1
                );
        }


        if (nameText != null)
        {
            nameText.text =
                "Player " +
                (result.clientId + 1);
        }


        if (pointsText != null)
        {
            pointsText.text =
                result.points +
                " Points";
        }
    }


    private string GetPlaceText(
        int place)
    {
        switch (place)
        {
            case 1:
                return "1st";

            case 2:
                return "2nd";

            case 3:
                return "3rd";

            default:
                return place + "th";
        }
    }


    private void HideAllRows()
    {
        if (player1Row != null)
            player1Row.SetActive(false);

        if (player2Row != null)
            player2Row.SetActive(false);

        if (player3Row != null)
            player3Row.SetActive(false);

        if (player4Row != null)
            player4Row.SetActive(false);
    }


    private void RestartGame()
    {
        if (NetworkManager.Singleton == null)
        {
           

            return;
        }

      
        if (!NetworkManager.Singleton.IsServer)
        {
            

            return;
        }

       


        // Clear saved board data.
        if (BoardGameState.Instance != null)
        {
            BoardGameState.Instance.ClearState();
        }


        // Reload the board scene for everyone.
        string boardSceneName =
            SceneManager.GetActiveScene().name;

        NetworkManager.Singleton.SceneManager.LoadScene(
            boardSceneName,
            LoadSceneMode.Single
        );
    }


    private class PlayerResult
    {
        public ulong clientId;

        public int finishPosition;

        public int points;
    }
}