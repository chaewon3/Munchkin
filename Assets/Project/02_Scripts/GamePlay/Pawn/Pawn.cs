using Fusion;

using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 몸. Brain에게 의사를 묻고 Motor에게 전달하는 조립 담당이다.
    ///
    /// Shared Mode라서 이 Pawn을 스폰한 클라이언트가 StateAuthority를 갖는다.
    /// 플레이어 Pawn은 그 플레이어가, NPC Pawn은 마스터 클라이언트가 주인이다.
    /// Brain은 주인 쪽에서만 돌고, 나머지 클라이언트에는 NetworkTransform이
    /// 결과만 보간해서 보여준다.
    ///
    /// 역할(사장/회사원/AI)은 절대 [Networked]로 두지 않는다. 자세한 이유는 PawnRole 참고.
    /// </summary>
    [RequireComponent(typeof(PawnMotor))]
    public sealed class Pawn : NetworkBehaviour
    {
        /// <summary>해고 여부는 숨길 정보가 아니다. 모두가 봐야 하므로 복제해도 된다.</summary>
        [Networked, OnChangedRender(nameof(OnFiredChanged))]
        public NetworkBool IsFired { get; set; }

        /// <summary>
        /// 이 클라이언트가 아는 이 Pawn의 정체.
        /// 내 Pawn과 마스터가 소유한 NPC를 빼면 Unknown으로 남는다.
        /// 복제되지 않는 평범한 필드라는 점이 핵심이다.
        /// </summary>
        public PawnRole KnownRole { get; private set; } = PawnRole.Unknown;

        private PawnMotor _motor;
        private PawnInteractor _interactor;
        private IPawnBrain _brain;

        private void Awake()
        {
            _motor = GetComponent<PawnMotor>();
            _interactor = GetComponent<PawnInteractor>();
            _brain = GetComponent<IPawnBrain>();
        }

        public override void Spawned()
        {
            // 프록시라고 CharacterController를 끄면 안 된다.
            // 프리팹에 다른 콜라이더가 없어서, 끄는 순간 물리 씬에서 사라지고
            // 사장의 PawnInteractor가 아무도 찾지 못하게 된다.
            //
            // 켜둬도 안전하다. CharacterController는 Move()를 부를 때만 밀어내는데
            // 프록시는 Move()를 부르지 않으므로 그냥 콜라이더로만 남는다.
            // 덤으로 다른 플레이어를 뚫고 지나가지 못하게 된다.

            gameObject.name = HasStateAuthority ? $"Pawn_Mine_{Object.Id}" : $"Pawn_{Object.Id}";

            // InputAuthority가 있다는 건 "사람이 조종하는 Pawn"이라는 뜻이고,
            // 그게 나라면 내 캐릭터다. NPC는 InputAuthority 없이 스폰되므로 걸리지 않는다.
            if (HasInputAuthority)
            {
                PawnCamera.Instance?.SetTarget(transform);
            }

            // 누가 스폰했든 마스터에도 복제본이 생기면서 여기가 불린다.
            // 그때 마스터가 스스로 역할표에 올리므로 별도 RPC가 필요 없다.
            if (Runner.IsSharedModeMasterClient && GameDirector.Instance != null)
            {
                GameDirector.Instance.RegisterPawn(Object);
            }
        }

        /// <summary>
        /// 해고당해서 관전자가 되거나, 사람이 나가서 AI가 대신 조종하는 경우
        /// Brain만 갈아끼우면 된다. 몸은 그대로 남는다.
        /// </summary>
        public void SetBrain(IPawnBrain brain)
        {
            _brain = brain;
        }

        /// <summary>
        /// GameDirector가 이 Pawn의 주인에게만 알려주는 정체.
        /// 다른 클라이언트에서는 호출되지 않으므로 Unknown으로 남는다.
        /// </summary>
        public void SetKnownRole(PawnRole role)
        {
            KnownRole = role;
        }

        public override void FixedUpdateNetwork()
        {
            // 주인이 아니면 시뮬레이션하지 않는다. 위치는 NetworkTransform이 가져온다.
            if (!HasStateAuthority)
            {
                return;
            }

            float deltaTime = Runner.DeltaTime;

            if (IsFired)
            {
                // 해고된 뒤에는 입력을 무시하되 감속은 그대로 태운다.
                // 즉시 멈추면 다른 클라이언트 화면에서 뚝 끊겨 보인다.
                _motor.Tick(PawnIntent.None, deltaTime);
                return;
            }

            PawnIntent intent = _brain != null
                ? _brain.Think(deltaTime)
                : PawnIntent.None;

            _motor.Tick(intent, deltaTime);

            if (intent.Interact)
            {
                HandleInteract();
            }
        }

        /// <summary>
        /// 역할에 따라 상호작용이 갈라지는 유일한 지점.
        /// 몸은 같고 할 수 있는 일만 다르다는 설계가 여기서 드러난다.
        /// </summary>
        private void HandleInteract()
        {
            switch (KnownRole)
            {
                case PawnRole.Boss:
                    TryFire();
                    break;

                case PawnRole.Worker:
                    // TODO: 회사 뿌시기 미션. 지금은 자리만 잡아둔다.
                    GCDebug.Log($"[Pawn] 미션 상호작용 시도 ({Object.Id})");
                    break;

                default:
                    break;
            }
        }

        private void TryFire()
        {
            if (_interactor == null)
            {
                return;
            }

            Pawn target = _interactor.FindTarget();
            if (target == null)
            {
                GCDebug.Log("[Pawn] 해고 대상 없음");
                return;
            }

            // 판정은 마스터가 한다. 사장 클라이언트가 직접 판정하면
            // 그 순간 상대의 정체를 알게 되어 게임이 무너진다.
            GameDirector.Instance?.RpcRequestFire(Object, target.Object);
        }

        /// <summary>마스터의 판정 결과. 이 Pawn의 주인만 자기 상태를 바꿀 수 있다.</summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcApplyFired()
        {
            IsFired = true;
        }

        private void OnFiredChanged()
        {
            if (!IsFired)
            {
                return;
            }

            GCDebug.Warning($"[Pawn] {Object.Id} 해고됨");
            // TODO: 관전 전환 / 시각 처리. 지금은 움직임만 멈춘다.
        }
    }
}
