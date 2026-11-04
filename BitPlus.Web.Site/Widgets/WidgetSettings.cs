using BitPlus.Web.HTTP.Support;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// widget.json 의 settings(항목 목록: key, type, default, min, max)에 맞춰 위젯 설정값을 다룹니다.
/// 형식: number(정수, min~max), bool, text(200자), lines(여러 줄 4000자), html(정제, 20000자), boards(게시판 번호 목록), board(게시판 번호).
/// 다국어 지원: text, lines, html 형식은 단순 문자열 또는 다국어 객체({"ko":"...","en":"..."})를 모두 지원합니다.
/// 목록에 없는 키는 버리고, 빠지거나 형식이 아닌 값은 기본값을 씁니다.
/// </summary>
public static class WidgetSettings
{
    /// <summary>
    /// 저장된 설정값을 항목 목록에 맞춰 정리합니다 (빠진 값 = 기본값, 형식 맞춤, HTML 정제).
    /// </summary>
    /// <param name="schema">widget.json 의 settings JSON 배열 문자열</param>
    /// <param name="saved">저장된(또는 보내온) 설정값</param>
    public static JsonObject Normalize(string schema, JsonObject? saved)
    {
        var result = new JsonObject();
        foreach (var item in (JsonNode.Parse(schema) as JsonArray ?? []).OfType<JsonObject>())
        {
            var key = item["key"]?.GetValue<string>();
            if (string.IsNullOrEmpty(key)) continue;
            result[key] = ValueOf(item, saved?[key]);
        }
        return result;
    }

    /// <summary>항목 하나의 값: 보내온 값을 형식에 맞추고, 맞지 않으면 기본값</summary>
    private static JsonNode? ValueOf(JsonObject item, JsonNode? value)
    {
        var fallback = item["default"]?.DeepClone();
        return item["type"]?.GetValue<string>() switch
        {
            "number" => Number(value, item) is { } n ? n : fallback,
            "bool" => value is JsonValue b && b.TryGetValue<bool>(out var flag) ? flag : fallback,
            "text" => value is JsonObject objText ? NormalizeMap(objText, s => s.Clip(200)) : (value is JsonValue t && t.TryGetValue<string>(out var text) ? text.Clip(200) : fallback),
            "lines" => value is JsonObject objLines ? NormalizeMap(objLines, s => s.Trim().Clip(4000)) : (value is JsonValue l && l.TryGetValue<string>(out var lines) ? lines.Trim().Clip(4000) : fallback),
            "html" => value is JsonObject objHtml ? NormalizeMap(objHtml, s => EditorFile.Sanitize(s.Clip(20000))) : (value is JsonValue h && h.TryGetValue<string>(out var html) ? EditorFile.Sanitize(html.Clip(20000)) : fallback),
            "board" => value is JsonValue r && r.TryGetValue<int>(out var no) && no > 0 ? no : fallback,
            "boards" => value is JsonArray list ? new JsonArray([.. list.Select(p => p is JsonValue v && v.TryGetValue<int>(out var x) ? x : 0).Where(p => p > 0).Distinct().Take(20).Select(p => (JsonNode)p)]) : fallback,
            _ => fallback
        };
    }

    /// <summary>다국어 객체({"ko":"...","en":"..."})의 각 언어별 문자열을 정리합니다.</summary>
    private static JsonObject NormalizeMap(JsonObject obj, Func<string, string> cleaner)
    {
        var result = new JsonObject();
        foreach (var (culture, val) in obj)
            if (val is JsonValue v && v.TryGetValue<string>(out var s))
                result[culture] = cleaner(s);
        return result;
    }

    /// <summary>정수 값을 min~max 로 맞춤 (숫자가 아니면 null)</summary>
    private static int? Number(JsonNode? value, JsonObject item)
    {
        if (value is not JsonValue v || !v.TryGetValue<double>(out var number)) return null;
        var min = item["min"]?.GetValue<int>() ?? int.MinValue;
        var max = item["max"]?.GetValue<int>() ?? int.MaxValue;
        return Math.Clamp((int)Math.Round(number), min, max);
    }
}
