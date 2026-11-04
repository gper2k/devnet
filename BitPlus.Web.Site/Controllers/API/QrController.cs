using BitPlus.Web.HTTP.API;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// QR 코드 Web API 컨트롤러 (api/qr). 사이트 하단 "GET QR CODE" 가 지금 보고 있는 페이지 주소의 QR 코드 이미지를 받는 데 씁니다.
/// 아무 주소나 QR 로 만들어 주는 생성기가 되지 않도록 이 사이트 안의 경로만 받고, 주소는 요청의 스킴·호스트로 서버가 완성합니다.
/// </summary>
public class QrController : APIControllerBase
{
    /// <summary>
    /// <see cref="QrController"/> 기본 생성자.
    /// </summary>
    public QrController() : base() { }

    /// <summary>
    /// 사이트 경로의 전체 주소(현재 요청의 스킴·호스트 + 경로)를 담은 QR 코드를 SVG 이미지로 돌려줍니다 (하루 동안 브라우저 캐시).
    /// </summary>
    /// <param name="path">사이트 안의 경로와 쿼리 (<c>/</c> 로 시작, <c>//</c>·역슬래시 불가, 2048자 이내)</param>
    /// <returns>QR 코드 SVG (image/svg+xml), 경로가 맞지 않으면 400</returns>
    [HttpGet]
    public IActionResult Get([FromQuery] string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > 2048 || !path.StartsWith('/') || path.StartsWith("//") || path.Contains('\\')) return this.Error(WebResources.ValidRegex.Formats("path")!);
        var url = $"{this.Request.Scheme}://{this.Request.Host}{path}";
        using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.M);
        this.Response.Headers.CacheControl = "private, max-age=86400";
        return this.Content(new SvgQRCode(data).GetGraphic(8), "image/svg+xml");
    }
}
