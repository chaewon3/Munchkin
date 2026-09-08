using System.Collections.Generic;

using Fusion;

using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 마스터 클라이언트가 소유하는 심판. 이 게임의 모든 비밀은 여기 한 곳에만 모인다.
    ///
    /// Shared Mode에는 서버가 없어서 비밀을 보관할 신뢰 지점이 없다.
    /// 그래서 마스터가 그 역할을 대신하고, 역할표는 [Networked]가 아닌
    /// 평범한 Dictionary에 담는다. 복제되지 않으니 다른 클라이언트는 읽을 수 없다.
    ///
    /// 한계는 알고 가야 한다. 마스터가 되는 플레이어는 이론상 전부 볼 수 있다.
    /// 그걸 막으려면 전용 서버 빌드가 필요하다.
    /// </summary>
    public sealed class GameDirector : NetworkBehaviour
    {
        public static GameDirector Instance { get; private set; }

        [Header("프리팹")]
        [Tooltip("사장. 드러나도 되므로 회사원과 달라도 된다")]
        [SerializeField] private NetworkObject _bossPrefab;

        [Tooltip("회사원 + AI 공용. 반드시 하나여야 한다. 나뉘는 순간 구분이 가능해진다")]
        [SerializeField] private NetworkObject _workerPrefab;

        [Header("구성")]
        [SerializeField] private int _bossCount = 1;
        [SerializeField] private int _npcCount = 6;

        // 마스터만 채우는 진짜 정보. 복제되지 않는다.
        private readonly Dictionary<PlayerRef, PawnRole> _playerRoles = new();
        private readonly Dictionary<NetworkId, PawnRole> _pawnRoles = new();

        private int _spawnIndex;

        public override void Spawned()
        {
            Instance = this;

            if (HasStateAuthority)
            {
                SpawnNpcs();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region 마스터 전용

        private void SpawnNpcs()
        {
            if (_workerPrefab == null)
            {
                GCDebug.Error("[Director] worker 프리팹이 비어 있다");
                return;
            }

            for (int i = 0; i < _npcCount; i++)
            {
                Vector3 position = NextSpawnPosition();
                Transform[] route = LevelSpawnData.Instance != null
                    ? LevelSpawnData.Instance.BuildPatrolRoute(i)
                    : System.Array.Empty<Transform>();

                NetworkObject npc = Runner.Spawn(
                    _workerPrefab, position, Quaternion.identity, null,
                    (runner, obj) => ConfigureAsAi(obj, route));

                if (npc != null)
                {
                    _pawnRoles[npc.Id] = PawnRole.Ai;
                }
            }

            GCDebug.Success($"[Director] NPC {_npcCount}명 스폰");
        }

        private Vector3 NextSpawnPosition()
        {
            Vector3 position = LevelSpawnData.Instance != null
                ? LevelSpawnData.Instance.GetSpawnPosition(_spawnIndex)
                : Vector3.up;

            _spawnIndex++;
            return position;
        }

        /// <summary>
        /// 플레이어 Pawn을 역할표에 올린다.
        ///
        /// RPC가 아니라는 점이 중요하다. 어느 클라이언트가 Pawn을 스폰하든
        /// 마스터에도 그 복제본이 생기면서 Spawned()가 불린다. 그때 마스터가
        /// 스스로 등록하면 되므로, "방금 스폰한 오브젝트를 상대가 아직 모르는"
        /// 경쟁 상태를 아예 피할 수 있다.
        /// </summary>
        internal void RegisterPawn(NetworkObject pawn)
        {
            if (!HasStateAuthority || pawn == null)
            {
                return;
            }

            // NPC는 스폰하는 순간 이미 등록했다.
            if (_pawnRoles.ContainsKey(pawn.Id))
            {
                return;
            }

            PlayerRef owner = pawn.InputAuthority;
            if (owner == PlayerRef.None)
            {
                return;
            }

            _pawnRoles[pawn.Id] = _playerRoles.TryGetValue(owner, out PawnRole role)
                ? role
                : PawnRole.Worker;
        }

        /// <summary>플레이어 수와 설정에 따라 사장/회사원을 정한다.</summary>
        private PawnRole DecideRole(PlayerRef player)
        {
            if (_playerRoles.TryGetValue(player, out PawnRole existing))
            {
                return existing;
            }

            int bossSoFar = 0;
            foreach (PawnRole role in _playerRoles.Values)
            {
                if (role == PawnRole.Boss)
                {
                    bossSoFar++;
                }
            }

            PawnRole assigned = bossSoFar < _bossCount ? PawnRole.Boss : PawnRole.Worker;
            _playerRoles[player] = assigned;
            return assigned;
        }

        #endregion

        #region RPC

        /// <summary>게임 씬에 들어온 클라이언트가 자기 역할을 요청한다.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestRole(RpcInfo info = default)
        {
            PlayerRef sender = info.Source;
            PawnRole role = DecideRole(sender);

            GCDebug.Log($"[Director] {sender} 에게 {role} 배정");

            // 마스터 자신이 요청한 경우에는 RPC를 태우지 않는다.
            // [RpcTarget]은 "서버로 보낸 뒤 대상에게 전달"하는 방식이라
            // 보내는 사람과 받는 사람이 같으면 그 왕복이 성립하지 않는다.
            if (sender == Runner.LocalPlayer)
            {
                PawnSpawner.Local?.OnRoleGranted(role);
                return;
            }

            RpcGrantRole(sender, (byte)role);
        }

        /// <summary>
        /// 요청한 플레이어 한 명에게만 간다. [RpcTarget] 덕분에 다른 클라이언트는
        /// 이 메시지 자체를 받지 못한다. 역할 은닉이 실제로 성립하는 지점이다.
        /// </summary>
        [Rpc]
        public void RpcGrantRole([RpcTarget] PlayerRef target, byte role)
        {
            PawnSpawner.Local?.OnRoleGranted((PawnRole)role);
        }

        /// <summary>사장이 해고를 시도한다. 판정은 여기(마스터)에서만 이뤄진다.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestFire(NetworkObject attacker, NetworkObject target, RpcInfo info = default)
        {
            if (attacker == null || target == null)
            {
                return;
            }

            // 사장이 아닌 클라이언트가 보낸 요청은 버린다.
            if (!_pawnRoles.TryGetValue(attacker.Id, out PawnRole attackerRole) || attackerRole != PawnRole.Boss)
            {
                GCDebug.Warning($"[Director] 사장이 아닌 해고 요청 무시 ({info.Source})");
                return;
            }

            if (!_pawnRoles.TryGetValue(target.Id, out PawnRole targetRole) || targetRole == PawnRole.Boss)
            {
                return;
            }

            // 해고 자체는 사람이든 AI든 성립한다. 차이는 사장이 무엇을 잃었는가다.
            // 오해고 페널티는 다음 단계에서 여기 붙는다.
            _pawnRoles.Remove(target.Id);

            Pawn targetPawn = target.GetComponent<Pawn>();
            if (targetPawn != null)
            {
                targetPawn.RpcApplyFired();
            }

            RpcAnnounceFireResult((byte)targetRole);
        }

        /// <summary>
        /// 해고 결과 공개. 이미 게임에서 빠진 대상의 정체만 알리므로
        /// 남아 있는 사람들의 정체는 새지 않는다.
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RpcAnnounceFireResult(byte revealedRole)
        {
            if ((PawnRole)revealedRole == PawnRole.Ai)
            {
                GCDebug.Fail("[Director] 오해고. AI를 잘랐다");
            }
            else
            {
                GCDebug.Success("[Director] 해고 성공. 진짜 회사원이었다");
            }
        }

        #endregion

        #region 스폰 설정

        internal NetworkObject GetPrefab(PawnRole role)
        {
            return role == PawnRole.Boss ? _bossPrefab : _workerPrefab;
        }

        /// <summary>NPC가 먼저 자리를 차지하므로, 플레이어는 그 뒤 인덱스부터 쓴다.</summary>
        internal int NpcCount => _npcCount;

        internal Vector3 GetSpawnPosition(int index)
        {
            return LevelSpawnData.Instance != null
                ? LevelSpawnData.Instance.GetSpawnPosition(index)
                : Vector3.up;
        }

        /// <summary>
        /// 스폰 직후, Spawned()가 불리기 전에 Brain을 확정한다.
        /// 사람용 Brain과 AI용 Brain이 같은 프리팹에 다 붙어 있고 여기서 하나만 고른다.
        /// 프리팹을 나누지 않는 이유는, 나뉘는 순간 둘이 갈라지기 때문이다.
        /// </summary>
        private static void ConfigureAsAi(NetworkObject obj, Transform[] route)
        {
            var pawn = obj.GetComponent<Pawn>();
            var ai = obj.GetComponent<DummyAiBrain>();
            var player = obj.GetComponent<PlayerBrain>();

            if (player != null)
            {
                player.enabled = false;
            }

            if (ai != null)
            {
                ai.enabled = true;
                ai.SetRoute(route);

                if (pawn != null)
                {
                    pawn.SetBrain(ai);
                }
            }

            if (pawn != null)
            {
                pawn.SetKnownRole(PawnRole.Ai);
            }
        }

        internal static void ConfigureAsPlayer(NetworkObject obj, PawnRole role)
        {
            var pawn = obj.GetComponent<Pawn>();
            var ai = obj.GetComponent<DummyAiBrain>();
            var player = obj.GetComponent<PlayerBrain>();

            if (ai != null)
            {
                ai.enabled = false;
            }

            if (player != null)
            {
                player.enabled = true;

                if (pawn != null)
                {
                    pawn.SetBrain(player);
                }
            }

            if (pawn != null)
            {
                pawn.SetKnownRole(role);
            }
        }

        #endregion
    }
}
