using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 영역의 슬롯 하나를 그릴 정보 — 위젯 출력 뷰(<c>Views/Widgets/{키}/View.cshtml</c>)의 모델이기도 합니다.
/// </summary>
public class SlotView
{
    /// <summary>슬롯 키 (영역 정의의 키, 가로형은 1부터 번호)</summary>
    public string Key { get; set; }

    /// <summary>위젯 키 (비었으면 빈 슬롯)</summary>
    public string? Widget { get; set; }

    /// <summary>위젯 제목 (현재 화면 언어, 없으면 null — 제목 줄 없음)</summary>
    public string? Title { get; set; }

    /// <summary>위젯 설정값 (widget.json 기본값 위에 저장값)</summary>
    public JsonObject Settings { get; set; }

    /// <summary>데이터 로더 결과 (로더가 없으면 null)</summary>
    public object? Data { get; set; }

    /// <summary>제목 옆 +more 링크 주소 (데이터 로더가 정함 — 게시판 위젯은 대상 게시판이 하나일 때 그 목록, 없으면 null)</summary>
    public string? MoreUrl { get; set; }

    /// <summary>위젯 종류가 없어졌거나 꺼짐 (화면에서는 건너뛰고 편집 모드에서만 표시)</summary>
    public bool Missing { get; set; }

    /// <summary>공통 카드 틀(제목 줄) 안에 그릴지 (widget.json <c>frame</c>, 기본 true — false 면 위젯 출력 뷰가 자기 틀을 그림)</summary>
    public bool Frame { get; set; }

    /// <summary>슬롯 가로 크기 (가로 4칸 격자 기준 칸 수)</summary>
    public double Width { get; set; }

    /// <summary>슬롯 세로 크기 (행 수)</summary>
    public int Height { get; set; }

    /// <summary>현재 HTTP 컨텍스트 (다국어 문화권 해석용)</summary>
    public HttpContext? HttpContext { get; set; }

    /// <summary>
    /// <see cref="SlotView"/> 기본 생성자
    /// </summary>
    public SlotView()
    {
        this.Key = string.Empty;
        this.Widget = null;
        this.Title = null;
        this.Settings = [];
        this.Data = null;
        this.MoreUrl = null;
        this.Missing = false;
        this.Frame = true;
        this.Width = 0;
        this.Height = 0;
        this.HttpContext = null;
    }

    /// <summary>문자열 설정값 (단일 문자열 또는 다국어 객체{"ko":"...","en":"..."}에서 현재 UI 문화권에 맞춰 추출, 없으면 빈 문자열)</summary>
    public string Text(string key)
    {
        var node = this.Settings[key];
        if (node is null) return string.Empty;
        if (node is JsonValue v && v.TryGetValue<string>(out var s) && !s.TrimStart().StartsWith('{')) return s;
        return CultureText.Of(this.HttpContext?.Request, node, string.Empty) ?? string.Empty;
    }

    /// <summary>정수 설정값 (없으면 기본값)</summary>
    public int Int(string key, int fallback) => this.Settings[key] is JsonValue v && v.TryGetValue<int>(out var n) ? n : fallback;

    /// <summary>참/거짓 설정값 (없으면 기본값)</summary>
    public bool Bool(string key, bool fallback) => this.Settings[key] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : fallback;


