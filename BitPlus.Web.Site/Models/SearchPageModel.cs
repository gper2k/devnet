using BitPlus.Web.Search;

namespace BitPlus.Web.Site.Models;

/// <summary>
/// 통합 검색 페이지(<c>/search</c>) 뷰 모델: 검색어, 고른 구분(없으면 전체), 페이지, 구분(공급자)별 결과.
/// </summary>
public class SearchPageModel
{
    /// <summary>
    /// <see cref="SearchPageModel"/> 기본 생성자
    /// </summary>
    public SearchPageModel()
    {
        this.Keyword = string.Empty;
        this.Type = null;
        this.Page = 1;
        this.PageSize = 0;
        this.Groups = [];
        this.Providers = [];
    }

    /// <summary>검색어 (비었으면 검색하지 않음)</summary>
    public string Keyword { get; set; }

    /// <summary>고른 구분 키 (null 이면 전체 — 구분마다 앞부분만)</summary>
    public string? Type { get; set; }

    /// <summary>고른 구분의 페이지 번호 (1부터)</summary>
    public int Page { get; set; }

    /// <summary>한 페이지 항목 수 (전체 보기에서는 구분마다 보여 주는 수)</summary>
    public int PageSize { get; set; }

    /// <summary>구분(공급자)별 결과 — 표시 순서대로 (Name = 구분 키, Title = 표시명)</summary>
    public List<(string Name, string Title, SearchResult Result)> Groups { get; set; }

    /// <summary>검색 범위(공급자) 목록 — 검색 전에도 보이는 구분 탭 (Name = 구분 키, Title = 표시명)</summary>
    public List<(string Name, string Title)> Providers { get; set; }
}
