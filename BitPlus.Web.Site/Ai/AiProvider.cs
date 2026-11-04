using System.Text;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Ai;

/// <summary>
/// AI 요청 형식(<see cref="AiFormat"/>)별 프로바이더 공통 기반. 형식마다 요청 만들기(<see cref="BuildRequest"/>)와 응답 글 읽기(<see cref="ReadText"/>)만 구현하고,
/// HTTP 전송·오류 처리는 여기서 공용으로 합니다. 프로바이더는 상태가 없어 형식당 하나를 함께 씁니다(<see cref="Of"/>).
/// </summary>
public abstract class AiProvider
{
    /// <summary>모든 프로바이더가 함께 쓰는 HTTP 클라이언트 (응답 대기 최대 5분)</summary>
    // ponytail: 정적 HttpClient 하나, 프록시·재시도 정책이 필요해지면 IHttpClientFactory 로
    private static readonly HttpClient http;

    /// <summary>형식별 프로바이더</summary>
    private static readonly Dictionary<AiFormat, AiProvider> providers;

    /// <summary>
    /// 공용 HTTP 클라이언트와 형식별 프로바이더를 준비합니다.
    /// </summary>
    static AiProvider()
    {
        http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        providers = new Dictionary<AiFormat, AiProvider>
        {
            [AiFormat.OpenAI] = new OpenAiProvider(),
            [AiFormat.Claude] = new ClaudeProvider(),
            [AiFormat.Gemini] = new GeminiProvider()
        };
    }

    /// <summary>최대 출력 토큰을 정하지 않았을 때 쓰는 값 (Claude 처럼 필수인 형식용)</summary>
    protected const int DefaultMaxTokens = 16000;

    /// <summary>
    /// 요청 형식에 맞는 프로바이더를 돌려줍니다.
    /// </summary>
    /// <param name="format">요청 형식</param>
    public static AiProvider Of(AiFormat format) => providers[format];

    /// <summary>
    /// 접속 정보로 대화를 보내고 응답 글을 받습니다.
    /// </summary>
    /// <param name="connection">접속 정보 (모델·옵션)</param>
    /// <param name="apiKey">복호화한 API 키 (없으면 null)</param>
    /// <param name="system">시스템(초기화) 프롬프트</param>
    /// <param name="messages">대화 메시지 (마지막은 사용자 메시지)</param>
    /// <param name="cancellationToken">비동기 취소 토큰</param>
    /// <returns>성공 시 응답 글, 실패 시 사유</returns>
    public async Task<Result<string>> SendAsync(AiConnection connection, string? apiKey, string? system, IReadOnlyList<AiMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = this.BuildRequest(connection, apiKey, system, messages);
            using var response = await http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) return Result<string>.Fail($"{(int)response.StatusCode} {ErrorOf(body)}");
            var text = this.ReadText(JsonNode.Parse(body));
            return text is null ? Result<string>.Fail(ErrorOf(body)) : Result<string>.Complete(null, text);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) // 호출한 쪽 취소가 아니면 HTTP 제한 시간 초과 (기본 사유는 소켓 중단 문구라 알아보기 어려움)
        {
            return Result<string>.Fail($"AI request timed out after {http.Timeout.TotalSeconds:0} seconds.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return Result<string>.Fail(ex.GetBaseException().Message);
        }
    }

    /// <summary>
    /// 형식에 맞는 HTTP 요청을 만듭니다.
    /// </summary>
    /// <param name="connection">접속 정보</param>
    /// <param name="apiKey">API 키 (없으면 null)</param>
    /// <param name="system">시스템 프롬프트</param>
    /// <param name="messages">대화 메시지</param>
    /// <returns>보낼 요청</returns>
    protected abstract HttpRequestMessage BuildRequest(AiConnection connection, string? apiKey, string? system, IReadOnlyList<AiMessage> messages);

    /// <summary>
    /// 성공 응답에서 AI 가 쓴 글을 꺼냅니다.
    /// </summary>
    /// <param name="response">응답 JSON</param>
    /// <returns>응답 글 (없거나 거절이면 null — 오류로 처리)</returns>
    protected abstract string? ReadText(JsonNode? response);

    /// <summary>
    /// 주소 끝에 경로를 붙입니다 (주소 끝 <c>/</c> 중복 방지).
    /// </summary>
    /// <param name="connection">접속 정보</param>
    /// <param name="path">붙일 경로</param>
    protected static Uri UrlOf(AiConnection connection, string path) => new($"{connection.Endpoint.TrimEnd('/')}/{path}");

    /// <summary>
    /// JSON 본문을 담은 POST 요청을 만듭니다.
    /// </summary>
    /// <param name="url">요청 주소</param>
    /// <param name="body">요청 본문</param>
    protected static HttpRequestMessage PostJson(Uri url, JsonObject body) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
    };

    /// <summary>오류 응답 본문에서 사람이 읽을 메시지를 꺼냅니다 (형식마다 error.message 또는 본문 앞부분)</summary>
    private static string ErrorOf(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);
            return node?["error"]?["message"]?.ToString() ?? node?["error"]?.ToString() ?? body.Clip(300);
        }
        catch (System.Text.Json.JsonException) { return body.Clip(300); }
    }
}
