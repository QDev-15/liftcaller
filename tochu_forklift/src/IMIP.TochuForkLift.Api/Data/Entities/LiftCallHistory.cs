namespace IMIP.TochuForkLift.Api.Data.Entities;

/// <summary>Closed calls — written in the same transaction that clears LiftCall on Complete.</summary>
public class LiftCallHistory
{
    public long HistoryId { get; set; }
    public int UnitNo { get; set; }
    public byte CallType { get; set; }
    public byte Palette { get; set; }
    public DateTime CalledAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    /// <summary>Computed column (PERSISTED), SQL: DATEDIFF(SECOND, CalledAt, CompletedAt).</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Computed column (PERSISTED), SQL: DATEDIFF(SECOND, CalledAt, RespondedAt).</summary>
    public int? ResponseWaitSeconds { get; set; }
}
