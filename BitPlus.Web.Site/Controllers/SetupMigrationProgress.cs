using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace BitPlus.Web.Site.Controllers;

/// <summary>
/// 설치 마이그레이션 진행 상태 (DB 에 CREATE/ALTER 문이 실행될 때마다 1 씩 올라가고, 화면이 주기적으로 조회해 진행바를 그린다).
/// 설치는 한 번에 하나만 진행하므로 정적 상태 하나만 둡니다. DB 종류별 SQL 개수 차이는 총 작업 수(마이그레이션 작업 개수)를 넘지 않게 맞춥니다.
/// </summary>
public class SetupMigrationProgress : DbCommandInterceptor
{
    private static readonly SetupMigrationProgress instance = new();
    private int total;
    private int current;
    private bool running;
    private bool succeeded;
    private string? message;
    private string? error;

    /// <summary>
    /// <see cref="SetupMigrationProgress"/> 기본 생성자.
    /// </summary>
    public SetupMigrationProgress()
    {
        this.total = 0;
        this.current = 0;
        this.running = false;
        this.succeeded = false;
        this.message = null;
        this.error = null;
    }

    /// <summary>공유 진행 상태 (인터셉터 겸용)</summary>
    public static SetupMigrationProgress Instance => instance;

    /// <summary>진행 중 여부, 실행한 작업 수, 총 작업 수, 성공 여부, 결과 메시지, 오류 메시지</summary>
    public (bool Running, int Current, int Total, bool Succeeded, string? Message, string? Error) Snapshot
        => (this.running, Math.Min(this.current, this.total), this.total, this.succeeded, this.message, this.error);

    /// <summary>
    /// 새 진행을 시작합니다.
    /// </summary>
    /// <param name="totalOperations">총 작업 수 (마이그레이션 작업 개수)</param>
    public void Start(int totalOperations)
    {
        this.total = totalOperations;
        this.current = 0;
        this.running = true;
        this.succeeded = false;
        this.message = null;
        this.error = null;
    }

    /// <summary>
    /// 진행을 끝냅니다.
    /// </summary>
    /// <param name="succeeded">성공 여부</param>
    /// <param name="message">성공 메시지</param>
    /// <param name="error">오류 메시지</param>
    public void Finish(bool succeeded, string? message = null, string? error = null)
    {
        if (succeeded) this.current = this.total;
        this.running = false;
        this.succeeded = succeeded;
        this.message = message;
        this.error = error;
    }

    /// <inheritdoc/>
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        this.Count(command);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        this.Count(command);
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// 스키마를 바꾸는 문(CREATE/ALTER)이면 진행을 올립니다.
    /// </summary>
    private void Count(DbCommand command)
    {
        if (!this.running) return;
        var text = command.CommandText.AsSpan().TrimStart();
        if (text.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) || text.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase)) Interlocked.Increment(ref this.current);
    }
}
