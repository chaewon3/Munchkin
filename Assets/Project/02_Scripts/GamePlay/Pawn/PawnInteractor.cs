using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 앞쪽 일정 범위 안에서 상호작용 대상 Pawn을 찾는다.
    ///
    /// 여기서는 "누가 있냐"까지만 답한다. 그게 사람인지 AI인지는 알지 못하고,
    /// 알아서도 안 된다. 판정은 마스터(GameDirector)의 몫이다.
    /// </summary>
    public sealed class PawnInteractor : MonoBehaviour
    {
        [Tooltip("이 거리 안에 있어야 대상이 된다")]
        [SerializeField] private float _range = 2.5f;

        [Tooltip("정면 기준 좌우 몇 도까지 인정할지")]
        [SerializeField] private float _halfAngle = 60f;

        [Tooltip("대상 Pawn이 올라가 있는 레이어. 벽까지 긁지 않으려면 좁혀두는 게 좋다")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        // 매 입력마다 배열을 새로 만들지 않도록 재사용한다.
        private readonly Collider[] _hits = new Collider[16];

        private Pawn _self;

        private void Awake()
        {
            _self = GetComponent<Pawn>();
        }

        /// <summary>범위·각도 안에서 가장 가까운 Pawn. 없으면 null.</summary>
        public Pawn FindTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _range, _hits, _targetLayers, QueryTriggerInteraction.Ignore);

            Pawn best = null;
            float bestDistance = float.MaxValue;
            float cosLimit = Mathf.Cos(_halfAngle * Mathf.Deg2Rad);

            for (int i = 0; i < count; i++)
            {
                Pawn candidate = _hits[i].GetComponentInParent<Pawn>();
                if (candidate == null || candidate == _self || candidate.IsFired)
                {
                    continue;
                }

                Vector3 delta = candidate.transform.position - transform.position;
                delta.y = 0f;

                float distance = delta.magnitude;
                if (distance < 0.001f || distance >= bestDistance)
                {
                    continue;
                }

                // 뒤통수에 대고 누르는 건 인정하지 않는다. 사장이 대상을 보고 있어야 한다.
                if (Vector3.Dot(transform.forward, delta / distance) < cosLimit)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, _range);

            Quaternion left = Quaternion.Euler(0f, -_halfAngle, 0f);
            Quaternion right = Quaternion.Euler(0f, _halfAngle, 0f);
            Gizmos.DrawRay(transform.position, left * transform.forward * _range);
            Gizmos.DrawRay(transform.position, right * transform.forward * _range);
        }
    }
}
