using System.Text;
using WinTuner.Core;
using WinTuner.Core.Backup;
using WinTuner.Core.Models;
using WinTuner.Core.Profiles;
using WinTuner.Core.RegistryTools;
using WinTuner.Core.Services;
using WinTuner.Core.Tasks;

// WinTuner CLI — declarative, snapshot-first Windows tuning.
// Every apply writes a snapshot to %ProgramData%\WinTuner\snapshots so it can be
// reversed with `wintuner rollback <file>`.

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || IsHelp(args[0]))
{
    PrintUsage();
    return 0;
}

var snapshotDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "WinTuner", "snapshots");

var services = new ServiceManager();
var registry = new RegistryToolkit();
var tasks = new ScheduledTaskManager();
var snapshotEngine = new SnapshotEngine(services, registry, tasks);
var engine = TuningEngine.CreateDefault();

try
{
    return args[0].ToLowerInvariant() switch
    {
        "list" => ListPlans(),
        "status" => ShowStatus(args.Skip(1).ToArray()),
        "plan" => ShowPlan(args.ElementAtOrDefault(1)),
        "apply" => Apply(args.ElementAtOrDefault(1), args.Contains("--dry-run")),
        "rollback" => Rollback(args.ElementAtOrDefault(1)),
        _ => Unknown(args[0]),
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 3;
}

int ListPlans()
{
    Console.WriteLine("Built-in plans:");
    foreach (var plan in BuiltInProfiles.All())
    {
        Console.WriteLine($"  {plan.Name,-24} {plan.Description} ({plan.Changes.Count} changes)");
    }

    return 0;
}

int ShowStatus(string[] names)
{
    var targets = names.Length > 0
        ? names
        : new[] { "wuauserv", "UsoSvc", "WaaSMedicSvc", "BITS", "dosvc", "InstallService" };

    Console.WriteLine($"{"Service",-18} {"Exists",-7} {"StartMode",-12} Status");
    foreach (var name in targets)
    {
        var state = services.GetState(name);
        Console.WriteLine($"{state.Name,-18} {(state.Exists ? "yes" : "no"),-7} " +
                          $"{(state.StartMode?.ToString() ?? "-"),-12} {state.Status}");
    }

    return 0;
}

int ShowPlan(string? id)
{
    var plan = Resolve(id);
    if (plan is null)
    {
        return 2;
    }

    Console.WriteLine($"Plan: {plan.Name}");
    Console.WriteLine($"      {plan.Description}");
    Console.WriteLine();
    Console.WriteLine($"{"Kind",-20} {"Target",-70} {"Current",-14} Desired");
    foreach (var change in plan.Changes)
    {
        var current = snapshotEngine.ReadCurrent(change.Kind, change.TargetId) ?? "<absent>";
        Console.WriteLine($"{change.Kind,-20} {Shorten(change.TargetId),-70} {Truncate(current, 14),-14} {change.DesiredValue}");
    }

    return 0;
}

int Apply(string? id, bool dryRun)
{
    var plan = Resolve(id);
    if (plan is null)
    {
        return 2;
    }

    var journal = engine.Apply(plan, dryRun);
    PrintJournal(journal, dryRun);

    if (dryRun || journal.Snapshot is null)
    {
        return 0;
    }

    Directory.CreateDirectory(snapshotDir);
    var path = Path.Combine(snapshotDir, $"{journal.Snapshot.Id}.json");
    snapshotEngine.Save(journal.Snapshot, path);
    Console.WriteLine();
    Console.WriteLine($"Snapshot saved: {path}");
    Console.WriteLine($"Roll back with: wintuner rollback \"{path}\"");

    return journal.Records.Any(r => !r.Applied) ? 1 : 0;
}

int Rollback(string? file)
{
    if (string.IsNullOrWhiteSpace(file))
    {
        Console.Error.WriteLine("usage: wintuner rollback <snapshot.json>");
        return 2;
    }

    var snapshot = snapshotEngine.Load(file);
    var records = engine.Rollback(snapshot);

    Console.WriteLine($"Restored {records.Count(r => r.Applied)} of {records.Count} entries from {snapshot.Id}.");
    foreach (var record in records.Where(r => !r.Applied))
    {
        Console.WriteLine($"  skipped {record.TargetId}: {record.Message}");
    }

    return records.Any(r => !r.Applied) ? 1 : 0;
}

int Unknown(string command)
{
    Console.Error.WriteLine($"error: unknown command '{command}'.");
    PrintUsage();
    return 2;
}

ChangePlan? Resolve(string? id)
{
    if (string.IsNullOrWhiteSpace(id))
    {
        Console.Error.WriteLine("error: a plan id is required (see `wintuner list`).");
        return null;
    }

    var plan = BuiltInProfiles.Find(id);
    if (plan is null)
    {
        Console.Error.WriteLine($"error: no built-in plan '{id}' (see `wintuner list`).");
    }

    return plan;
}

void PrintJournal(ChangeJournal journal, bool dryRun)
{
    Console.WriteLine(dryRun
        ? $"DRY RUN — {journal.PlanName} ({journal.Records.Count} changes, nothing written)"
        : $"APPLIED — {journal.PlanName} ({journal.AppliedCount} ok, {journal.SkippedCount} skipped)");
    Console.WriteLine();

    foreach (var record in journal.Records)
    {
        var mark = record.Applied ? "ok " : "!! ";
        Console.WriteLine($"{mark}{record.Kind,-20} {Shorten(record.TargetId),-70} " +
                          $"{record.Before ?? "<absent>"} -> {record.After ?? "<failed>"}");
        if (!record.Applied && record.Message is not null)
        {
            Console.WriteLine($"     {record.Message}");
        }
    }
}

static bool IsHelp(string arg) => arg is "-h" or "--help" or "help";

static string Shorten(string value) => value.Length > 68 ? "…" + value[^67..] : value;

static string Truncate(string value, int max) => value.Length > max ? value[..(max - 1)] + "…" : value;

static void PrintUsage()
{
    Console.WriteLine("""
        WinTuner — declarative, rollback-safe Windows tuning.

        usage:
          wintuner list                    List built-in plans.
          wintuner status [service...]     Show service start modes and status.
          wintuner plan <id>               Preview a plan (no changes written).
          wintuner apply <id> [--dry-run]  Apply a plan (snapshot written first).
          wintuner rollback <snapshot>     Restore a saved snapshot.

        notes:
          - Run elevated (Administrator) when applying or rolling back.
          - Snapshots are stored under %ProgramData%\WinTuner\snapshots.
        """);
}
