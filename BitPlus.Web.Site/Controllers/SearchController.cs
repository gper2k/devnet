using BitPlus.Web.HTTP.View;
using BitPlus.Web.Search;
using BitPlus.Web.Site.Models;
using Microsoft.AspNetCore.Mvc;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 통합 검색 페이지 뷰 컨트롤러 (<c>/search</c>). 등록된 통합 검색 공급자(<see cref="ISearchProvider"/>)마다 결과를 구분해 보여 줍니다.
/// </summary>
public class SearchController : ViewControllerBase
{
    /// <summary>전체 보기에서 구분마다 보여 주는 항목 수</summary>
    private const int GroupSize = 5;

    /// <summary>구분 하나를 고른 보기의 페이지 크기</summary>
    private const int PageSize = 20;

    /// <summary>
    /// <see cref="SearchController"/> 기본 생성자.
    /// </summary>
    public SearchController() : base() { }

    /// <summary>
    /// 통합 검색 페이지: 검색어가 있으면 공급자를 표시 순서대로 불러 결과를 모읍니다.
    /// 구분을 고르지 않으면(전체) 구분마다 앞의 5건을, 고르면 그 구분을 20건씩 페이지로 보여 주고 다른 구분은 건수만 셉니다.
    /// </summary>
    /// <param name="q">검색어 (100자까지)</param>
    /// <param name="type">구분 키 (없거나 모르는 값이면 전체)</param>
    /// <param name="page">고른 구분의 페이지 (1부터)</param>
    /// <returns>통합 검색 뷰</returns>
    [HttpGet("search")]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? type, [FromQuery] int page = 1)
    {
        this.SetTitle(this.Request.GetLanguage("IntegratedSearch") ?? "Search");
        var providers = this.HttpContext.RequestServices.GetServices<ISearchProvider>().OrderBy(p => p.Order).ToList();
        var model = new SearchPageModel { Keyword = q.Clip(100), Type = providers.Any(p => p.Name == type) ? type : null, Page = Math.Max(page, 1) };
        model.PageSize = model.Type is null ? GroupSize : PageSize;
        model.Providers = [.. providers.Select(p => (p.Name, p.TitleOf(this.HttpContext)))];
        if (model.Keyword.Length == 0) return this.View(model);

        foreach (var provider in providers) // 같은 요청의 DbContext 를 쓰므로 차례로 부름
        {
            var mine = model.Type is null || provider.Name == model.Type;
            var result = await provider.SearchAsync(new SearchQuery(model.Keyword, mine ? model.Page : 1, mine ? model.PageSize : 0, this.HttpContext), this.HttpContext.RequestAborted);
            model.Groups.Add((provider.Name, provider.TitleOf(this.HttpContext), result));
        }
        return this.View(model);
    }
}
