using Fusion;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;

using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;

public class LobbyNetworkState : NetworkState
{
    public List<SessionInfo> m_RoomInfoList = new();


    #region ActionCallback
    public event Action OnLobbyJoined;
    public event Action<List<SessionInfo>> OnSessionListUpdated;
    public event Action<NetworkPlayer> OnPlayerJoined;
    #endregion

    #region Handler
    public override void OnPlayerResistered(NetworkPlayer player)
    {
        OnPlayerJoined?.Invoke(player);
    }

    public async void RequestJoinSession(SessionInfo session, Action sueccessCallback, Action failureCallback = null)
    {
        var result = await _networkManager.JoinSession(session);
        if (!result.Ok)
        {
            
            GCDebug.Fail($"·ë ÀÔÀå ½ÇÆÐ : {result.ErrorMessage}");
            failureCallback?.Invoke();
            return;
        }

        GCDebug.Success("·ë ÀÔÀå ¼º°ø");
        sueccessCallback?.Invoke();
    }
    public async void RequestJoinRandomSession(Action sueccessCallback, Action failureCallback = null)
    {
        var result = await _networkManager.JoinRandomSession();
        if (!result.Ok)
        {

            GCDebug.Fail($"·ë ÀÔÀå ½ÇÆÐ : {result.ErrorMessage}");
            failureCallback?.Invoke();
            return;
        }

        GCDebug.Success("·ë ÀÔÀå ¼º°ø");
        sueccessCallback?.Invoke();

    }
    public async void RequestCreateSession(Action sueccessCallback, Action failureCallback = null)
    {
        var result = await _networkManager.CreateSession();

        if (!result.Ok)
        {
            GCDebug.Fail($"·ë »ý¼º ½ÇÆÐ : {result.ErrorMessage}");
            failureCallback?.Invoke();
            return;
        }

        GCDebug.Success("·ë »ý¼º ¼º°ø");
        sueccessCallback?.Invoke();
    }


    #endregion

    #region Test
    public void Create()
    {
        RequestCreateSession(() => { });
    }

    public void Enter()
    {
        RequestJoinRandomSession(() => { });
    }
    #endregion
}
