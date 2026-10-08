using Unity.Netcode;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    public Color MyColor { get; private set; }

    private Renderer playerRenderer;

    public override void OnNetworkSpawn()
    {
        MyColor = GetPlayerColor(OwnerClientId);

        playerRenderer = GetComponentInChildren<Renderer>();

        if (playerRenderer != null)
        {
            playerRenderer.material.color = MyColor;
        }

        Debug.Log(
            $"Player {OwnerClientId} color: {MyColor}"
        );
    }

    private Color GetPlayerColor(ulong clientId)
    {
        switch (clientId)
        {
            case 0:
                return Color.red;

            case 1:
                return Color.blue;

            case 2:
                return Color.yellow;

            case 3:
                return Color.green;

            default:
                return Color.white;
        }
    }
}