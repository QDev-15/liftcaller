using System.Globalization;
using System.Text;
using IMIP.TochuForkLift.Api.Services;
using IMIP.TochuForkLift.Shared.Contracts;

namespace IMIP.TochuForkLift.Api.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/history", async (
            DateTime? from, DateTime? to, int? unitNo, CallType? callType, string? format,
            ICallService service, CancellationToken ct) =>
        {
            var entries = await service.GetHistoryAsync(from, to, unitNo, callType, ct);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var csv = ToCsv(entries);
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
                return Results.Bytes(bytes, "text/csv", $"history_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            }

            return Results.Ok(entries);
        })
        .WithName("GetHistory");
    }

    private static string ToCsv(List<HistoryEntryDto> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("HistoryId,UnitNo,CallType,Palette,CalledAt,RespondedAt,CompletedAt,CompletedBy,DurationSeconds,ResponseWaitSeconds");
        foreach (var e in entries)
        {
            sb.AppendLine(string.Join(",",
                e.HistoryId,
                e.UnitNo,
                e.CallType,
                e.Palette,
                e.CalledAt.ToString("O", CultureInfo.InvariantCulture),
                e.RespondedAt?.ToString("O", CultureInfo.InvariantCulture),
                e.CompletedAt.ToString("O", CultureInfo.InvariantCulture),
                CsvEscape(e.CompletedBy),
                e.DurationSeconds,
                e.ResponseWaitSeconds));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
