using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Xml.Linq;

namespace BitPlus.Web.Site.Plugins;

/// <summary>
/// 어셈블리 내장 리소스(정적 웹 리소스, Razor/위젯 JSON 등)를 가상 디렉터리 구조로 제공하는 <see cref="IFileProvider"/> 구현 클래스입니다.
/// </summary>
public class PluginEmbeddedFileProvider : IFileProvider
{
    private const string ManifestResourceName = "Microsoft.Extensions.FileProviders.Embedded.Manifest.xml";
    private readonly Assembly assembly;
    private readonly string? rootFolder;
    private readonly DateTimeOffset lastModified;
    private readonly Dictionary<string, (string ResourceName, long Length)> fileMap;
    private readonly HashSet<string> directories;

    /// <summary>
    /// <see cref="PluginEmbeddedFileProvider"/> 생성자.
    /// </summary>
    /// <param name="assembly">내장 리소스를 포함한 대상 어셈블리</param>
    /// <param name="rootFolder">서빙할 가상 루트 폴더 (예: <c>wwwroot</c>, 전체는 null)</param>
    public PluginEmbeddedFileProvider(Assembly assembly, string? rootFolder = null)
    {
        this.assembly = assembly;
        this.rootFolder = string.IsNullOrWhiteSpace(rootFolder) ? null : rootFolder.Trim('/', '\\').Replace('\\', '/');
        this.lastModified = File.Exists(assembly.Location) ? File.GetLastWriteTimeUtc(assembly.Location) : DateTimeOffset.UtcNow;
        this.fileMap = new Dictionary<string, (string, long)>(StringComparer.OrdinalIgnoreCase);
        this.directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        this.InitializeResourceMap();
    }

    /// <inheritdoc/>
    public IFileInfo GetFileInfo(string subpath)
    {
        var path = this.ResolvePath(subpath);
        if (path is not null && this.fileMap.TryGetValue(path, out var item))
            return new PluginFileInfo(this.assembly, item.ResourceName, Path.GetFileName(path), item.Length, this.lastModified);

        return new NotFoundFileInfo(Path.GetFileName(subpath));
    }

    /// <inheritdoc/>
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var targetDir = this.ResolvePath(subpath) ?? string.Empty;
        var dirPrefix = string.IsNullOrEmpty(targetDir) ? string.Empty : targetDir + "/";

        var files = new List<IFileInfo>();
        var seenSubDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in this.directories)
        {
            if (string.IsNullOrEmpty(dirPrefix) || (dir.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase) && dir.Length > dirPrefix.Length))
            {
                var rel = string.IsNullOrEmpty(dirPrefix) ? dir : dir[dirPrefix.Length..];
                var subDir = rel.Split('/')[0];
                if (seenSubDirs.Add(subDir)) files.Add(new PluginVirtualDirectoryInfo(subDir, this.lastModified));
            }
        }

        foreach (var (path, item) in this.fileMap)
        {
            if (string.IsNullOrEmpty(dirPrefix) ? !path.Contains('/') : path.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var rel = string.IsNullOrEmpty(dirPrefix) ? path : path[dirPrefix.Length..];
                if (!rel.Contains('/'))
                    files.Add(new PluginFileInfo(this.assembly, item.ResourceName, Path.GetFileName(path), item.Length, this.lastModified));
            }
        }

        return files.Count > 0 || string.IsNullOrEmpty(targetDir) || this.directories.Contains(targetDir)
            ? new PluginDirectoryContents(files, true)
            : new PluginDirectoryContents([], false);
    }

    /// <inheritdoc/>
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    /// <summary>
    /// 가상 하위 경로를 루트 폴더와 결합하여 정규화합니다.
    /// </summary>
    private string? ResolvePath(string? subpath)
    {
        if (string.IsNullOrWhiteSpace(subpath)) return this.rootFolder;
        var clean = subpath.Trim().TrimStart('/', '\\').Replace('\\', '/');
        return string.IsNullOrEmpty(this.rootFolder) ? clean : $"{this.rootFolder}/{clean}".TrimEnd('/');
    }

    /// <summary>
    /// 어셈블리의 Manifest.xml 또는 매니페스트 리소스 이름을 스캔하여 파일 및 폴더 맵을 초기화합니다.
    /// </summary>
    private void InitializeResourceMap()
    {
        if (this.TryLoadFromManifest()) return;

        var baseNamespace = this.assembly.GetName().Name ?? string.Empty;
        foreach (var name in this.assembly.GetManifestResourceNames())
        {
            if (name.Equals(ManifestResourceName, StringComparison.OrdinalIgnoreCase)) continue;
            var path = ConvertResourceNameToPath(name, baseNamespace);
            if (string.IsNullOrEmpty(path)) continue;

            long length = 0;
            using (var stream = this.assembly.GetManifestResourceStream(name)) length = stream?.Length ?? 0;
            this.fileMap[path] = (name, length);
            this.RegisterParentDirectories(path);
        }
    }

    /// <summary>
    /// GenerateEmbeddedFilesManifest 로 생성된 Manifest.xml 이 있는 경우 이를 파싱하여 정확한 경로로 등록합니다.
    /// </summary>
    private bool TryLoadFromManifest()
    {
        using var stream = this.assembly.GetManifestResourceStream(ManifestResourceName);
        if (stream is null) return false;

        try
        {
            var doc = XDocument.Load(stream);
            foreach (var elem in doc.Descendants("File"))
            {
                var path = elem.Attribute("Path")?.Value?.Replace('\\', '/');
                var resName = elem.Attribute("ResourcePath")?.Value;
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(resName)) continue;

                long length = 0;
                using (var resStream = this.assembly.GetManifestResourceStream(resName)) length = resStream?.Length ?? 0;
                this.fileMap[path] = (resName, length);
                this.RegisterParentDirectories(path);
            }
            return this.fileMap.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 경로의 모든 부모 디렉터리 경로를 집합에 등록합니다.
    /// </summary>
    private void RegisterParentDirectories(string path)
    {
        var idx = path.LastIndexOf('/');
        while (idx > 0)
        {
            var dir = path[..idx];
            this.directories.Add(dir);
            idx = dir.LastIndexOf('/');
        }
    }

    /// <summary>
    /// 표준 리소스 이름을 가상 경로 문자열로 변환합니다 (Manifest 가 없을 때의 폴백).
    /// </summary>
    private static string ConvertResourceNameToPath(string resourceName, string baseNamespace)
    {
        var rel = resourceName.StartsWith(baseNamespace + ".", StringComparison.OrdinalIgnoreCase)
            ? resourceName[(baseNamespace.Length + 1)..]
            : resourceName;

        var lastDot = rel.LastIndexOf('.');
        if (lastDot <= 0) return rel;

        var ext = rel[lastDot..];
        var dirPart = rel[..lastDot].Replace('.', '/');
        return dirPart + ext;
    }
}
