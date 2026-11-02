using LiftCall.Shared;
using Microsoft.Data.SqlClient;

namespace LiftCall.Api;

/// <summary>SQL Server 版。テーブルは Sql/01_CreateTables.sql を参照。</summary>
public class SqlServerLiftCallRepository : ILiftCallRepository
{
    private readonly string _connectionString;

    public SqlServerLiftCallRepository(string connectionString)
        => _connectionString = connectionString;

    private async Task<SqlConnection> OpenAsync()
    {
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<LiftStateDto> GetLiftStateAsync()
    {
        await using var conn = await OpenAsync();
        await using var cmd = new SqlCommand(
            "SELECT IsRunning, UpdatedAt FROM dbo.LiftState WHERE Id = 1", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return new LiftStateDto(reader.GetBoolean(0), reader.GetDateTime(1));

        // 初期行が無い場合は運転中として扱う
        return new LiftStateDto(true, DateTime.Now);
    }

    public async Task SetLiftStateAsync(bool isRunning, string? updatedBy)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new SqlCommand(@"
UPDATE dbo.LiftState
   SET IsRunning = @IsRunning, UpdatedAt = SYSDATETIME(), UpdatedBy = @UpdatedBy
 WHERE Id = 1;
IF @@ROWCOUNT = 0
    INSERT INTO dbo.LiftState (Id, IsRunning, UpdatedAt, UpdatedBy)
    VALUES (1, @IsRunning, SYSDATETIME(), @UpdatedBy);", conn);
        cmd.Parameters.AddWithValue("@IsRunning", isRunning);
        cmd.Parameters.AddWithValue("@UpdatedBy", (object?)updatedBy ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<CallDto>> GetCallsAsync()
    {
        var list = new List<CallDto>();
        await using var conn = await OpenAsync();
        await using var cmd = new SqlCommand(@"
SELECT UnitNo, CallType, Palette, CalledAt, IsResponding
  FROM dbo.LiftCall
 ORDER BY CalledAt, UnitNo", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CallDto(
                reader.GetInt32(0),
                (CallType)reader.GetByte(1),
                (PaletteType)reader.GetByte(2),
                reader.GetDateTime(3),
                reader.GetBoolean(4)));
        }
        return list;
    }

    public async Task UpsertCallAsync(int unitNo, CallType type, PaletteType palette)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new SqlCommand(@"
MERGE dbo.LiftCall WITH (HOLDLOCK) AS t
USING (SELECT @UnitNo AS UnitNo) AS s ON t.UnitNo = s.UnitNo
WHEN MATCHED THEN
    UPDATE SET CallType = @CallType, Palette = @Palette,
               CalledAt = SYSDATETIME(), IsResponding = 0, RespondedAt = NULL
WHEN NOT MATCHED THEN
    INSERT (UnitNo, CallType, Palette, CalledAt, IsResponding)
    VALUES (@UnitNo, @CallType, @Palette, SYSDATETIME(), 0);", conn);
        cmd.Parameters.AddWithValue("@UnitNo", unitNo);
        cmd.Parameters.AddWithValue("@CallType", (byte)type);
        cmd.Parameters.AddWithValue("@Palette", (byte)palette);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCallAsync(int unitNo)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new SqlCommand("DELETE FROM dbo.LiftCall WHERE UnitNo = @UnitNo", conn);
        cmd.Parameters.AddWithValue("@UnitNo", unitNo);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> SetRespondingAsync(int unitNo, bool isResponding)
    {
        await using var conn = await OpenAsync();
        await using var cmd = new SqlCommand(@"
UPDATE dbo.LiftCall
   SET IsResponding = @IsResponding,
       RespondedAt = CASE WHEN @IsResponding = 1 THEN SYSDATETIME() ELSE NULL END
 WHERE UnitNo = @UnitNo", conn);
        cmd.Parameters.AddWithValue("@UnitNo", unitNo);
        cmd.Parameters.AddWithValue("@IsResponding", isResponding);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> CompleteCallAsync(int unitNo, string? completedBy)
    {
        await using var conn = await OpenAsync();

        // 履歴への記録と呼出の削除を1トランザクションで実行
        await using var cmd = new SqlCommand(@"
SET XACT_ABORT ON;
BEGIN TRAN;

INSERT INTO dbo.LiftCallHistory
    (UnitNo, CallType, Palette, CalledAt, RespondedAt, CompletedAt, CompletedBy)
SELECT UnitNo, CallType, Palette, CalledAt, RespondedAt, SYSDATETIME(), @CompletedBy
  FROM dbo.LiftCall WITH (UPDLOCK, HOLDLOCK)
 WHERE UnitNo = @UnitNo;

DELETE FROM dbo.LiftCall WHERE UnitNo = @UnitNo;
DECLARE @Deleted INT = @@ROWCOUNT;

COMMIT;
SELECT @Deleted;", conn);
        cmd.Parameters.AddWithValue("@UnitNo", unitNo);
        cmd.Parameters.AddWithValue("@CompletedBy", (object?)completedBy ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
}
