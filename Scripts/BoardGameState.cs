using UnityEngine;
using System.Collections.Generic;

public class BoardGameState : MonoBehaviour
{
    public static BoardGameState Instance;

   
    private Dictionary<ulong, int> playerSpaces =
        new Dictionary<ulong, int>();

 
    private Dictionary<ulong, int> playerPoints =
        new Dictionary<ulong, int>();

    private ulong nextPlayerClientId = 0;

    private bool hasSavedNextPlayer = false;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void SavePlayerPosition(
        ulong clientId,
        int spaceIndex)
    {
        playerSpaces[clientId] = spaceIndex;

      
    }

    public bool HasPlayerPosition(
        ulong clientId)
    {
        return playerSpaces.ContainsKey(clientId);
    }

    public int GetPlayerPosition(
        ulong clientId)
    {
        if (playerSpaces.TryGetValue(
            clientId,
            out int space))
        {
            return space;
        }

        return 0;
    }


    public void SavePlayerPoints(
        ulong clientId,
        int points)
    {
        playerPoints[clientId] = points;

      
    }

    public bool HasPlayerPoints(
        ulong clientId)
    {
        return playerPoints.ContainsKey(clientId);
    }

    public int GetPlayerPoints(
        ulong clientId)
    {
        if (playerPoints.TryGetValue(
            clientId,
            out int points))
        {
            return points;
        }

        return 0;
    }

    public void AddPoints(
        ulong clientId,
        int amount)
    {
        int currentPoints =
            GetPlayerPoints(clientId);

        currentPoints += amount;

    
        if (currentPoints < 0)
        {
            currentPoints = 0;
        }

        SavePlayerPoints(
            clientId,
            currentPoints
        );
    }

    public void RemovePoints(
        ulong clientId,
        int amount)
    {
        int currentPoints =
            GetPlayerPoints(clientId);

        currentPoints -= amount;

        
        if (currentPoints < 0)
        {
            currentPoints = 0;
        }

        SavePlayerPoints(
            clientId,
            currentPoints
        );
    }

    public void SaveNextPlayer(
        ulong clientId)
    {
        nextPlayerClientId = clientId;
        hasSavedNextPlayer = true;

       
    }

    public bool HasSavedNextPlayer()
    {
        return hasSavedNextPlayer;
    }

    public ulong GetNextPlayer()
    {
        return nextPlayerClientId;
    }

   
    public void ClearState()
    {
        playerSpaces.Clear();
        playerPoints.Clear();

        nextPlayerClientId = 0;

        hasSavedNextPlayer = false;
    }
}