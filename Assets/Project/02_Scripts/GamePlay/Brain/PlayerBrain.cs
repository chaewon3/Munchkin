using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.GamePlay
{
    /// <summary>
    /// 키보드 입력을 PawnIntent로 번역한다.
    ///
    /// 입력을 읽는 코드가 이 파일 안에만 갇혀 있는 게 중요하다.
    /// 나중에 .inputactions 애셋으로 바꾸든 뭘 하든 고칠 파일은 여기 하나뿐이다.
    /// </summary>
    public sealed class PlayerBrain : MonoBehaviour, IPawnBrain
    {
        private Pawn _pawn;

        // "이번에 눌렸다"를 Update에서 받아두고 Think에서 한 번만 꺼내 쓴다.
        private bool _interactLatched;

        private void Awake()
        {
            _pawn = GetComponent<Pawn>();
        }

        /// <summary>
        /// 눌린 순간은 렌더 프레임 기준으로만 알 수 있는데,
        /// Think()는 FixedUpdateNetwork(네트워크 틱)에서 불린다.
        /// 둘의 주기가 달라서 여기서 걸어두지 않으면 입력이 통째로 사라진다.
        /// 네트워크 게임에서 "가끔 상호작용이 안 먹는" 버그는 대부분 이것 때문이다.
        /// </summary>
        private void Update()
        {
            if (!IsMine())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                _interactLatched = true;
            }
        }

        public PawnIntent Think(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return PawnIntent.None;
            }

            // 화면 기준으로 움직여야 한다. 카메라를 돌렸는데 W가 계속 월드 +Z로 가면
            // 3인칭에서 조작이 완전히 어긋난다.
            Vector2 move = ToCameraSpace(ReadMove(keyboard));

            bool interact = _interactLatched;
            _interactLatched = false;

            return new PawnIntent
            {
                Move = move,

                // 지금은 가는 방향을 그대로 본다. 마우스 시점이 붙으면 이 줄만 바뀐다.
                Yaw = move.sqrMagnitude > 0.001f
                    ? PawnIntent.YawFromDirection(move)
                    : transform.eulerAngles.y,

                Sprint = keyboard.leftShiftKey.isPressed,
                Interact = interact,
            };
        }

        // 남의 Pawn 복제본에서도 이 컴포넌트는 살아 있다. 거기서 키보드를 읽으면
        // 내 조작이 남의 캐릭터에 섞여 들어간 것처럼 보이는 버그가 된다.
        private bool IsMine()
        {
            return _pawn != null
                && _pawn.Object != null
                && _pawn.Object.IsValid
                && _pawn.HasStateAuthority;
        }

        /// <summary>입력을 카메라가 보는 방향 기준으로 돌린다. 카메라가 없으면 월드 기준 그대로.</summary>
        private static Vector2 ToCameraSpace(Vector2 input)
        {
            PawnCamera camera = PawnCamera.Instance;
            if (camera == null || input.sqrMagnitude < 0.0001f)
            {
                return input;
            }

            float radians = camera.Yaw * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            return new Vector2(
                input.x * cos + input.y * sin,
                -input.x * sin + input.y * cos);
        }

        private static Vector2 ReadMove(Keyboard keyboard)
        {
            Vector2 move = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;

            return Vector2.ClampMagnitude(move, 1f);
        }
    }
}
