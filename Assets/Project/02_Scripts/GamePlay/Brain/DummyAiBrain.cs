using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 웨이포인트를 왕복하는 임시 AI. 진짜 행동 스케줄러가 들어오기 전까지의 자리채움이다.
    ///
    /// 여기서 중요한 건 동작이 아니라, 이 클래스가 PawnMotor를 한 줄도 건드리지 않는다는 점이다.
    /// AI가 할 수 있는 건 "저쪽으로 가고 싶다"는 PawnIntent를 채우는 것뿐이고,
    /// 실제로 어떻게 움직이는지는 PlayerBrain일 때와 완전히 동일하다.
    /// </summary>
    public sealed class DummyAiBrain : MonoBehaviour, IPawnBrain
    {
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("이 거리 안에 들어오면 도착으로 친다")]
        [SerializeField] private float _arriveDistance = 0.6f;

        [Tooltip("도착 후 다음 지점으로 출발하기까지 멈춰 있는 시간")]
        [SerializeField] private float _waitTime = 1f;

        private int _index;
        private float _waitTimer;

        private void Awake()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogWarning($"[DummyAiBrain] {name}: 웨이포인트가 비어 있다. 제자리에 서 있게 된다.", this);
            }
        }

        public PawnIntent Think(float deltaTime)
        {
            Transform target = CurrentWaypoint();
            if (target == null)
            {
                return Idle();
            }

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;   // 높이 차이는 무시. 안 그러면 바닥에 박힌 웨이포인트로 파고들려 한다.

            if (delta.sqrMagnitude <= _arriveDistance * _arriveDistance)
            {
                _waitTimer += deltaTime;
                if (_waitTimer >= _waitTime)
                {
                    _waitTimer = 0f;
                    _index = (_index + 1) % _waypoints.Length;
                }

                return Idle();
            }

            Vector2 direction = new Vector2(delta.x, delta.z).normalized;

            return new PawnIntent
            {
                Move = direction,
                Yaw = PawnIntent.YawFromDirection(direction),

                // 지금은 절대 안 뛴다. 나중에 이게 그대로 남으면
                // "안 뛰는 놈이 AI"라는 단서가 되어버린다. 진짜 AI 만들 때 손볼 것.
                Sprint = false,
                Interact = false,
            };
        }

        // 멈춰 있을 때도 보던 방향은 유지해야 한다.
        // Yaw를 0으로 두면 도착할 때마다 북쪽으로 홱 돌아서 바로 티가 난다.
        private PawnIntent Idle()
        {
            return new PawnIntent
            {
                Move = Vector2.zero,
                Yaw = transform.eulerAngles.y,
            };
        }

        private Transform CurrentWaypoint()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return null;
            }

            return _waypoints[_index % _waypoints.Length];
        }

        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            Gizmos.color = Color.cyan;

            for (int i = 0; i < _waypoints.Length; i++)
            {
                Transform current = _waypoints[i];
                Transform next = _waypoints[(i + 1) % _waypoints.Length];
                if (current == null || next == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(current.position, _arriveDistance);
                Gizmos.DrawLine(current.position, next.position);
            }
        }
    }
}
