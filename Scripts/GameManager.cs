using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TMP_Text statusText;


    private ISession currentSession;

    private async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetStatus("Starting Unity Services...");

        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            SetStatus("Initializing Unity Services...");

            await UnityServices.InitializeAsync();

            SetStatus("Unity Services initialized!\nSigning in...");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            SetStatus(
                "Signed in!\n\n" +
                "Ready to play!"
            );

            Debug.Log("Player ID: " +
                      AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            SetStatus("INITIALIZATION ERROR:\n\n" + e.Message);
            Debug.LogException(e);
        }
    }

    public async void HostGame()
    {
        try
        {
            SetStatus("Creating game...");

            var options = new SessionOptions
            {
                MaxPlayers = 4
            }.WithRelayNetwork();

            currentSession =
    await MultiplayerService.Instance.CreateSessionAsync(options);

            currentSession.PlayerJoined += OnPlayerJoined;

            SetStatus(
                "HOST STARTED!\n\n" +
                "JOIN CODE:\n" +
                currentSession.Code +
                "\n\n" +
                "Players: " +
                currentSession.Players.Count +
                " / " +
                currentSession.MaxPlayers
            );

            Debug.Log("Game created!");
            Debug.Log("Join Code: " + currentSession.Code);

            // IMPORTANT:
            // Do NOT call StartHost().
            // WithRelayNetwork() already started networking.
        }
        catch (Exception e)
        {
            SetStatus("HOST ERROR:\n\n" + e.Message);
            Debug.LogException(e);
        }
    }

    public async void JoinGame(string joinCode)
    {
        try
        {
            joinCode = joinCode.Trim().ToUpper();

            if (string.IsNullOrEmpty(joinCode))
            {
                SetStatus("Please enter a join code.");
                return;
            }

            SetStatus(
                "Joining game...\n\n" +
                "Code: " + joinCode
            );

            currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            SetStatus(
                 "CONNECTED!\n\n" +
                "Joined game!\n\n" +
                "Waiting for game to start..."
            );

            Debug.Log("Joined session: " + currentSession.Id);

            // IMPORTANT:
            // Do NOT call StartClient().
            // Joining the session already starts networking.
        }
        catch (Exception e)
        {
            SetStatus("JOIN ERROR:\n\n" + e.Message);
            Debug.LogException(e);
        }
    }

    public async void LeaveGame()
    {
        try
        {
            SetStatus("Leaving game...");

            if (currentSession != null)
            {
                await currentSession.LeaveAsync();
                currentSession = null;
            }

            SetStatus("Left the game.");
        }
        catch (Exception e)
        {
            SetStatus("LEAVE ERROR:\n\n" + e.Message);
            Debug.LogException(e);
        }
    }

    private void SetStatus(string message)
    {
        Debug.Log(message);

        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void OnPlayerJoined(string playerId)
    {
        Debug.Log("Player joined: " + playerId);

        if (currentSession != null)
        {
            SetStatus(
                "PLAYER JOINED!\n\n" +
                "JOIN CODE:\n" +
                currentSession.Code +
                "\n\n" +
                "Players: " +
                currentSession.Players.Count +
                " / " +
                currentSession.MaxPlayers
            );
        }
    }
}