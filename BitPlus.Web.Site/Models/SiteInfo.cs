namespace BitPlus.Web.Site.Models;

/// <summary>
/// 사이트 기본 정보 (설정 "SiteInfo", AppConfig 범주 — 캐시 → DB → appsettings). 관리자 "시스템 설정 > 사이트 정보" 에서 저장하고,
/// 레이아웃이 문서 머리(제목·검색 키워드·설명·파비콘·공유 미리보기)와 화면 상단·하단(로고·사이트명·회사 정보·카피라이트) 및 활성 레이아웃 결정에 씁니다.
/// 저장된 값이 없는 항목은 기본값을 씁니다.
/// </summary>
public class SiteInfo
{
    /// <summary>설정 키</summary>
    public const string Key = "SiteInfo";

    /// <summary>
    /// <see cref="SiteInfo"/> 기본 생성자 (기본값)
    /// </summary>
    public SiteInfo()
    {
        this.Title = "BitPlus";
        this.Keywords = string.Empty;
        this.Description = string.Empty;
        this.Favicon = "/img/infinity-outlined.svg";
        this.ShareImage = string.Empty;
        this.Logo = "/img/infinity-outlined.svg";
        this.SiteName = "Dev<b>Net</b> <small>.biz</small>";
        this.Slogan = "Connecting technologies, automating workflows, and driving continuous business growth.";
        this.CompanyName = string.Empty;
        this.Phone = "+82-01-123-4567";
        this.Email = "admin@bitplus.kr";
        this.Address = Properties.Resources.SiteSampleAddress;
        this.Copyright = "Copyright &copy; <b>BitPlus.kr</b>. All Rights Reserved.";
        this.GeneralLayout = "Default";
        this.AccountLayout = "Default";
        this.Editor = string.Empty;
    }

    /// <summary>사이트 기본 제목 (문서 제목 뒤에 붙음: "페이지 제목 - 사이트 제목")</summary>
    public string Title { get; set; }

    /// <summary>검색 키워드 (meta keywords, 쉼표로 구분)</summary>
    public string Keywords { get; set; }

    /// <summary>사이트 설명 (meta description, 공유 미리보기 설명의 기본값)</summary>
    public string Description { get; set; }

    /// <summary>파비콘 경로</summary>
    public string Favicon { get; set; }

    /// <summary>공유 미리보기 기본 이미지 경로 (og:image, 비우면 로고)</summary>
    public string ShareImage { get; set; }

    /// <summary>상단·하단 로고 이미지 경로</summary>
    public string Logo { get; set; }

    /// <summary>상단·하단 사이트명 (간단한 강조 태그 b·small 등 허용, 저장할 때 정제)</summary>
    public string SiteName { get; set; }

    /// <summary>하단 로고 아래 소개 문구</summary>
    public string Slogan { get; set; }

    /// <summary>회사명 (하단 연락처 위)</summary>
    public string CompanyName { get; set; }

    /// <summary>회사 전화</summary>
    public string Phone { get; set; }

    /// <summary>회사 메일</summary>
    public string Email { get; set; }

    /// <summary>회사 주소</summary>
    public string Address { get; set; }

    /// <summary>하단 카피라이트 (간단한 강조 태그 허용, 저장할 때 정제)</summary>
    public string Copyright { get; set; }

    /// <summary>일반 페이지 레이아웃 이름 (Views/Layouts/General/{이름}.cshtml, 기본값 "Default")</summary>
    public string GeneralLayout { get; set; }

    /// <summary>인증 페이지 레이아웃 이름 (Views/Layouts/Account/{이름}.cshtml, 기본값 "Default")</summary>
    public string AccountLayout { get; set; }

    /// <summary>주 에디터: 모든 글쓰기 화면이 쓰는 에디터 공급자 이름 (site.ts 에 등록된 이름, 예: jodit·suneditor). 비우면 클라이언트 기본 에디터</summary>
    public string Editor { get; set; }

    /// <summary>일반 레이아웃 가상 경로</summary>
    public string GeneralLayoutPath => ResolveLayout("General", this.GeneralLayout, "~/Views/Layouts/General/Default/Layout.cshtml");

    /// <summary>인증 레이아웃 가상 경로</summary>
    public string AccountLayoutPath => ResolveLayout("Account", this.AccountLayout, "~/Views/Layouts/Account/Default/Layout.cshtml");

    /// <summary>어셈블리에 사전 컴파일된 모든 Razor 뷰 식별자 캐시 (예외 방어용 지연 로딩)</summary>
    private static readonly Lazy<HashSet<string>> CompiledViews = new(() =>
    {
        try
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var loader = new Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemLoader();
            IEnumerable<System.Reflection.Assembly> assemblies;
            try { assemblies = Plugins.PluginManager.Instance.AllAssemblies; }
            catch { assemblies = [typeof(SiteInfo).Assembly]; }

            foreach (var asm in assemblies)
            {
                try
                {
                    foreach (var item in loader.LoadItems(asm))
                    {
                        var id = item.Identifier?.TrimStart('~') ?? string.Empty;
                        if (!string.IsNullOrEmpty(id)) set.Add(id);
                    }
                }
                catch { }
            }
            return set;
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    });

    /// <summary>
    /// 가상 경로의 Razor 뷰가 컴파일된 어셈블리 또는 로컬 물리 디스크에 존재하는지 확인합니다.
    /// </summary>
    /// <param name="virtualPath">가상 경로 (예: ~/Views/Layouts/General/Default/Layout.cshtml)</param>
    /// <returns>존재 여부</returns>
    public static bool FileExistsOrCompiled(string virtualPath)
    {
        var normalized = virtualPath.TrimStart('~');
        if (CompiledViews.Value.Contains(normalized)) return true;

        var rel = normalized.TrimStart('/');
        var appPath = App.AppPath(rel);
        if (File.Exists(appPath)) return true;

        try
        {
            var physical = WebConfig.MapPath(virtualPath);
            if (File.Exists(physical)) return true;
        }
        catch { }

        return false;
    }

