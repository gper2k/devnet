namespace BitPlus.Web.Site.Ai;

/// <summary>
/// AI 대화 메시지 하나 (형식에 상관없는 공용 표현 — 각 프로바이더가 자기 요청 형식으로 바꿉니다).
/// 시스템(초기화) 프롬프트는 메시지가 아니라 <see cref="AiClient.CompleteAsync"/> 의 system 인자로 따로 넘깁니다.
/// </summary>
/// <param name="Role">역할 (<see cref="UserRole"/> 또는 <see cref="AssistantRole"/>)</param>
/// <param name="Content">내용</param>
public record AiMessage(string Role, string Content)
{
    /// <summary>사용자 역할</summary>
    public const string UserRole = "user";

    /// <summary>AI(응답) 역할</summary>
    public const string AssistantRole = "assistant";

    /// <summary>
    /// 사용자 메시지를 만듭니다.
    /// </summary>
    /// <param name="content">내용</param>
    public static AiMessage User(string content) => new(UserRole, content);

    /// <summary>
    /// AI(응답) 메시지를 만듭니다 (이전 대화를 이어 보낼 때).
    /// </summary>
    /// <param name="content">내용</param>
    public static AiMessage Assistant(string content) => new(AssistantRole, content);
}
