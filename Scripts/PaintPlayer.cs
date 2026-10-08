using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaintPlayer : NetworkBehaviour
{
    [Header("Cursor")]
    [SerializeField] private GameObject cursorPrefab;
    [SerializeField] private float cursorOffset = 0.1f;

    private Camera mainCamera;
    private GameObject cursor;

    private Collider paintCanvas;
    private PaintCanvas paintCanvasScript;

    private NetworkVariable<Vector3> cursorPosition =
        new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    public override void OnNetworkSpawn()
    {
        mainCamera = Camera.main;

        GameObject canvasObject =
            GameObject.Find("PaintCanvas");

        if (canvasObject == null)
        {
            Debug.LogError("PaintCanvas NOT FOUND!");
            return;
        }

        paintCanvas =
            canvasObject.GetComponent<Collider>();

        paintCanvasScript =
            canvasObject.GetComponent<PaintCanvas>();

        if (paintCanvas == null)
        {
            Debug.LogError("PaintCanvas Collider NOT FOUND!");
        }

        if (paintCanvasScript == null)
        {
            Debug.LogError("PaintCanvas Script NOT FOUND!");
        }

        if (cursorPrefab == null)
        {
            Debug.LogError("Cursor Prefab NOT ASSIGNED!");
            return;
        }

        cursor = Instantiate(cursorPrefab);

        Color playerColor =
            GetPlayerColor(OwnerClientId);

        Renderer cursorRenderer =
            cursor.GetComponent<Renderer>();

        if (cursorRenderer != null)
        {
            cursorRenderer.material.color =
                playerColor;
        }

        cursorPosition.OnValueChanged +=
            OnCursorPositionChanged;

        UpdateCursorPosition(
            cursorPosition.Value
        );
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (paintCanvas == null)
            return;

        if (paintCanvasScript == null)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            mainCamera.ScreenPointToRay(
                mousePosition
            );

        if (paintCanvas.Raycast(
            ray,
            out RaycastHit hit,
            100f))
        {
            Vector3 cursorPos =
                hit.point +
                hit.normal * cursorOffset;

            cursorPosition.Value =
                cursorPos;

            if (Mouse.current.leftButton.isPressed)
            {
                if (PaintGameManager.Instance != null &&
                    !PaintGameManager.Instance.CanPaint())
                {
                    return;
                }

                Color playerColor =
                    GetPlayerColor(OwnerClientId);

                PaintServerRpc(
                    hit.textureCoord,
                    playerColor
                );
            }
        }
    }

    [ServerRpc]
    private void PaintServerRpc(
        Vector2 uv,
        Color color)
    {
        PaintClientRpc(uv, color);
    }

    [ClientRpc]
    private void PaintClientRpc(
        Vector2 uv,
        Color color)
    {
        if (paintCanvasScript == null)
        {
            GameObject canvasObject =
                GameObject.Find("PaintCanvas");

            if (canvasObject != null)
            {
                paintCanvasScript =
                    canvasObject.GetComponent<PaintCanvas>();
            }
        }

        if (paintCanvasScript != null)
        {
            paintCanvasScript.Paint(
                uv,
                color
            );
        }
    }

    private void OnCursorPositionChanged(
        Vector3 oldPosition,
        Vector3 newPosition)
    {
        UpdateCursorPosition(
            newPosition
        );
    }

    private void UpdateCursorPosition(
        Vector3 position)
    {
        if (cursor != null)
        {
            cursor.transform.position =
                position;
        }
    }

    private Color GetPlayerColor(
        ulong clientId)
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

    public override void OnNetworkDespawn()
    {
        cursorPosition.OnValueChanged -=
            OnCursorPositionChanged;

        if (cursor != null)
        {
            Destroy(cursor);
        }
    }
}