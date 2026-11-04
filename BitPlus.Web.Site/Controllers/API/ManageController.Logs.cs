using BitPlus.Web.Site.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace BitPlus.Web.Site.Controllers.API;

public partial class ManageController
{
    #region Logs & Statistics (LogController 및 VisitCounter 위임)

    /// <summary>
    /// 서버에 기록된 시스템 로그 파일 목록을 조회합니다.
    /// <see cref="LogController.Files"/>에 직접 위임합니다.
    /// </summary>
    /// <returns>로그 파일 목록</returns>
    [HttpGet("logs/files")]
    public IActionResult GetLogFiles() => this.LogCtrl.Files();

    /// <summary>
    /// 지정한 로그 파일의 세부 항목을 필터링하여 조회합니다.
    /// <see cref="LogController.Entries"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="file">로그 파일 상대 경로</param>
    /// <param name="offset">읽기 시작할 바이트 위치 (기본 0)</param>
    /// <param name="from">시작 시각 (HH:mm:ss)</param>
    /// <param name="to">종료 시각 (HH:mm:ss)</param>
    /// <param name="level">최소 로그 수준 (Information, Warning, Error 등)</param>
    /// <param name="keyword">검색 키워드</param>
    /// <param name="take">가져올 항목 수 (기본 100)</param>
    /// <returns>로그 항목 목록 및 다음 읽기 위치</returns>
    [HttpGet("logs/entries")]
    public IActionResult GetLogEntries(
        [FromQuery] string? file,
        [FromQuery] long offset = 0,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? level = null,
        [FromQuery] string? keyword = null,
        [FromQuery] int take = 100)
        => this.LogCtrl.Entries(file, offset, from, to, level, keyword, take);

    /// <summary>
    /// 최근 N일간의 일별 페이지뷰 및 순방문자 통계를 조회합니다.
    /// </summary>
    /// <param name="days">조회 일수 (기본 7일, 최대 60일)</param>
    /// <returns>일자별 방문 통계 목록</returns>
    [HttpGet("logs/visits")]
    public IActionResult GetVisits([FromQuery] int days = 7)
    {
        days = Math.Clamp(days, 1, 60);
        var today = VisitCounter.Today;
        var list = new List<object>();

        for (var i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var stats = this.Counter?.Day(date);
            list.Add(new
            {
                date = date.ToString("yyyy-MM-dd"),
                pages = stats?.Pages ?? 0,
                visitors = stats?.Visitors ?? 0
            });
        }

        return this.Complete(new { items = list });
    }

    #endregion
}
