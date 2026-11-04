using BitPlus.Web.HTTP.API;
using BitPlus.Web.Security.Policy;
using BitPlus.Web.Services;
using Microsoft.AspNetCore.Mvc;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 관리자 "서비스" 화면용 Web API 컨트롤러 (api/service). 서비스 목록·상태 조회, 활성 전환, 서비스별 설정(양식 입력값) 조회·저장을 맡으며 관리자 정책만 쓸 수 있습니다.
/// 설정 양식은 서비스마다 다르므로(<c>Views/Manager/Service/Forms/{이름}.cshtml</c>) 저장은 폼 필드 전체를 입력값으로 받습니다.
/// </summary>
[Auth(WebConfig.ManagerPolicyName)]
public class ServiceController : APIControllerBase
{
    /// <summary>설정 입력값 하나의 최대 길이</summary>
    private const int MaxValueLength = 4000;

    /// <summary>설정 입력값 최대 개수</summary>
    private const int MaxValueCount = 50;

    /// <summary>
    /// <see cref="ServiceController"/> 기본 생성자.
    /// </summary>
    public ServiceController() : base() { }

    /// <summary>서비스 매니저 (설치 전이면 null)</summary>
    private ServiceManager? Manager => this.HttpContext.GetService<ServiceManager>();

    /// <summary>
    /// 서비스 목록과 상태를 조회합니다 (이름순).
    /// </summary>
    /// <returns>[{name, title, description, state, events, queued, processed, lastRun, lastError, lastErrorAt}]</returns>
    [HttpGet]
    public IActionResult GetList() => this.Complete(this.Manager?.Services.Select(p => new
    {
        p.Name, p.Title, description = this.Lang(p.Description), state = p.State.ToString(), p.Events,
        p.Queued, p.Processed, p.LastRun, p.LastError, p.LastErrorAt
    }) ?? []);

    /// <summary>
    /// 서비스 하나의 저장 설정을 조회합니다.
    /// </summary>
    /// <param name="name">서비스 이름</param>
    /// <returns>{enabler, values}</returns>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetSetting(string name)
    {
        if (this.Manager?.Find(name) is not { } service) return this.NotFoundResult();
        var setting = await this.Manager.SettingOfAsync(service);
        return this.Complete(new { setting.Enabler, setting.Values });
    }

    /// <summary>
    /// 서비스의 활성 여부만 바꿉니다 (설정 입력값은 그대로). 비활성으로 바꾸면 쌓인 작업을 버립니다.
    /// </summary>
    /// <param name="name">서비스 이름</param>
    /// <param name="enabler">활성 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPut("{name}/enabler")]
    public async Task<IActionResult> SaveEnabler(string name, [FromForm] bool enabler)
    {
        if (this.Manager?.Find(name) is not { } service) return this.NotFoundResult();
        var setting = await this.Manager.SettingOfAsync(service);
        setting.Enabler = enabler;
        await this.Manager.SaveAsync(this.DbContext!, service, setting);
        return this.Complete(message: WebResources.SavedComplate);
    }

    /// <summary>
    /// 서비스 설정을 저장합니다: 활성 여부와 설정 양식의 입력값(enabler 를 뺀 모든 폼 필드, 같은 이름이 여럿이면 쉼표로 묶음). 저장하면 서비스가 설정을 다시 읽습니다.
    /// </summary>
    /// <param name="name">서비스 이름</param>
    /// <param name="enabler">활성 여부</param>
    /// <returns>처리 결과</returns>
    [HttpPut("{name}")]
    public async Task<IActionResult> SaveSetting(string name, [FromForm] bool enabler)
    {
        if (this.Manager?.Find(name) is not { } service) return this.NotFoundResult();
        var fields = this.Request.Form.Where(p => !p.Key.Equals("enabler", StringComparison.OrdinalIgnoreCase)).ToList();
        if (fields.Count > MaxValueCount) return this.Error(this.Lang("MaxCountOf").Formats(this.Lang("Settings"), MaxValueCount.ToString())!);
        var setting = new ServiceSetting { Enabler = enabler };
        foreach (var (key, value) in fields) setting.Values[key] = value.ToString().Clip(MaxValueLength);
        await this.Manager.SaveAsync(this.DbContext!, service, setting);
        return this.Complete(message: WebResources.SavedComplate);
    }

}
