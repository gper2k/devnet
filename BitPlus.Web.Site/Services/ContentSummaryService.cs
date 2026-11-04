using BitPlus.Data.Account;
using BitPlus.Data.Content;
using BitPlus.Web.Data.Content;
using BitPlus.Web.Services;
using BitPlus.Web.Site.Ai;
using BitPlus.Web.Site.Properties;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BitPlus.Web.Site.Services;

/// <summary>
/// 컨텐츠 AI 서비스 (검사 → 요약): 글 등록·수정·번역 저장 이벤트와 관리자의 요약 갱신 요청을 받아 한 큐에서 차례로 처리합니다 — AI 서버가 하나뿐일 때 호출이 겹치지 않게 AI 기능을 이 서비스 하나에 모읍니다.
/// 글마다 1) 검사(설정으로 켬): 보기 전용 페이지를 뺀 모든 글을 스팸·광고·욕설·혐오·음란·불법인지 판정해, 걸리면 사유를 글 속성 <see cref="ReasonKey"/> 에 남기고 글을 감춘 뒤(설정으로 알리기만) 최종 관리자와 글쓴이에게 알리고 멈춥니다.
/// 2) 요약: 문제 없는 일반·포럼 게시판 글을 요약해 글 속성 <see cref="DefaultContentHandler.SummaryKey"/> 에 저장합니다 (순수 글이 기준 길이 이하면 AI 없이 그 글 그대로).
/// 번역(수동, 글쓰기 번역 탭·관리 API)이 저장되면(<see cref="ServiceEvents.ContentTranslated"/>) 같은 규칙으로 그 번역본을 요약해 <see cref="ContentTranslation.Summary"/> 에 저장합니다.
/// 검사 AI 접속은 따로 고를 수 있고(비우면 요약과 같은 접속), 요약 갱신 요청은 요약만 다시 합니다. 글은 실행 시점에 작업 범위의 DB 에서 읽으므로 기다리는 동안 고친 글은 마지막 내용으로 처리합니다.
/// </summary>
public class ContentSummaryService : ServiceBase
{
    /// <summary>설정 값: 요약 지시문</summary>
    public const string PromptKey = "prompt";

    /// <summary>설정 값: 이 길이(글자 수) 이하의 글은 AI 없이 순수 글을 요약으로</summary>
    public const string PlainLengthKey = "plainLength";

    /// <summary>설정 값: AI 글 검사 사용 ("true")</summary>
    public const string ReviewKey = "review";

    /// <summary>설정 값: 걸린 글을 감추지 않고 알리기만 할지 ("true")</summary>
    public const string NotifyOnlyKey = "notifyOnly";

    /// <summary>설정 값: 검사에 쓸 AI 접속 식별키 (비우면 요약과 같은 접속)</summary>
    public const string ReviewAiKey = "reviewAiConnection";

    /// <summary>설정 값: 추가 검사 기준 (기본 기준 뒤에 덧붙임)</summary>
    public const string CriteriaKey = "criteria";

    /// <summary>검사 판정 사유를 저장하는 글 속성 키</summary>
    public const string ReasonKey = "ReviewReason";

    /// <summary>순수 글 기준 길이를 비웠을 때의 기본값</summary>
    public const int DefaultPlainLength = 200;

    /// <summary>지시문을 비웠을 때 쓰는 기본 요약 지시문</summary>
    public const string DefaultPrompt = "Summarize the post in three sentences or fewer, in the same language as the post. Reply with the summary only.";

    /// <summary>검사 기준 (기본)</summary>
    private const string ReviewRule = "Decide whether the post is spam, advertising, abusive, hateful, sexual, or illegal.";

    /// <summary>AI 에 보내는 본문 최대 길이 (넘으면 자름)</summary>
    private const int MaxInputLength = 20000;

    private readonly WebConfig config;

    /// <summary>
    /// <see cref="ContentSummaryService"/> 생성자.
    /// </summary>
    /// <param name="config">웹 설정 (AI 접속 목록·마스터 키)</param>
    public ContentSummaryService(WebConfig config) : base() => this.config = config;

    /// <inheritdoc/>
    public override string Title => Resources.ResourceManager.GetString("ContentSummaryService") ?? this.Name;

    /// <inheritdoc/>
    public override string Description => "ContentSummaryServiceInfo";

    /// <inheritdoc/>
    public override IReadOnlyCollection<string> Events => [ServiceEvents.ContentCreated, ServiceEvents.ContentUpdated, ServiceEvents.ContentSummary, ServiceEvents.ContentTranslated];

