using LiftCall.Api;
using LiftCall.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 保存先の切り替え（appsettings.json の "Storage"）
var storage = builder.Configuration["Storage"] ?? "Memory";
if (storage.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString("LiftCall")
        ?? throw new InvalidOperationException("ConnectionStrings:LiftCall が未設定です。");
    builder.Services.AddSingleton<ILiftCallRepository>(new SqlServerLiftCallRepository(connectionString));
}
else
{
    builder.Services.AddSingleton<ILiftCallRepository, InMemoryLiftCallRepository>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "LiftCall API is running.");

var api = app.MapGroup("/api");

// ---- ポーリング用：運転状態＋呼出一覧を一括取得 ----
api.MapGet("/status", async (ILiftCallRepository repo) =>
{
    var state = await repo.GetLiftStateAsync();
    var calls = await repo.GetCallsAsync();
    return Results.Ok(new StatusDto(state.IsRunning, state.UpdatedAt, calls));
});

// ---- 運転状態 ----
api.MapGet("/lift-state", async (ILiftCallRepository repo)
    => Results.Ok(await repo.GetLiftStateAsync()));

api.MapPut("/lift-state", async (SetLiftStateRequest req, ILiftCallRepository repo) =>
{
    await repo.SetLiftStateAsync(req.IsRunning, req.UpdatedBy);
    return Results.Ok(await repo.GetLiftStateAsync());
});

// ---- 呼出 ----
api.MapPut("/calls/{unitNo:int}", async (int unitNo, PutCallRequest req, ILiftCallRepository repo) =>
{
    if (unitNo <= 0)
        return Results.BadRequest("号機番号が不正です。");
    if (!Enum.IsDefined(req.Type))
        return Results.BadRequest("呼出内容が不正です。");
    if (req.Type == CallType.ProductPickup && req.Palette == PaletteType.None)
        return Results.BadRequest("製品引取にはパレットの指定が必要です。");

    // 製品引取以外はパレット指定なしとして保存
    var palette = req.Type == CallType.ProductPickup ? req.Palette : PaletteType.None;
    await repo.UpsertCallAsync(unitNo, req.Type, palette);
    return Results.NoContent();
});

api.MapDelete("/calls/{unitNo:int}", async (int unitNo, ILiftCallRepository repo) =>
{
    await repo.DeleteCallAsync(unitNo);
    return Results.NoContent();
});

// 対応完了：履歴へ記録して呼出リストから削除（所要時間は DB 側で保持）
api.MapPost("/calls/{unitNo:int}/complete", async (int unitNo, CompleteCallRequest req, ILiftCallRepository repo) =>
{
    var found = await repo.CompleteCallAsync(unitNo, req.CompletedBy);
    return found ? Results.NoContent() : Results.NotFound();
});

api.MapPut("/calls/{unitNo:int}/response", async (int unitNo, SetRespondingRequest req, ILiftCallRepository repo) =>
{
    var found = await repo.SetRespondingAsync(unitNo, req.IsResponding);
    return found ? Results.NoContent() : Results.NotFound();
});

app.Run();
