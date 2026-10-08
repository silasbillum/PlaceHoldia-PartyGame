using Unity.Netcode;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    public static GameState Instance;

    public NetworkVariable<bool> GameStarted =
        new NetworkVariable<bool>(false);

    private void Awake()
    {
        Instance = this;
    }

    public void StartGame()
    {
        // Only the host/server is allowed to start the game
        if (!IsServer)
            return;

        GameStarted.Value = true;
    }
}