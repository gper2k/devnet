namespace BitPlus.Web.Site.Models;

/// <summary>
/// 회원 페이지(<c>/user/...</c>) 뷰 모델 — 내 페이지(정보·메시지·작성글·환경설정)와 다른 사용자의 작성글 보기가 함께 씁니다.
/// 내 페이지가 아니면 이름·이미지·가입일만 채웁니다.
/// </summary>
public class UserPageModel
{
    /// <summary>
    /// <see cref="UserPageModel"/> 기본 생성자
    /// </summary>
    public UserPageModel()
    {
        this.Tab = string.Empty;
        this.IsMe = false;
        this.UserId = Guid.Empty;
        this.Name = string.Empty;
        this.UserName = string.Empty;
        this.Email = string.Empty;
        this.EmailConfirmed = false;
        this.Phone = string.Empty;
        this.DpNames = [];
        this.Photo = string.Empty;
        this.Joined = DateTime.MinValue;
        this.LastAccess = null;
        this.Groups = [];
        this.Roles = [];
        this.Unread = 0;
        this.UnreadNotice = 0;
        this.ReceiveMessage = true;
    }

    /// <summary>지금 페이지 (info, notice, message, posts, setting)</summary>
    public string Tab { get; set; }

    /// <summary>지금 사용자 본인의 페이지인지</summary>
    public bool IsMe { get; set; }

    /// <summary>사용자 식별키</summary>
    public Guid UserId { get; set; }

    /// <summary>표시 이름 (문화권에 맞는 표시명, 없으면 사용자 ID)</summary>
    public string Name { get; set; }

    /// <summary>사용자 ID (내 페이지만)</summary>
    public string UserName { get; set; }

    /// <summary>메일 (내 페이지만)</summary>
    public string Email { get; set; }

    /// <summary>메일 인증 여부 (내 페이지만)</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>전화번호 (내 페이지만)</summary>
    public string Phone { get; set; }

    /// <summary>문화권별 표시 이름 (내 페이지만 — 정보 편집의 다국어 이름 입력 초기값)</summary>
    public Dictionary<string, string> DpNames { get; set; }

    /// <summary>사용자 이미지 경로 (없으면 빈 문자열 — 기본 아이콘)</summary>
    public string Photo { get; set; }

    /// <summary>가입 시각 (UTC)</summary>
    public DateTime Joined { get; set; }

    /// <summary>마지막 접속 시각 (UTC, 내 페이지만)</summary>
    public DateTime? LastAccess { get; set; }

    /// <summary>소속 그룹 (표시 이름, 활성 여부, 기본(주 소속) 여부) — 내 페이지만</summary>
    public List<(string Name, bool Active, bool Default)> Groups { get; set; }

    /// <summary>역할 (표시 이름, 활성 여부, 기본 여부) — 내 페이지만</summary>
    public List<(string Name, bool Active, bool Default)> Roles { get; set; }

    /// <summary>안 읽은 받은 메시지 수 (내 페이지만)</summary>
    public int Unread { get; set; }

    /// <summary>확인하지 않은 알림 수 (내 페이지만)</summary>
    public int UnreadNotice { get; set; }

    /// <summary>다른 사용자의 메시지를 받는지 (환경설정, 내 페이지만)</summary>
    public bool ReceiveMessage { get; set; }
}
