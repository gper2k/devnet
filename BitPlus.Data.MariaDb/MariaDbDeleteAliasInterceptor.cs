using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace BitPlus.Data.MariaDb;

/// <summary>
/// EF Core 일괄 삭제(<c>ExecuteDeleteAsync</c>)가 만드는 <c>DELETE FROM `표` AS `별칭` WHERE …</c> 를 MariaDB 가 받는 <c>DELETE `별칭` FROM `표` AS `별칭` WHERE …</c> 로 바꿉니다.
/// MySQL 8.0.16+ 의 단일 표 DELETE 별칭 문법을 MariaDB 10 은 문법 오류로 거부하므로, 프로바이더가 이 문법을 만들 경우를 대비해 호출하는 코드마다 고치지 않고 이 프로바이더에서 한 번에 맞춥니다.
/// </summary>
public partial class MariaDbDeleteAliasInterceptor : DbCommandInterceptor
{
    /// <summary>
    /// <see cref="MariaDbDeleteAliasInterceptor"/> 기본 생성자.
    /// </summary>
    public MariaDbDeleteAliasInterceptor() { }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Rewrite(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Rewrite(command);
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 명령문이 별칭 붙은 단일 표 DELETE 면 다중 표 DELETE 문법(삭제 대상 별칭을 DELETE 뒤에 명시)으로 바꿉니다.
    /// </summary>
    /// <param name="command">실행할 명령</param>
    public static void Rewrite(DbCommand command) => command.CommandText = DeleteAliasRegex().Replace(command.CommandText, "DELETE $2 FROM $1 AS $2");

    /// <summary>문장 시작의 <c>DELETE FROM `표` AS `별칭`</c></summary>
    [GeneratedRegex(@"(?<=^|;\s*)DELETE FROM (`[^`]+`) AS (`[^`]+`)", RegexOptions.Multiline)]
    private static partial Regex DeleteAliasRegex();
}
