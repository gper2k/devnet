using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BitPlus.Web.Site.Models;

/// <summary>
/// 레이아웃 테마 색상 설정 모델 (main, sub, point, deco 및 다크 테마).
/// </summary>
public class LayoutThemeConfig
{
    /// <summary>라이트 테마 주 색상 (main)</summary>
    public string Main { get; set; }

    /// <summary>라이트 테마 보조 색상 (sub)</summary>
    public string Sub { get; set; }

    /// <summary>라이트 테마 포인트 색상 (point)</summary>
    public string Point { get; set; }

    /// <summary>라이트 테마 장식 색상 (deco)</summary>
    public string Deco { get; set; }

    /// <summary>다크 테마 주 색상 (main)</summary>
    public string DarkMain { get; set; }

    /// <summary>다크 테마 보조 색상 (sub)</summary>
    public string DarkSub { get; set; }

    /// <summary>다크 테마 포인트 색상 (point)</summary>
    public string DarkPoint { get; set; }

    /// <summary>다크 테마 장식 색상 (deco)</summary>
    public string DarkDeco { get; set; }

    /// <summary>
    /// 레이아웃 좌측 패널 등에 노출할 다국어 인사말 (문화권 코드 -> 인사말 문구, 예: "ko" -> "방문해 주셔서 감사합니다")
    /// </summary>
    public Dictionary<string, string> Greetings { get; set; }

