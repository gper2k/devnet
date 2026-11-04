using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 외부 로그인 처리기 이벤트: 토큰을 받으면 사용자 정보 주소에서 고유 식별자·메일·이름을 읽고,
/// 확인이 끝나면 쿠키 인증으로 로그인하지 않고(이 사이트는 자체 토큰 사용) <see cref="ExternalIdentity"/> 로 보관한 뒤 외부 로그인 화면으로 보냅니다.
/// 실패·취소는 로그인 화면(<c>?external=failed</c>)으로 보냅니다.
/// </summary>
public class OAuthLoginEvents : OAuthEvents
{
    /// <summary>메일 검증 여부 클레임 이름</summary>
    public const string VerifiedClaim = "email_verified";

    /// <summary>돌아갈 주소를 담는 인증 속성 키</summary>
    public const string ReturnUrlItem = "returnUrl";

    private readonly OAuthProvider provider;

    /// <summary>
    /// <see cref="OAuthLoginEvents"/> 생성자.
    /// </summary>
    /// <param name="provider">처리할 서비스</param>
    public OAuthLoginEvents(OAuthProvider provider)
    {
        this.provider = provider;
    }

    /// <inheritdoc/>
    public override async Task CreatingTicket(OAuthCreatingTicketContext context)
    {
        var preset = this.provider.Preset();
        using var request = new HttpRequestMessage(HttpMethod.Get, preset.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));

        var id = ValueOf(json.RootElement, preset.IdPath);
        if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("The external login response has no user identifier.");
        context.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
        context.Identity.AddClaim(new Claim(ClaimTypes.Email, ValueOf(json.RootElement, preset.EmailPath) ?? string.Empty));
        context.Identity.AddClaim(new Claim(VerifiedClaim, (preset.VerifiedPath.Length > 0 && ValueOf(json.RootElement, preset.VerifiedPath) is "true" or "True").ToString()));
        context.Identity.AddClaim(new Claim(ClaimTypes.Name, preset.NamePath.Split(',').Select(p => ValueOf(json.RootElement, p)).FirstOrDefault(p => !string.IsNullOrEmpty(p)) ?? string.Empty));
    }

    /// <inheritdoc/>
    public override async Task TicketReceived(TicketReceivedContext context)
    {
        var claims = context.Principal!;
        await new ExternalIdentity
        {
            ProviderId = this.provider.Id,
            ProviderName = this.provider.Name,
            Key = claims.FindFirstValue(ClaimTypes.NameIdentifier)!,
            Email = claims.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            EmailVerified = claims.FindFirstValue(VerifiedClaim) == bool.TrueString,
            Name = claims.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            ReturnUrl = context.Properties?.Items.TryGetValue(ReturnUrlItem, out var returnUrl) == true ? returnUrl : null
        }.SaveAsync(context.HttpContext);
        context.Response.Redirect("/Account/External");
        context.HandleResponse();
    }

    /// <inheritdoc/>
    public override Task RemoteFailure(RemoteFailureContext context)
    {
        context.Response.Redirect("/Account/SignIn?external=failed");
        context.HandleResponse();
        return Task.CompletedTask;
    }

    /// <summary>
    /// JSON 에서 점 표기 경로의 값을 문자열로 읽습니다 (숫자·참거짓 포함).
    /// </summary>
    /// <param name="root">응답 JSON</param>
    /// <param name="path">경로 (예: kakao_account.email)</param>
    /// <returns>값 (없으면 null)</returns>
    private static string? ValueOf(JsonElement root, string path)
    {
        var node = root;
        foreach (var name in path.Trim().Split('.'))
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty(name, out node)) return null;
        return node.ValueKind switch
        {
            JsonValueKind.String => node.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => node.GetRawText(),
            _ => null
        };
    }
}
