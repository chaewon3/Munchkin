using Fusion;

using UnityEngine;

/// <summary>
/// 네트워크 콜백 관련해서만 처리함
/// 씬별로 개별처리
/// </summary>
public class NetworkState : MonoBehaviour
{
    protected NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = NetworkManager.Instance;
        _networkManager.m_CurrentState = this;
    }

    public virtual void OnPlayerJoinedHandler(NetworkRunner runner, PlayerRef player) { }
    public virtual void OnPlayerResistered(NetworkPlayer player) { }

}
