namespace BitPlus.Web.Site.Widgets;

/// <summary>
/// 위젯 데이터 로더 (선택): 데이터가 필요한 위젯은 같은 키의 로더를 하나 두면, 출력 뷰(<c>View.cshtml</c>)의 모델 <see cref="SlotView.Data"/> 로 결과를 받습니다.
/// 로더가 없는 위젯은 설정값만으로 그립니다. 구현 클래스는 사이트·플러그인 어셈블리에서 자동으로 찾아 요청 단위(scoped)로 등록되므로 생성자로 DbContext 를 받을 수 있습니다.
/// 결과에는 현재 사용자가 볼 수 있는 것만 담아야 합니다.
/// </summary>
public interface IWidgetLoader
{
    /// <summary>위젯 키 (Views/Widgets 의 폴더 이름)</summary>
    string Widget { get; }

    /// <summary>
    /// 설정값으로 위젯 데이터를 만듭니다 (null 이면 위젯을 그리지 않음).
    /// </summary>
    /// <param name="context">페이지·슬롯·설정·현재 요청</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task<object?> LoadAsync(WidgetContext context, CancellationToken cancellationToken = default);
}
