using UnityEngine;
using Unity.Netcode;

public class KnockoutPlayerCleanup : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsServer)
            return;

        RemoveBoardPlayers();
    }

    private void RemoveBoardPlayers()
    {
        BoardPlayer[] boardPlayers =
            FindObjectsByType<BoardPlayer>(
                FindObjectsSortMode.None
            );

        Debug.Log(
            "Knockout cleanup found " +
            boardPlayers.Length +
            " BoardPlayers."
        );

        foreach (BoardPlayer boardPlayer in boardPlayers)
        {
            if (boardPlayer == null)
                continue;

            NetworkObject networkObject =
                boardPlayer.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogWarning(
                    "BoardPlayer has no NetworkObject: " +
                    boardPlayer.gameObject.name
                );

                continue;
            }

            if (!networkObject.IsSpawned)
                continue;

            Debug.Log(
                "Knockout cleanup removing BoardPlayer: " +
                boardPlayer.gameObject.name
            );

            networkObject.Despawn(true);
        }
    }
}