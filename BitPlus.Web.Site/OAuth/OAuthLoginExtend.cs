using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 외부 로그인(OAuth) 서비스 등록 확장.
/// </summary>
public static class OAuthLoginExtend
{
    /// <summary>
    /// 관리 화면에 저장된 외부 로그인 서비스를 인증 스킴으로 쓰도록 등록합니다 (스킴 공급자 교체, 옵션 구성). 기본 인증 스킴은 두지 않습니다.
    /// 콜백은 파이프라인의 <c>UseAuthentication</c> 이 처리합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>체이닝을 위한 <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddOAuthLogin(this IServiceCollection services)
    {
        services.AddAuthentication();
        services.Replace(ServiceDescriptor.Singleton<IAuthenticationSchemeProvider, OAuthSchemeProvider>());
        services.AddSingleton<IConfigureOptions<OAuthOptions>, OAuthOptionsSetup>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OAuthOptions>, OAuthPostConfigureOptions<OAuthOptions, OAuthHandler<OAuthOptions>>>());
        return services;
    }
}
