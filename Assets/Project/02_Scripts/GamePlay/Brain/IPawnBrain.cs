namespace Project.GamePlay
{
    /// <summary>
    /// Pawn을 조종하는 주체. 플레이어 입력일 수도 있고 AI일 수도 있다.
    ///
    /// Pawn 입장에서는 둘을 구분하지 않는다. 이게 이 게임의 핵심 제약이다.
    /// 상속으로 PlayerPawn / NpcPawn을 나누면 언젠가 이동 코드가 갈라지고,
    /// 그때부터 사장 플레이어는 움직임만 보고 사람을 찾아낼 수 있게 된다.
    /// </summary>
    public interface IPawnBrain
    {
        PawnIntent Think(float deltaTime);
    }
}
