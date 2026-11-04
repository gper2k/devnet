using BitPlus.Web.Site.Models;
using BitPlus.Web.Site.Widgets;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Resources;
using System.Runtime.Loader;
using System.Text.Json.Nodes;

namespace BitPlus.Web.Site.Plugins;

/// <summary>
/// 외부 플러그인 어셈블리(*.dll)를 로드하고 레이아웃, 위젯, 페이지, 서비스, 정적 리소스, 다국어 리소스를 통합 관리하는 싱글턴 매니저입니다.
/// </summary>
public class PluginManager
{
    /// <summary>플러그인 기본 폴더 (콘텐츠 루트 기준)</summary>
    public const string PluginFolder = "Plugins";

    private static PluginManager? instance;
    private readonly List<Assembly> plugins;
    private readonly List<Assembly> allAssemblies;
    private readonly List<PluginEmbeddedFileProvider> fileProviders;
    private readonly List<PluginEmbeddedFileProvider> wwwrootProviders;
    private readonly List<ResourceManager> resourceManagers;

    /// <summary>
    /// <see cref="PluginManager"/> 비공개 생성자.
    /// </summary>
    private PluginManager(string contentRootPath)
    {
        this.plugins = [];
        this.fileProviders = [];
        this.wwwrootProviders = [];
        this.resourceManagers = [];

        this.LoadPlugins(contentRootPath);
        this.allAssemblies = [.. new[] { typeof(SiteInfo).Assembly }.OfType<Assembly>(), .. this.plugins];
        this.InitializeProviders();
        this.InitializeResourceManagers();
    }

    /// <summary>전역 싱글턴 인스턴스</summary>
    public static PluginManager Instance => instance ?? throw new InvalidOperationException("PluginManager is not initialized. Call PluginManager.Init() first.");

    /// <summary>로드된 플러그인 어셈블리 목록</summary>
    public IReadOnlyList<Assembly> Plugins => this.plugins;

    /// <summary>사이트 진입 어셈블리와 모든 플러그인 어셈블리를 포함한 전체 목록</summary>
    public IReadOnlyList<Assembly> AllAssemblies => this.allAssemblies;

    /// <summary>플러그인 어셈블리에서 발견된 다국어 리소스 매니저 목록</summary>
    public IReadOnlyList<ResourceManager> ResourceManagers => this.resourceManagers;

    /// <summary>
    /// 플러그인 매니저를 초기화하고 인스턴스를 반환합니다.
    /// </summary>
    /// <param name="contentRootPath">웹 호스트 콘텐츠 루트 경로</param>
    public static PluginManager Init(string contentRootPath) => instance ??= new PluginManager(contentRootPath);

    /// <summary>
    /// 호스트 웹 루트 파일 프로바이더와 각 플러그인의 내장 wwwroot 파일 프로바이더를 결합한 복합 프로바이더를 생성합니다.
    /// </summary>
    /// <param name="webRootFileProvider">호스트 웹 루트 파일 프로바이더</param>
    public IFileProvider GetCompositeFileProvider(IFileProvider webRootFileProvider)
    {
        if (this.wwwrootProviders.Count == 0) return webRootFileProvider;
        List<IFileProvider> list = [webRootFileProvider, .. this.wwwrootProviders];
        return new CompositeFileProvider(list);
    }