    /// <summary>여러 줄 설정값을 줄마다 "|" 로 나눈 칸 목록으로 (빈 줄 제외, 칸은 앞뒤 공백 제거)</summary>
    public List<string[]> Lines(string key)
        => [.. this.Text(key).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split('|').Select(p => p.Trim()).ToArray()).Where(p => p.Length > 0 && p[0].Length > 0)];

    /// <summary>
    /// 주소 설정값이 같은 사이트 경로(<c>/…</c>)나 http(s) 주소일 때만 돌려줍니다 (javascript: 등은 null).
    /// 숫자 ID(예: "7")는 페이지 뷰 주소(<c>/view/{번호}</c>)로, "@아이디"(예: "@gper") 또는 "bmac:아이디"는 Buy Me a Coffee 후원 주소로 자동 변환합니다.
    /// </summary>
    /// <param name="url">주소, 번호 또는 아이디</param>
    public static string? SafeUrl(string? url)
    {
        var value = url?.Trim() ?? string.Empty;
        if (value.Length == 0) return null;
        if ((value.StartsWith('/') && !value.StartsWith("//")) || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return value;
        if (int.TryParse(value, out var contentNo) && contentNo > 0) return $"/view/{contentNo}";
        if (value.StartsWith('@') && value.Length > 1 && !value.Contains('/')) return $"https://buymeacoffee.com/{value[1..]}";
        if (value.StartsWith("bmac:", StringComparison.OrdinalIgnoreCase) && value.Length > 5) return $"https://buymeacoffee.com/{value[5..].Trim()}";
        if (value.StartsWith("coffee:", StringComparison.OrdinalIgnoreCase) && value.Length > 7) return $"https://buymeacoffee.com/{value[7..].Trim()}";
        return null;
    }

    /// <summary>테마 색 SVG 캐시 (전체 경로 → 파일 수정 시각, 정리된 SVG)</summary>
    private static readonly ConcurrentDictionary<string, (DateTime Modified, string Svg)> ThemeSvgCache = new();

    /// <summary>
    /// 같은 사이트의 <c>/img/</c> 아래 SVG 가 CSS 변수(<c>var(--</c>)를 쓰면 페이지에 바로 넣을 SVG 태그를 돌려줍니다 (아니면 null — img 태그로 출력).
    /// <c>img</c> 로 읽은 SVG 는 페이지 CSS 변수를 볼 수 없어서, 레이아웃 색(<c>--main</c>·<c>--point</c> 등, 다크 테마 포함)을 따르려면 문서 안에 넣어야 합니다.
    /// 스크립트·foreignObject·on* 속성은 지우고, id 는 파일 이름을 앞에 붙여 페이지의 다른 id 와 겹치지 않게 합니다.
    /// </summary>
    /// <param name="url">이미지 주소 (<c>/img/…/*.svg</c>)</param>
    /// <param name="webRoot">wwwroot 전체 경로</param>
    /// <param name="style">svg 태그 style (비율·높이)</param>
    /// <param name="label">대체 글자 (aria-label)</param>
    public static string? ThemeSvg(string? url, string webRoot, string? style, string? label)
    {
        var value = url?.Trim() ?? string.Empty;
        if (!value.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) || !value.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || value.Contains('?') || value.Contains('#')) return null;

        var root = Path.GetFullPath(webRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, value.TrimStart('/')));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;

        var modified = File.GetLastWriteTimeUtc(path);
        if (!ThemeSvgCache.TryGetValue(path, out var cached) || cached.Modified != modified)
        {
            cached = (modified, LoadThemeSvg(path));
            ThemeSvgCache[path] = cached;
        }
        if (cached.Svg.Length == 0) return null;

        var attributes = $" role=\"img\" focusable=\"false\" aria-label=\"{HtmlEncoder.Default.Encode(label ?? string.Empty)}\"";
        if (!string.IsNullOrEmpty(style)) attributes += $" style=\"{HtmlEncoder.Default.Encode(style)}\"";
        return "<svg" + attributes + cached.Svg[4..];
    }

    /// <summary>SVG 파일을 읽어 정리 (CSS 변수를 쓰지 않거나 읽을 수 없으면 빈 문자열)</summary>
    private static string LoadThemeSvg(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            if (!text.Contains("var(--")) return string.Empty;

            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var svg = XDocument.Load(reader).Root;
            if (svg is null || svg.Name.LocalName != "svg") return string.Empty;

            svg.DescendantNodes().OfType<XComment>().Remove();
            svg.Descendants().Where(p => p.Name.LocalName is "script" or "foreignObject").Remove();
            foreach (var element in svg.DescendantsAndSelf())
                element.Attributes().Where(p => p.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)).Remove();

            // 크기는 출력 쪽(CSS)이 정함 — viewBox 비율을 유지하며 칸을 채우고 넘치는 부분은 자름 (img 의 object-fit: cover 와 같게)
            svg.Attribute("width")?.Remove();
            svg.Attribute("height")?.Remove();
            if (svg.Attribute("preserveAspectRatio") is null) svg.SetAttributeValue("preserveAspectRatio", "xMidYMid slice");

            var prefix = "svg" + string.Concat(Path.GetFileNameWithoutExtension(path).Where(char.IsAsciiLetterOrDigit)) + "_";
            var ids = svg.DescendantsAndSelf().Select(p => p.Attribute("id")).OfType<XAttribute>().ToList();
            foreach (var id in ids)
            {
                var name = id.Value;
                id.Value = prefix + name;
                foreach (var attribute in svg.DescendantsAndSelf().SelectMany(p => p.Attributes()))
                {
                    if (attribute.Name.LocalName == "href" && attribute.Value == "#" + name) attribute.Value = "#" + prefix + name;
                    else if (attribute.Value.Contains($"url(#{name})")) attribute.Value = attribute.Value.Replace($"url(#{name})", $"url(#{prefix}{name})");
                }
            }

            var result = svg.ToString(SaveOptions.DisableFormatting);
            return result.StartsWith("<svg") ? result : string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>아이콘 클래스 설정값이 Fluent Icons 별칭 형식(fi + 영문·숫자)일 때만 (아니면 fallback)</summary>
    public static string IconOf(string? icon, string fallback = "fiCheckCircle")
        => icon is { Length: > 2 and < 64 } && icon.StartsWith("fi") && icon.All(char.IsAsciiLetterOrDigit) ? icon : fallback;
}
