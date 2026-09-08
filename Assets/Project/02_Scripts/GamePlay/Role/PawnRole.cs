namespace Project.GamePlay
{
    /// <summary>
    /// Pawn의 정체. 이 값은 절대 [Networked] 프로퍼티로 만들면 안 된다.
    ///
    /// [Networked]는 모든 클라이언트에 복제되기 때문에, 역할을 거기 두는 순간
    /// 누가 사장이고 누가 사람인지가 상대 메모리에서 그대로 읽힌다.
    /// 숨바꼭질 게임에서 정답지를 공개하는 것과 같다.
    ///
    /// 진짜 역할표는 마스터 클라이언트가 일반 메모리에 들고 있고(GameDirector),
    /// 각 플레이어에게는 targeted RPC로 자기 것만 내려준다.
    /// </summary>
    public enum PawnRole : byte
    {
        /// <summary>
        /// 남의 Pawn. 이 클라이언트는 정체를 모른다.
        /// 기본값이 Unknown인 게 중요하다 — 실수로 역할이 새는 경로를 막는다.
        /// </summary>
        Unknown = 0,

        /// <summary>사장. 겉으로 드러나며 회사원을 찾아 해고한다.</summary>
        Boss = 1,

        /// <summary>진짜 회사원(사람). AI인 척하며 미션을 수행한다.</summary>
        Worker = 2,

        /// <summary>AI 회사원. 겉모습과 이동이 Worker와 완전히 동일해야 한다.</summary>
        Ai = 3,
    }
}
