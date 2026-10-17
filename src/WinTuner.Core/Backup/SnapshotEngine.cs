using System.Text.Json;
using System.Text.Json.Serialization;
using WinTuner.Core.Models;
using WinTuner.Core.RegistryTools;
using WinTuner.Core.Services;
using WinTuner.Core.Tasks;

namespace WinTuner.Core.Backup;

/// <summary>
/// Captures the current value of every target referenced by a plan, producing a
/// <see cref="Snapshot"/>. Snapshots are the foundation of WinTuner's rollback
/// guarantee: nothing is ever changed without a snapshot first.
/// </summary>
public sealed class SnapshotEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ServiceManager _services;
    private readonly RegistryToolkit _registry;
    private readonly ScheduledTaskManager _tasks;

    public SnapshotEngine(ServiceManager services, RegistryToolkit registry, ScheduledTaskManager tasks)
    {
        _services = services;
        _registry = registry;
        _tasks = tasks;
    }

    /// <summary>Captures a snapshot of all distinct targets in <paramref name="changes"/>.</summary>
    public Snapshot Capture(string label, IEnumerable<PlannedChange> changes)
    {
        var snapshot = new Snapshot { Label = label };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var change in changes)
        {
            if (!seen.Add($"{change.Kind}:{change.TargetId}"))
            {
                continue;
            }

            snapshot.Entries.Add(new SnapshotEntry(change.Kind, change.TargetId, ReadCurrent(change.Kind, change.TargetId)));
        }

        return snapshot;
    }

    /// <summary>Reads the current encoded value of a single target.</summary>
    public string? ReadCurrent(ChangeTargetKind kind, string targetId) => kind switch
    {
        ChangeTargetKind.ServiceStartMode => _services.GetState(targetId).StartMode?.ToString(),
        ChangeTargetKind.RegistryValue => _registry.ReadValue(targetId),
        ChangeTargetKind.ScheduledTaskState => _tasks.GetState(targetId),
        _ => null,
    };

    /// <summary>Writes a snapshot as readable JSON to the given file path.</summary>
    public void Save(Snapshot snapshot, string path)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    /// <summary>Loads a snapshot from a JSON file.</summary>
    public Snapshot Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Snapshot>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Snapshot file '{path}' is empty or invalid.");
    }
}
