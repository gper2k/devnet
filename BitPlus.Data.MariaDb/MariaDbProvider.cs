using BitPlus.Data.Provider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;

namespace BitPlus.Data.MariaDb;

/// <summary>
/// MariaDB 10 프로바이더. EF Core 는 MIT 라이선스 프로바이더 Microting.EntityFrameworkCore.MySql(Pomelo 포크, EF Core 10 지원, MySqlConnector 기반)를 씁니다.
/// 접속 테스트와 DB 생성은 MySqlConnection 으로 서버(DB 미지정)에 직접 접속해 처리합니다.
/// </summary>
public class MariaDbProvider : IDbProvider
{
    /// <summary>EF 가 생성할 SQL 의 기준 MariaDB 버전 (설계 시점에도 DB 접속 없이 쓰도록 고정)</summary>
    private static MariaDbServerVersion ServerVersion => new(new Version(10, 6));

    /// <summary>
    /// <see cref="MariaDbProvider"/> 기본 생성자.
    /// </summary>
    public MariaDbProvider()
    {
        this.Fields =
        [
            new("server", "DbServer", required: true),
            new("port", "DbPort", "number", "3306"),
            new("database", "DbName", required: true),
            new("user", "DbUser", required: true),
            new("password", "Password", "password"),
        ];
    }

    /// <inheritdoc/>
    public string Name => "MariaDb";

    /// <inheritdoc/>
    public string Title => "MariaDB 10";

    /// <inheritdoc/>
    public IReadOnlyList<DbProviderField> Fields { get; }

    /// <inheritdoc/>
    public string EfProviderName => "Microting.EntityFrameworkCore.MySql";

    /// <inheritdoc/>
    public string? MigrationsAssembly => null;

    /// <inheritdoc/>
    public string BuildConnectionString(IReadOnlyDictionary<string, string?> values)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = values.GetValueOrDefault("server")?.Trim() ?? string.Empty,
            Port = uint.TryParse(values.GetValueOrDefault("port"), out var port) ? port : 3306,
            Database = values.GetValueOrDefault("database")?.Trim() ?? string.Empty,
            UserID = values.GetValueOrDefault("user")?.Trim() ?? string.Empty,
            Password = values.GetValueOrDefault("password") ?? string.Empty,
            CharacterSet = "utf8mb4",
        };
        return builder.ConnectionString;
    }

    /// <inheritdoc/>
    public async Task<(string Version, bool DatabaseExists)> TestAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            var (server, database) = Server(connectionString);
            await using var conn = new MySqlConnection(server);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VERSION(); SELECT COUNT(*) FROM information_schema.SCHEMATA WHERE SCHEMA_NAME = @name;";
            cmd.Parameters.AddWithValue("@name", database);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            var version = reader.GetString(0);
            await reader.NextResultAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return (version, reader.GetInt64(0) > 0);
        }
        catch (MySqlException)
        {
            // 서버 수준(DB 미지정) 접속 권한이 없는 계정은 지정된 대상 DB 로 직접 접속 시도
            await using var conn = new MySqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VERSION();";
            var version = (await cmd.ExecuteScalarAsync(cancellationToken))?.ToString() ?? string.Empty;
            return (version, true); // 대상 DB 에 직접 접속 성공했으므로 DB 가 이미 존재함
        }
    }

    /// <inheritdoc/>
    public async Task<bool> EnsureDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var (_, exists) = await this.TestAsync(connectionString, cancellationToken);
        if (exists) return false;

        var (server, database) = Server(connectionString);
        await using var conn = new MySqlConnection(server);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE `{database.Replace("`", "``", StringComparison.Ordinal)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public void UseProvider(DbContextOptionsBuilder options, string connectionString, string? prefix)
    {
        options.UseMySql(connectionString, ServerVersion, mysql =>
        {
            mysql.MigrationsHistoryTable(string.IsNullOrWhiteSpace(prefix) ? "__EFMigrationsHistory" : $"{prefix.Trim()}_EFMigrationsHistory");
            if (this.MigrationsAssembly is not null) mysql.MigrationsAssembly(this.MigrationsAssembly);
            mysql.UseParameterizedCollectionMode(ParameterTranslationMode.MultipleParameters); // ID 목록 조회(Contains)를 개별 매개변수 IN 으로 (SQL Server 와 같게)
        });
        options.ReplaceService<IMigrationsAssembly, ProviderMigrationsAssembly>();
        options.ReplaceService<IHistoryRepository, MariaDbHistoryRepository>(); // MariaDB 호환 마이그레이션 잠금
        options.AddInterceptors(new MariaDbDeleteAliasInterceptor()); // ExecuteDeleteAsync 의 DELETE 별칭 문법을 MariaDB 문법으로
    }

    /// <summary>
    /// 접속 문자열을 서버 접속용(DB 미지정, 15초 제한)으로 바꾸고 대상 DB 이름을 돌려줍니다.
    /// </summary>
    private static (string Server, string Database) Server(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        builder.Database = string.Empty;
        builder.ConnectionTimeout = 15;
        return (builder.ConnectionString, database);
    }
}
