namespace WinTuner.Core.Models;

/// <summary>
/// A single intended mutation. <see cref="TargetId"/> is a stable identifier
/// whose meaning depends on <see cref="Kind"/>: a service name, a
/// <c>HIVE\Path\ValueName</c> registry address, or a scheduled-task path.
/// </summary>
public sealed record PlannedChange(
    ChangeTargetKind Kind,
    string TargetId,
    string DesiredValue,
    string? Description = null);

/// <summary>
/// A named, ordered set of intended changes (e.g. a built-in profile). A plan is
/// declarative and side-effect free until it is applied by the engine.
/// </summary>
public sealed class ChangePlan
{
    public string Name { get; init; } = "custom";
    public string? Description { get; init; }
    public IReadOnlyList<PlannedChange> Changes { get; init; } = Array.Empty<PlannedChange>();

    public ChangePlan() { }

    public ChangePlan(string name, string? description, IEnumerable<PlannedChange> changes)
    {
        Name = name;
        Description = description;
        Changes = changes.ToArray();
    }
}
