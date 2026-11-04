using Microsoft.Extensions.FileProviders;
using System.Collections;

namespace BitPlus.Web.Site.Plugins;

/// <summary>
/// 어셈블리 내장 가상 디렉터리의 항목 목록(<see cref="IDirectoryContents"/>)을 제공하는 클래스입니다.
/// </summary>
public class PluginDirectoryContents : IDirectoryContents
{
    private readonly IEnumerable<IFileInfo> entries;
    private readonly bool exists;

    /// <summary>
    /// <see cref="PluginDirectoryContents"/> 생성자.
    /// </summary>
    /// <param name="entries">디렉터리 내부 파일/폴더 정보 목록</param>
    /// <param name="exists">디렉터리 존재 여부</param>
    public PluginDirectoryContents(IEnumerable<IFileInfo> entries, bool exists = true)
    {
        this.entries = entries;
        this.exists = exists;
    }

    /// <inheritdoc/>
    public bool Exists => this.exists;

    /// <inheritdoc/>
    public IEnumerator<IFileInfo> GetEnumerator() => this.entries.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