    /// <summary>
    /// 지정된 범주 및 레이아웃의 설정 뷰(Config.cshtml)가 존재하는지 여부를 확인합니다.
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <returns>설정 뷰 존재 여부</returns>
    public static bool HasConfig(string category, string? name)
    {
        var layoutName = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        var virtualPath = $"~/Views/Layouts/{category}/{layoutName}/Config.cshtml";
        return FileExistsOrCompiled(virtualPath);
    }

    /// <summary>
    /// 레이아웃 경로를 해석합니다. Views/Layouts/{category}/{name}/Layout.cshtml 이 존재하면 해당 경로를, 없으면 단일 파일 형식 또는 fallback 을 반환합니다.
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <param name="name">레이아웃 이름</param>
    /// <param name="fallback">파일 미존재 시 폴백 경로</param>
    /// <returns>레이아웃 가상 경로</returns>
    public static string ResolveLayout(string category, string? name, string fallback)
    {
        var layoutName = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();

        // 1. 신규 폴더 구조: ~/Views/Layouts/{category}/{name}/Layout.cshtml
        var folderLayout = $"~/Views/Layouts/{category}/{layoutName}/Layout.cshtml";
        if (FileExistsOrCompiled(folderLayout)) return folderLayout;

        // 2. 단일 파일 구조 (하위 호환): ~/Views/Layouts/{category}/{name}.cshtml
        var fileLayout = $"~/Views/Layouts/{category}/{layoutName}.cshtml";
        if (FileExistsOrCompiled(fileLayout)) return fileLayout;

        // 3. 사용 가능한 레이아웃 목록에 존재하면 폴더 레이아웃 경로 반환
        if (AvailableLayouts(category).Any(l => string.Equals(l, layoutName, StringComparison.OrdinalIgnoreCase)))
            return folderLayout;

        return fallback;
    }

    /// <summary>
    /// 지정된 범주(General, Account)의 사용 가능한 레이아웃 이름 목록을 조회합니다.
    /// 컴파일된 Razor 어셈블리 뷰와 로컬 물리 디렉터리를 모두 검색하여 배포(Publish) 환경에서도 정상 작동합니다.
    /// </summary>
    /// <param name="category">레이아웃 범주 (General, Account)</param>
    /// <returns>사용 가능한 레이아웃 이름 목록</returns>
    public static List<string> AvailableLayouts(string category)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefix = $"/Views/Layouts/{category}/";

        // 1. 사전 컴파일된 Razor 뷰 캐시에서 스캔
        try
        {
            foreach (var id in CompiledViews.Value)
            {
                if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                var rel = id[prefix.Length..];
                var parts = rel.Split('/');
                if (parts.Length >= 2 && parts[1].Equals("Layout.cshtml", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = parts[0];
                    if (!string.IsNullOrEmpty(dir) && !dir.StartsWith('_')) names.Add(dir);
                }
                else if (parts.Length == 1 && rel.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                {
                    var file = Path.GetFileNameWithoutExtension(rel);
                    if (!string.IsNullOrEmpty(file) && !file.StartsWith('_')) names.Add(file);
                }
            }
        }
        catch { }

        // 2. 로컬 물리 디스크 스캔 (개발 환경 및 비컴파일 파일)
        // 2-1. 애플리케이션 루트 하위 Views/Layouts/{category}
        try
        {
            var appFolder = App.AppPath($"Views/Layouts/{category}");
            if (Directory.Exists(appFolder))
            {
                foreach (var dir in Directory.EnumerateDirectories(appFolder))
                {
                    var dirName = Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(dirName) && !dirName.StartsWith('_') && File.Exists(Path.Combine(dir, "Layout.cshtml")))
                        names.Add(dirName);
                }
                foreach (var file in Directory.EnumerateFiles(appFolder, "*.cshtml"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrEmpty(fileName) && !fileName.StartsWith('_')) names.Add(fileName);
                }
            }
        }
        catch { }

        // 2-2. WebConfig.MapPath 하위 (혹시 배포 시 wwwroot 로 이동된 경우)
        try
        {
            var folder = WebConfig.MapPath($"~/Views/Layouts/{category}");
            if (Directory.Exists(folder))
            {
                foreach (var dir in Directory.EnumerateDirectories(folder))
                {
                    var dirName = Path.GetFileName(dir);
                    if (!string.IsNullOrEmpty(dirName) && !dirName.StartsWith('_') && File.Exists(Path.Combine(dir, "Layout.cshtml")))
                        names.Add(dirName);
                }
                foreach (var file in Directory.EnumerateFiles(folder, "*.cshtml"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrEmpty(fileName) && !fileName.StartsWith('_')) names.Add(fileName);
                }
            }
        }
        catch { }

        var list = names.OrderBy(p => p).ToList();
        return list.Count > 0 ? list : ["Default"];
    }

    /// <summary>
    /// 저장된 사이트 정보 (없으면 기본값). 설정 조회는 캐시를 거치므로 매 요청 호출해도 됩니다.
    /// 설치 전(설치 마법사 화면)처럼 DB 를 쓸 수 없어 읽기에 실패하면 기본값입니다.
    /// </summary>
    /// <param name="config">웹 설정 (없으면 기본값)</param>
    public static SiteInfo Of(WebConfig? config)
    {
        try { return config?.GetModel<SiteInfo>(Key) ?? new SiteInfo(); }
        catch (Exception) { return new SiteInfo(); }
    }
}
