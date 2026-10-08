using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        int playerNumber = (int)OwnerClientId;

        Vector3 spawnPosition = new Vector3(
            playerNumber * 2f - 1f,
            1f,
            0f
        );

        transform.position = spawnPosition;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Keyboard.current == null)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1;

        Vector3 movement = new Vector3(input.x, 0f, input.y);

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        transform.position += movement * moveSpeed * Time.deltaTime;
    }
}