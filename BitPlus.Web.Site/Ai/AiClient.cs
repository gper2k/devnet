namespace BitPlus.Web.Site.Ai;

/// <summary>
/// 등록된 AI 접속 하나로 만든 커넥션. 접속 정보의 요청 형식에 맞는 프로바이더(<see cref="AiProvider.Of"/>)를 연결하고 API 키를 복호화해 둡니다.
/// 기능 쪽에서는 <see cref="Create"/> 로 만든 뒤 시스템(초기화) 프롬프트와 메시지를 넘겨 <see cref="AskAsync"/>/<see cref="CompleteAsync"/> 로 실행합니다.
/// </summary>
public class AiClient
{
    /// <summary>접속 정보</summary>
    public AiConnection Connection { get; }

    /// <summary>요청 형식 프로바이더</summary>
    private readonly AiProvider provider;

    /// <summary>복호화한 API 키 (없으면 null)</summary>
    private readonly string? apiKey;

    /// <summary>
    /// <see cref="AiClient"/> 생성자. 등록된 접속은 <see cref="Create"/> 로 만들고, 저장 전 값 테스트처럼 접속 정보를 직접 줄 때만 씁니다.
    /// </summary>
    /// <param name="connection">접속 정보</param>
    /// <param name="apiKey">복호화한 API 키</param>
    public AiClient(AiConnection connection, string? apiKey)
    {
        this.Connection = connection;
        this.apiKey = apiKey;
        this.provider = AiProvider.Of(connection.Format);
    }

    /// <summary>
    /// 등록된 접속으로 커넥션을 만듭니다. 식별키를 주면 그 접속, 비우면 기본 접속(없으면 사용 중인 첫 접속)을 씁니다. 사용 안 함 접속은 고르지 않습니다.
    /// </summary>
    /// <param name="config">웹 설정 (접속 목록·마스터 키)</param>
    /// <param name="id">접속 식별키 (비우면 기본 접속)</param>
    /// <returns>커넥션 (쓸 수 있는 접속이 없으면 null)</returns>
    /// <exception cref="InvalidOperationException">저장된 API 키를 복호화할 수 없는 경우 (마스터 키 변경 등, 키를 다시 저장해야 함)</exception>
    public static AiClient? Create(WebConfig config, string? id = null)
    {
        var enabled = AiConnection.ListOf(config).Where(p => p.Enabler).ToList();
        var connection = string.IsNullOrEmpty(id) ? enabled.FirstOrDefault(p => p.Active) ?? enabled.FirstOrDefault() : enabled.FirstOrDefault(p => p.Id == id);
        return connection is null ? null : new AiClient(connection, config.UnprotectSecret(connection.ApiKey, AiConnection.ProtectPurpose, "The saved AI API key cannot be decrypted. Enter and save the API key again."));
    }

    /// <summary>
    /// 시스템 프롬프트와 대화 메시지를 보내고 응답 글을 받습니다 (이전 대화를 이어 갈 때).
    /// </summary>
    /// <param name="system">시스템(초기화) 프롬프트 — 역할·규칙·출력 형식</param>
    /// <param name="messages">대화 메시지 (마지막은 사용자 메시지)</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>성공 시 응답 글, 실패 시 사유</returns>
    public Task<Result<string>> CompleteAsync(string? system, IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default)
        => this.provider.SendAsync(this.Connection, this.apiKey, system, messages, cancellationToken);

    /// <summary>
    /// 시스템 프롬프트와 명령(사용자 메시지) 하나를 보내고 응답 글을 받습니다.
    /// </summary>
    /// <param name="system">시스템(초기화) 프롬프트</param>
    /// <param name="prompt">명령 (예: 요약할 본문)</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>성공 시 응답 글, 실패 시 사유</returns>
    public Task<Result<string>> AskAsync(string? system, string prompt, CancellationToken cancellationToken = default)
        => this.CompleteAsync(system, [AiMessage.User(prompt)], cancellationToken);

    /// <summary>
    /// 등록된 접속으로 명령 하나를 보내고 응답 글을 받습니다 (서비스처럼 실패를 예외로 다루는 쪽용). 쓸 수 있는 접속이 없거나 응답이 실패·빈 값이면 예외입니다.
    /// </summary>
    /// <param name="config">웹 설정 (접속 목록·마스터 키)</param>
    /// <param name="id">접속 식별키 (비우면 기본 접속)</param>
    /// <param name="system">시스템(초기화) 프롬프트</param>
    /// <param name="prompt">명령</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>응답 글 (앞뒤 공백 제거)</returns>
    /// <exception cref="InvalidOperationException">접속이 없거나 호출이 실패한 경우</exception>
    public static async Task<string> AskOrThrowAsync(WebConfig config, string? id, string? system, string prompt, CancellationToken cancellationToken = default)
    {
        var client = Create(config, id) ?? throw new InvalidOperationException("No usable AI connection. Register one in External Services > AI API.");
        var result = await client.AskAsync(system, prompt, cancellationToken);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Data)) throw new InvalidOperationException(result.Message ?? "Empty AI response.");
        return result.Data.Trim();
    }
}
