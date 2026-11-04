using System.Globalization;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 문화권별 JSON 객체(<c>{"ko":"공지","en":"Notices"}</c>)에서 표시할 글자를 다국어 출력 규칙(<see cref="HttpRequestExtend.PickCulture{T, TValue}"/>)으로 고릅니다.
/// </summary>
public static class CultureText
{
    /// <summary>
    /// 표시할 글자 (단일 문자열이면 그대로, 다국어 객체이면 현재 UI 문화권에 맞게 선택, 없으면 fallback).
    /// </summary>
    /// <param name="request">현재 요청 (생략 시 현재 스레드의 UI 문화권 기준)</param>
    /// <param name="node">문화권별 JSON 객체 또는 그 JSON 문자열 / 단일 문자열</param>
    /// <param name="fallback">고를 것이 없을 때 기본값</param>
    public static string? Of(HttpRequest? request, JsonNode? node, string? fallback = null)
    {
        if (node is JsonValue v && v.TryGetValue<string>(out var text) && !text.TrimStart().StartsWith('{'))
            return text;

        var map = node as JsonObject ?? (node is JsonValue jv && jv.TryGetValue<string>(out var json) && json.TrimStart().StartsWith('{') ? JsonNode.Parse(json) as JsonObject : null);
        if (map is null) return fallback;

        var items = map.Select(p => (Culture: p.Key, Text: p.Value is JsonValue value && value.TryGetValue<string>(out var s) ? s : null))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        if (items.Count == 0) return fallback;

        if (request is not null)
            return request.PickCulture(items, p => p.Culture, p => p.Text) ?? fallback;

        var current = CultureInfo.CurrentUICulture;
        var currentName = current.GetLastName();
        var currentIso = current.TwoLetterISOLanguageName;

        var match = items.FirstOrDefault(p => p.Culture.NEqual(currentName) || p.Culture.NEqual(currentIso));
        if (!string.IsNullOrWhiteSpace(match.Text)) return match.Text;

        match = items.FirstOrDefault(p => p.Culture.NEqual("ko"));
        if (!string.IsNullOrWhiteSpace(match.Text)) return match.Text;

        return items[0].Text ?? fallback;
    }

    /// <summary>
    /// 현재 UI 문화권 기준으로 표시할 글자를 고릅니다.
    /// </summary>
    /// <param name="node">문화권별 JSON 객체 또는 단일 문자열</param>
    /// <param name="fallback">고를 것이 없을 때 기본값</param>
    public static string? Of(JsonNode? node, string? fallback = null) => Of(null, node, fallback);

    /// <summary>
    /// 문화권별 JSON 문자열에서 표시할 글자를 고릅니다.
    /// </summary>
    /// <param name="request">현재 요청</param>
    /// <param name="json">문화권별 JSON 문자열</param>
    /// <param name="fallback">기본값</param>
    public static string? Of(HttpRequest? request, string? json, string? fallback = null)
        => string.IsNullOrWhiteSpace(json) ? fallback : Of(request, JsonNode.Parse(json), fallback);

    /// <summary>
    /// 현재 UI 문화권 기준으로 문화권별 JSON 문자열에서 표시할 글자를 고릅니다.
    /// </summary>
    /// <param name="json">문화권별 JSON 문자열</param>
    /// <param name="fallback">기본값</param>
    public static string? Of(string? json, string? fallback = null) => Of(null, json, fallback);
}
