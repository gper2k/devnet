using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 위젯 데이터 로더(<see cref="IWidgetLoader"/>)에 넘기는 정보: 페이지 키, 설정값(widget.json 기본값을 채운 것), 현재 요청.
/// </summary>
public class WidgetContext
{
    /// <summary>페이지 키 (home, portal …)</summary>
    public string Page { get; }

    /// <summary>설정값</summary>
    public JsonObject Settings { get; }

    /// <summary>현재 요청 (사용자 자산·정책·문화권)</summary>
    public HttpContext Http { get; }

    /// <summary>로더가 정하는 "더 보기" 주소 — 제목 옆 +more 링크 (예: 대상 게시판이 하나면 그 목록, 없으면 null)</summary>
    public string? MoreUrl { get; set; }

    /// <summary>
    /// <see cref="WidgetContext"/> 생성자.
    /// </summary>
    /// <param name="page">페이지 키</param>
    /// <param name="settings">설정값</param>
    /// <param name="http">현재 요청</param>
    public WidgetContext(string page, JsonObject settings, HttpContext http)
    {
        this.Page = page;
        this.Settings = settings;
        this.Http = http;
        this.MoreUrl = null;
    }

    /// <summary>정수 설정값 (없거나 형식이 아니면 기본값)</summary>
    public int Int(string key, int fallback) => this.Settings[key] is JsonValue v && v.TryGetValue<int>(out var n) ? n : fallback;

    /// <summary>참/거짓 설정값 (없으면 기본값)</summary>
    public bool Bool(string key, bool fallback) => this.Settings[key] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : fallback;

    /// <summary>정수 배열 설정값 (게시판 번호 목록 등, 없으면 빈 목록)</summary>
    public List<int> Ints(string key) => [.. (this.Settings[key] as JsonArray)?.Select(p => p is JsonValue v && v.TryGetValue<int>(out var n) ? n : 0).Where(p => p > 0) ?? []];
}
