using System.Threading.Tasks;

using Fusion;

using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 개발용 진입점. Splash와 Lobby를 건너뛰고 이 씬에서 바로 세션을 연다.
    ///
    /// 팀원이 만드는 정식 흐름(Splash → Lobby → GameScene)과 겹치지 않도록
    /// 일부러 분리해두었다. 게임플레이를 만드는 동안 매번 로비를 통과하지 않아도 되고,
    /// 나중에 정식 흐름이 완성되면 이 스크립트만 씬에서 빼면 된다.
    /// </summary>
    public sealed class DevGameLauncher : MonoBehaviour
    {
        [Tooltip("같은 이름이면 같은 방에 들어간다. 클론 에디터와 맞춰둘 것")]
        [SerializeField] private string _sessionName = "DevRoom";

        [Tooltip("마스터 클라이언트만 스폰한다. 심판 역할")]
        [SerializeField] private NetworkObject _directorPrefab;

        [SerializeField] private PawnSpawner _pawnSpawner;
        [SerializeField] private bool _autoStart = true;

        private NetworkRunner _runner;

        private async void Start()
        {
            if (_autoStart)
            {
                await StartShared();
            }
        }

        public async Task StartShared()
        {
            if (_runner != null)
            {
                return;
            }

            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            var args = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = _sessionName,

                // Scene을 지정하지 않으면 지금 열려 있는 씬을 그대로 쓴다.
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            };

            StartGameResult result = await _runner.StartGame(args);

            if (!result.Ok)
            {
                GCDebug.Fail($"[Dev] 세션 시작 실패: {result.ShutdownReason}");
                return;
            }

            GCDebug.Success($"[Dev] 세션 시작. 마스터: {_runner.IsSharedModeMasterClient}");

            // Director는 마스터 한 명만 스폰한다. 나머지는 복제본을 받는다.
            if (_runner.IsSharedModeMasterClient && _directorPrefab != null)
            {
                _runner.Spawn(_directorPrefab);
            }

            if (_pawnSpawner != null)
            {
                _pawnSpawner.Begin(_runner);
            }
        }
    }
}