    /// <summary>이 길이 이하의 글은 AI 를 부르지 않고 순수 글(HTML 제거)을 그대로 요약으로 저장 (설정 값, 비우면 <see cref="DefaultPlainLength"/>, 0 이면 항상 AI)</summary>
    private int PlainLength => int.TryParse(this.Values.GetValueOrDefault(PlainLengthKey), out var length) && length >= 0 ? length : DefaultPlainLength;

    /// <summary>요약 지시문 (설정 값, 비우면 기본)</summary>
    private string SummaryPrompt => this.Values.GetValueOrDefault(PromptKey) is { Length: > 0 } custom ? custom : DefaultPrompt;

    /// <summary>
    /// 이벤트 대상 글을 처리합니다: 번역 저장 이벤트면 그 번역본만 요약하고, 아니면 1) 검사(설정으로 켠 경우, 요약 갱신 요청 제외) — 걸리면 사유를 남기고 감춘 뒤(설정으로 알리기만) 알리고 여기서 멈춤,
    /// 2) 요약(일반·포럼). 단계마다 저장하고, AI 호출이나 응답 해석이 실패하면 예외(서비스의 마지막 오류)로 남깁니다.
    /// </summary>
    /// <param name="e">글 등록·수정·번역 저장·요약 갱신 요청 이벤트 (대상 = 글 식별키, 번역 저장은 값 culture)</param>
    /// <param name="services">작업 범위 서비스</param>
    /// <param name="token">앱 종료 토큰</param>
    protected override async Task HandleAsync(ServiceEvent e, IServiceProvider services, CancellationToken token)
    {
        if (!Guid.TryParse(e.TargetId, out var id)) return;
        var db = services.GetRequiredService<IContentDbContext>();
        var content = await db.Contents.Include(p => p.Properties).Include(p => p.ContentRule).FirstOrDefaultAsync(p => p.Id == id, token);
        if (content?.ContentRule is not { OnlyView: false } rule) return;
        var summarize = DefaultContentHandler.UsesSummary(rule.Type);
        if (e.Name == ServiceEvents.ContentTranslated)
        {
            if (summarize) await this.SummarizeTranslationAsync(db, content.Id, e.Values.GetValueOrDefault("culture"), token);
            return;
        }
        var text = PlainText(content.ContentBody);
        if (text.Length == 0 && content.Title.Length == 0) return;

        if (e.Name != ServiceEvents.ContentSummary && this.Values.GetValueOrDefault(ReviewKey) == "true" && !await this.ReviewAsync(services, db, content, text, token)) return; // 문제 있는 글은 요약하지 않음
        if (!summarize || text.Length == 0) return;
        ContentManager.SetContentProperty(content, DefaultContentHandler.SummaryKey, await this.SummarizeAsync(content.Title, text, token));
        await ((DbContext)db).SaveChangesAsync(token);
    }

    /// <summary>
    /// 번역본 하나를 원문 요약과 같은 규칙으로 요약해 <see cref="ContentTranslation.Summary"/> 에 저장합니다 (번역 기준 시간·수정 시간은 그대로).
    /// </summary>
    /// <param name="db">컨텐츠 DB 컨텍스트</param>
    /// <param name="contentId">글 식별키</param>
    /// <param name="culture">번역 문화권</param>
    /// <param name="token">앱 종료 토큰</param>
    private async Task SummarizeTranslationAsync(IContentDbContext db, Guid contentId, string? culture, CancellationToken token)
    {
        if (string.IsNullOrEmpty(culture)) return;
        var translation = await db.ContentTranslations.FirstOrDefaultAsync(p => p.ContentId == contentId && p.Culture == culture, token);
        if (translation is null) return; // 그 사이 지워진 번역
        var text = PlainText(translation.Body);
        translation.Summary = text.Length == 0 ? null : await this.SummarizeAsync(translation.Title, text, token);
        await ((DbContext)db).SaveChangesAsync(token);
    }

    /// <summary>요약: 순수 글이 기준 길이 이하면 AI 없이 그대로, 넘으면 AI 요약 (지시문이 글과 같은 언어로 요약하게 함)</summary>
    /// <param name="title">제목</param>
    /// <param name="text">순수 글 본문</param>
    /// <param name="token">앱 종료 토큰</param>
    private async Task<string> SummarizeAsync(string title, string text, CancellationToken token)
        => text.Length > this.PlainLength ? await AiClient.AskOrThrowAsync(this.config, this.SummaryAi, this.SummaryPrompt, $"{title}\n\n{text}", token) : text;

