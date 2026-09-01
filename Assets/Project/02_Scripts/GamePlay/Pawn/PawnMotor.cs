using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// 유일하게 실제로 캐릭터를 움직이는 곳. 속도·가속·회전속도 상수를 독점한다.
    ///
    /// AI 전용 속도값을 다른 데 두는 순간 움직임이 갈라지고 바로 들킨다.
    /// 이동에 관한 숫자는 전부 여기에만 있어야 한다.
    ///
    /// 스스로 Update를 갖지 않고 Tick()을 밖에서 불러주는 구조인 게 중요하다.
    /// 나중에 Fusion을 얹을 때 Pawn의 FixedUpdateNetwork에서 이걸 부르면
    /// 이 파일은 거의 손대지 않고 넘어간다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PawnMotor : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _sprintSpeed = 5.5f;
        [SerializeField] private float _acceleration = 25f;

        [Header("회전")]
        [SerializeField] private float _turnSpeed = 720f;   // 초당 degree

        [Header("중력")]
        [SerializeField] private float _gravity = -20f;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        /// <summary>
        /// 애니메이터는 나중에 입력이 아니라 이 값을 봐야 한다.
        /// 입력을 보면 AI는 입력이 없어서 같은 코드를 쓸 수 없다.
        /// </summary>
        public Vector3 Velocity => _horizontalVelocity;

        public float SpeedRatio => _horizontalVelocity.magnitude / _sprintSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void Tick(in PawnIntent intent, float deltaTime)
        {
            ApplyMove(intent, deltaTime);
            ApplyTurn(intent, deltaTime);
        }

        private void ApplyMove(in PawnIntent intent, float deltaTime)
        {
            Vector2 input = Vector2.ClampMagnitude(intent.Move, 1f);
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            float speed = intent.Sprint ? _sprintSpeed : _walkSpeed;
            Vector3 targetVelocity = direction * speed;

            // 즉시 최고속도가 되면 조작감이 미끄럽고, 무엇보다 AI와 사람의
            // 움직임 차이가 가속 구간에서 드러나기 때문에 이 값이 중요하다.
            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity, targetVelocity, _acceleration * deltaTime);

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                // 0으로 두면 isGrounded가 매 프레임 깜빡인다. 살짝 눌러둔다.
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += _gravity * deltaTime;
            }

            Vector3 velocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * deltaTime);
        }

        private void ApplyTurn(in PawnIntent intent, float deltaTime)
        {
            Quaternion target = Quaternion.Euler(0f, intent.Yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, _turnSpeed * deltaTime);
        }
    }
}
