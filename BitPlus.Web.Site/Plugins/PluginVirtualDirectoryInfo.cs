using Microsoft.Extensions.FileProviders;

namespace BitPlus.Web.Site.Plugins;

/// <summary>
/// 어셈블리 내장 가상 디렉터리 항목 정보(<see cref="IFileInfo"/>)를 제공하는 클래스입니다.
/// </summary>
public class PluginVirtualDirectoryInfo : IFileInfo
{
    private readonly string name;
    private readonly DateTimeOffset lastModified;

    /// <summary>
    /// <see cref="PluginVirtualDirectoryInfo"/> 생성자.
    /// </summary>
    /// <param name="name">디렉터리 이름</param>
    /// <param name="lastModified">수정 시각</param>
    public PluginVirtualDirectoryInfo(string name, DateTimeOffset lastModified)
    {
        this.name = name;
        this.lastModified = lastModified;
    }

    /// <inheritdoc/>
    public bool Exists => true;

    /// <inheritdoc/>
    public bool IsDirectory => true;

    /// <inheritdoc/>
    public DateTimeOffset LastModified => this.lastModified;

    /// <inheritdoc/>
    public long Length => -1;

    /// <inheritdoc/>
    public string Name => this.name;

    /// <inheritdoc/>
    public string? PhysicalPath => null;

    /// <inheritdoc/>
    public Stream CreateReadStream() => throw new InvalidOperationException("Cannot create read stream for a directory.");
}
