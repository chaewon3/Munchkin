using Fusion;

using System;

using UnityEngine;

/// <summary>
/// 각 플레이어를 대표하는 네트워크 오브젝트
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    public PlayerRef Owner => Object.InputAuthority;

    [Networked] public NetworkString<_32> Nickname { get; set; }
    [Networked, OnChangedRender(nameof(OnReadyChanged))] public NetworkBool IsLobbyReady { get; set; }
    public event Action<bool> ReadyChanged;

    public override void Spawned()
    {
        NetworkManager.Instance.RegisterPlayer(Object.InputAuthority, this);

        gameObject.transform.SetParent(NetworkManager.Instance.transform);
        gameObject.name = $"Player_{Object.InputAuthority.PlayerId}";
    }

    #region RPC메서드
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetNickname(string name)
    {
        Nickname = name;
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady()
    {
        IsLobbyReady = !IsLobbyReady;
    }
    #endregion

    private void OnReadyChanged()
    {
        ReadyChanged?.Invoke(IsLobbyReady);
    }

}
