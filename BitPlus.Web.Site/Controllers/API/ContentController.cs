using BitPlus.Data.Account;
using BitPlus.Data.CommonModel;
using BitPlus.Data.Content;
using BitPlus.Data.File;
using BitPlus.Web.Data.Content;
using BitPlus.Web.HTTP.API;
using BitPlus.Web.HTTP.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using WebResources = BitPlus.Web.Properties.Resources;

namespace BitPlus.Web.Site.Controllers.API;

/// <summary>
/// 컨텐츠 Web API 컨트롤러 (api/content). 컨텐츠 유형별 처리(게시판 설정, 글, 보기 전용 페이지 컨텐츠)는 모두 컨텐츠 매니저(<see cref="ContentManager"/>)가 요청 값에서 유형을 읽어 처리하므로,
/// 기능마다 API 하나씩만 두고 요청 값을 그대로 매니저에 넘겨 결과만 내보냅니다. 컨텐츠 첨부에 쓰이는 파일 규칙(<c>filerule</c>, 시스템 설정의 파일 규칙 탭) 관리는 이 컨트롤러가 직접 합니다.
/// </summary>
public class ContentController : APIControllerBase
{
    /// <summary>
    /// <see cref="ContentController"/> 기본 생성자.
    /// </summary>
    public ContentController() : base() { }

    #region Content (유형별 처리는 컨텐츠 매니저가 맡음)

