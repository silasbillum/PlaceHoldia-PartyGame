using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

public class PlaceholdiaNetwork : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    private ISession currentSession;

    private void Start()
    {
        SetStatus("Starting Unity Services...");
        _ = InitializeServices();
    }

    private async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();

            SetStatus("Unity Services initialized.");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                SetStatus("Signing in anonymously...");

                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            SetStatus(
                "Signed in!\n" +
                "Player ID: " + AuthenticationService.Instance.PlayerId
            );
        }
        catch (Exception e)
        {
            SetStatus("ERROR:\n" + e.Message);
            Debug.LogException(e);
        }
    }

    public async void HostGame()
    {
        try
        {
            SetStatus("Creating session...");

            var options = new SessionOptions
            {
                MaxPlayers = 4
            }.WithRelayNetwork();

            currentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);

            SetStatus(
                "SESSION CREATED!\n\n" +
                "Join Code: " + currentSession.Code + "\n\n" +
                "Starting host..."
            );

            bool started = NetworkManager.Singleton.StartHost();

            if (started)
            {
                SetStatus(
                    "HOST STARTED!\n\n" +
                    "Join Code: " + currentSession.Code
                );
            }
            else
            {
                SetStatus("ERROR: Host failed to start.");
            }
        }
        catch (Exception e)
        {
            SetStatus("HOST ERROR:\n" + e.Message);
            Debug.LogException(e);
        }
    }

    public async void JoinGame(string joinCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                SetStatus("Please enter a join code.");
                return;
            }

            joinCode = joinCode.Trim().ToUpper();

            SetStatus(
                "Joining session...\n\n" +
                "Code: " + joinCode
            );

            currentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

            SetStatus(
                "SESSION JOINED!\n\n" +
                "Starting client..."
            );

            bool started = NetworkManager.Singleton.StartClient();

            if (started)
            {
                SetStatus(
                    "CLIENT STARTED!\n\n" +
                    "Connected to host."
                );
            }
            else
            {
                SetStatus("ERROR: Client failed to start.");
            }
        }
        catch (Exception e)
        {
            SetStatus("JOIN ERROR:\n" + e.Message);
            Debug.LogException(e);
        }
    }

    public async void LeaveGame()
    {
        try
        {
            if (currentSession != null)
            {
                await currentSession.LeaveAsync();
                currentSession = null;
            }

            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            SetStatus("Left the game.");
        }
        catch (Exception e)
        {
            SetStatus("LEAVE ERROR:\n" + e.Message);
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
}