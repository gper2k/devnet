using BitPlus.Web.HTTP.View;
using Microsoft.AspNetCore.Mvc;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 사이트 메인 홈 및 기본 안내 페이지를 렌더링하는 MVC 뷰 컨트롤러입니다.
/// </summary>
public class HomeController : ViewControllerBase
{
    /// <summary>
    /// <see cref="HomeController"/> 기본 생성자.
    /// </summary>
    public HomeController() : base() { }

    /// <summary>
    /// 사이트 메인 인덱스 페이지 뷰를 반환합니다.
    /// </summary>
    /// <returns>인덱스 뷰 결과</returns>
    [HttpGet]
    public IActionResult Index()
    {
        this.SetTitle("Home");
        this.SetPageKey("Home");
        return this.View();
    }

    /// <summary>
    /// 디자인 시스템 및 컨트롤 UI 샘플 페이지 뷰를 반환합니다.
    /// </summary>
    /// <returns>샘플 뷰 결과</returns>
    [HttpGet]
    public IActionResult Sample()
    {
        this.SetTitle("Medical Dashboard & Design System");
        this.SetPageKey("Sample");
        return this.View();
    }
}
