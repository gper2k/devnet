using Microsoft.AspNetCore.Mvc;

namespace BitPlus.Web.Site.Controllers.API;

public partial class ManageController
{
    #region Configuration (ConfigController 위임)

    /// <summary>
    /// 사이트 기본 정보(사이트명, 키워드, 설명, 로고 등)를 조회합니다.
    /// <see cref="ConfigController.GetSite"/>에 직접 위임합니다.
    /// </summary>
    /// <returns>사이트 기본 설정 정보</returns>
    [HttpGet("config/site")]
    public IActionResult GetSiteConfig() => this.ConfigCtrl.GetSite();

    /// <summary>
    /// 시스템 다국어 사전 항목을 조회합니다.
    /// <see cref="ConfigController.GetLanguages"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>다국어 목록</returns>
    [HttpGet("config/languages")]
    public Task<IActionResult> GetLanguages([FromQuery] string? keyword = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => this.ConfigCtrl.GetLanguages(keyword, page, pageSize);

    #endregion
}
