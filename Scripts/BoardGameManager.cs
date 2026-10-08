using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class BoardGameManager : NetworkBehaviour
{
    public static BoardGameManager Instance;

    [SerializeField] private Transform[] boardSpaces;

    private List<BoardPlayer> players =
        new List<BoardPlayer>();

    private List<ulong> finishedPlayers =
        new List<ulong>();

    private bool gameFinished = false;

    private NetworkVariable<int> currentPlayerIndex =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<ulong> currentPlayerClientId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<bool> diceLocked =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private ulong diceRollClientId =
        ulong.MaxValue;

    private bool waitingForMovement = false;

    public int CurrentPlayerIndex =>
        currentPlayerIndex.Value;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        StartCoroutine(
            SetupPlayers()
        );
    }

    private IEnumerator SetupPlayers()
    {
        yield return new WaitForSeconds(1f);

        players.Clear();
        finishedPlayers.Clear();

        gameFinished = false;

        if (NetworkManager.Singleton == null)
            yield break;

        List<ulong> clientIds =
            new List<ulong>(
                NetworkManager.Singleton.ConnectedClientsIds
            );

        clientIds.Sort();

        foreach (ulong clientId in clientIds)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                clientId,
                out NetworkClient client))
            {
                continue;
            }

            if (client.PlayerObject == null)
                continue;

            BoardPlayer boardPlayer =
                client.PlayerObject.GetComponent<BoardPlayer>();

            if (boardPlayer == null)
                continue;

            players.Add(boardPlayer);
        }

        if (players.Count == 0)
            yield break;

        if (boardSpaces == null ||
            boardSpaces.Length == 0)
            yield break;

        int playerIndex = 0;

        if (BoardGameState.Instance != null &&
            BoardGameState.Instance.HasSavedNextPlayer())
        {
            ulong savedClientId =
                BoardGameState.Instance.GetNextPlayer();

            bool foundPlayer = false;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null)
                    continue;

                if (players[i].OwnerClientId ==
                    savedClientId)
                {
                    playerIndex = i;
                    foundPlayer = true;
                    break;
                }
            }

            if (!foundPlayer)
            {
                playerIndex = 0;
            }
        }
        else
        {
            playerIndex = 0;
        }

        currentPlayerIndex.Value =
            playerIndex;

        currentPlayerClientId.Value =
            players[playerIndex].OwnerClientId;

        diceLocked.Value = false;

        diceRollClientId =
            ulong.MaxValue;

        waitingForMovement = false;

        if (BoardCameraManager.Instance != null)
        {
            BoardCameraManager.Instance.SetDiceCamera();
        }
    }

    [ServerRpc(
        RequireOwnership = false
    )]
    public void RequestDiceRollServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (currentPlayerClientId.Value !=
            senderClientId)
        {
            return;
        }

        if (diceLocked.Value)
            return;

        diceLocked.Value = true;

        diceRollClientId =
            senderClientId;

        StartDiceRollClientRpc(
            senderClientId
        );
    }

    [ClientRpc]
    private void StartDiceRollClientRpc(
        ulong rollingClientId)
    {
        BoardDice dice =
            FindAnyObjectByType<BoardDice>();

        if (dice == null)
            return;

        dice.StartApprovedRoll(
            rollingClientId
        );
    }

    [ServerRpc(
        RequireOwnership = false
    )]
    public void DiceResultServerRpc(
        int result,
        ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (senderClientId !=
            currentPlayerClientId.Value)
        {
            return;
        }

        if (senderClientId !=
            diceRollClientId)
        {
            return;
        }

        if (!diceLocked.Value)
            return;

        if (result < 1 ||
            result > 6)
        {
            return;
        }

        DiceResult(result);
    }

    private void DiceResult(
        int result)
    {
        if (!IsServer)
            return;

        if (waitingForMovement)
            return;

        if (players.Count == 0)
            return;

        if (currentPlayerIndex.Value < 0 ||
            currentPlayerIndex.Value >= players.Count)
        {
            return;
        }

        BoardPlayer currentPlayer =
            players[
                currentPlayerIndex.Value
            ];

        if (currentPlayer == null)
            return;

        ShowDiceResultClientRpc(result);

        waitingForMovement = true;

        if (BoardCameraManager.Instance != null)
        {
            BoardCameraManager.Instance.SetFollowCamera(
                currentPlayer.OwnerClientId
            );
        }

        currentPlayer.MoveSpaces(
            result,
            boardSpaces
        );
    }

    public void PlayerFinishedMove()
    {
        if (!IsServer)
            return;

        if (!waitingForMovement)
            return;

        waitingForMovement = false;

        if (BoardDice.Instance != null)
        {
            BoardDice.Instance.HideResult();
        }

        if (BoardCameraManager.Instance != null)
        {
            BoardCameraManager.Instance.SetBoardCamera();
        }

        if (players.Count == 0)
            return;

        if (currentPlayerIndex.Value < 0 ||
            currentPlayerIndex.Value >= players.Count)
        {
            return;
        }

        BoardPlayer player =
            players[
                currentPlayerIndex.Value
            ];

        if (player == null)
            return;

        int spaceIndex =
            player.CurrentSpace;

        if (BoardGameState.Instance != null)
        {
            BoardGameState.Instance.SavePlayerPosition(
                player.OwnerClientId,
                player.CurrentSpace
            );
        }

        if (spaceIndex < 0 ||
            spaceIndex >= boardSpaces.Length)
        {
            NextTurn();
            return;
        }

        BoardFinishSpace finishSpace =
            boardSpaces[spaceIndex]
                .GetComponent<BoardFinishSpace>();

        if (finishSpace != null)
        {
            FinishPlayer(
                player,
                finishSpace
            );

            return;
        }

        BoardSpace space =
            boardSpaces[spaceIndex]
                .GetComponent<BoardSpace>();

        if (space == null)
        {
            NextTurn();
            return;
        }

        switch (space.GetSpaceType())
        {
            case BoardSpace.SpaceType.Normal:

                NextTurn();

                break;

            case BoardSpace.SpaceType.AddPoints:

                int pointsToAdd =
                    space.GetPoints();

                if (BoardGameState.Instance != null)
                {
                    ulong clientId =
                        player.OwnerClientId;

                    BoardGameState.Instance.AddPoints(
                        clientId,
                        pointsToAdd
                    );

                    int newPoints =
                        BoardGameState.Instance.GetPlayerPoints(
                            clientId
                        );

                    UpdatePointsClientRpc(
                        clientId,
                        newPoints
                    );
                }

                NextTurn();

                break;

            case BoardSpace.SpaceType.RemovePoints:

                int pointsToRemove =
                    space.GetPoints();

                if (BoardGameState.Instance != null)
                {
                    ulong clientId =
                        player.OwnerClientId;

                    BoardGameState.Instance.RemovePoints(
                        clientId,
                        pointsToRemove
                    );

                    int newPoints =
                        BoardGameState.Instance.GetPlayerPoints(
                            clientId
                        );

                    UpdatePointsClientRpc(
                        clientId,
                        newPoints
                    );
                }

                NextTurn();

                break;

            case BoardSpace.SpaceType.Minigame:

                HandleMinigameSpace(
                    space
                );

                break;
        }
    }

    private void FinishPlayer(
        BoardPlayer player,
        BoardFinishSpace finishSpace)
    {
        if (!IsServer)
            return;

        if (player == null)
            return;

        if (finishSpace == null)
            return;

        ulong clientId =
            player.OwnerClientId;

        if (finishedPlayers.Contains(clientId))
            return;

        finishedPlayers.Add(clientId);

        int finishingPlace =
            finishedPlayers.Count;

        int bonusPoints =
            finishSpace.GetFinishBonus(
                finishingPlace
            );

        if (BoardGameState.Instance != null)
        {
            BoardGameState.Instance.AddPoints(
                clientId,
                bonusPoints
            );

            int newPoints =
                BoardGameState.Instance.GetPlayerPoints(
                    clientId
                );

            UpdatePointsClientRpc(
                clientId,
                newPoints
            );
        }

        PlayerFinishedClientRpc(
            clientId,
            finishingPlace,
            bonusPoints
        );

        if (finishedPlayers.Count >= players.Count)
        {
            FinishGame();
            return;
        }

        NextTurn();
    }

    private bool HasFinished(
        ulong clientId)
    {
        return finishedPlayers.Contains(
            clientId
        );
    }

    private void FindNextPlayer()
    {
        if (!IsServer)
            return;

        if (players.Count == 0)
            return;

        int startingIndex =
            currentPlayerIndex.Value;

        for (int i = 1;
             i <= players.Count;
             i++)
        {
            int nextIndex =
                (startingIndex + i) %
                players.Count;

            BoardPlayer nextPlayer =
                players[nextIndex];

            if (nextPlayer == null)
                continue;

            ulong clientId =
                nextPlayer.OwnerClientId;

            if (HasFinished(clientId))
                continue;

            currentPlayerIndex.Value =
                nextIndex;

            currentPlayerClientId.Value =
                clientId;

            diceLocked.Value = false;

            diceRollClientId =
                ulong.MaxValue;

            waitingForMovement = false;

            if (BoardCameraManager.Instance != null)
            {
                BoardCameraManager.Instance.SetDiceCamera();
            }

            return;
        }
    }

    private void NextTurn()
    {
        if (!IsServer)
            return;

        if (players.Count == 0)
            return;

        if (gameFinished)
            return;

        FindNextPlayer();
    }

    private void FinishGame()
    {
        if (!IsServer)
            return;

        if (gameFinished)
            return;

        gameFinished = true;

        diceLocked.Value = true;

        ulong[] finishingOrder =
            finishedPlayers.ToArray();

        int[] points =
            new int[
                finishingOrder.Length
            ];

        for (int i = 0;
             i < finishingOrder.Length;
             i++)
        {
            ulong clientId =
                finishingOrder[i];

            points[i] =
                GetPlayerPoints(
                    clientId
                );
        }

        ShowFinalResultsClientRpc(
            finishingOrder,
            points
        );
    }

    private int GetPlayerPoints(
        ulong clientId)
    {
        if (BoardGameState.Instance == null)
            return 0;

        return BoardGameState.Instance.GetPlayerPoints(
            clientId
        );
    }

    [ClientRpc]
    private void PlayerFinishedClientRpc(
        ulong clientId,
        int finishingPlace,
        int bonusPoints)
    {
    }

    [ClientRpc]
    private void ShowFinalResultsClientRpc(
        ulong[] finishingOrder,
        int[] points)
    {
        if (BoardResultsUI.Instance == null)
            return;

        BoardResultsUI.Instance.ShowResults(
            finishingOrder,
            points
        );
    }

    private void HandleMinigameSpace(
        BoardSpace space)
    {
        if (!IsServer)
            return;

        int nextPlayerIndex =
            currentPlayerIndex.Value + 1;

        if (nextPlayerIndex >= players.Count)
        {
            nextPlayerIndex = 0;
        }

        ulong nextPlayerClientId =
            players[nextPlayerIndex].OwnerClientId;

        if (BoardGameState.Instance != null)
        {
            BoardGameState.Instance.SaveNextPlayer(
                nextPlayerClientId
            );
        }

        if (BoardSpaceManager.Instance == null)
            return;

        string minigameName;
        string sceneName;

        bool success =
            BoardSpaceManager.Instance.GetRandomMinigame(
                out minigameName,
                out sceneName
            );

        if (!success)
            return;

        ShowMinigamePopupClientRpc(
            minigameName,
            sceneName
        );
    }

    [ClientRpc]
    private void ShowMinigamePopupClientRpc(
        string minigameName,
        string sceneName)
    {
        if (BoardSpaceManager.Instance != null)
        {
            BoardSpaceManager.Instance.ShowMinigamePopup(
                minigameName,
                sceneName
            );
        }
    }

    public void ContinueFromMinigame()
    {
        if (!IsServer)
            return;
    }

    public bool IsClientTurn(
        ulong clientId)
    {
        if (currentPlayerClientId.Value ==
            ulong.MaxValue)
        {
            return false;
        }

        return currentPlayerClientId.Value ==
               clientId;
    }

    public bool IsDiceLocked()
    {
        return diceLocked.Value;
    }

    public Transform GetSpace(
        int index)
    {
        if (boardSpaces == null ||
            boardSpaces.Length == 0)
        {
            return null;
        }

        if (index < 0 ||
            index >= boardSpaces.Length)
        {
            return null;
        }

        return boardSpaces[index];
    }

    public int GetSpaceCount()
    {
        if (boardSpaces == null)
            return 0;

        return boardSpaces.Length;
    }

    public void DebugTurn()
    {
        if (NetworkManager.Singleton == null)
            return;

        
    }

    public ulong GetCurrentPlayerClientId()
    {
        return currentPlayerClientId.Value;
    }

    [ClientRpc]
    public void UpdatePointsClientRpc(
        ulong clientId,
        int points)
    {
        if (BoardGameState.Instance == null)
            return;

        BoardGameState.Instance.SavePlayerPoints(
            clientId,
            points
        );
    }

    [ClientRpc]
    private void ShowDiceResultClientRpc(
        int result)
    {
        if (BoardDice.Instance == null)
            return;

        BoardDice.Instance.ShowResultForEveryone(
            result
        );
    }
}