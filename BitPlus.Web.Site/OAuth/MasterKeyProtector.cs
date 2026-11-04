using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace BitPlus.Web.Site.OAuth;

/// <summary>
/// 마스터 키(<see cref="WebConfig.DeriveKey"/>)로 암호화하는 데이터 보호기. 외부 로그인의 state 값(돌아올 주소·PKCE 값)을 보호합니다.
/// ASP.NET Data Protection 키는 서버마다 달라 다른 서버로 돌아온 콜백을 풀 수 없으므로 쓰지 않습니다.
/// </summary>
public class MasterKeyProtector : IDataProtector
{
    private readonly WebConfig config;
    private readonly string purpose;

    /// <summary>
    /// <see cref="MasterKeyProtector"/> 생성자.
    /// </summary>
    /// <param name="config">웹 설정 (마스터 키)</param>
    /// <param name="purpose">용도 문자열 (용도마다 다른 키)</param>
    public MasterKeyProtector(WebConfig config, string purpose)
    {
        this.config = config;
        this.purpose = purpose;
    }

    /// <inheritdoc/>
    public IDataProtector CreateProtector(string purpose) => new MasterKeyProtector(this.config, $"{this.purpose}|{purpose}");

    /// <inheritdoc/>
    public byte[] Protect(byte[] plaintext)
        => Convert.FromBase64String(this.config.ProtectSecret(Convert.ToBase64String(plaintext), this.purpose)!);

    /// <inheritdoc/>
    public byte[] Unprotect(byte[] protectedData)
    {
        try { return Convert.FromBase64String(this.config.UnprotectSecret(Convert.ToBase64String(protectedData), this.purpose)!); }
        catch (InvalidOperationException ex) { throw new CryptographicException(ex.Message, ex); }
    }
}
