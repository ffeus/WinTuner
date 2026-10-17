namespace WinTuner.Core.Models;

/// <summary>
/// A captured point-in-time state of the settings WinTuner can modify. Written
/// before any apply so a rollback can restore exact previous values.
/// </summary>
public sealed class Snapshot
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Label { get; init; } = "";
    public string MachineName { get; init; } = Environment.MachineName;
    public List<SnapshotEntry> Entries { get; init; } = new();
}

/// <summary>One captured value inside a <see cref="Snapshot"/>.</summary>
public sealed record SnapshotEntry(
    ChangeTargetKind Kind,
    string TargetId,
    string? Value);
