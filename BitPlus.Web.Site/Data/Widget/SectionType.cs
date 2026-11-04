using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitPlus.Web.Site.Data.Widget;

/// <summary>
/// 영역(Section) 종류 정보 — <c>Views/Sections/{Key}/section.json</c> 을 사이트 시작(설치) 때 읽어 등록한 행.
/// <see cref="Layout"/> 은 화면 폭 4단계(1024 이상·1024 미만·780 미만·520 미만)별 슬롯 배치(CSS grid 영역 이름 줄)이며, 화면과 썸네일을 모두 이것으로 그립니다.
/// 갱신·사라짐 규칙은 <see cref="WidgetType"/> 과 같습니다.
/// </summary>
public class SectionType
{
    /// <summary>영역 키 (폴더 이름, 영문·숫자)</summary>
    public string Key { get; set; }

    /// <summary>section.json 의 version (올라가면 정보 갱신)</summary>
    public int Version { get; set; }

    /// <summary>표시명 — 문화권별 JSON 객체</summary>
    public string Name { get; set; }

    /// <summary>설명 — 문화권별 JSON 객체</summary>
    public string? Description { get; set; }

    /// <summary>슬롯·배치 정의 — section.json 의 slots/repeat/layouts 를 그대로 담은 JSON 객체</summary>
    public string Layout { get; set; }

    /// <summary>폴더에 있는지</summary>
    public bool Available { get; set; }

    /// <summary>사용 여부 (관리자가 끔)</summary>
    public bool Enabler { get; set; }

    /// <summary>표시 순서 (section.json 의 order, 영역 목록 순서)</summary>
    public int SortOrder { get; set; }

    /// <summary>수정일 (UTC)</summary>
    public DateTime UDate { get; set; }

    /// <summary>등록일 (UTC)</summary>
    public DateTime CDate { get; set; }

    /// <summary>
    /// <see cref="SectionType"/> 기본 생성자
    /// </summary>
    public SectionType()
    {
        this.Key = string.Empty;
        this.Version = 0;
        this.Name = "{}";
        this.Description = null;
        this.Layout = "{}";
        this.Available = true;
        this.Enabler = true;
        this.SortOrder = 0;
        this.UDate = DateTime.UtcNow;
        this.CDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Entity Framework Core Fluent API 모델 구성
    /// </summary>
    /// <param name="builder">엔티티 빌더</param>
    /// <param name="tablePrefix">테이블 접두사 (선택 사항)</param>
    public static void Configure(EntityTypeBuilder<SectionType> builder, string? tablePrefix = null)
    {
        builder.ToTable($"{tablePrefix.AnyAfterJoin("_")}SectionTypes").HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(64);
        builder.Property(x => x.Name).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Layout).IsRequired();
    }
}
