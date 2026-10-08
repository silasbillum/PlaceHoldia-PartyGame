using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MinigameManager : NetworkBehaviour
{
    public static MinigameManager Instance;

    [System.Serializable]
    public class MinigameSettings
    {
        [Header("Minigame")]
        public string minigameName;

        [Header("Points")]
        public int firstPlacePoints = 10;
        public int secondPlacePoints = 7;
        public int thirdPlacePoints = 4;
        public int fourthPlacePoints = 2;

        public int GetPointsForPlace(int place)
        {
            switch (place)
            {
                case 1:
                    return firstPlacePoints;

                case 2:
                    return secondPlacePoints;

                case 3:
                    return thirdPlacePoints;

                case 4:
                    return fourthPlacePoints;

                default:
                    return 0;
            }
        }
    }

    [Header("Minigames")]
    [SerializeField]
    private List<MinigameSettings> minigames =
        new List<MinigameSettings>();

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

    public override void OnNetworkSpawn()
    {
    
    }

    public MinigameSettings GetMinigame(
        string minigameName)
    {
        foreach (MinigameSettings minigame in minigames)
        {
            if (minigame == null)
                continue;

            if (minigame.minigameName ==
                minigameName)
            {
                return minigame;
            }
        }


        return null;
    }

    public int GetPointsForPlace(
        string minigameName,
        int place)
    {
        MinigameSettings minigame =
            GetMinigame(minigameName);

        if (minigame == null)
            return 0;

        return minigame.GetPointsForPlace(
            place
        );
    }

    public void GivePoints(
    ulong clientId,
    string minigameName,
    int place)
    {
        if (!IsServer)
        {
          

            return;
        }

        if (BoardGameState.Instance == null)
        {
           

            return;
        }

        int points =
            GetPointsForPlace(
                minigameName,
                place
            );

        int oldPoints =
            BoardGameState.Instance.GetPlayerPoints(
                clientId
            );

        BoardGameState.Instance.AddPoints(
            clientId,
            points
        );

        int newPoints =
            BoardGameState.Instance.GetPlayerPoints(
                clientId

            );
        SyncMinigamePointsClientRpc(
    clientId,
    newPoints
);
  

        
    }
   
    public void GivePointsForResults(
        string minigameName,
        List<ulong> finishingOrder)
    {
        if (!IsServer)
            return;

        if (finishingOrder == null ||
            finishingOrder.Count == 0)
        {
            

            return;
        }

        for (int i = 0;
             i < finishingOrder.Count &&
             i < 4;
             i++)
        {
            GivePoints(
                finishingOrder[i],
                minigameName,
                i + 1
            );
        }
    }

    [ClientRpc]
    private void SyncMinigamePointsClientRpc(
    ulong clientId,
    int newPoints)
    {
        if (BoardGameState.Instance == null)
        {
            

            return;
        }

        BoardGameState.Instance.SavePlayerPoints(
            clientId,
            newPoints
        );

       
    }
}