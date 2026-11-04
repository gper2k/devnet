using BitPlus.Logging;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 관리자 "시스템 설정 > 로그" 화면용 파일 로그 조회 Web API 컨트롤러 (api/log). 관리자 정책만.
/// 로그 폴더(<c>/Logs</c>) 아래의 <c>[yyyy-MM-dd] 이름(.확장자)</c> 파일(한 줄에 JSON 하나)을 종류(이름)·날짜별로 보여 주고,
/// 큰 파일은 바이트 위치(offset)부터 조각으로 나눠 읽으며 시간 범위·수준·검색어로 거릅니다.
/// 로그마다 구조가 달라(요청 스코프·DB 쿼리·일반 메시지) 목록은 공통 항목(시간·수준·요약·소요 시간)으로 맞추고, 상세는 원본 JSON 을 그대로 돌려줍니다.
/// 기록하지 않을 로그의 제외 규칙(<see cref="LogFilter"/>, 설정 "LogExclude")도 여기서 조회·저장합니다.
/// </summary>
public class LogController : APIControllerBase
{
    /// <summary>로그 루트 폴더 (가상 경로)</summary>
    private const string LogRoot = "/Logs";

    /// <summary>한 번에 돌려주는 최대 항목 수</summary>
    private const int MaxTake = 200;

    /// <summary>한 요청에서 훑는 최대 바이트 — 아주 큰 파일도 응답이 늦지 않게, 넘으면 다음 위치를 돌려주고 이어서 읽게 함 (40MB 대 파일을 훑는 데 0.3초 정도)</summary>
    private const long MaxScanBytes = 64L * 1024 * 1024;

    /// <summary>로그 수준 순서 (낮음 → 높음)</summary>
    private static readonly string[] Levels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    /// <summary>로그 파일 이름 형식: <c>[yyyy-MM-dd] 이름(.확장자)</c></summary>
    private static readonly Regex FileNamePattern = new(@"^\[(\d{4}-\d{2}-\d{2})\] (.+?)(\.[A-Za-z0-9]+)?$", RegexOptions.Compiled);

    /// <summary>
    /// <see cref="LogController"/> 기본 생성자.
    /// </summary>
    public LogController() : base() { }

