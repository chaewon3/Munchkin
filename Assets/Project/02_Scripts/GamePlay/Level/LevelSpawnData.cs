using System.Collections.Generic;

using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 씬에 배치된 스폰 지점과 순찰 지점을 모아둔 곳.
    ///
    /// GameDirector는 런타임에 스폰되는 NetworkObject라서 프리팹에
    /// 씬 오브젝트 참조를 미리 넣어둘 수 없다. 그래서 씬 쪽에 이 창구를 두고
    /// Director가 찾아 쓰는 구조로 간다.
    /// </summary>
    public sealed class LevelSpawnData : MonoBehaviour
    {
        public static LevelSpawnData Instance { get; private set; }

        [Tooltip("플레이어와 NPC가 등장할 위치들. 부족하면 순환해서 재사용한다")]
        [SerializeField] private Transform[] _spawnPoints;

        [Tooltip("NPC가 돌아다닐 후보 지점들. 각 NPC가 여기서 몇 개를 골라 순찰한다")]
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("NPC 하나가 순찰할 지점 개수")]
        [SerializeField] private int _routeLength = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>겹침을 피하려고 벌리는 최대 거리. 벽을 넘지 않을 만큼만 준다.</summary>
        private const float MaxScatterRadius = 1.2f;

        public bool HasSpawnPoints => _spawnPoints != null && _spawnPoints.Length > 0;

        /// <summary>
        /// index번째 스폰 위치. 지점보다 인원이 많으면 같은 자리에 겹치지 않도록 살짝 흩어준다.
        /// </summary>
        public Vector3 GetSpawnPosition(int index)
        {
            if (!HasSpawnPoints)
            {
                return Vector3.up;
            }

            int lap = index / _spawnPoints.Length;
            Transform point = _spawnPoints[index % _spawnPoints.Length];

            if (lap == 0)
            {
                return point.position;
            }

            // 두 바퀴째부터는 원을 그리며 벌린다. 같은 자리에 겹쳐서 스폰되면
            // CharacterController끼리 밀어내며 튀어나간다.
            //
            // 반지름에는 반드시 상한이 있어야 한다. 큰 인덱스가 들어오면
            // lap이 그만큼 커져서 방 밖으로 밀려나기 때문이다.
            float angle = index * 137.5f * Mathf.Deg2Rad;   // 황금각. 고르게 퍼진다
            float radius = Mathf.Min(0.9f * lap, MaxScatterRadius);
            return point.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        /// <summary>
        /// NPC 하나가 쓸 순찰 경로를 뽑는다. 시작 지점을 매번 다르게 해서
        /// 모든 NPC가 같은 자리에 몰리지 않게 한다.
        /// </summary>
        public Transform[] BuildPatrolRoute(int seed)
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return System.Array.Empty<Transform>();
            }

            int length = Mathf.Clamp(_routeLength, 1, _waypoints.Length);
            var route = new List<Transform>(length);

            for (int i = 0; i < length; i++)
            {
                Transform candidate = _waypoints[(seed + i) % _waypoints.Length];
                if (candidate != null)
                {
                    route.Add(candidate);
                }
            }

            return route.ToArray();
        }
    }
}
