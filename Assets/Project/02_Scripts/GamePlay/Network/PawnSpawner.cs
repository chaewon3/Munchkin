using Fusion;

using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 이 클라이언트 한 명분의 스폰 담당.
    ///
    /// 순서가 중요하다. 역할을 먼저 받고 나서 Pawn을 스폰한다.
    /// 역할에 따라 프리팹이 달라지기 때문이고, 무엇보다 이 클라이언트는
    /// 끝까지 자기 역할 하나만 알게 된다.
    /// </summary>
    public sealed class PawnSpawner : MonoBehaviour
    {
        public static PawnSpawner Local { get; private set; }

        private NetworkRunner _runner;
        private bool _requested;
        private NetworkObject _myPawn;

        public PawnRole MyRole { get; private set; } = PawnRole.Unknown;
        public NetworkObject MyPawn => _myPawn;

        private void Awake()
        {
            Local = this;
        }

        private void OnDestroy()
        {
            if (Local == this)
            {
                Local = null;
            }
        }

        public void Begin(NetworkRunner runner)
        {
            _runner = runner;
            _requested = false;
        }

        private void Update()
        {
            if (_requested || _runner == null || !_runner.IsRunning)
            {
                return;
            }

            // Director는 마스터가 스폰한 뒤 복제되어 오므로 몇 프레임 늦게 나타난다.
            if (GameDirector.Instance == null)
            {
                return;
            }

            _requested = true;
            GameDirector.Instance.RpcRequestRole();
        }

        /// <summary>마스터가 나에게만 보낸 역할. 여기서부터 내 Pawn을 만든다.</summary>
        public void OnRoleGranted(PawnRole role)
        {
            if (_myPawn != null)
            {
                return;
            }

            GameDirector director = GameDirector.Instance;
            if (director == null || _runner == null)
            {
                return;
            }

            NetworkObject prefab = director.GetPrefab(role);
            if (prefab == null)
            {
                GCDebug.Error($"[Spawner] {role} 프리팹이 비어 있다");
                return;
            }

            MyRole = role;

            // NPC가 앞쪽 인덱스를 쓰므로 그 뒤부터 이어서 쓴다.
            // 여기에 큰 수를 넣으면 스폰 지점에서 그만큼 밀려나니 주의.
            Vector3 position = director.GetSpawnPosition(director.NpcCount + _runner.LocalPlayer.PlayerId);

            _myPawn = _runner.Spawn(
                prefab, position, Quaternion.identity, _runner.LocalPlayer,
                (runner, obj) => GameDirector.ConfigureAsPlayer(obj, role));

            GCDebug.Success($"[Spawner] 내 역할: {role}");
        }
    }
}
