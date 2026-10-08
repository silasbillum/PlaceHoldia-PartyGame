using UnityEngine;
using Unity.Netcode;

public class PaintPlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject[] playerPrefabs;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        int spawnIndex = 0;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (spawnIndex >= spawnPoints.Length)
                break;

            if (spawnIndex >= playerPrefabs.Length)
                break;

            SpawnPlayer(clientId, spawnIndex);

            spawnIndex++;
        }
    }

    private void SpawnPlayer(ulong clientId, int spawnIndex)
    {
        GameObject player = Instantiate(
            playerPrefabs[spawnIndex],
            spawnPoints[spawnIndex].position,
            spawnPoints[spawnIndex].rotation
        );

        NetworkObject networkObject =
            player.GetComponent<NetworkObject>();

        networkObject.SpawnAsPlayerObject(clientId);

    }
}