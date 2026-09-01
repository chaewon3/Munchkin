using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 몸. Brain에게 의사를 묻고 Motor에게 전달하는 조립 담당이다.
    ///
    /// 여기에 "사장이냐 회사원이냐" 같은 역할 정보를 두면 안 된다.
    /// 네트워크를 붙이면 Pawn은 모든 클라이언트에 복제되기 때문에,
    /// 역할이 Pawn에 있으면 상대 클라이언트가 그냥 읽어볼 수 있다.
    /// 역할은 별도 객체에 두고 각자에게만 내려준다.
    /// </summary>
    [RequireComponent(typeof(PawnMotor))]
    public sealed class Pawn : MonoBehaviour
    {
        private PawnMotor _motor;
        private IPawnBrain _brain;

        private void Awake()
        {
            _motor = GetComponent<PawnMotor>();
            _brain = GetComponent<IPawnBrain>();
        }

        /// <summary>
        /// 해고당해서 관전자가 되거나, 사람이 나가서 AI가 대신 조종하는 경우
        /// Brain만 갈아끼우면 된다. 몸은 그대로 남는다.
        /// </summary>
        public void SetBrain(IPawnBrain brain)
        {
            _brain = brain;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            PawnIntent intent = _brain != null
                ? _brain.Think(deltaTime)
                : PawnIntent.None;

            _motor.Tick(intent, deltaTime);

            if (intent.Interact)
            {
                HandleInteract();
            }
        }

        // 지금은 로그만 찍는다. 나중에 여기가 호스트로 요청을 보내는 지점이 된다.
        // 미션도 해고도 전부 이 경로를 탄다.
        private void HandleInteract()
        {
            Debug.Log($"[Pawn] {name} 상호작용 시도", this);
        }
    }
}
