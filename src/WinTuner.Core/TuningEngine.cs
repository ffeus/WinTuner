using WinTuner.Core.Backup;
using WinTuner.Core.Models;
using WinTuner.Core.RegistryTools;
using WinTuner.Core.Services;
using WinTuner.Core.Tasks;

namespace WinTuner.Core;

/// <summary>
/// The single entry point for applying a <see cref="ChangePlan"/>. It always
/// snapshots first, then applies (or simulates) each change, returning a
/// <see cref="ChangeJournal"/> that pairs every mutation with its before/after
/// value and the snapshot needed to undo it.
/// </summary>
public sealed class TuningEngine
{
    private readonly SnapshotEngine _snapshots;
    private readonly RollbackEngine _rollback;
    private readonly ServiceManager _services;
    private readonly RegistryToolkit _registry;
    private readonly ScheduledTaskManager _tasks;

    public TuningEngine(
        SnapshotEngine snapshots,
        RollbackEngine rollback,
        ServiceManager services,
        RegistryToolkit registry,
        ScheduledTaskManager tasks)
    {
        _snapshots = snapshots;
        _rollback = rollback;
        _services = services;
        _registry = registry;
        _tasks = tasks;
    }

    /// <summary>Builds an engine wired to real system providers.</summary>
    public static TuningEngine CreateDefault()
    {
        var services = new ServiceManager();
        var registry = new RegistryToolkit();
        var tasks = new ScheduledTaskManager();
        return new TuningEngine(
            new SnapshotEngine(services, registry, tasks),
            new RollbackEngine(services, registry, tasks),
            services,
            registry,
            tasks);
    }

    /// <summary>
    /// Applies every change in <paramref name="plan"/>. With
    /// <paramref name="dryRun"/> the snapshot and diff are still produced but
    /// nothing is written to the machine.
    /// </summary>
    public ChangeJournal Apply(ChangePlan plan, bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var snapshot = _snapshots.Capture(plan.Name, plan.Changes);
        var journal = new ChangeJournal { PlanName = plan.Name, Snapshot = snapshot };

        foreach (var change in plan.Changes)
        {
            var before = _snapshots.ReadCurrent(change.Kind, change.TargetId);
            try
            {
                if (!dryRun)
                {
                    ApplyOne(change);
                }

                var after = dryRun ? change.DesiredValue : _snapshots.ReadCurrent(change.Kind, change.TargetId);
                journal.Records.Add(new ChangeRecord(
                    change.Kind, change.TargetId, before, after, Applied: !dryRun,
                    Message: change.Description));
            }
            catch (Exception ex)
            {
                journal.Records.Add(new ChangeRecord(
                    change.Kind, change.TargetId, before, null, Applied: false, Message: ex.Message));
            }
        }

        return journal;
    }

    /// <summary>Reverts the machine to the state recorded in a snapshot.</summary>
    public IReadOnlyList<ChangeRecord> Rollback(Snapshot snapshot) => _rollback.Restore(snapshot);

    private void ApplyOne(PlannedChange change)
    {
        switch (change.Kind)
        {
            case ChangeTargetKind.ServiceStartMode:
                if (!Enum.TryParse<ServiceStartMode>(change.DesiredValue, ignoreCase: true, out var mode))
                {
                    throw new ArgumentException($"'{change.DesiredValue}' is not a valid service start mode.");
                }

                _services.SetStartMode(change.TargetId, mode);
                break;

            case ChangeTargetKind.RegistryValue:
                _registry.WriteValue(change.TargetId, change.DesiredValue);
                break;

            case ChangeTargetKind.ScheduledTaskState:
                _tasks.SetState(change.TargetId, string.Equals(change.DesiredValue, "Enabled", StringComparison.OrdinalIgnoreCase));
                break;

            default:
                throw new NotSupportedException($"Unsupported change kind '{change.Kind}'.");
        }
    }
}
