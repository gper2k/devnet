using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 인증 스킴 이름(<see cref="OAuthProvider.SchemeName"/>)에 맞는 저장된 서비스로 OAuth 처리기 옵션을 채웁니다.
/// 옵션은 스킴 이름별로 한 번 만들어 두고, 서비스를 저장하면 스킴 이름이 바뀌어 새로 만듭니다.
/// </summary>
public class OAuthOptionsSetup : IConfigureNamedOptions<OAuthOptions>
{
    private readonly WebConfig config;

    /// <summary>
    /// <see cref="OAuthOptionsSetup"/> 생성자.
    /// </summary>
    /// <param name="config">웹 설정</param>
    public OAuthOptionsSetup(WebConfig config)
    {
        this.config = config;
    }

    /// <inheritdoc/>
    public void Configure(OAuthOptions options) { }

    /// <inheritdoc/>
    public void Configure(string? name, OAuthOptions options)
    {
        if (OAuthProvider.SchemeOf(this.config, name) is not { } provider) return;
        var preset = provider.Preset();
        options.ClientId = provider.ClientId;
        options.ClientSecret = this.config.UnprotectSecret(provider.ClientSecret, OAuthProvider.ProtectPurpose) ?? string.Empty;
        options.CallbackPath = provider.CallbackPath();
        options.AuthorizationEndpoint = preset.AuthorizationEndpoint;
        options.TokenEndpoint = preset.TokenEndpoint;
        options.UserInformationEndpoint = preset.UserInfoEndpoint;
        options.UsePkce = preset.UsePkce;
        options.Scope.Clear();
        foreach (var scope in preset.Scopes) options.Scope.Add(scope);
        options.DataProtectionProvider = new MasterKeyProtector(this.config, "BitPlus.OAuth.State");
        options.Events = new OAuthLoginEvents(provider);
    }
}
