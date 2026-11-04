using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace BitPlus.Data.MariaDb;

/// <summary>
/// MariaDB 마이그레이션 잠금 (GET_LOCK). 해제 시 RELEASE_LOCK 을 실행하며, 연결이 끊기면 서버가 잠금을 자동으로 풉니다.
/// </summary>
public class MariaDbMigrationsLock : IMigrationsDatabaseLock
{
    private readonly IRelationalConnection connection;

    /// <summary>
    /// <see cref="MariaDbMigrationsLock"/> 생성자.
    /// </summary>
    /// <param name="historyRepository">잠금을 잡은 기록 테이블 처리기</param>
    /// <param name="connection">잠금을 잡은 연결</param>
    public MariaDbMigrationsLock(IHistoryRepository historyRepository, IRelationalConnection connection)
    {
        this.HistoryRepository = historyRepository;
        this.connection = connection;
    }

    /// <inheritdoc/>
    public IHistoryRepository HistoryRepository { get; }

    /// <inheritdoc/>
    public IMigrationsDatabaseLock ReacquireIfNeeded(bool connectionReopened, bool? transactionRestarted)
        => connectionReopened ? this.HistoryRepository.AcquireDatabaseLock() : this;

    /// <inheritdoc/>
    public async Task<IMigrationsDatabaseLock> ReacquireIfNeededAsync(bool connectionReopened, bool? transactionRestarted, CancellationToken cancellationToken = default)
        => connectionReopened ? await this.HistoryRepository.AcquireDatabaseLockAsync(cancellationToken) : this;

    /// <summary>
    /// 잠금 해제 (연결이 이미 닫혔거나 끊긴 경우 서버가 자동으로 풀므로 안전하게 무시)
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (this.connection.DbConnection.State == System.Data.ConnectionState.Open)
            {
                using var command = this.connection.DbConnection.CreateCommand();
                command.CommandText = MariaDbHistoryRepository.ReleaseSql;
                command.ExecuteScalar();
            }
        }
        catch { }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 잠금 해제 (비동기, 연결이 이미 닫혔거나 끊긴 경우 서버가 자동으로 풀므로 안전하게 무시)
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (this.connection.DbConnection.State == System.Data.ConnectionState.Open)
            {
                await using var command = this.connection.DbConnection.CreateCommand();
                command.CommandText = MariaDbHistoryRepository.ReleaseSql;
                await command.ExecuteScalarAsync();
            }
        }
        catch { }
        GC.SuppressFinalize(this);
    }
}