    /// <summary>
    /// <see cref="LayoutThemeConfig"/> 기본 생성자. 기본 테마 색상으로 초기화합니다.
    /// </summary>
    public LayoutThemeConfig()
    {
        this.Main = "#1f382c";
        this.Sub = "#5288b7";
        this.Point = "#9be82e";
        this.Deco = "#638791";
        this.DarkMain = "#3e7157";
        this.DarkSub = "#3c688f";
        this.DarkPoint = "#78bd15";
        this.DarkDeco = "#4f6b73";
        this.Greetings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 지정된 문화권(또는 현재 UI 문화권)에 맞는 인사말을 반환합니다.
    /// </summary>
    /// <param name="culture">문화권 코드 (생략 시 CurrentUICulture)</param>
    /// <param name="fallback">일치하는 항목이 없을 때 반환할 기본 문구</param>
    /// <returns>인사말 문구</returns>
    public string? GetGreeting(string? culture = null, string? fallback = null)
    {
        if (this.Greetings == null || this.Greetings.Count == 0) return fallback;
        var cult = (culture ?? CultureInfo.CurrentUICulture.Name).Trim();
        var shortCult = cult.Length > 2 ? cult[..2] : cult;

        if (this.Greetings.TryGetValue(cult, out var msg) && !string.IsNullOrWhiteSpace(msg)) return msg;
        if (this.Greetings.TryGetValue(shortCult, out msg) && !string.IsNullOrWhiteSpace(msg)) return msg;
        if (this.Greetings.TryGetValue("ko", out msg) && !string.IsNullOrWhiteSpace(msg)) return msg;
        var first = this.Greetings.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        return !string.IsNullOrWhiteSpace(first) ? first : fallback;
    }

    /// <summary>
    /// 지정된 범주 및 레이아웃의 설정 키를 생성합니다.
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <returns>설정 키 (예: LayoutConfig:General:Default)</returns>
    public static string ConfigKey(string category, string name) => $"LayoutConfig:{category}:{name}";

    /// <summary>
    /// 설정된 색상 값들을 기준으로 CSS 커스텀 속성 재선언 블록(:root 및 [data-theme="dark"])을 생성합니다.
    /// </summary>
    /// <returns>CSS 스타일 문자열</returns>
    public string ToCss()
    {
        var sb = new StringBuilder();
        sb.AppendLine(":root {");
        AppendColorVars(sb, "main", this.Main, isDark: false);
        AppendColorVars(sb, "sub", this.Sub, isDark: false);
        AppendColorVars(sb, "point", this.Point, isDark: false);
        AppendColorVars(sb, "deco", this.Deco, isDark: false);
        AppendPrimaryAliases(sb);
        sb.AppendLine("}");

        sb.AppendLine("[data-theme=\"dark\"] {");
        AppendColorVars(sb, "main", this.DarkMain, isDark: true);
        AppendColorVars(sb, "sub", this.DarkSub, isDark: true);
        AppendColorVars(sb, "point", this.DarkPoint, isDark: true);
        AppendColorVars(sb, "deco", this.DarkDeco, isDark: true);
        AppendPrimaryAliases(sb);
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 레이아웃 전역 primary 계열 호환을 위해 point 및 main 계열 CSS 변수 별칭을 추가합니다.
    /// </summary>
    private static void AppendPrimaryAliases(StringBuilder sb)
    {
        sb.AppendLine("  --primary: var(--point);");
        sb.AppendLine("  --primaryLight: var(--pointLight);");
        sb.AppendLine("  --primaryDark: var(--pointDark);");
        sb.AppendLine("  --primary0: var(--main0);");
        sb.AppendLine("  --primary1: var(--point1);");
        sb.AppendLine("  --primary2: var(--point2);");
        sb.AppendLine("  --primary8: var(--main8);");
        sb.AppendLine("  --primary9: var(--main9);");
    }

    /// <summary>
    /// 단일 색상에 대해 Base, Light, Dark 및 1~9 단계 변수를 생성하여 버퍼에 추가합니다.
    /// </summary>
    private static void AppendColorVars(StringBuilder sb, string colorName, string hex, bool isDark)
    {
        if (string.IsNullOrWhiteSpace(hex)) return;
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        if (hex.Length != 7 && hex.Length != 4) return;

        var (h, s, l) = HexToHsl(hex);
        sb.AppendLine($"  --{colorName}: {hex};");

        if (!isDark)
        {
            var lightHex = HslToHex(h, s, Math.Clamp(l + 0.12, 0, 1));
            var darkHex = HslToHex(h, s, Math.Clamp(l - 0.08, 0, 1));
            sb.AppendLine($"  --{colorName}Light: {lightHex};");
            sb.AppendLine($"  --{colorName}Dark: {darkHex};");

            // Light 모드 1~9 단계: 1 = 밝은 틴트(94%), 9 = 고대비 텍스트(10%)
            double[] lightSteps = [0.94, 0.86, 0.75, 0.60, 0.46, 0.35, 0.25, 0.17, 0.10];
            double[] sFactors = [0.35, 0.45, 0.55, 0.70, 0.85, 0.95, 1.0, 1.0, 1.0];
            for (var i = 0; i < 9; i++)
            {
                var stepHex = HslToHex(h, Math.Clamp(s * sFactors[i], 0, 1), lightSteps[i]);
                sb.AppendLine($"  --{colorName}{i + 1}: {stepHex};");
            }
        }
        else
        {
            var lightHex = HslToHex(h, s, Math.Clamp(l + 0.06, 0, 1));
            var darkHex = HslToHex(h, s, Math.Clamp(l - 0.06, 0, 1));
            sb.AppendLine($"  --{colorName}Light: {lightHex};");
            sb.AppendLine($"  --{colorName}Dark: {darkHex};");

            // Dark 모드 1~9 단계: 1 = 어두운 틴트(15%), 9 = 밝은 글자(75%)
            double[] darkSteps = [0.15, 0.20, 0.26, 0.33, 0.40, 0.48, 0.56, 0.65, 0.75];
            double[] sFactors = [0.40, 0.50, 0.65, 0.80, 1.0, 1.0, 1.0, 1.0, 1.0];
            for (var i = 0; i < 9; i++)
            {
                var stepHex = HslToHex(h, Math.Clamp(s * sFactors[i], 0, 1), darkSteps[i]);
                sb.AppendLine($"  --{colorName}{i + 1}: {stepHex};");
            }
        }
    }

    /// <summary>HEX 색상 문자열을 HSL (0~360, 0~1, 0~1)로 변환합니다.</summary>
    public static (double h, double s, double l) HexToHsl(string hex)
    {
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length == 3) hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        if (hex.Length != 6) return (0, 0, 0);

        var r = int.Parse(hex[0..2], NumberStyles.HexNumber) / 255.0;
        var g = int.Parse(hex[2..4], NumberStyles.HexNumber) / 255.0;
        var b = int.Parse(hex[4..6], NumberStyles.HexNumber) / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var diff = max - min;
        var l = (max + min) / 2.0;

        if (Math.Abs(diff) < 0.00001) return (0, 0, l);

        var s = l > 0.5 ? diff / (2.0 - max - min) : diff / (max + min);
        double h;
        if (Math.Abs(max - r) < 0.00001) h = (g - b) / diff + (g < b ? 6.0 : 0.0);
        else if (Math.Abs(max - g) < 0.00001) h = (b - r) / diff + 2.0;
        else h = (r - g) / diff + 4.0;
        h *= 60.0;

        return (h, s, l);
    }

    /// <summary>HSL (0~360, 0~1, 0~1)을 HEX 문자열로 변환합니다.</summary>
    public static string HslToHex(double h, double s, double l)
    {
        double r, g, b;
        if (Math.Abs(s) < 0.00001)
        {
            r = g = b = l;
        }
        else
        {
            var q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
            var p = 2.0 * l - q;
            var hk = h / 360.0;
            r = HueToRgb(p, q, hk + 1.0 / 3.0);
            g = HueToRgb(p, q, hk);
            b = HueToRgb(p, q, hk - 1.0 / 3.0);
        }

        var ri = Math.Clamp((int)Math.Round(r * 255.0), 0, 255);
        var gi = Math.Clamp((int)Math.Round(g * 255.0), 0, 255);
        var bi = Math.Clamp((int)Math.Round(b * 255.0), 0, 255);

        return $"#{ri:x2}{gi:x2}{bi:x2}";
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1.0;
        if (t > 1) t -= 1.0;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }
}
