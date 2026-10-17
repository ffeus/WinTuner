using WinTuner.Core.Models;
using WinTuner.Core.RegistryTools;
using WinTuner.Core.Services;
using WinTuner.Core.Tasks;

namespace WinTuner.Core.Backup;

/// <summary>
/// Restores the exact values recorded in a <see cref="Snapshot"/>. Used both for
/// explicit user-initiated rollback and to undo a failed apply.
/// </summary>
public sealed class RollbackEngine
{
    private readonly ServiceManager _services;
    private readonly RegistryToolkit _registry;
    private readonly ScheduledTaskManager _tasks;

    public RollbackEngine(ServiceManager services, RegistryToolkit registry, ScheduledTaskManager tasks)
    {
        _services = services;
        _registry = registry;
        _tasks = tasks;
    }

    /// <summary>
    /// Restores every entry in <paramref name="snapshot"/> and returns one
    /// <see cref="ChangeRecord"/> per entry (before = current, after = restored).
    /// Entries whose original value was absent are skipped and reported.
    /// </summary>
    public IReadOnlyList<ChangeRecord> Restore(Snapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var records = new List<ChangeRecord>();
        foreach (var entry in snapshot.Entries)
        {
            var before = ReadCurrent(entry);
            try
            {
                if (entry.Value is null)
                {
                    records.Add(new ChangeRecord(entry.Kind, entry.TargetId, before, null,
                        Applied: false, "No original value captured; left unchanged."));
                    continue;
                }

                Apply(entry, entry.Value);
                records.Add(new ChangeRecord(entry.Kind, entry.TargetId, before, entry.Value, Applied: true));
            }
            catch (Exception ex)
            {
                records.Add(new ChangeRecord(entry.Kind, entry.TargetId, before, null, Applied: false, ex.Message));
            }
        }

        return records;
    }

    private string? ReadCurrent(SnapshotEntry entry) => entry.Kind switch
    {
        ChangeTargetKind.ServiceStartMode => _services.GetState(entry.TargetId).StartMode?.ToString(),
        ChangeTargetKind.RegistryValue => _registry.ReadValue(entry.TargetId),
        ChangeTargetKind.ScheduledTaskState => _tasks.GetState(entry.TargetId),
        _ => null,
    };

    private void Apply(SnapshotEntry entry, string value)
    {
        switch (entry.Kind)
        {
            case ChangeTargetKind.ServiceStartMode:
                if (Enum.TryParse<ServiceStartMode>(value, ignoreCase: true, out var mode))
                {
                    _services.SetStartMode(entry.TargetId, mode);
                }

                break;

            case ChangeTargetKind.RegistryValue:
                _registry.WriteValue(entry.TargetId, value);
                break;

            case ChangeTargetKind.ScheduledTaskState:
                _tasks.SetState(entry.TargetId, string.Equals(value, "Enabled", StringComparison.OrdinalIgnoreCase));
                break;
        }
    }
}
