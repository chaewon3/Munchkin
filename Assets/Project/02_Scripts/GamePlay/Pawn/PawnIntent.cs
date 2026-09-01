using UnityEngine;

namespace Project.GamePlay
{
    /// <summary>
    /// "이렇게 움직이고 싶다"는 의사 표현. 실제로 움직이는 건 PawnMotor다.
    ///
    /// 플레이어와 AI가 공유하는 유일한 통로다.
    /// AI가 이 구조체를 거치지 않고 캐릭터를 움직일 방법이 없어야,
    /// 사장 플레이어가 사람과 AI를 움직임으로 구분할 수 없게 된다.
    /// </summary>
    public struct PawnIntent
    {
        /// <summary>월드 XZ 평면 기준 이동 방향. 길이 0~1 (0.5면 절반 속도).</summary>
        public Vector2 Move;

        /// <summary>
        /// 바라볼 방향 (월드 Y축 각도, degree).
        /// 지금은 이동 방향과 같지만 나중에 마우스 시점이 붙으면 갈라진다.
        /// </summary>
        public float Yaw;

        public bool Sprint;

        /// <summary>이번 프레임에 "눌린 순간"인지. 누르고 있는 상태가 아니다.</summary>
        public bool Interact;

        public static PawnIntent None => default;

        /// <summary>
        /// XZ 이동 방향을 Y축 회전각으로. 두 Brain이 똑같은 계산을 쓰도록 여기 둔다.
        /// 각자 구현하면 미묘하게 달라지고, 그 차이가 곧 AI를 들키게 만든다.
        /// </summary>
        public static float YawFromDirection(Vector2 direction)
        {
            return Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        }
    }
}
