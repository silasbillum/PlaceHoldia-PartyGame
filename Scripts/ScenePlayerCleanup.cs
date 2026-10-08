using UnityEngine;
using Unity.Netcode;

public class ScenePlayerCleanup : MonoBehaviour
{
    private void Start()
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
                    NetworkObject player =
                        client.PlayerObject;

                    Debug.Log(
                        "Destroying old player for Client " +
                        clientId
                    );

                    player.Despawn(true);
                }
            }
        }
    }
}