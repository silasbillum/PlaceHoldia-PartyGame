using UnityEngine;
using Unity.Netcode;

public class BoardPlayerSpawner : MonoBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject[] playerPrefabs;

    [Header("Board Spaces")]
    [SerializeField] private Transform[] boardSpaces;

    private void Start()
    {
        

        if (NetworkManager.Singleton == null)
        {
            
            return;
        }

      

       

        if (!NetworkManager.Singleton.IsServer)
            return;

        SpawnPlayers();
    }
    private void SpawnPlayers()
    {
       
        
   

        int playerIndex = 0;

        foreach (ulong clientId in
            NetworkManager.Singleton.ConnectedClientsIds)
        {
           

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(
                clientId,
                out NetworkClient client))
            {
               
            }
            if (playerIndex >= playerPrefabs.Length)
            {
               

                break;
            }

            SpawnPlayer(
                clientId,
                playerIndex
            );

            playerIndex++;
        }
    }
    private void SpawnPlayer(
        ulong clientId,
        int playerIndex)
    {
        GameObject prefab =
            playerPrefabs[playerIndex];

        if (prefab == null)
        {
            

            return;
        }
        int spaceIndex = 0;

        // Restore saved position
        if (BoardGameState.Instance != null &&
            BoardGameState.Instance.HasPlayerPosition(
                clientId))
        {
            spaceIndex =
                BoardGameState.Instance.GetPlayerPosition(
                    clientId
                );

            
        }
        else
        {
            
        }

        if (boardSpaces == null ||
            boardSpaces.Length == 0)
        {
            

            return;
        }

        if (spaceIndex < 0 ||
            spaceIndex >= boardSpaces.Length)
        {
            

            spaceIndex = 0;
        }

        Transform spawnPoint =
            boardSpaces[spaceIndex];

        if (spawnPoint == null)
        {
            

            return;
        }
        GameObject player =
            Instantiate(
                prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

        NetworkObject networkObject =
            player.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
           

            Destroy(player);
            return;
        }
        networkObject.SpawnAsPlayerObject(
            clientId
        );
        BoardPlayer boardPlayer =
            player.GetComponent<BoardPlayer>();

        if (boardPlayer == null)
        {
            

            return;
        }

        // Tell BoardPlayer where it actually starts
        boardPlayer.SpawnAtStart(
            spawnPoint,
            spaceIndex
        );

       
    }
}