    /// <summary>
    /// 게시판(규칙) 설정을 조회합니다. 쿼리: <c>ruleNo</c>(없으면 새 규칙의 기본값), <c>type</c>(규칙 번호가 없을 때의 유형, 기본 일반).
    /// </summary>
    /// <returns>유형별 게시판 설정</returns>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings() => this.Respond(await this.Manager.GetSettingsAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 게시판(규칙) 설정을 저장합니다. 폼: <c>ruleNo</c>(없으면 새 규칙 등록, 있으면 수정), <c>type</c> 과 유형별 설정 값.
    /// </summary>
    /// <returns>저장된 규칙 식별키·번호</returns>
    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings() => this.Respond(await this.Manager.SaveSettingsAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 게시판 삭제 확인 정보(딸린 글·댓글·첨부·신고 수, 글을 옮길 수 있는 게시판)를 조회합니다. 쿼리: <c>ruleNo</c>. 컨텐츠 관리자 정책만.
    /// </summary>
    /// <returns>삭제 확인 정보</returns>
    [HttpGet("settings/delete")]
    public async Task<IActionResult> GetDeleteInfo() => this.Respond(await this.Manager.GetDeleteInfoAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 게시판을 영구 삭제합니다: 바로 비활성으로 바꾸고 글 이동·딸린 데이터 삭제는 내부에서 이어서 처리합니다. 쿼리: <c>ruleNo</c>, <c>moveTo</c>(글을 먼저 옮길 같은 형식 게시판 번호, 없으면 모두 삭제). 컨텐츠 관리자 정책만.
    /// </summary>
    /// <returns>삭제한 규칙 번호</returns>
    [HttpDelete("settings")]
    public async Task<IActionResult> DeleteSettings() => this.Respond(await this.Manager.DeleteSettingsAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글을 불러옵니다 (수정 화면 채우기용, 비밀번호가 주소에 남지 않도록 POST). 폼: <c>ruleNo</c>(보기 전용은 <c>type</c>=PageView), <c>contentNo</c>, <c>password</c>.
    /// </summary>
    /// <returns>유형별 글 데이터</returns>
    [HttpPost("post/load")]
    public async Task<IActionResult> LoadPost() => this.Respond(await this.Manager.LoadPostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글을 저장합니다. 폼: <c>ruleNo</c>(보기 전용은 <c>type</c>=PageView), <c>contentNo</c>(없으면 새 글 등록, 있으면 수정)와 유형별 글 값.
    /// </summary>
    /// <returns>글 번호(contentNo)와 이동 주소(url)</returns>
    [HttpPost("post")]
    public async Task<IActionResult> SavePost() => this.Respond(await this.Manager.SavePostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글 목록을 조회합니다 (읽기 권한과 상관없이 누구나, 보기 전용 <c>type</c>=PageView 는 관리자 전용 페이지 컨텐츠 목록). 쿼리: <c>ruleNo</c>,<c>q</c>(제목·내용·글쓴이·태그에서 동시에 검색), <c>category</c>(카테고리 값, 없으면 전체), <c>page</c>(1부터), <c>size</c>(페이지당 글 수).
    /// </summary>
    /// <returns>items, total, page, pageSize, pageCount</returns>
    [HttpGet("list")]
    public async Task<IActionResult> ListPosts() => this.Respond(await this.Manager.ListPostsAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글을 삭제합니다. 쿼리: <c>ruleNo</c>(보기 전용은 <c>type</c>=PageView), <c>contentNo</c>.
    /// </summary>
    /// <returns>처리 결과</returns>
    [HttpDelete("post")]
    public async Task<IActionResult> DeletePost() => this.Respond(await this.Manager.DeletePostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글을 같은 형식의 다른 게시판으로 옮깁니다 (관리자·글쓴이 본인, 대상 게시판 쓰기 권한 필요). 폼: <c>ruleNo</c>, <c>contentNo</c>, <c>targetRuleNo</c>.
    /// </summary>
    /// <returns>옮긴 글 번호(contentNo)와 보기 주소(url)</returns>
    [HttpPost("post/move")]
    public async Task<IActionResult> MovePost() => this.Respond(await this.Manager.MovePostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글의 언어별 번역을 불러옵니다 (다국어 게시판, 글을 수정할 수 있는 사람만). 폼: <c>ruleNo</c>, <c>contentNo</c>, (비회원 글) <c>password</c>.
    /// </summary>
    /// <returns>원문 언어(culture)와 번역 목록(items)</returns>
    [HttpPost("post/translation/load")]
    public async Task<IActionResult> LoadTranslations() => this.Respond(await this.Manager.LoadTranslationsAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글의 한 언어 번역을 저장합니다 (제목·본문이 모두 비면 삭제). 폼: <c>ruleNo</c>, <c>contentNo</c>, <c>culture</c>, <c>title</c>, <c>body</c>, (비회원 글) <c>password</c>.
    /// </summary>
    /// <returns>저장 결과</returns>
    [HttpPost("post/translation")]
    public async Task<IActionResult> SaveTranslation() => this.Respond(await this.Manager.SaveTranslationAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글 좋아요를 켜고 끕니다. 폼: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpPost("post/like")]
    public async Task<IActionResult> LikePost() => this.Respond(await this.Manager.LikePostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글의 댓글 목록을 조회합니다. 쿼리: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpGet("comments")]
    public async Task<IActionResult> ListComments() => this.Respond(await this.Manager.ListCommentsAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 댓글을 등록하거나 수정합니다. 폼: <c>ruleNo</c>, <c>contentNo</c>, <c>body</c>, 수정이면 <c>commentId</c>.
    /// </summary>
    [HttpPost("comment")]
    public async Task<IActionResult> SaveComment() => this.Respond(await this.Manager.SaveCommentAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 댓글을 삭제합니다. 쿼리/폼: <c>ruleNo</c>, <c>contentNo</c>, <c>commentId</c>.
    /// </summary>
    [HttpDelete("comment")]
    public async Task<IActionResult> DeleteComment() => this.Respond(await this.Manager.DeleteCommentAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 댓글 좋아요를 켜고 끕니다. 폼: <c>ruleNo</c>, <c>contentNo</c>, <c>commentId</c>.
    /// </summary>
    [HttpPost("comment/like")]
    public async Task<IActionResult> LikeComment() => this.Respond(await this.Manager.LikeCommentAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 메뉴얼의 페이지 그룹 안 페이지 순서를 다시 정합니다. 폼: <c>ruleNo</c>, <c>groupNo</c>(그룹 글번호), <c>order</c>(페이지 글번호 JSON 배열, 새 순서).
    /// </summary>
    [HttpPost("manual/order")]
    public async Task<IActionResult> ReorderManual() => this.Respond(await this.Manager.ReorderAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 설문을 조회합니다(소개문·항목, 참여했거나 관리자·글쓴이면 결과도). 쿼리: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpGet("survey")]
    public async Task<IActionResult> Survey() => this.Respond(await this.Manager.SurveyAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 설문에 참여합니다(한 번만). 폼: <c>ruleNo</c>, <c>contentNo</c>, <c>answers</c>(JSON {질문id: [옵션id]}).
    /// </summary>
    [HttpPost("survey/vote")]
    public async Task<IActionResult> VoteSurvey() => this.Respond(await this.Manager.VoteSurveyAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글·댓글을 신고합니다. 폼: <c>ruleNo</c>, <c>contentNo</c>(글 또는 포럼 댓글의 글번호), 기본 댓글이면 <c>commentId</c>, <c>message</c>(신고 내용).
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> Report() => this.Respond(await this.Manager.ReportAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글의 요약글을 다시 만들도록 요약 서비스에 요청합니다 (게시판 관리자, 일반·포럼 게시판, 서비스가 꺼져 있으면 실패). 폼: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpPost("post/summary")]
    public async Task<IActionResult> RefreshSummary() => this.Respond(await this.Manager.RefreshSummaryAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글의 변경 기록을 조회합니다. 폼: <c>ruleNo</c>, <c>contentNo</c>, 비회원 글은 <c>password</c>, 버전이면 <c>versionNo</c>.
    /// </summary>
    [HttpPost("post/history")]
    public async Task<IActionResult> LoadHistory() => this.Respond(await this.Manager.LoadHistoryAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글의 승인 현황을 조회합니다. 쿼리: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpGet("post/approval")]
    public async Task<IActionResult> GetApproval() => this.Respond(await this.Manager.GetApprovalAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글을 반려합니다. 폼: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpPost("post/reject")]
    public async Task<IActionResult> RejectPost() => this.Respond(await this.Manager.RejectPostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글을 승인합니다. 폼: <c>ruleNo</c>, <c>contentNo</c>.
    /// </summary>
    [HttpPost("post/approve")]
    public async Task<IActionResult> ApprovePost() => this.Respond(await this.Manager.ApprovePostAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글쓰기 화면이 이어 쓸 임시 글을 찾거나 만듭니다 (임시 글은 글 번호가 없어 목록에 나오지 않음). 폼: <c>ruleNo</c>, 답글이면 <c>replyTo</c>(원본 글 번호), 비회원은 브라우저가 가진 <c>draftKey</c>, 없으면 만들려면 <c>create</c>=true.
    /// </summary>
    /// <returns>임시 글 데이터(id, title, body, …, files), 없으면 id 가 null</returns>
    [HttpPost("draft")]
    public async Task<IActionResult> LoadDraft() => this.Respond(await this.Manager.LoadDraftAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 글쓰기 화면이 쓸 첨부 설정을 조회합니다. 쿼리: <c>ruleNo</c>.
    /// </summary>
    /// <returns>enabled, canUpload, extensions, count, size, totalSize</returns>
    [HttpGet("file")]
    public async Task<IActionResult> GetFiles() => this.Respond(await this.Manager.GetFilesAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 첨부 파일을 올립니다 (파일 하나씩 올려 진행률을 보여 줄 수 있음). 폼: <c>ruleNo</c>, <c>file</c>, 새 글은 <c>draftKey</c> / 수정은 <c>contentNo</c>(+ 비회원 글 <c>password</c>), 삭제 예정 파일 <c>removeFiles</c>.
    /// </summary>
    /// <returns>올린 파일 [{id, name, size}]</returns>
    [HttpPost("file")]
    public async Task<IActionResult> UploadFile() => this.Respond(await this.Manager.UploadFileAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 큰 첨부 파일(조각 크기보다 큰 파일, 최대 2GB)의 조각 업로드를 시작합니다. 폼: <c>file</c> 대신 <c>fileName</c>, <c>fileSize</c>, 나머지는 <c>POST file</c> 과 같음. 권한·규칙·디스크 공간을 먼저 검사합니다.
    /// </summary>
    /// <returns>uploadId, chunkSize</returns>
    [HttpPost("file/chunk/start")]
    public async Task<IActionResult> StartUpload() => this.Respond(await this.Manager.StartUploadAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 조각 하나를 올립니다 (요청 크기는 조각 크기 + 1MB 까지). 폼: <c>uploadId</c>, <c>offset</c>(이 조각의 시작 위치), <c>chunk</c>(조각). 위치가 맞지 않으면 붙이지 않고 받은 크기만 돌려주므로, 끊기면 <c>received</c> 부터 다시 보냅니다.
    /// </summary>
    /// <returns>received(지금까지 받은 크기)</returns>
    [HttpPost("file/chunk")]
    [RequestSizeLimit(ChunkUpload.ChunkRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = ChunkUpload.ChunkRequestLimit)]
    public async Task<IActionResult> UploadChunk() => this.Respond(await this.Manager.UploadChunkAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 다 받은 조각 업로드를 첨부 파일로 저장합니다. 폼: <c>uploadId</c> + <c>POST file/chunk/start</c> 와 같은 값 (권한·규칙을 다시 검사).
    /// </summary>
    /// <returns>올린 파일 [{id, name, size}]</returns>
    [HttpPost("file/chunk/complete")]
    public async Task<IActionResult> CompleteUpload() => this.Respond(await this.Manager.CompleteUploadAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 조각 업로드를 취소하고 받은 조각을 지웁니다. 폼: <c>uploadId</c>.
    /// </summary>
    [HttpPost("file/chunk/cancel")]
    public IActionResult CancelUpload() => this.Respond(this.Manager.CancelUpload(ContentManager.ValuesOf(this.Request)));

    /// <summary>
    /// 아직 글에 등록되지 않은 첨부 파일을 지웁니다. 폼: <c>ruleNo</c>, <c>fileId</c>, 새 글은 <c>draftKey</c> / 수정은 <c>contentNo</c>(+ 비회원 글 <c>password</c>).
    /// </summary>
    [HttpPost("file/delete")]
    public async Task<IActionResult> DeleteFile() => this.Respond(await this.Manager.DeleteFileAsync(ContentManager.ValuesOf(this.Request)));

    /// <summary>컨텐츠 매니저 (이 요청의 사용자·DB 로 처리)</summary>
    private ContentManager Manager => new(this);

    /// <summary>컨텐츠 처리 결과를 표준 응답으로 내보냅니다 (상태에 맞는 오류 응답 포함).</summary>
    private IActionResult Respond(ContentResponse response) => response.Status switch
    {
        200 => this.Complete(response.Data, response.Message),
        401 => this.UnauthorizedResult(response.Message),
        403 => this.ForbiddenResult(response.Message),
        404 => this.NotFoundResult(response.Message),
        _ => this.Error(response.Message ?? WebResources.ValidRegex, response.Data, response.Status)
    };

    #endregion

    #region Rule (관리자 콘솔 게시판 목록)

    /// <summary>
    /// 게시판(컨텐츠 규칙, 보기 전용 제외) 목록을 검색어(다국어 제목)로 검색해 규칙 번호 순으로 페이지 단위 조회합니다. 비활성 규칙도 포함합니다.
    /// 관리자 정책 또는 컨텐츠 관리자 정책을 만족하는 사용자만 쓸 수 있습니다 (비로그인 401, 그 밖 403).
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호 (1부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <returns>items([{ruleNo, title, type, enabler, deleting(삭제 진행 중), count, cDate}]), total, page, pageSize</returns>
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var db = this.DbContext!;
        await DefaultContentHandler.ResumeDeletesAsync(db, this.HttpContext.RequestServices, this.Config!); // 서버가 멈춰 끝나지 않은 게시판 삭제를 이어서
        var query = db.ContentRules.AsNoTracking().Where(p => !p.OnlyView);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p => p.Title.Any(t => t.Name.Contains(kw)));
        }

        var total = await query.CountAsync();
        var rules = await query.Include(p => p.Title).Include(p => p.Properties).OrderBy(p => p.RuleNo).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var ids = rules.Select(p => p.Id).ToList();
        var counts = await db.Contents.AsNoTracking().Where(p => ids.Contains(p.ContentRuleId) && p.ContentNo != null).GroupBy(p => p.ContentRuleId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(p => p.Key, p => p.Count);
        var items = rules.Select(p => new { ruleNo = p.RuleNo, title = this.Request.DisplayName(p.Title, p.RuleNo.ToString()), type = p.Type.ToString(), enabler = p.Enabler, deleting = DefaultContentHandler.IsDeleting(p), count = counts.GetValueOrDefault(p.Id), cDate = p.CDate });
        return this.Complete(new { items, total, page, pageSize });
    }

    #endregion

    #region Report (관리자 콘솔 신고 목록·처리)

    /// <summary>
    /// 게시물·댓글 신고 목록을 최신순으로 페이지 단위 조회합니다. 검색어는 신고 내용·신고자 이름에서 찾습니다.
    /// 관리자 정책 또는 컨텐츠 관리자 정책을 만족하는 사용자만 쓸 수 있습니다 (비로그인 401, 그 밖 403).
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호 (1부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <returns>items([{id, board, target(Content|Comment), title(글 제목), excerpt(신고된 댓글·포럼 답글 내용 일부), writer(신고된 글·댓글 작성자), writerId, writerUserName(작성자 아이디), ruleNo, contentNo(글 번호 — 댓글이면 그 글), targetId(글·댓글 식별키), url(글 새 창 주소), exists(대상이 남아 있는지), hidden(글이 감춰졌는지), reports(같은 대상 신고 수), reporter(신고자 표시명), reporterId, reporterUserName(신고자 아이디, 탈퇴했으면 null), message, cDate}]), total, page, pageSize</returns>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var db = this.DbContext!;
        var query = db.ContentReports.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p => p.Message.Contains(kw) || p.ReporterName.Contains(kw));
        }
        var total = await query.CountAsync();
        var reports = await query.OrderByDescending(p => p.CDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // 대상(글·댓글), 댓글이면 그 글, 게시판, 작성자, 같은 대상 신고 수
        var targetIds = reports.Select(p => p.TargetId).Distinct().ToList();
        var comments = await db.Comments.AsNoTracking().Where(p => targetIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var contentIds = targetIds.Concat(comments.Values.Select(p => p.ContentId)).Distinct().ToList();
        var contents = await db.Contents.AsNoTracking().Where(p => contentIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var ruleIds = reports.Select(p => p.ContentRuleId).Distinct().ToList();
        var rules = await db.ContentRules.AsNoTracking().Include(p => p.Title).Where(p => ruleIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var userIds = contents.Values.Select(p => p.WriterId).Concat(comments.Values.Select(p => p.CommentUserId)).OfType<Guid>().Concat(reports.Select(p => p.ReporterId)).Distinct().ToList();
        var people = await ((DbContext)db).Set<BitPlus.Data.Account.Model.AppUser>().AsNoTracking().Include(p => p.DpNames).Where(p => userIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => (Name: this.Request.DisplayName(p.DpNames, p.UserName), p.UserName));
        var counts = await db.ContentReports.AsNoTracking().Where(p => targetIds.Contains(p.TargetId)).GroupBy(p => p.TargetId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(p => p.Key, p => p.Count);
        static string Excerpt(string? html)
        {
            var plain = System.Net.WebUtility.HtmlDecode(html.ClearHtml() ?? string.Empty).Trim();
            return plain.Length > 200 ? plain[..200] + "…" : plain;
        }

        var items = reports.Select(p =>
        {
            var comment = p.TargetType == ContentReportTarget.Comment ? comments.GetValueOrDefault(p.TargetId) : null;
            var target = p.TargetType == ContentReportTarget.Content ? contents.GetValueOrDefault(p.TargetId) : null;
            var post = comment is null ? target : contents.GetValueOrDefault(comment.ContentId);
            var rule = rules.GetValueOrDefault(p.ContentRuleId);
            var isReply = target is not null && rule is not null && ForumContentHandler.UsesTopicComments(rule.Type) && (target.LevelNo ?? 0) > 0; // 포럼 답글은 내용을 보여 줌
            var writerId = comment?.CommentUserId ?? target?.WriterId;
            return new
            {
                id = p.Id,
                board = rule is null ? string.Empty : this.Request.DisplayName(rule.Title, rule.RuleNo.ToString()),
                target = p.TargetType.ToString(),
                title = post?.Title ?? string.Empty,
                excerpt = comment is not null ? Excerpt(comment.CommentBody) : isReply ? Excerpt(target!.ContentBody) : string.Empty,
                writer = writerId is { } wid && people.TryGetValue(wid, out var w) ? w.Name : string.Empty,
                writerId,
                writerUserName = writerId is { } wu && people.TryGetValue(wu, out var wn) ? wn.UserName : null,
                ruleNo = rule?.RuleNo,
                contentNo = p.TargetNo,
                targetId = p.TargetId,
                url = rule is null || p.TargetNo is null ? null : $"/content/{rule.RuleNo}/view/{p.TargetNo}",
                exists = comment is not null || target is not null,
                hidden = target is { Visible: false },
                reports = counts.GetValueOrDefault(p.TargetId),
                reporter = people.TryGetValue(p.ReporterId, out var r) ? r.Name : p.ReporterName,
                reporterId = p.ReporterId,
                reporterUserName = people.TryGetValue(p.ReporterId, out var ru) ? ru.UserName : null,
                message = p.Message,
                cDate = p.CDate
            };
        });
        return this.Complete(new { items, total, page, pageSize });
    }

    /// <summary>
    /// 신고된 글(글·포럼 토픽·포럼 답글)을 감추거나 다시 보입니다 (<see cref="ContentModel.Visible"/>). 기본 댓글은 감출 수 없습니다.
    /// </summary>
    /// <param name="id">신고 식별키</param>
    /// <param name="hidden">true = 감추기, false = 다시 보이기</param>
    /// <returns>처리 결과</returns>
    [HttpPost("report/{id:guid}/hide")]
    public async Task<IActionResult> HideReported(Guid id, [FromForm] bool hidden)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        var db = this.DbContext!;
        var report = await db.ContentReports.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (report is null || report.TargetType != ContentReportTarget.Content) return this.NotFoundResult();
        var updated = await db.Contents.Where(p => p.Id == report.TargetId).ExecuteUpdateAsync(p => p.SetProperty(x => x.Visible, !hidden));
        return updated == 0 ? this.NotFoundResult() : this.Complete();
    }

    /// <summary>
    /// 신고 한 건을 처리 완료로 지웁니다 (같은 대상의 다른 신고와 글·댓글은 그대로).
    /// </summary>
    /// <param name="id">신고 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("report/{id:guid}")]
    public async Task<IActionResult> ResolveReport(Guid id)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        var db = this.DbContext!;
        await db.ContentReports.Where(p => p.Id == id).ExecuteDeleteAsync();
        return this.Complete();
    }

    #endregion

    #region FileRule

    // 파일 규칙은 컨텐츠 첨부에 쓰이므로 컨텐츠 관리 화면에서 관리한다

    /// <summary>
    /// 파일 규칙 목록을 검색어(규칙명)로 검색해 페이지 단위로 조회합니다.
    /// </summary>
    /// <param name="keyword">검색어</param>
    /// <param name="page">페이지 번호 (1부터)</param>
    /// <param name="pageSize">페이지 크기 (1~100, 기본 10)</param>
    /// <returns>items, total, page, pageSize</returns>
    [HttpGet("filerule/list")]
    public async Task<IActionResult> GetFileRuleList([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = this.DbContext!.FileRules.AsNoTracking().Where(p => !p.IsSystem);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                webroot = p.RegistWebRoot,
                ext = p.AllowExtensions,
                count = p.AllowCount,
                size = p.AllowSize,
                totalsize = p.AllowTotalSize,
                isSystem = p.IsSystem,
                cDate = p.CDate,
                uDate = p.UDate
            })
            .ToListAsync();

        return this.Complete(new { items, total, page, pageSize });
    }

    /// <summary>
    /// 파일 규칙 단건 상세 정보를 조회합니다 (권한 규칙 목록 포함).
    /// </summary>
    /// <param name="id">파일 규칙 식별키</param>
    /// <returns>규칙 상세 정보 및 권한 규칙 목록</returns>
    [HttpGet("filerule/{id:guid}")]
    public async Task<IActionResult> GetFileRule(Guid id)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        var rule = await this.DbContext!.FileRules
            .Include(p => p.AccessRules)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (rule is null) return this.NotFoundResult(WebResources.NotFoundData);

        var names = await this.TargetNamesAsync(rule.AccessRules.Select(p => (p.TargetType, p.TargetKey)));
        var accessItems = rule.AccessRules.Select(p => new
        {
            mode = p.Mode.ToString(),
            type = p.TargetType.ToString(),
            key = p.TargetKey,
            name = names.GetValueOrDefault((p.TargetType, p.TargetKey)) ?? p.TargetKey.ToString()
        });

        return this.Complete(new
        {
            id = rule.Id,
            name = rule.Name,
            webroot = rule.RegistWebRoot,
            ext = rule.AllowExtensions,
            count = rule.AllowCount,
            size = rule.AllowSize,
            totalsize = rule.AllowTotalSize,
            isSystem = rule.IsSystem,
            cDate = rule.CDate,
            uDate = rule.UDate,
            accessRules = accessItems
        });
    }

    /// <summary>
    /// 새 파일 규칙을 생성합니다.
    /// </summary>
    /// <param name="name">규칙명</param>
    /// <param name="registWebRoot">웹 저장 경로</param>
    /// <param name="allowExtensions">허용 확장자</param>
    /// <param name="allowCount">허용 개수 (-1: 무제한)</param>
    /// <param name="allowSize">개별 허용 용량 (-1: 무제한)</param>
    /// <param name="allowTotalSize">전체 허용 용량 (-1: 무제한)</param>
    /// <param name="accessRules">권한 규칙 JSON 문자열</param>
    /// <returns>생성된 파일 규칙 기본 정보</returns>
    [HttpPost("filerule")]
    public async Task<IActionResult> CreateFileRule(
        [FromForm] string? name,
        [FromForm] string? registWebRoot,
        [FromForm] string? allowExtensions,
        [FromForm] int? allowCount,
        [FromForm] long? allowSize,
        [FromForm] long? allowTotalSize,
        [FromForm] string? accessRules)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        name = name?.Trim();
        registWebRoot = registWebRoot?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return this.Error(WebResources.ValidRequired.Formats(this.Request.GetLanguage("Name") ?? "Name")!);
        if (string.IsNullOrWhiteSpace(registWebRoot))
            return this.Error(WebResources.ValidRequired.Formats(this.Request.GetLanguage("RegistWebRoot") ?? "Web Root")!);

        var exists = await this.DbContext!.FileRules.AnyAsync(p => p.Name == name);
        if (exists)
            return this.Error(WebResources.ValidAlreadyItem.Formats(this.Request.GetLanguage("Name") ?? "Name")!);

        var rule = new UpFileRule
        {
            Name = name,
            RegistWebRoot = registWebRoot,
            AllowExtensions = allowExtensions?.Trim(),
            AllowCount = allowCount ?? -1,
            AllowSize = allowSize ?? -1,
            AllowTotalSize = allowTotalSize ?? -1,
            CDate = DateTime.UtcNow,
            UDate = DateTime.UtcNow
        };

        ParseFileAccessRules(rule, accessRules);

        this.DbContext!.FileRules.Add(rule);
        if (this.DbContext is DbContext efDb)
            await efDb.SaveChangesAsync();

        return this.Complete(new { id = rule.Id, name = rule.Name }, WebResources.AddedComplete);
    }

    /// <summary>
    /// 기존 파일 규칙을 수정합니다.
    /// </summary>
    /// <param name="id">파일 규칙 식별키</param>
    /// <param name="name">규칙명</param>
    /// <param name="registWebRoot">웹 저장 경로</param>
    /// <param name="allowExtensions">허용 확장자</param>
    /// <param name="allowCount">허용 개수 (-1: 무제한)</param>
    /// <param name="allowSize">개별 허용 용량 (-1: 무제한)</param>
    /// <param name="allowTotalSize">전체 허용 용량 (-1: 무제한)</param>
    /// <param name="accessRules">권한 규칙 JSON 문자열</param>
    /// <returns>수정된 파일 규칙 기본 정보</returns>
    [HttpPut("filerule/{id:guid}")]
    public async Task<IActionResult> UpdateFileRule(
        Guid id,
        [FromForm] string? name,
        [FromForm] string? registWebRoot,
        [FromForm] string? allowExtensions,
        [FromForm] int? allowCount,
        [FromForm] long? allowSize,
        [FromForm] long? allowTotalSize,
        [FromForm] string? accessRules)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        name = name?.Trim();
        registWebRoot = registWebRoot?.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return this.Error(WebResources.ValidRequired.Formats(this.Request.GetLanguage("Name") ?? "Name")!);
        if (string.IsNullOrWhiteSpace(registWebRoot))
            return this.Error(WebResources.ValidRequired.Formats(this.Request.GetLanguage("RegistWebRoot") ?? "Web Root")!);

        var rule = await this.DbContext!.FileRules
            .Include(p => p.AccessRules)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (rule is null) return this.NotFoundResult(WebResources.NotFoundData);
        if (rule.IsSystem) return this.Error(this.Lang("SystemChangeWarning"));

        var exists = await this.DbContext!.FileRules.AnyAsync(p => p.Id != id && p.Name == name);
        if (exists)
            return this.Error(WebResources.ValidAlreadyItem.Formats(this.Request.GetLanguage("Name") ?? "Name")!);

        rule.Name = name;
        rule.RegistWebRoot = registWebRoot;
        rule.AllowExtensions = allowExtensions?.Trim();
        rule.AllowCount = allowCount ?? -1;
        rule.AllowSize = allowSize ?? -1;
        rule.AllowTotalSize = allowTotalSize ?? -1;
        rule.UDate = DateTime.UtcNow;

        this.DbContext!.FileAccessRules.RemoveRange(rule.AccessRules);
        rule.AccessRules.Clear();
        ParseFileAccessRules(rule, accessRules);

        if (this.DbContext is DbContext efDb)
            await efDb.SaveChangesAsync();

        return this.Complete(new { id = rule.Id, name = rule.Name }, WebResources.UpdatedComplete);
    }

    /// <summary>
    /// 파일 규칙을 삭제합니다. 시스템 파일 규칙은 삭제할 수 없습니다.
    /// </summary>
    /// <param name="id">삭제할 파일 규칙 식별키</param>
    /// <returns>처리 결과</returns>
    [HttpDelete("filerule/{id:guid}")]
    public async Task<IActionResult> DeleteFileRule(Guid id)
    {
        if (!this.IsManager && !this.IsAllowed(WebConfig.ContentManagerPolicyName)) return this.IsAuthenticated ? this.ForbiddenResult() : this.UnauthorizedResult();
        var rule = await this.DbContext!.FileRules
            .Include(p => p.AccessRules)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (rule is null) return this.NotFoundResult(WebResources.NotFoundData);
        if (rule.IsSystem) return this.Error(WebResources.SystemDelWarning);

        this.DbContext!.FileRules.Remove(rule);
        if (this.DbContext is DbContext efDb)
            await efDb.SaveChangesAsync();

        return this.Complete(message: WebResources.DeletedComplete);
    }

    /// <summary>
    /// 전달받은 JSON 문자열에서 권한 규칙들을 역직렬화하여 파일 규칙에 추가합니다.
    /// </summary>
    /// <param name="rule">파일 규칙 인스턴스</param>
    /// <param name="accessRulesJson">JSON 문자열</param>
    private static void ParseFileAccessRules(UpFileRule rule, string? accessRulesJson)
    {
        if (string.IsNullOrWhiteSpace(accessRulesJson)) return;
        try
        {
            var node = JsonNode.Parse(accessRulesJson) as JsonArray;
            if (node is null) return;
            foreach (var item in node)
            {
                if (item is null) continue;
                var modeStr = item["mode"]?.GetValue<string>();
                var typeStr = item["type"]?.GetValue<string>();
                var keyStr = item["key"]?.GetValue<string>();

                if (!Enum.TryParse<UpFileMode>(modeStr, true, out var mode)) continue;
                if (!Enum.TryParse<AccountTypes>(typeStr, true, out var targetType)) continue;
                if (!Guid.TryParse(keyStr, out var targetKey) || targetKey == Guid.Empty) continue;

                if (rule.AccessRules.Any(r => r.Mode == mode && r.TargetType == targetType && r.TargetKey == targetKey))
                    continue;

                rule.AccessRules.Add(new UpFileAccessRule
                {
                    RuleId = rule.Id,
                    Mode = mode,
                    TargetType = targetType,
                    TargetKey = targetKey,
                    CDate = DateTime.UtcNow
                });
            }
        }
        catch { }
    }

    #endregion
}
