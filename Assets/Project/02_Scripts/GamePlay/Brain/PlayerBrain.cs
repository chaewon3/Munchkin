using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.GamePlay
{
    /// <summary>
    /// 키보드 입력을 PawnIntent로 번역한다.
    ///
    /// 입력을 읽는 코드가 이 파일 안에만 갇혀 있는 게 중요하다.
    /// 나중에 .inputactions 애셋으로 바꾸든 Fusion 네트워크 입력으로 바꾸든
    /// 고칠 파일은 여기 하나뿐이다.
    /// </summary>
    public sealed class PlayerBrain : MonoBehaviour, IPawnBrain
    {
        public PawnIntent Think(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return PawnIntent.None;
            }

            Vector2 move = ReadMove(keyboard);

            return new PawnIntent
            {
                Move = move,

                // 지금은 가는 방향을 그대로 본다. 마우스 시점이 붙으면 이 줄만 바뀐다.
                Yaw = move.sqrMagnitude > 0.001f
                    ? PawnIntent.YawFromDirection(move)
                    : transform.eulerAngles.y,

                Sprint = keyboard.leftShiftKey.isPressed,

                // 주의: wasPressedThisFrame은 Update 프레임 기준이다.
                // Fusion으로 넘어가면 FixedUpdateNetwork가 한 프레임에 0번 또는 여러 번
                // 돌기 때문에 이 방식은 그대로 못 쓴다. NetworkButtons로 바꿔야 할 지점.
                Interact = keyboard.eKey.wasPressedThisFrame,
            };
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
