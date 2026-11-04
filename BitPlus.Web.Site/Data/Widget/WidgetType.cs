using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitPlus.Web.Site.Data.Widget;

/// <summary>
/// 위젯 종류 정보 — <c>Views/Widgets/{Key}/widget.json</c> 을 사이트 시작(설치) 때 읽어 등록한 행.
/// 설명·크기·설정 항목은 JSON 의 <see cref="Version"/> 이 올라가면 갱신되고, 사용 여부(<see cref="Enabler"/>)는 관리자가 정한 값을 유지합니다.
/// 폴더에서 사라지면 지우지 않고 <see cref="Available"/> = false (페이지 배치가 키를 참조할 수 있으므로).
/// </summary>
public class WidgetType
{
    /// <summary>위젯 키 (폴더 이름, 영문·숫자)</summary>
    public string Key { get; set; }

    /// <summary>widget.json 의 version (올라가면 정보 갱신)</summary>
    public int Version { get; set; }

    /// <summary>표시명 — 문화권별 JSON 객체 (<c>{"ko":"최신 글","en":"Latest posts"}</c>)</summary>
    public string Name { get; set; }

    /// <summary>설명 — 문화권별 JSON 객체</summary>
    public string? Description { get; set; }

    /// <summary>카테고리 (쉼표 구분, 예: board) — 페이지가 쓸 수 있는 카테고리만 고를 수 있음</summary>
    public string Categories { get; set; }

    /// <summary>최소 가로 크기 (가로 4칸 격자의 칸 수)</summary>
    public double MinWidth { get; set; }

    /// <summary>최소 세로 크기 (행 수)</summary>
    public int MinHeight { get; set; }

    /// <summary>최대 가로 크기 (칸 수)</summary>
    public double MaxWidth { get; set; }

    /// <summary>최대 세로 크기 (행 수)</summary>
    public int MaxHeight { get; set; }

    /// <summary>설정 항목 — JSON 배열 (<c>[{"key","type","default",...}]</c>)</summary>
    public string Settings { get; set; }

    /// <summary>폴더에 있는지 (없어진 위젯은 false, 화면에서 건너뜀)</summary>
    public bool Available { get; set; }

    /// <summary>사용 여부 (관리자가 끔)</summary>
    public bool Enabler { get; set; }

    /// <summary>수정일 (UTC)</summary>
    public DateTime UDate { get; set; }

    /// <summary>등록일 (UTC)</summary>
    public DateTime CDate { get; set; }

    /// <summary>
    /// <see cref="WidgetType"/> 기본 생성자
    /// </summary>
    public WidgetType()
    {
        this.Key = string.Empty;
        this.Version = 0;
        this.Name = "{}";
        this.Description = null;
        this.Categories = string.Empty;
        this.MinWidth = 1;
        this.MinHeight = 1;
        this.MaxWidth = 4;
        this.MaxHeight = 4;
        this.Settings = "[]";
        this.Available = true;
        this.Enabler = true;
        this.UDate = DateTime.UtcNow;
        this.CDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Entity Framework Core Fluent API 모델 구성
    /// </summary>
    /// <param name="builder">엔티티 빌더</param>
    /// <param name="tablePrefix">테이블 접두사 (선택 사항)</param>
    public static void Configure(EntityTypeBuilder<WidgetType> builder, string? tablePrefix = null)
    {
        builder.ToTable($"{tablePrefix.AnyAfterJoin("_")}WidgetTypes").HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(64);
        builder.Property(x => x.Name).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Categories).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Settings).IsRequired();
    }
}
