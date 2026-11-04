namespace BitPlus.Web.Site.Models;

/// <summary>
/// 컨텐츠 글 보기(<c>/content/{ruleNo}/view/{contentNo}</c>, <c>/content/view/{contentKey}</c>)에 보일 글 한 개: 화면에 필요한 값만 담습니다 (글쓴이는 익명 규칙이면 이미 가려진 값).
/// </summary>
public class ContentPostItem
{
    /// <summary>
    /// <see cref="ContentPostItem"/> 기본 생성자.
    /// </summary>
    public ContentPostItem()
    {
        this.Id = Guid.Empty;
        this.ContentNo = 0;
        this.Title = string.Empty;
        this.Body = string.Empty;
        this.Category = string.Empty;
        this.IsNotice = false;
        this.Writer = string.Empty;
        this.WriterId = null;
        this.WriterPhoto = null;
        this.WriterGroup = null;
        this.WriterRole = null;
        this.CDate = DateTime.MinValue;
        this.UDate = DateTime.MinValue;
        this.ReadCount = 0;
        this.Tags = [];
        this.Files = [];
        this.Links = [];
        this.MoveTargets = [];
        this.IsPublished = false;
        this.IsApproved = false;
        this.IsRejected = false;
        this.WriterIp = string.Empty;
        this.Start = null;
        this.End = null;
        this.HasApprovals = false;
        this.CanEdit = false;
        this.CanDelete = false;
        this.CanReply = false;
        this.CanHistory = false;
        this.NeedPassword = false;
        this.LikePoint = 0;
        this.Vote = 0;
        this.LevelNo = 0;
        this.PreviousNo = 0;
        this.PreviousTitle = string.Empty;
        this.NextNo = 0;
        this.NextTitle = string.Empty;
        this.Culture = string.Empty;
        this.OriginalCulture = string.Empty;
        this.Translations = [];
    }

    /// <summary>보이는 제목·본문의 문화권 (번역을 보이면 그 언어, 원문이면 <see cref="OriginalCulture"/>, 다국어 게시판이 아니면 빈 값)</summary>
    public string Culture { get; set; }

    /// <summary>원문 문화권 (다국어 게시판이 아니면 빈 값)</summary>
    public string OriginalCulture { get; set; }

    /// <summary>이 글의 번역 목록 (언어 선택 칩용: 문화권, 언어 이름, AI 번역 여부, 원문이 번역 뒤에 바뀌었는지)</summary>
    public List<(string Culture, string Name, bool IsMachine, bool Outdated)> Translations { get; set; }

    /// <summary>글 삭제 버튼을 보일지 (관리자, 또는 쓰기 권한 + "삭제 허용" 옵션 + 본인 글)</summary>
    public bool CanDelete { get; set; }

    /// <summary>답글 버튼을 보일지 ("답글 허용" 옵션 + 쓰기 권한)</summary>
    public bool CanReply { get; set; }

    /// <summary>변경 기록을 보일지 ("글 변경 기록" 옵션 + 변경 기록이 있을 때만, 글을 열 수 있는 사람 모두)</summary>
    public bool CanHistory { get; set; }

    /// <summary>삭제에 비밀번호가 필요한지 (작성자 정보 없는 비회원 글을 관리자가 아닌 사람이 볼 때)</summary>
    public bool NeedPassword { get; set; }

    /// <summary>좋아요(+1)·싫어요(-1) 합산값</summary>
    public int LikePoint { get; set; }

    /// <summary>현재 사용자의 표 (1 좋아요, -1 싫어요, 0 없음)</summary>
    public int Vote { get; set; }

    /// <summary>현재 사용자가 내려받을 수 있는 첨부 파일 (Url: 다운로드 경로, Name: 표시 글자 "파일명 (용량)", DownCount: 다운로드 횟수, InlineUrl: 웹 이미지면 화면 표시용 경로 아니면 빈 문자열) — 권한이 없는 파일은 빠짐</summary>
    public List<(string Url, string Name, int DownCount, string InlineUrl)> Files { get; set; }

    /// <summary>이전 글을 이 사용자가 열 수 없는지 (잠금 표시, 링크 없음)</summary>
    public bool PreviousLocked { get; set; }

    /// <summary>다음 글을 이 사용자가 열 수 없는지 (잠금 표시, 링크 없음)</summary>
    public bool NextLocked { get; set; }

    /// <summary>답글 깊이 (0 이 원본글)</summary>
    public int LevelNo { get; set; }

    /// <summary>반려된 글인지 (승인자가 있는 게시판에서 승인 상태가 false)</summary>
    public bool IsRejected { get; set; }

    /// <summary>작성자 IP (가운데 3문자를 가린 값, 기록이 없거나 익명 게시판이면 빈 문자열)</summary>
    public string WriterIp { get; set; }

    /// <summary>게시 시작 일시 (UTC, 없으면 null)</summary>
    public DateTime? Start { get; set; }

