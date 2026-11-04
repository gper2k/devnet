using BitPlus.Data.Account;
using BitPlus.Web.Services;
using BitPlus.Web.Site.Data;
using BitPlus.Web.Site.Properties;
using UserApi = BitPlus.Web.Site.Controllers.API.UserController;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text;

namespace BitPlus.Web.Site.Services;

/// <summary>
/// 오류 알림 서비스: 시스템 오류 이벤트(요청 처리 중 미처리 예외)를 받아 최종 관리자 역할(<see cref="SystemNames.SystemManagerRole"/>, 직접 역할 + 소속 그룹의 역할)을 가진
/// 활성 사용자에게 심각도·발생 시각·요청·사용자·오류 형식·내용·상세(스택)를 정리한 메일을 보냅니다 (SMTP 는 외부 서비스 > SMTP 구성).
/// 같은 오류(형식 + 경로)는 설정한 간격(분) 안에 한 번만 보내고, 그동안 건너뛴 횟수를 다음 메일에 적습니다. 메일 문구는 받는 사람의 화면 언어(사용자 속성, 없으면 사이트 기본 언어)입니다.
/// ponytail: 건너뛴 기록은 메모리 — 재시작하면 초기화, 서버별로 따로 셈.
/// </summary>
public class ErrorAlertService : ServiceBase
{
    /// <summary>설정 값: 같은 오류 알림 간격 (분)</summary>
    public const string IntervalKey = "interval";

    /// <summary>간격을 비웠을 때의 기본값 (분)</summary>
    public const int DefaultInterval = 10;

    private readonly WebConfig config;
    private readonly Dictionary<string, (DateTime Sent, int Skipped)> history;

    /// <summary>
    /// <see cref="ErrorAlertService"/> 생성자.
    /// </summary>
    /// <param name="config">웹 설정 (SMTP 구성·사이트 기본 언어)</param>
    public ErrorAlertService(WebConfig config) : base()
    {
        this.config = config;
        this.history = [];
    }

    /// <inheritdoc/>
    public override string Title => Resources.ResourceManager.GetString("ErrorAlertService") ?? this.Name;

    /// <inheritdoc/>
    public override string Description => "ErrorAlertServiceInfo";

    /// <inheritdoc/>
    public override IReadOnlyCollection<string> Events => [ServiceEvents.SystemError];

    /// <summary>같은 오류 알림 간격 (설정 값, 비우거나 잘못되면 <see cref="DefaultInterval"/>, 0 이면 매번)</summary>
    private TimeSpan Interval => TimeSpan.FromMinutes(int.TryParse(this.Values.GetValueOrDefault(IntervalKey), out var minutes) && minutes >= 0 ? minutes : DefaultInterval);

    /// <summary>
    /// 오류 이벤트 하나를 최종 관리자들에게 메일로 알립니다 (같은 오류가 간격 안에 다시 오면 건너뛰고 횟수만 셈).
    /// </summary>
    /// <param name="e">시스템 오류 이벤트 (값: method, path, query, ip, user, type, message, detail)</param>
    /// <param name="services">작업 범위 서비스</param>
    /// <param name="token">앱 종료 토큰</param>
    protected override async Task HandleAsync(ServiceEvent e, IServiceProvider services, CancellationToken token)
    {
        var key = $"{e.Values.GetValueOrDefault("type")}|{KeyPath(e.Values.GetValueOrDefault("path"))}";
        var skipped = this.history.TryGetValue(key, out var last) ? last.Skipped : 0;
        if (last.Sent != default && e.Time - last.Sent < this.Interval)
        {
            this.history[key] = (last.Sent, skipped + 1);
            return;
        }
        this.history[key] = (e.Time, 0);

        var mails = await this.ManagerMailsAsync(services.GetRequiredService<IIdentityDbContext>(), token);
        if (mails.Count == 0) return;
        var sender = this.config.CreateMailSender();
        foreach (var group in mails.GroupBy(p => p.Culture)) // 받는 사람의 화면 언어(사용자 속성)별로 한 번씩 만든다
        {
            var culture = CultureInfo.GetCultureInfo(group.Key);
            var severity = this.Lang(SeverityOf(e.Values.GetValueOrDefault("type")), culture);
            var subject = $"[{severity}] {this.Lang("ErrorAlertSubject", culture)}: {e.Values.GetValueOrDefault("message").Clip(80)}";
            var body = this.BodyOf(e, severity, skipped, culture);
            foreach (var (mail, _) in group) await sender.SendAsync(mail, subject, body, true, token);
        }
    }

