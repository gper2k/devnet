namespace BitPlus.Web.Site.Ai;

/// <summary>
/// AI API 접속 정보 하나 (설정 "AiConnections" = 이 모델의 목록, AppConfig 범주 — 캐시 → DB → appsettings). 관리자 "외부 서비스 > AI API" 에서 저장합니다.
/// API 키는 마스터 키의 용도 키(<see cref="ProtectPurpose"/>)로 암호화해 두고 화면에는 내려주지 않습니다.
/// </summary>
public class AiConnection
{
    /// <summary>설정 키 (접속 정보 목록)</summary>
    public const string Key = "AiConnections";

    /// <summary>API 키 암호화 용도 문자열 (<see cref="WebConfig.ProtectSecret"/>)</summary>
    public const string ProtectPurpose = "BitPlus.Ai.ApiKey";

    /// <summary>AI 를 쓰는 서비스의 설정 값 키: 고른 접속 식별키 (비우면 기본 접속, 설정 양식 조각 <c>Views/Manager/Service/Forms/_AiConnection.cshtml</c>)</summary>
    public const string ServiceSettingKey = "aiConnection";

    /// <summary>추론 강도로 받을 수 있는 값 (빈 값 = 쓰지 않음)</summary>
    public static string[] ReasoningEfforts => ["", "low", "medium", "high"];

    /// <summary>
    /// <see cref="AiConnection"/> 기본 생성자.
    /// </summary>
    public AiConnection()
    {
        this.Id = string.Empty;
        this.Name = string.Empty;
        this.Format = AiFormat.OpenAI;
        this.Endpoint = string.Empty;
        this.ApiKey = null;
        this.Model = string.Empty;
        this.ReasoningEffort = string.Empty;
        this.Enabler = true;
        this.Active = false;
    }

    /// <summary>식별키 (목록 안에서 유일, 저장 시 비었으면 새로 만듦)</summary>
    public string Id { get; set; }

    /// <summary>표시 이름</summary>
    public string Name { get; set; }

    /// <summary>요청 형식</summary>
    public AiFormat Format { get; set; }

    /// <summary>API 기본 주소 (예: https://api.openai.com/v1, http://localhost:11434/v1)</summary>
    public string Endpoint { get; set; }

    /// <summary>암호화된 API 키 (없으면 null — Ollama 등 키 없는 서버)</summary>
    public string? ApiKey { get; set; }

    /// <summary>모델 이름</summary>
    public string Model { get; set; }

    /// <summary>온도 (0~2, null = 서버 기본값)</summary>
    public double? Temperature { get; set; }

    /// <summary>Top P (0~1, null = 서버 기본값)</summary>
    public double? TopP { get; set; }

    /// <summary>Top K (null = 서버 기본값, OpenAI 공식 API 는 지원하지 않음)</summary>
    public int? TopK { get; set; }

    /// <summary>최대 출력 토큰 (null = 서버 기본값, Claude 는 필수라 비우면 호출하는 쪽 기본값)</summary>
    public int? MaxTokens { get; set; }

    /// <summary>추론 강도 (<see cref="ReasoningEfforts"/>, OpenAI reasoning_effort / Claude effort / Gemini thinkingLevel)</summary>
    public string ReasoningEffort { get; set; }

    /// <summary>사용 여부</summary>
    public bool Enabler { get; set; }

    /// <summary>기본 접속 (목록에서 하나만)</summary>
    public bool Active { get; set; }

    /// <summary>
    /// 저장된 접속 정보 목록 (없거나 읽기에 실패하면 빈 목록).
    /// </summary>
    /// <param name="config">웹 설정</param>
    public static List<AiConnection> ListOf(WebConfig? config)
    {
        try { return config?.GetModel<List<AiConnection>>(Key) ?? []; }
        catch (Exception) { return []; }
    }
}