    /// <summary>게시 종료 일시 (UTC, 없으면 null)</summary>
    public DateTime? End { get; set; }

    /// <summary>승인 기록 항목을 보일지: 관리자·승인자·글쓴이이고, 승인 대기·반려 글이거나 승인된 기록이 있을 때</summary>
    public bool HasApprovals { get; set; }

    /// <summary>글 식별키</summary>
    public Guid Id { get; set; }

    /// <summary>규칙 안의 글 번호</summary>
    public int ContentNo { get; set; }

    /// <summary>제목</summary>
    public string Title { get; set; }

    /// <summary>본문 HTML (에디터가 정제해 저장한 값)</summary>
    public string Body { get; set; }

    /// <summary>카테고리 표시 글자 (없으면 빈 문자열)</summary>
    public string Category { get; set; }

    /// <summary>공지글 여부</summary>
    public bool IsNotice { get; set; }

    /// <summary>글쓴이 표시 이름 (익명 규칙이면 "익명", 비회원 글은 빈 문자열)</summary>
    public string Writer { get; set; }

    /// <summary>글쓴이 사용자 식별키 (작성자 메뉴용, 익명 규칙·비회원 글은 null)</summary>
    public Guid? WriterId { get; set; }

    /// <summary>글쓴이 이미지 주소 (내 정보에서 올린 이미지, 없거나 익명 규칙이면 null — 기본 아이콘)</summary>
    public string? WriterPhoto { get; set; }

    /// <summary>글쓴이 활성 그룹·역할 표시 이름 (활성 → 기본 → 첫 항목, 소속이 없거나 익명 규칙이면 null)</summary>
    public string? WriterGroup { get; set; }

    /// <summary>글쓴이 활성 역할 표시 이름 (활성 → 기본 → 첫 항목, 없거나 익명 규칙이면 null)</summary>
    public string? WriterRole { get; set; }

    /// <summary>등록일 (UTC)</summary>
    public DateTime CDate { get; set; }

    /// <summary>수정일 (UTC)</summary>
    public DateTime UDate { get; set; }

    /// <summary>조회수 (이번 조회 포함)</summary>
    public int ReadCount { get; set; }

    /// <summary>태그 목록</summary>
    public List<string> Tags { get; set; }

    /// <summary>연관 링크 목록 (http/https 주소)</summary>
    public List<string> Links { get; set; }

    /// <summary>게시 여부 (임시 저장 글이면 false — 관리자와 글쓴이 본인에게만 보임)</summary>
    public bool IsPublished { get; set; }

    /// <summary>승인 여부 (승인 전이면 false — 관리자와 글쓴이 본인에게만 보임)</summary>
    public bool IsApproved { get; set; }

    /// <summary>메뉴얼에서 새 페이지를 추가할 수 있는 그룹의 글번호 (이 글이 그룹이면 자기 번호, 그룹의 페이지이면 소속 그룹 번호, 추가할 수 없으면 0) — "페이지 쓰기" 버튼 표시용</summary>
    public int AddPageToNo { get; set; }

    /// <summary>메뉴얼에서 이 글이 속한 페이지 그룹의 글번호 (이 글이 그룹이면 자기 번호, 그룹의 페이지이면 소속 그룹 번호, 메뉴얼이 아니면 0)</summary>
    public int GroupNo { get; set; }

    /// <summary>현재 사용자가 이 메뉴얼 페이지 그룹의 페이지 순서를 바꿀 수 있는지 (관리자, 또는 그 그룹의 작성자) — "순서 변경" 항목 표시용</summary>
    public bool CanReorder { get; set; }

    /// <summary>현재 사용자가 이 글을 신고할 수 있는지 (로그인한 회원이 본인이 아닌 게시·승인된 글을 볼 때) — 신고 항목 표시용</summary>
    public bool CanReport { get; set; }

    /// <summary>이 글을 옮길 수 있는 게시판 (규칙 번호, 표시 제목) — 관리자·글쓴이 본인에게, 같은 형식이면서 쓰기 권한이 있는 다른 게시판. 비어 있으면 "이동" 항목을 그리지 않음</summary>
    public List<(int RuleNo, string Title)> MoveTargets { get; set; }

    /// <summary>현재 사용자가 이 글을 수정할 수 있는지 (관리자, 또는 쓰기 권한 + 수정 허용 + 본인 글/비회원 글) — 수정 버튼 표시용</summary>
    public bool CanEdit { get; set; }

    /// <summary>이전 글 번호 (없으면 0)</summary>
    public int PreviousNo { get; set; }

    /// <summary>이전 글 제목</summary>
    public string PreviousTitle { get; set; }

    /// <summary>다음 글 번호 (없으면 0)</summary>
    public int NextNo { get; set; }

    /// <summary>다음 글 제목</summary>
    public string NextTitle { get; set; }
}