    /// <summary>
    /// 최종 관리자 역할을 직접 가졌거나 그 역할이 있는 그룹에 속한 활성 사용자의 메일 주소와 화면 언어(사용자 속성 Culture, 없거나 지원하지 않으면 사이트 기본 언어). 메일 인증 여부와 상관없이, 탈퇴 회원 제외.
    /// </summary>
    private async Task<List<(string Mail, string Culture)>> ManagerMailsAsync(IIdentityDbContext db, CancellationToken token)
    {
        var rows = await SystemManagers.Of(db).Where(p => p.Email != null)
            .Select(p => new { Mail = p.Email!, Culture = p.Properties.Where(x => x.Key == UserApi.CultureKey).Select(x => x.Value).FirstOrDefault() }).ToListAsync(token);
        var cultures = this.config.Cultures;
        return [.. rows.Where(p => p.Mail.Contains('@')).DistinctBy(p => p.Mail, StringComparer.OrdinalIgnoreCase) // 탈퇴 회원의 메일은 #번호로 바뀌어 있어 빠짐
            .Select(p => (p.Mail, p.Culture is { } culture && cultures.Contains(culture) ? culture : this.config.Culture))];
    }

    /// <summary>중복 알림 키용 경로 정규화: 경로 조각 중 GUID·숫자 식별자를 <c>*</c> 로 묶어 파일·글 번호마다 알림이 따로 나가지 않게 합니다.</summary>
    private static string KeyPath(string? path) => string.IsNullOrEmpty(path) ? string.Empty
        : string.Join('/', path.Split('/').Select(p => Guid.TryParse(p, out _) || long.TryParse(p, out _) ? "*" : p));

    /// <summary>
    /// 오류 형식으로 심각도 리소스 키를 정합니다: DB·캐시·메모리·시간 초과 등 기반 오류는 치명(Critical), 입력·형식 오류는 보통(Medium), 그 밖은 높음(High).
    /// </summary>
    /// <param name="type">오류 형식 전체 이름</param>
    private static string SeverityOf(string? type) => type switch
    {
        null => "High",
        _ when new[] { "Sql", "DbUpdate", "Redis", "OutOfMemory", "Timeout", "IOException", "Socket", "Cryptographic" }.Any(type.Contains) => "Critical",
        _ when new[] { "Argument", "Format", "Json", "BadHttpRequest", "OperationCanceled", "TaskCanceled" }.Any(type.Contains) => "Medium",
        _ => "High"
    };

    /// <summary>메일 본문 (항목 표 + 상세 스택)</summary>
    private string BodyOf(ServiceEvent e, string severity, int skipped, CultureInfo culture)
    {
        var html = new StringBuilder();
        html.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse\">");
        void Row(string labelKey, string? value) => html.Append($"<tr><th align=\"left\">{WebUtility.HtmlEncode(this.Lang(labelKey, culture))}</th><td>{WebUtility.HtmlEncode(value ?? string.Empty)}</td></tr>");
        Row("ErrorAlertSeverity", severity);
        Row("ErrorAlertTime", e.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
        Row("ErrorAlertRequest", $"{e.Values.GetValueOrDefault("method")} {e.Values.GetValueOrDefault("path")}{e.Values.GetValueOrDefault("query")}");
        Row("User", $"{e.Values.GetValueOrDefault("user")} ({e.Values.GetValueOrDefault("ip")})");
        Row("ErrorAlertType", e.Values.GetValueOrDefault("type"));
        Row("Message", e.Values.GetValueOrDefault("message"));
        html.Append("</table>");
        if (skipped > 0) html.Append($"<p>{WebUtility.HtmlEncode(this.Lang("ErrorAlertRepeated", culture).Formats(skipped.ToString(CultureInfo.InvariantCulture)))}</p>");
        html.Append($"<pre style=\"white-space:pre-wrap\">{WebUtility.HtmlEncode(e.Values.GetValueOrDefault("detail") ?? string.Empty)}</pre>");
        return html.ToString();
    }

    /// <summary>사이트 리소스 문구 (지정 언어, 없으면 키)</summary>
    private string Lang(string key, CultureInfo culture) => Resources.ResourceManager.GetString(key, culture) ?? key;
}
