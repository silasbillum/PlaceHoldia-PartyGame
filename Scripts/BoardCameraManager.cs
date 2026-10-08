using UnityEngine;
using Unity.Netcode;

public class BoardCameraManager : NetworkBehaviour
{
    public static BoardCameraManager Instance;

    public enum CameraMode
    {
        Board,
        Dice,
        Follow
    }

    [SerializeField] private Camera boardCamera;
    [SerializeField] private Camera diceCamera;
    [SerializeField] private Camera followCamera;

    [SerializeField] private Vector3 followOffset = new Vector3(0f, 5f, -7f);
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float followSmoothSpeed = 8f;

    private NetworkVariable<CameraMode> currentMode =
        new NetworkVariable<CameraMode>(
            CameraMode.Board,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<ulong> followClientId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private Transform followTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        currentMode.OnValueChanged += OnCameraModeChanged;
        followClientId.OnValueChanged += OnFollowPlayerChanged;

        ApplyCameraMode();
        FindFollowTarget();
    }

    public override void OnNetworkDespawn()
    {
        currentMode.OnValueChanged -= OnCameraModeChanged;
        followClientId.OnValueChanged -= OnFollowPlayerChanged;
    }

    private void Update()
    {
        if (currentMode.Value != CameraMode.Follow)
            return;

        if (followTarget == null)
        {
            FindFollowTarget();
            return;
        }

        FollowPlayer();
    }

    public void SetBoardCamera()
    {
        if (!IsServer)
            return;

        followClientId.Value = ulong.MaxValue;
        currentMode.Value = CameraMode.Board;

        ApplyCameraMode();
    }

    public void SetDiceCamera()
    {
        if (!IsServer)
            return;

        followClientId.Value = ulong.MaxValue;
        currentMode.Value = CameraMode.Dice;

        ApplyCameraMode();
    }

    public void SetFollowCamera(ulong clientId)
    {
        if (!IsServer)
            return;

        followClientId.Value = clientId;
        currentMode.Value = CameraMode.Follow;

        ApplyCameraMode();
    }

    private void OnCameraModeChanged(
        CameraMode oldMode,
        CameraMode newMode)
    {
        ApplyCameraMode();
    }

    private void ApplyCameraMode()
    {
        if (boardCamera != null)
            boardCamera.enabled = false;

        if (diceCamera != null)
            diceCamera.enabled = false;

        if (followCamera != null)
            followCamera.enabled = false;

        switch (currentMode.Value)
        {
            case CameraMode.Board:

                if (boardCamera != null)
                    boardCamera.enabled = true;

                break;

            case CameraMode.Dice:

                if (diceCamera != null)
                    diceCamera.enabled = true;

                break;

            case CameraMode.Follow:

                if (followCamera != null)
                    followCamera.enabled = true;

                FindFollowTarget();

                break;
        }
    }

    private void OnFollowPlayerChanged(
        ulong oldClientId,
        ulong newClientId)
    {
        FindFollowTarget();
    }

    private void FindFollowTarget()
    {
        if (followClientId.Value == ulong.MaxValue)
        {
            followTarget = null;
            return;
        }

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
            followClientId.Value,
            out NetworkClient client))
        {
            return;
        }

        if (client.PlayerObject == null)
            return;

        followTarget =
            client.PlayerObject.transform;

        if (currentMode.Value == CameraMode.Follow)
        {
            PositionFollowCameraImmediately();
        }
    }

    private void FollowPlayer()
    {
        if (followCamera == null)
            return;

        if (followTarget == null)
            return;

        Vector3 targetPosition =
            followTarget.position +
            followOffset;

        followCamera.transform.position =
            Vector3.Lerp(
                followCamera.transform.position,
                targetPosition,
                followSmoothSpeed *
                Time.deltaTime
            );

        Vector3 lookPosition =
            followTarget.position +
            lookAtOffset;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                lookPosition -
                followCamera.transform.position
            );

        followCamera.transform.rotation =
            Quaternion.Slerp(
                followCamera.transform.rotation,
                targetRotation,
                followSmoothSpeed *
                Time.deltaTime
            );
    }

    private void PositionFollowCameraImmediately()
    {
        if (followCamera == null)
            return;

        if (followTarget == null)
            return;

        followCamera.transform.position =
            followTarget.position +
            followOffset;

        Vector3 lookPosition =
            followTarget.position +
            lookAtOffset;

        followCamera.transform.LookAt(
            lookPosition
        );
    }
}