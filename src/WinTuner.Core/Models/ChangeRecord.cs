namespace WinTuner.Core.Models;

/// <summary>
/// The observed result of applying one <see cref="PlannedChange"/>: what the
/// value was before, what it is now, and whether the write succeeded.
/// </summary>
public sealed record ChangeRecord(
    ChangeTargetKind Kind,
    string TargetId,
    string? Before,
    string? After,
    bool Applied,
    string? Message = null);

/// <summary>
/// A complete audit log for one apply operation, linked to the pre-change
/// snapshot so the whole batch can be reverted as a unit.
/// </summary>
public sealed class ChangeJournal
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;
    public string PlanName { get; init; } = "";
    public Snapshot? Snapshot { get; init; }
    public List<ChangeRecord> Records { get; init; } = new();

    public int AppliedCount => Records.Count(r => r.Applied);
    public int SkippedCount => Records.Count(r => !r.Applied);
}
