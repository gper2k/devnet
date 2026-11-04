using BitPlus.Web.Site.Models.Manage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BitPlus.Web.Site.Controllers.API;

public partial class ManageController
{
    #region Content (ContentManager 및 ContentController 위임)

    /// <summary>
    /// 새 글을 등록하거나 기존 글을 수정합니다.
    /// <see cref="BitPlus.Web.Data.Content.ContentManager.SavePostAsync"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="request">글 저장 요청 모델</param>
    /// <returns>생성/수정된 글 번호 및 이동 URL</returns>
    [HttpPost("content/post")]
    public async Task<IActionResult> SavePost([FromBody] ManagePostSaveRequest request)
    {
        var values = new Dictionary<string, object?>
        {
            ["ruleNo"] = request.RuleNo,
            ["contentNo"] = request.ContentNo,
            ["title"] = request.Title,
            ["body"] = request.Body,
            ["category"] = request.Category,
            ["isNotice"] = request.IsNotice,
            ["published"] = request.Published,
            ["culture"] = request.Culture
        };

        if (request.Tags is { Count: > 0 }) values["tags"] = JsonSerializer.Serialize(request.Tags);
        if (request.Links is { Count: > 0 }) values["links"] = JsonSerializer.Serialize(request.Links);

        return this.Respond(await this.Manager.SavePostAsync(values));
    }

    /// <summary>
    /// 게시판 글 목록을 검색 조건과 함께 페이지 단위로 조회합니다.
    /// <see cref="BitPlus.Web.Data.Content.ContentManager.ListPostsAsync"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="ruleNo">게시판 규칙 번호</param>
    /// <param name="q">검색어 (제목, 내용, 작성자 등 동시 검색)</param>
    /// <param name="category">카테고리 분류 (선택)</param>
    /// <param name="page">페이지 번호 (1부터 시작, 기본 1)</param>
    /// <param name="size">페이지당 글 수 (기본 10)</param>
    /// <returns>글 목록 및 페이징 정보</returns>
    [HttpGet("content/posts")]
    public async Task<IActionResult> ListPosts([FromQuery] int ruleNo, [FromQuery] string? q = null, [FromQuery] string? category = null, [FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var values = new Dictionary<string, object?>
        {
            ["ruleNo"] = ruleNo,
            ["q"] = q,
            ["category"] = category,
            ["page"] = Math.Max(page, 1),
            ["size"] = Math.Clamp(size, 1, 100)
        };
        return this.Respond(await this.Manager.ListPostsAsync(values));
    }

    /// <summary>
    /// 단일 글의 상세 내용 및 본문을 조회합니다.
    /// <see cref="BitPlus.Web.Data.Content.ContentManager.LoadPostAsync"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="ruleNo">게시판 규칙 번호</param>
    /// <param name="contentNo">글 번호</param>
    /// <returns>글 상세 정보</returns>
    [HttpGet("content/post/{ruleNo:int}/{contentNo:long}")]
    public async Task<IActionResult> GetPost(int ruleNo, long contentNo)
    {
        var values = new Dictionary<string, object?>
        {
            ["ruleNo"] = ruleNo,
            ["contentNo"] = contentNo
        };
        return this.Respond(await this.Manager.LoadPostAsync(values));
    }

    /// <summary>
    /// 글을 삭제합니다.
    /// <see cref="BitPlus.Web.Data.Content.ContentManager.DeletePostAsync"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="ruleNo">게시판 규칙 번호</param>
    /// <param name="contentNo">삭제할 글 번호</param>
    /// <returns>삭제 처리 결과</returns>
    [HttpDelete("content/post/{ruleNo:int}/{contentNo:long}")]
    public async Task<IActionResult> DeletePost(int ruleNo, long contentNo)
    {
        var values = new Dictionary<string, object?>
        {
            ["ruleNo"] = ruleNo,
            ["contentNo"] = contentNo
        };
        return this.Respond(await this.Manager.DeletePostAsync(values));
    }

    /// <summary>
    /// 사이트에 등록된 게시판(규칙) 목록을 조회합니다.
    /// <see cref="ContentController.GetRules"/>에 직접 위임합니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>게시판 목록</returns>
    [HttpGet("content/boards")]
    public Task<IActionResult> GetBoards([FromQuery] string? keyword = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => this.ContentCtrl.GetRules(keyword, page, pageSize);

    /// <summary>
    /// 글의 언어별 번역(원문 언어, 원문 수정 시간, 번역 목록)을 조회합니다. 게시판 옵션 "다국어 사용"이 켜진 게시판만 됩니다.
    /// <see cref="BitPlus.Web.Data.Content.ContentManager.LoadTranslationsAsync"/>에 직접 위임합니다 (웹 화면과 같은 권한 검사).
    /// </summary>
    /// <param name="ruleNo">게시판 규칙 번호</param>
    /// <param name="contentNo">글 번호</param>
    /// <returns>culture, uDate, items([{culture, title, body, isMachine, outdated, uDate}])</returns>
    [HttpGet("content/post/{ruleNo:int}/{contentNo:long}/translations")]
    public async Task<IActionResult> GetTranslations(int ruleNo, long contentNo)
        => this.Respond(await this.Manager.LoadTranslationsAsync(new Dictionary<string, object?> { ["ruleNo"] = ruleNo, ["contentNo"] = contentNo }));

    /// <summary>
    /// 글의 한 언어 번역을 등록하거나 수정합니다 (언어마다 따로). AI 번역이면 isMachine 을 켭니다.
    /// <see cref="BitPlus.Web.Data.Content.ContentManager.SaveTranslationAsync"/>에 직접 위임합니다 (웹 화면과 같은 처리·권한 검사).
    /// </summary>
    /// <param name="ruleNo">게시판 규칙 번호</param>
    /// <param name="contentNo">글 번호</param>
    /// <param name="culture">번역 문화권 (사이트 지원 문화권, 원문 언어와 달라야 함)</param>
    /// <param name="title">번역 제목</param>
    /// <param name="body">번역 본문 (HTML)</param>
    /// <param name="isMachine">AI(기계) 번역 여부</param>
    /// <returns>culture, uDate</returns>
    [HttpPost("content/post/{ruleNo:int}/{contentNo:long}/translation")]
    public async Task<IActionResult> SaveTranslation(int ruleNo, long contentNo, [FromForm] string culture, [FromForm] string title, [FromForm] string body, [FromForm] bool isMachine = false)
        => this.Respond(await this.Manager.SaveTranslationAsync(new Dictionary<string, object?>
        {
            ["ruleNo"] = ruleNo,
            ["contentNo"] = contentNo,
            ["culture"] = culture,
            ["title"] = title,
            ["body"] = body,
            ["isMachine"] = isMachine.ToString()
        }));

    /// <summary>
    /// 글의 한 언어 번역을 삭제합니다 (제목·본문을 비워 저장하는 웹 화면의 삭제와 같은 처리).
    /// </summary>
    /// <param name="ruleNo">게시판 규칙 번호</param>
    /// <param name="contentNo">글 번호</param>
    /// <param name="culture">지울 번역 문화권</param>
    /// <returns>culture, deleted</returns>
    [HttpDelete("content/post/{ruleNo:int}/{contentNo:long}/translation/{culture}")]
    public async Task<IActionResult> DeleteTranslation(int ruleNo, long contentNo, string culture)
        => this.Respond(await this.Manager.SaveTranslationAsync(new Dictionary<string, object?> { ["ruleNo"] = ruleNo, ["contentNo"] = contentNo, ["culture"] = culture, ["title"] = string.Empty, ["body"] = string.Empty }));

    #endregion
}