    /// <summary>본문 HTML 의 순수 글 (태그 제거, 공백 정리, 최대 <see cref="MaxInputLength"/> 자)</summary>
    /// <param name="html">본문 HTML</param>
    private static string PlainText(string? html) => Regex.Replace(WebUtility.HtmlDecode(html.ClearHtml() ?? string.Empty), @"\s+", " ").Trim().Clip(MaxInputLength);

    /// <summary>요약 AI 접속 식별키 (설정, 비우면 기본 접속)</summary>
    private string? SummaryAi => this.Values.GetValueOrDefault(AiConnection.ServiceSettingKey);

    /// <summary>설정 값으로 고른 AI 접속, 비우면 요약과 같은 접속</summary>
    /// <param name="key">설정 키</param>
    private string? AiOf(string key) => this.Values.GetValueOrDefault(key) is { Length: > 0 } chosen ? chosen : this.SummaryAi;

    /// <summary>
    /// 글을 AI 로 검사해(JSON {flag, reason}) 판정 사유를 글 속성 <see cref="ReasonKey"/> 에 남깁니다. 걸리면 글을 감추고(설정으로 알리기만) 최종 관리자와 글쓴이에게 알립니다.
    /// </summary>
    /// <param name="services">작업 범위 서비스</param>
    /// <param name="db">컨텐츠 DB 컨텍스트</param>
    /// <param name="content">대상 글 (속성 포함)</param>
    /// <param name="text">순수 글 본문</param>
    /// <param name="token">앱 종료 토큰</param>
    /// <returns>문제 없는 글이면 true</returns>
    private async Task<bool> ReviewAsync(IServiceProvider services, IContentDbContext db, ContentModel content, string text, CancellationToken token)
    {
        var criteria = this.Values.GetValueOrDefault(CriteriaKey) is { Length: > 0 } extra ? $" Additional rules: {extra}" : string.Empty;
        var prompt = $"You review posts on a community website. {ReviewRule}{criteria} "
            + "Reply with one JSON object only: {\"flag\": true or false, \"reason\": \"a short reason in the post's language, empty when flag is false\"}.";
        var json = JsonOf(await AiClient.AskOrThrowAsync(this.config, this.AiOf(ReviewAiKey), prompt, $"{content.Title}\n\n{text}", token));
        var (flag, reason) = (json["flag"]!.GetValue<bool>(), json["reason"]?.ToString() ?? string.Empty);
        var hide = flag && this.Values.GetValueOrDefault(NotifyOnlyKey) != "true";
        ContentManager.SetContentProperty(content, ReasonKey, flag ? reason.Clip(500) : null);
        if (hide) content.Visible = false;
        await ((DbContext)db).SaveChangesAsync(token);
        if (flag) await NotifyAsync(services.GetRequiredService<IIdentityDbContext>(), content, reason, hide, token);
        return !flag;
    }

    /// <summary>
    /// AI 응답에서 첫 JSON 객체를 읽습니다 (불리언 flag 가 있어야 함). 읽을 수 없으면 예외입니다.
    /// </summary>
    /// <param name="answer">AI 응답 글</param>
    private static JsonObject JsonOf(string answer)
    {
        var (start, end) = (answer.IndexOf('{'), answer.LastIndexOf('}'));
        try
        {
            if (start >= 0 && end > start && JsonNode.Parse(answer[start..(end + 1)]) is JsonObject json && json["flag"] is JsonValue flag && flag.TryGetValue<bool>(out _)) return json;
        }
        catch (JsonException) { }
        throw new InvalidOperationException($"Unreadable AI review answer: {answer.Clip(200)}");
    }

    /// <summary>검사에 걸린 글을 최종 관리자들과 (감췄으면) 글쓴이에게 알립니다 (알림 → 글 보기로).</summary>
    private static async Task NotifyAsync(IIdentityDbContext db, ContentModel content, string reason, bool hidden, CancellationToken token)
    {
        var url = $"/content/view/{content.Id}";
        var managers = await SystemManagers.Of(db).Select(p => p.Id).ToListAsync(token);
        foreach (var managerId in managers) db.AddNotice(managerId, "moderation", hidden ? "NoticeReviewHidden" : "NoticeReviewFlagged", [content.Title, reason], url);
        if (hidden && content.WriterId is { } writerId && !managers.Contains(writerId)) db.AddNotice(writerId, "moderation", "NoticeReviewHiddenWriter", [content.Title, reason], url);
        await ((DbContext)db).SaveChangesAsync(token);
    }
}
