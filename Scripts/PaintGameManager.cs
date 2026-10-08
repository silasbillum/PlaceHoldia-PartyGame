using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PaintGameManager : NetworkBehaviour
{
    public static PaintGameManager Instance;

    [Header("Timer")]
    [SerializeField] private float roundDuration = 15f;
    [SerializeField] private TMP_Text timerText;

    [Header("Results")]
    [SerializeField] private TMP_Text resultsText;
    [SerializeField] private Image resultBG;

    [Header("Winner")]
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private CanvasGroup winnerCanvasGroup;

    [SerializeField] private Button ReturnButton;

    private NetworkVariable<float> timeRemaining =
        new NetworkVariable<float>(15f);

    private NetworkVariable<bool> roundFinished =
        new NetworkVariable<bool>(false);

    private PaintCanvas paintCanvas;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        paintCanvas =
            FindFirstObjectByType<PaintCanvas>();

        timeRemaining.OnValueChanged +=
            OnTimeChanged;

        roundFinished.OnValueChanged +=
            OnRoundFinished;

        if (IsServer)
        {
            timeRemaining.Value =
                roundDuration;

            roundFinished.Value =
                false;
        }

        UpdateTimer(
            timeRemaining.Value
        );
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (roundFinished.Value)
            return;

        timeRemaining.Value -=
            Time.deltaTime;

        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;

            FinishRound();
        }
    }
    private void OnTimeChanged(
        float oldValue,
        float newValue)
    {
        UpdateTimer(newValue);
    }

    private void UpdateTimer(
        float time)
    {
        if (timerText != null)
        {
            timerText.text =
                Mathf.CeilToInt(time).ToString();
        }
    }

    private void FinishRound()
    {
        if (roundFinished.Value)
            return;

        roundFinished.Value = true;

        float[] percentages =
            CalculatePercentages();

        bool[] playersPresent =
            new bool[4];

        foreach (ulong clientId in
                 NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId < 4)
            {
                playersPresent[clientId] = true;
            }
        }

        List<PlayerResult> playerResults =
            CalculatePlayerResults(
                percentages,
                playersPresent
            );

        GiveMinigamePoints(
            playerResults
        );

        ShowResultsClientRpc(
            percentages[0],
            percentages[1],
            percentages[2],
            percentages[3],
            playersPresent[0],
            playersPresent[1],
            playersPresent[2],
            playersPresent[3]
        );
    }
    private float[] CalculatePercentages()
    {
        float[] results =
            new float[4];

        if (paintCanvas == null)
        {
            return results;
        }

        Texture2D texture =
            paintCanvas.GetPaintTextureAsTexture2D();

        if (texture == null)
        {

            return results;
        }

        Color32[] pixels =
            texture.GetPixels32();

        int[] counts =
            new int[4];

        foreach (Color32 pixel in pixels)
        {
            if (IsColor(pixel, Color.red))
            {
                counts[0]++;
            }
            else if (IsColor(pixel, Color.blue))
            {
                counts[1]++;
            }
            else if (IsColor(pixel, Color.yellow))
            {
                counts[2]++;
            }
            else if (IsColor(pixel, Color.green))
            {
                counts[3]++;
            }
        }

        int totalPainted =
            counts[0] +
            counts[1] +
            counts[2] +
            counts[3];

        if (totalPainted == 0)
        {
            Destroy(texture);

            return results;
        }

        for (int i = 0; i < 4; i++)
        {
            results[i] =
                (float)counts[i] /
                totalPainted *
                100f;
        }

        Destroy(texture);

        return results;
    }

    private bool IsColor(
        Color32 pixel,
        Color target)
    {
        Color32 targetColor =
            target;

        return
            Mathf.Abs(
                pixel.r -
                targetColor.r
            ) < 5 &&
            Mathf.Abs(
                pixel.g -
                targetColor.g
            ) < 5 &&
            Mathf.Abs(
                pixel.b -
                targetColor.b
            ) < 5;
    }
    private List<PlayerResult> CalculatePlayerResults(
        float[] percentages,
        bool[] playersPresent)
    {
        List<PlayerResult> results =
            new List<PlayerResult>();

        foreach (ulong clientId in
                 NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId >= 4)
                continue;

            if (!playersPresent[clientId])
                continue;

            results.Add(
                new PlayerResult
                {
                    clientId = clientId,
                    percentage =
                        percentages[clientId]
                }
            );
        }

      
        results.Sort(
            (a, b) =>
                b.percentage.CompareTo(
                    a.percentage
                )
        );

     
        for (int i = 0;
             i < results.Count;
             i++)
        {
            results[i].place =
                i + 1;
        }

        return results;
    }
    private void GiveMinigamePoints(
        List<PlayerResult> playerResults)
    {
        if (!IsServer)
            return;

        if (MinigameManager.Instance == null)
        {

            return;
        }

        foreach (PlayerResult result in playerResults)
        {
            int points =
                MinigameManager.Instance
                    .GetPointsForPlace(
                        "Paint Throwing",
                        result.place
                    );

            result.points =
                points;

            MinigameManager.Instance.GivePoints(
                result.clientId,
                "Paint Throwing",
                result.place
            );

        }
    }
    private class PlayerResult
    {
        public ulong clientId;

        public float percentage;

        public int place;

        public int points;
    }
    [ClientRpc]
    private void ShowResultsClientRpc(
        float red,
        float blue,
        float yellow,
        float green,
        bool redPresent,
        bool bluePresent,
        bool yellowPresent,
        bool greenPresent)
    {
        if (resultsText != null)
        {
            resultsText.gameObject.SetActive(
                true
            );

            if (resultBG != null)
            {
                resultBG.gameObject.SetActive(
                    true
                );
            }

            if (ReturnButton != null)
            {
                ReturnButton.gameObject.SetActive(
                    true
                );
            }
            List<ColorResult> results =
                new List<ColorResult>();

            if (redPresent)
            {
                results.Add(
                    new ColorResult
                    {
                        colorName = "RED",
                        percentage = red
                    }
                );
            }

            if (bluePresent)
            {
                results.Add(
                    new ColorResult
                    {
                        colorName = "BLUE",
                        percentage = blue
                    }
                );
            }

            if (yellowPresent)
            {
                results.Add(
                    new ColorResult
                    {
                        colorName = "YELLOW",
                        percentage = yellow
                    }
                );
            }

            if (greenPresent)
            {
                results.Add(
                    new ColorResult
                    {
                        colorName = "GREEN",
                        percentage = green
                    }
                );
            }

            results.Sort(
                (a, b) =>
                    b.percentage.CompareTo(
                        a.percentage
                    )
            );
            string resultString =
                "RESULTS\n\n";

            for (int i = 0;
                 i < results.Count;
                 i++)
            {
                int place =
                    i + 1;

                int points =
                    GetPointsForPlace(
                        place
                    );

                resultString +=
                    GetPlaceText(place) +
                    results[i].colorName +
                    ": " +
                    results[i].percentage.ToString("F1") +
                    "%    +" +
                    points +
                    " POINTS\n";
            }

            resultsText.text =
                resultString;
        }
        string winnerName = "";

        float highestScore = -1f;

        if (redPresent &&
            red > highestScore)
        {
            highestScore = red;

            winnerName = "RED";
        }

        if (bluePresent &&
            blue > highestScore)
        {
            highestScore = blue;

            winnerName = "BLUE";
        }

        if (yellowPresent &&
            yellow > highestScore)
        {
            highestScore = yellow;

            winnerName = "YELLOW";
        }

        if (greenPresent &&
            green > highestScore)
        {
            highestScore = green;

            winnerName = "GREEN";
        }

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(
                true
            );

            winnerText.text =
                "WINNER!\n" +
                winnerName;

            winnerText.color =
                new Color(
                    1f,
                    0.75f,
                    0.1f
                );
        }

        if (winnerCanvasGroup != null)
        {
            winnerCanvasGroup.alpha = 1f;
        }
    }
    private int GetPointsForPlace(
        int place)
    {
        if (MinigameManager.Instance == null)
        {
            return 0;
        }

        return MinigameManager.Instance
            .GetPointsForPlace(
                "Paint Throwing",
                place
            );
    }
    private class ColorResult
    {
        public string colorName;

        public float percentage;
    }

    private string GetPlaceText(
        int place)
    {
        switch (place)
        {
            case 1:
                return "1st: ";

            case 2:
                return "2nd: ";

            case 3:
                return "3rd: ";

            case 4:
                return "4th: ";

            default:
                return place + "th: ";
        }
    }
    private void OnRoundFinished(
        bool oldValue,
        bool newValue)
    {
        if (!newValue)
            return;

        if (timerText != null)
        {
            timerText.text = "0";
        }
    }
    public bool CanPaint()
    {
        return !roundFinished.Value;
    }
    public override void OnNetworkDespawn()
    {
        timeRemaining.OnValueChanged -=
            OnTimeChanged;

        roundFinished.OnValueChanged -=
            OnRoundFinished;
    }
    public void ReturnToBoard()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        foreach (ulong clientId in
                 NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(
                clientId,
                out NetworkClient client))
            {
                if (client.PlayerObject != null)
                {
                    client.PlayerObject.Despawn(
                        true
                    );
                }
            }
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            "World",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}