    /// <summary>
    /// 플러그인 어셈블리에서 특정 위젯의 widget.json 을 읽습니다.
    /// </summary>
    /// <param name="key">위젯 고유 키</param>
    public JsonObject? GetWidgetJson(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var relPath = $"Views/Widgets/{key}/widget.json";

        foreach (var provider in this.fileProviders)
        {
            var info = provider.GetFileInfo(relPath);
            if (!info.Exists) continue;
            try
            {
                using var stream = info.CreateReadStream();
                if (JsonNode.Parse(stream) is JsonObject json) return json;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// 모든 플러그인에 내장된 위젯(Views/Widgets/{키}/widget.json) 목록을 스캔하여 반환합니다.
    /// </summary>
    public Dictionary<string, JsonObject> GetPluginWidgets()
    {
        var result = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in this.fileProviders)
        {
            var dir = provider.GetDirectoryContents("Views/Widgets");
            if (!dir.Exists) continue;

            foreach (var item in dir)
            {
                if (!item.IsDirectory || !WidgetCatalog.KeyPattern().IsMatch(item.Name)) continue;
                var fileInfo = provider.GetFileInfo($"Views/Widgets/{item.Name}/widget.json");
                if (!fileInfo.Exists) continue;
                try
                {
                    using var stream = fileInfo.CreateReadStream();
                    if (JsonNode.Parse(stream) is JsonObject json) result[item.Name] = json;
                }
                catch { }
            }
        }
        return result;
    }

    /// <summary>
    /// 모든 플러그인에 내장된 섹션(Views/Sections/{키}/section.json) 목록을 스캔하여 반환합니다.
    /// </summary>
    public Dictionary<string, JsonObject> GetPluginSections()
    {
        var result = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in this.fileProviders)
        {
            var dir = provider.GetDirectoryContents("Views/Sections");
            if (!dir.Exists) continue;

            foreach (var item in dir)
            {
                if (!item.IsDirectory || item.Name.StartsWith('_') || !WidgetCatalog.KeyPattern().IsMatch(item.Name)) continue;
                var fileInfo = provider.GetFileInfo($"Views/Sections/{item.Name}/section.json");
                if (!fileInfo.Exists) continue;
                try
                {
                    using var stream = fileInfo.CreateReadStream();
                    if (JsonNode.Parse(stream) is JsonObject json) result[item.Name] = json;
                }
                catch { }
            }
        }
        return result;
    }

    /// <summary>
    /// 플러그인 어셈블리에서 특정 페이지의 정의 JSON(Views/Sections/_Pages/{page}.json)을 읽습니다.
    /// </summary>
    /// <param name="page">페이지 키</param>
    public JsonObject? GetPageDefinitionJson(string page)
    {
        if (string.IsNullOrWhiteSpace(page)) return null;
        var relPath = $"Views/Sections/_Pages/{page}.json";

        foreach (var provider in this.fileProviders)
        {
            var info = provider.GetFileInfo(relPath);
            if (!info.Exists) continue;
            try
            {
                using var stream = info.CreateReadStream();
                if (JsonNode.Parse(stream) is JsonObject json) return json;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// 모든 플러그인에 내장된 페이지 정의(Views/Sections/_Pages/*.json) 목록을 스캔하여 반환합니다.
    /// </summary>
    public Dictionary<string, JsonObject> GetPluginPageDefinitions()
    {
        var result = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in this.fileProviders)
        {
            var dir = provider.GetDirectoryContents("Views/Sections/_Pages");
            if (!dir.Exists) continue;

            foreach (var item in dir)
            {
                if (item.IsDirectory || !item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                var page = Path.GetFileNameWithoutExtension(item.Name);
                if (!WidgetCatalog.KeyPattern().IsMatch(page)) continue;
                try
                {
                    using var stream = item.CreateReadStream();
                    if (JsonNode.Parse(stream) is JsonObject json) result[page] = json;
                }
                catch { }
            }
        }
        return result;
    }

    /// <summary>
    /// Plugins 폴더 및 하위 폴더의 모든 DLL 을 어셈블리로 로드합니다.
    /// </summary>
    private void LoadPlugins(string contentRootPath)
    {
        var folder = Path.Combine(contentRootPath, PluginFolder);
        if (!Directory.Exists(folder)) return;

        var loadedNames = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(p => p.GetName().Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var asmName = AssemblyName.GetAssemblyName(file);
                if (asmName.Name is not null && loadedNames.Contains(asmName.Name)) continue;

                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                this.plugins.Add(asm);
                if (asmName.Name is not null) loadedNames.Add(asmName.Name);
            }
            catch (Exception ex) when (ex is FileLoadException or BadImageFormatException)
            {
                System.Diagnostics.Trace.TraceWarning($"Plugin load skipped [{file}]: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 각 플러그인 어셈블리에 대한 전체 내장 리소스 및 wwwroot 가상 파일 프로바이더를 구성합니다.
    /// </summary>
    private void InitializeProviders()
    {
        foreach (var asm in this.plugins)
        {
            this.fileProviders.Add(new PluginEmbeddedFileProvider(asm));
            this.wwwrootProviders.Add(new PluginEmbeddedFileProvider(asm, "wwwroot"));
        }
    }

    /// <summary>
    /// 각 플러그인 어셈블리 내에 정의된 다국어 리소스 매니저(ResourceManager)를 자동 추출합니다.
    /// </summary>
    private void InitializeResourceManagers()
    {
        foreach (var asm in this.plugins)
        {
            try
            {
                foreach (var type in asm.GetTypes())
                {
                    var prop = type.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (prop?.PropertyType == typeof(ResourceManager) && prop.GetValue(null) is ResourceManager rm)
                    {
                        this.resourceManagers.Add(rm);
                    }
                }
            }
            catch { }
        }
    }
}