    /// <summary>
    /// 로그 파일 목록을 날짜 최신순·종류순으로 돌려줍니다. 종류는 파일 이름에서 날짜·확장자를 뺀 것(예: DataBase, Microsoft.AspNetCore.Hosting.Diagnostics)입니다.
    /// </summary>
    /// <returns>items([{kind, date(yyyy-MM-dd), file(로그 루트 기준 경로), size(바이트)}])</returns>
    [HttpGet("files")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult Files()
    {
        var root = WebConfig.MapPath(LogRoot);
        if (!Directory.Exists(root)) return this.Complete(new { items = Array.Empty<object>() });
        var items = Directory.EnumerateFiles(root, "[*", SearchOption.AllDirectories)
            .Select(path => (path, match: FileNamePattern.Match(Path.GetFileName(path))))
            .Where(p => p.match.Success)
            .Select(p => new { kind = p.match.Groups[2].Value, date = p.match.Groups[1].Value, file = Path.GetRelativePath(root, p.path).Replace('\\', '/'), size = new FileInfo(p.path).Length })
            .OrderByDescending(p => p.date).ThenBy(p => p.kind);
        return this.Complete(new { items });
    }

    /// <summary>
    /// 로그 파일을 바이트 위치(offset)부터 읽어 조건에 맞는 항목을 파일 순서(시간순)대로 돌려줍니다.
    /// 조건에 맞는 항목을 take 개 모았거나, 한 번에 훑는 한도(64MB)에 닿았거나, 파일 끝이면 멈추고 다음에 이어 읽을 위치(next)를 돌려줍니다.
    /// 시간 범위는 로그 시각(created/start)의 시:분(:초)으로 비교하고, 검색어는 줄 원문(모든 속성)에서 대소문자 없이 찾습니다.
    /// </summary>
    /// <param name="file">로그 파일 (<see cref="Files"/> 의 file)</param>
    /// <param name="offset">읽기 시작할 바이트 위치 (0 = 처음)</param>
    /// <param name="from">이 시각부터 (HH:mm 또는 HH:mm:ss, 비우면 처음부터)</param>
    /// <param name="to">이 시각까지 (HH:mm 또는 HH:mm:ss, 비우면 끝까지)</param>
    /// <param name="level">이 수준 이상만 (Trace~Critical, 비우면 전부)</param>
    /// <param name="keyword">검색어 (비우면 전부)</param>
    /// <param name="take">가져올 항목 수 (1~200)</param>
    /// <returns>items([{offset, time, level, summary, duration(ms)}]), next(이어 읽을 위치), size(파일 크기), done(파일 끝까지 읽음)</returns>
    [HttpGet("entries")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult Entries([FromQuery] string? file, [FromQuery] long offset = 0, [FromQuery] string? from = null, [FromQuery] string? to = null,
        [FromQuery] string? level = null, [FromQuery] string? keyword = null, [FromQuery] int take = 100)
    {
        if (this.ResolveFile(file) is not { } path) return this.NotFoundResult(WebResources.NotFoundData);
        var fromTime = ParseTime(from);
        var toTime = ParseTime(to);
        var minLevel = Array.IndexOf(Levels, level ?? string.Empty);
        var word = keyword?.Trim();
        take = Math.Clamp(take, 1, MaxTake);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var size = stream.Length;
        offset = Math.Clamp(offset, 0, size);
        if (offset == 0 && fromTime is { } seek) offset = SeekTime(stream, seek); // 큰 파일: 시작 시각 근처로 바로 이동
        var items = new List<object>();
        var next = offset;
        var ended = false;
        foreach (var (start, end, line) in ReadLines(stream, offset))
        {
            if (next - offset >= MaxScanBytes) break; // 한도까지 훑었으면 여기서 멈추고 이어 읽게 함
            next = end;
            var time = TimeOf(line);
            if (fromTime is { } f && time?.TimeOfDay < f) continue;
            if (toTime is { } t && time?.TimeOfDay > t) { ended = true; break; } // 시간순으로 쓰이므로 끝 시각을 넘으면 더 볼 필요 없음
            if (word is { Length: > 0 } && !line.Contains(word, StringComparison.OrdinalIgnoreCase)) continue;
            if (minLevel > 0 && !HasLevelAtLeast(line, minLevel)) continue;
            items.Add(Summarize(start, line, time));
            if (items.Count >= take) break;
        }
        return this.Complete(new { items, next, size, done = ended || next >= size });
    }

    /// <summary>
    /// 로그 항목 하나의 원본 JSON 을 돌려줍니다 (구조가 로그마다 달라 가공하지 않음).
    /// </summary>
    /// <param name="file">로그 파일</param>
    /// <param name="offset">항목 시작 바이트 위치 (<see cref="Entries"/> 의 offset)</param>
    /// <returns>원본 JSON 문자열</returns>
    [HttpGet("entry")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult Entry([FromQuery] string? file, [FromQuery] long offset)
    {
        if (this.ResolveFile(file) is not { } path) return this.NotFoundResult(WebResources.NotFoundData);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (offset < 0 || offset >= stream.Length) return this.NotFoundResult(WebResources.NotFoundData);
        var line = ReadLines(stream, offset).Select(p => p.Line).FirstOrDefault();
        return line is null ? this.NotFoundResult(WebResources.NotFoundData) : this.Complete(line);
    }

    /// <summary>요청한 로그 파일의 실제 경로 (로그 루트 밖·로그 이름 형식이 아닌 파일·없는 파일이면 null — 경로 조작 방지)</summary>
    private string? ResolveFile(string? file)
    {
        if (string.IsNullOrWhiteSpace(file)) return null;
        var root = Path.GetFullPath(WebConfig.MapPath(LogRoot)).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, file));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !FileNamePattern.IsMatch(Path.GetFileName(path))) return null;
        return System.IO.File.Exists(path) ? path : null;
    }

