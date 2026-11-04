using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace BitPlus.Data.MariaDb;

/// <summary>
/// MariaDB 용 마이그레이션 기록 테이블 처리. MariaDB 는 GET_LOCK(이름, -1)(무제한 대기)에 NULL 을 돌려줘 마이그레이션이 실패하므로
/// 양수 대기 시간으로 잠금을 잡습니다.
/// </summary>
public class MariaDbHistoryRepository : HistoryRepository
{
    /// <summary>마이그레이션 잠금 이름</summary>
    private const string LockName = "__EFMigrationsLock";

    /// <summary>잠금 대기 시간(초)</summary>
    private const int LockTimeoutSeconds = 3600;

    /// <summary>
    /// <see cref="MariaDbHistoryRepository"/> 생성자 (EF 서비스 주입).
    /// </summary>
    /// <param name="dependencies">기록 테이블 의존성</param>
    public MariaDbHistoryRepository(HistoryRepositoryDependencies dependencies) : base(dependencies) { }

    /// <inheritdoc/>
    public override LockReleaseBehavior LockReleaseBehavior => LockReleaseBehavior.Connection;

    /// <inheritdoc/>
    protected override string ExistsSql
        => $"SELECT 1 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = {this.Dependencies.TypeMappingSource.GetMapping(typeof(string)).GenerateSqlLiteral(this.TableName)};";

    /// <inheritdoc/>
    protected override bool InterpretExistsResult(object? value) => value is not null && value != DBNull.Value;

    /// <inheritdoc/>
    public override string GetCreateIfNotExistsScript()
        => this.GetCreateScript().Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS", StringComparison.Ordinal);

    /// <inheritdoc/>
    public override string GetBeginIfNotExistsScript(string migrationId) => throw new NotSupportedException("Idempotent scripts are not supported for MariaDB.");

    /// <inheritdoc/>
    public override string GetBeginIfExistsScript(string migrationId) => throw new NotSupportedException("Idempotent scripts are not supported for MariaDB.");

    /// <inheritdoc/>
    public override string GetEndIfScript() => throw new NotSupportedException("Idempotent scripts are not supported for MariaDB.");

    /// <inheritdoc/>
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        var connection = this.Dependencies.Connection;
        connection.Open();
        using var command = connection.DbConnection.CreateCommand();
        command.CommandText = $"SELECT GET_LOCK('{LockName}', {LockTimeoutSeconds});";
        if (Convert.ToInt64(command.ExecuteScalar() ?? 0L) != 1) throw new TimeoutException("Could not acquire the migration lock.");
        return new MariaDbMigrationsLock(this, connection);
    }

    /// <inheritdoc/>
    public override async Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
    {
        var connection = this.Dependencies.Connection;
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.DbConnection.CreateCommand();
        command.CommandText = $"SELECT GET_LOCK('{LockName}', {LockTimeoutSeconds});";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L) != 1) throw new TimeoutException("Could not acquire the migration lock.");
        return new MariaDbMigrationsLock(this, connection);
    }

    /// <summary>
    /// 잠금 해제 SQL
    /// </summary>
    internal static string ReleaseSql => $"SELECT RELEASE_LOCK('{LockName}');";
}
