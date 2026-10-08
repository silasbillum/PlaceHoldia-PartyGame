using UnityEngine;
using System.Collections.Generic;

public class BoardState : MonoBehaviour
{
    public static BoardState Instance;

    private List<int> playerSpaces =
        new List<int>();

    private int nextPlayerIndex;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void SaveState(
        List<BoardPlayer> players,
        int nextPlayer)
    {
        playerSpaces.Clear();

        foreach (BoardPlayer player in players)
        {
            playerSpaces.Add(
                player.CurrentSpace
            );
        }

        nextPlayerIndex = nextPlayer;

        Debug.Log(
            "Board state saved."
        );
    }

    public int GetPlayerSpace(int playerIndex)
    {
        if (playerIndex < 0 ||
            playerIndex >= playerSpaces.Count)
        {
            return 0;
        }

        return playerSpaces[playerIndex];
    }

    public int GetPlayerCount()
    {
        return playerSpaces.Count;
    }

    public int GetNextPlayer()
    {
        return nextPlayerIndex;
    }
}

