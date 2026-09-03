using UnityEngine;
using Fusion;

using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;


public class NetworkManager : FusionMonoBehaviour, INetworkRunnerCallbacks
{
    [HideInInspector] public static NetworkManager Instance;
    [HideInInspector] public static NetworkRunner Runner;

    private Dictionary<PlayerRef, NetworkPlayer> _players = new();


    [HideInInspector]  public NetworkPlayer m_LocalPlayer;

    [Tooltip("현재 씬의 네트워크컨트롤러(상속객체). 씬 진입 시 자동할당됩니다.")]
    [HideInInspector]  public NetworkState m_CurrentState;

    public bool m_IsSessionOwner => Runner.IsSharedModeMasterClient || Runner.IsSinglePlayer;

    private void Awake()
    {
        Instance = this;
        Runner = gameObject.AddComponent<NetworkRunner>();
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlayer(PlayerRef player, NetworkPlayer data)
    {
        _players[player] = data;
        if(player == Runner.LocalPlayer)
            m_LocalPlayer = data;
    }

    public NetworkPlayer GetPlayerData(PlayerRef player)
    {
        return _players.TryGetValue(player, out var data) ? data : null;
    }

    public async Task<StartGameResult> JoinLobby()
    {
        return await Runner.JoinSessionLobby(SessionLobby.Custom, "Lobby");
    }
    
    public async Task<StartGameResult> JoinSession(SessionInfo session)
    {
        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = session.Name,
            CustomLobbyName = "Lobby",
        };
        return await Runner.StartGame(args);
    }
    public async Task<StartGameResult> JoinRandomSession()
    {
        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            CustomLobbyName = "Lobby",
        };
        return await Runner.StartGame(args);
    }

    public async Task<StartGameResult> CreateSession()
    {
        int roomnum = UnityEngine.Random.Range(0000, 9999);
        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = roomnum.ToString(),
            CustomLobbyName = "Lobby",
            PlayerCount = 2,
        };

        return await Runner.StartGame(args);
    }

    #region Fusion Callbacks

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    #endregion
}