    /// <summary>스트림을 offset 부터 줄 단위로 읽어 (줄 시작 위치, 다음 줄 시작 위치, 줄 내용)을 돌려줍니다. offset 이 줄 중간이면 그 줄은 건너뜁니다. 빈 줄·BOM 은 뺍니다.</summary>
    private static IEnumerable<(long Start, long End, string Line)> ReadLines(Stream stream, long offset)
    {
        stream.Position = offset;
        var skipPartial = offset > 0 && ByteBefore(stream, offset) != '\n';
        var buffer = new byte[64 * 1024];
        var pending = new MemoryStream();
        var lineStart = offset;
        var position = offset;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            var from = 0;
            for (var i = 0; i < read; i++)
            {
                if (buffer[i] != '\n') continue;
                pending.Write(buffer, from, i - from);
                var end = position + i + 1;
                if (!skipPartial && DecodeLine(pending) is { Length: > 0 } text) yield return (lineStart, end, text);
                skipPartial = false;
                pending.SetLength(0);
                lineStart = end;
                from = i + 1;
            }
            pending.Write(buffer, from, read - from);
            position += read;
        }
        // 마지막 줄(줄바꿈 전에 쓰는 중일 수 있음)은 완전한 JSON 일 때만
        if (!skipPartial && DecodeLine(pending) is { Length: > 0 } last && last.EndsWith('}')) yield return (lineStart, position, last);
    }

    /// <summary>시간순으로 쓰인 파일에서 from 시각 직전 위치를 이분 탐색으로 찾습니다 (64KB 안쪽까지 좁힘, 그 뒤는 시간 조건으로 거름).</summary>
    private static long SeekTime(Stream stream, TimeSpan from)
    {
        long low = 0, high = stream.Length;
        while (high - low > 64 * 1024)
        {
            var mid = low + (high - low) / 2;
            var time = ReadLines(stream, mid).Select(p => TimeOf(p.Line)).FirstOrDefault(p => p is not null);
            if (time is null || time.Value.TimeOfDay >= from) high = mid;
            else low = mid;
        }
        return low;
    }

    /// <summary>offset 바로 앞 바이트 (스트림 위치는 offset 으로 되돌림)</summary>
    private static int ByteBefore(Stream stream, long offset)
    {
        stream.Position = offset - 1;
        var value = stream.ReadByte();
        stream.Position = offset;
        return value;
    }

    /// <summary>줄 바이트를 UTF-8 문자열로 (BOM·끝 \r 제거)</summary>
    private static string DecodeLine(MemoryStream bytes) => Encoding.UTF8.GetString(bytes.GetBuffer(), 0, (int)bytes.Length).TrimStart('﻿').TrimEnd('\r');

    /// <summary>"HH:mm" / "HH:mm:ss" 를 하루 안의 시각으로 (비었거나 형식이 아니면 null)</summary>
    private static TimeSpan? ParseTime(string? value)
        => TimeSpan.TryParseExact(value?.Trim(), [@"hh\:mm", @"hh\:mm\:ss"], CultureInfo.InvariantCulture, out var time) ? time : null;

    /// <summary>
    /// 줄의 로그 시각: 앞쪽에 처음 나오는 "start"(요청 스코프) 또는 "created"(일반·DB) 값. 전체를 파싱하지 않고 찾아 큰 파일도 빨리 거릅니다.
    /// (스코프 로그는 안쪽 로그의 created 가 먼저 나오지만 start 가 그보다 앞에 있어 시작 시각이 잡힘)
    /// </summary>
    private static DateTime? TimeOf(string line)
    {
        var index = new[] { line.IndexOf("\"start\":\"", StringComparison.Ordinal), line.IndexOf("\"created\":\"", StringComparison.Ordinal) }.Where(p => p >= 0).DefaultIfEmpty(-1).Min();
        if (index < 0) return null;
        var valueStart = line.IndexOf(':', index) + 2;
        var valueEnd = line.IndexOf('"', valueStart);
        return valueEnd > valueStart && DateTime.TryParse(line.AsSpan(valueStart, valueEnd - valueStart), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time : null;
    }

    /// <summary>줄에 minLevel 이상의 수준이 있는지 (스코프 안쪽 로그 포함). DB 로그는 state 가 Success 가 아니면 Error 로 봅니다.</summary>
    private static bool HasLevelAtLeast(string line, int minLevel)
    {
        if (Levels.Skip(minLevel).Any(p => line.Contains($"\"level\":\"{p}\"", StringComparison.Ordinal))) return true;
        return minLevel <= Array.IndexOf(Levels, "Error") && line.Contains("\"state\":\"", StringComparison.Ordinal) && !line.Contains("\"state\":\"Success\"", StringComparison.Ordinal);
    }

    /// <summary>
    /// 구조가 다른 로그 줄을 목록 공통 항목으로 맞춥니다. 새 구조가 생기면 여기에 분기를 더합니다 (상세는 원본 그대로라 영향 없음).
    /// DB 쿼리(query): 요약 = 쿼리, 소요 = duration(초)×1000, 수준 = state 가 Success 면 Information 아니면 Error.
    /// 요청 스코프(name + logs): 요약 = name, 소요 = durationMs, 수준 = 안쪽(중첩 포함) 로그 중 가장 높은 수준.
    /// 그 밖: 요약 = message(없으면 stack), 수준 = level.
    /// </summary>
    private static object Summarize(long offset, string line, DateTime? time)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            double? Number(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

            if (Text(root, "query") is { } query)
                return Row(offset, time, Text(root, "state") is "Success" ? "Information" : "Error", query, Number("duration") * 1000);
            if (Text(root, "name") is { } name && root.TryGetProperty("logs", out var logs) && logs.ValueKind == JsonValueKind.Array)
            {
                var top = MaxLevel(root);
                return Row(offset, time, top >= 0 ? Levels[top] : null, name, Number("durationMs"));
            }
            return Row(offset, time, Text(root, "level"), Text(root, "message") ?? Text(root, "stack") ?? line, Number("durationMs"));
        }
        catch (JsonException)
        {
            return Row(offset, time, null, line, null);
        }
    }

    /// <summary>요소와 그 안쪽(중첩 스코프의 logs 포함)에서 가장 높은 level 의 순서 (없으면 -1)</summary>
    private static int MaxLevel(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().Select(p => p.Name == "level" && p.Value.ValueKind == JsonValueKind.String ? Array.IndexOf(Levels, p.Value.GetString()) : MaxLevel(p.Value)).DefaultIfEmpty(-1).Max(),
        JsonValueKind.Array => element.EnumerateArray().Select(MaxLevel).DefaultIfEmpty(-1).Max(),
        _ => -1
    };

    /// <summary>목록 한 행 (요약은 첫 300자)</summary>
    private static object Row(long offset, DateTime? time, string? level, string summary, double? duration)
        => new { offset, time = time?.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture), level, summary = summary.Length > 300 ? summary[..300] : summary, duration = duration is { } d ? Math.Round(d, 1) : (double?)null };

    /// <summary>제외 규칙 최대 개수</summary>
    private const int MaxExcludeRules = 100;

    /// <summary>
    /// 로그 제외 규칙을 조회합니다 (설정 "LogExclude", 저장한 적이 없으면 기본 규칙). 관리자 정책만.
    /// </summary>
    /// <returns>rules([{category, maxLevel, contains}]), defaults(기본 규칙)</returns>
    [HttpGet("exclude")]
    [Auth(WebConfig.ManagerPolicyName)]
    public IActionResult GetExclude() => this.Complete(new
    {
        rules = this.Config!.LogExcludeRules.Select(RuleOf),
        defaults = LogFilter.Defaults.Select(RuleOf)
    });

    /// <summary>
    /// 로그 제외 규칙을 저장하고 바로 적용합니다 (설정 "LogExclude"). 다른 서버는 1분 안에 다시 읽어 반영합니다. 관리자 정책만.
    /// 카테고리는 로거 이름의 앞부분(DB 명령 로그는 <see cref="LogFilter.DataBaseCategory"/>), 수준은 이 수준 이하만 제외, 포함 글자는 비우면 카테고리·수준만 봅니다.
    /// </summary>
    /// <param name="rules">규칙 JSON 배열 [{category, maxLevel(Trace~Critical), contains}] — 빈 배열이면 아무것도 제외하지 않음</param>
    /// <returns>처리 결과</returns>
    [HttpPut("exclude")]
    [Auth(WebConfig.ManagerPolicyName)]
    public async Task<IActionResult> SaveExclude([FromForm] string? rules)
    {
        List<LogExcludeRule>? list;
        try
        {
            list = JsonSerializer.Deserialize<List<LogExcludeRule>>(string.IsNullOrWhiteSpace(rules) ? "[]" : rules, WebConfig.LogExcludeJson);
        }
        catch (JsonException)
        {
            list = null;
        }
        if (list is null || list.Count > MaxExcludeRules) return this.Error(WebResources.ValidRegex.Formats(this.Lang("LogExclude"))!);
        foreach (var rule in list)
        {
            rule.Category = rule.Category.Trim();
            rule.Contains = string.IsNullOrWhiteSpace(rule.Contains) ? null : rule.Contains.Trim();
            if (rule.Category.Length is 0 or > 256 || rule.Contains?.Length > 256 || !Enum.IsDefined(rule.MaxLevel) || rule.MaxLevel == LogLevel.None)
                return this.Error(WebResources.ValidRegex.Formats(this.Lang("LogExclude"))!);
        }
        await this.Config!.SetAsync(this.DbContext!, WebConfig.LogExcludeKey, JsonSerializer.Serialize(list, WebConfig.LogExcludeJson));
        LogFilter.Set(list);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>규칙 응답 모양</summary>
    private static object RuleOf(LogExcludeRule rule) => new { category = rule.Category, maxLevel = rule.MaxLevel.ToString(), contains = rule.Contains };
}
