using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace BitPlus.Web.Site.Plugins;

/// <summary>
/// 어셈블리 내장 리소스 파일의 가상 정보(<see cref="IFileInfo"/>)를 제공하는 클래스입니다.
/// </summary>
public class PluginFileInfo : IFileInfo
{
    private readonly Assembly assembly;
    private readonly string resourceName;
    private readonly string name;
    private readonly long length;
    private readonly DateTimeOffset lastModified;

    /// <summary>
    /// <see cref="PluginFileInfo"/> 생성자.
    /// </summary>
    /// <param name="assembly">리소스가 내장된 어셈블리</param>
    /// <param name="resourceName">어셈블리 내부 매니페스트 리소스 이름</param>
    /// <param name="name">표시용 파일 이름</param>
    /// <param name="length">파일 크기(바이트)</param>
    /// <param name="lastModified">최종 수정 시각</param>
    public PluginFileInfo(Assembly assembly, string resourceName, string name, long length, DateTimeOffset lastModified)
    {
        this.assembly = assembly;
        this.resourceName = resourceName;
        this.name = name;
        this.length = length;
        this.lastModified = lastModified;
    }

    /// <inheritdoc/>
    public bool Exists => true;

    /// <inheritdoc/>
    public bool IsDirectory => false;

    /// <inheritdoc/>
    public DateTimeOffset LastModified => this.lastModified;

    /// <inheritdoc/>
    public long Length => this.length;

    /// <inheritdoc/>
    public string Name => this.name;

    /// <inheritdoc/>
    public string? PhysicalPath => null;

    /// <inheritdoc/>
    public Stream CreateReadStream()
    {
        var stream = this.assembly.GetManifestResourceStream(this.resourceName);
        if (stream is null) throw new FileNotFoundException($"Embedded resource not found: {this.resourceName}");
        return stream;
    }
}
