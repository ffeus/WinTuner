using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using WinTuner.Core;
using WinTuner.Core.Backup;
using WinTuner.Core.Models;
using WinTuner.Core.Profiles;
using WinTuner.Core.RegistryTools;
using WinTuner.Core.Services;
using WinTuner.Core.Tasks;

namespace WinTuner.Gui.ViewModels;

/// <summary>
/// Presentation state for the main window. Thin: it delegates all real work to
/// <see cref="TuningEngine"/> and only exposes observable collections/status.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly TuningEngine _engine;
    private readonly SnapshotEngine _snapshotEngine;
    private readonly string _snapshotDir;
    private Snapshot? _activeSnapshot;

    public ObservableCollection<ChangePlan> Plans { get; } = new();
    public ObservableCollection<ChangeRecord> Records { get; } = new();

    private ChangePlan? _selectedPlan;
    public ChangePlan? SelectedPlan
    {
        get => _selectedPlan;
        set { _selectedPlan = value; OnPropertyChanged(); }
    }

    private string _status = "Ready. Snapshot-first: nothing changes without a backup.";
    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        var services = new ServiceManager();
        var registry = new RegistryToolkit();
        var tasks = new ScheduledTaskManager();

        _snapshotEngine = new SnapshotEngine(services, registry, tasks);
        _engine = TuningEngine.CreateDefault();
        _snapshotDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "WinTuner", "snapshots");

        foreach (var plan in BuiltInProfiles.All())
        {
            Plans.Add(plan);
        }

        SelectedPlan = Plans.FirstOrDefault();
    }

    /// <summary>Shows what a plan would do, writing nothing.</summary>
    public void Preview()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var journal = _engine.Apply(SelectedPlan, dryRun: true);
        ReplaceRecords(journal.Records);
        Status = $"Preview of '{journal.PlanName}': {journal.Records.Count} changes (nothing written).";
    }

    /// <summary>Applies the selected plan and persists a rollback snapshot.</summary>
    public void Apply()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var journal = _engine.Apply(SelectedPlan, dryRun: false);
        ReplaceRecords(journal.Records);
        _activeSnapshot = journal.Snapshot;

        if (journal.Snapshot is null)
        {
            Status = $"Applied '{journal.PlanName}': {journal.AppliedCount} ok, {journal.SkippedCount} skipped.";
            return;
        }

        try
        {
            Directory.CreateDirectory(_snapshotDir);
            var path = Path.Combine(_snapshotDir, $"{journal.Snapshot.Id}.json");
            _snapshotEngine.Save(journal.Snapshot, path);
            Status = $"Applied '{journal.PlanName}': {journal.AppliedCount} ok, " +
                     $"{journal.SkippedCount} skipped. Snapshot: {path}";
        }
        catch (Exception ex)
        {
            Status = $"Applied '{journal.PlanName}' but saving the snapshot failed: {ex.Message}";
        }
    }

    /// <summary>Restores the snapshot captured by the most recent apply.</summary>
    public void Rollback()
    {
        if (_activeSnapshot is null)
        {
            Status = "Nothing to roll back in this session — apply a plan first.";
            return;
        }

        var records = _engine.Rollback(_activeSnapshot);
        ReplaceRecords(records);
        Status = $"Rolled back {records.Count(r => r.Applied)} of {records.Count} entries.";
    }

    private void ReplaceRecords(IEnumerable<ChangeRecord> records)
    {
        Records.Clear();
        foreach (var record in records)
        {
            Records.Add(record);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
