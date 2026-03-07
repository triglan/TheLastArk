/// <summary>
/// 맵 노드의 종류를 정의합니다.
/// </summary>
public enum NodeType
{
    /// <summary>1층 시작 지점</summary>
    Start,

    /// <summary>일반 전투 (기본 확률 50%)</summary>
    Combat,

    /// <summary>이벤트 (기본 확률 25%)</summary>
    Event,

    /// <summary>엘리트 전투 (기본 확률 15%)</summary>
    Elite,

    /// <summary>마을/휴식 (기본 확률 10%)</summary>
    Rest,

    /// <summary>15층 보스</summary>
    Boss
}