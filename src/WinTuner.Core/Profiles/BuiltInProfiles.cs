using WinTuner.Core.Models;

namespace WinTuner.Core.Profiles;

/// <summary>
/// Declarative, side-effect-free plans shipped with WinTuner.
/// <para>
/// The service names and start modes reflect publicly documented Windows Update
/// behavior; they are data, not code copied from another tool. Every plan can be
/// dry-run first and is fully reversible through the snapshot engine.
/// </para>
/// </summary>
public static class BuiltInProfiles
{
    private const string NoAutoUpdateKey =
        @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\NoAutoUpdate";

    private static readonly (string Service, ServiceStartMode Enabled, ServiceStartMode Disabled)[] UpdateServices =
    {
        ("wuauserv",       ServiceStartMode.Automatic, ServiceStartMode.Disabled),
        ("UsoSvc",         ServiceStartMode.Automatic, ServiceStartMode.Manual),
        ("WaaSMedicSvc",   ServiceStartMode.Manual,    ServiceStartMode.Disabled),
        ("BITS",           ServiceStartMode.Manual,    ServiceStartMode.Manual),
        ("dosvc",          ServiceStartMode.Automatic, ServiceStartMode.Disabled),
        ("InstallService", ServiceStartMode.Manual,    ServiceStartMode.Disabled),
    };

    /// <summary>Plan that turns Windows Update off and keeps it off.</summary>
    public static ChangePlan DisableWindowsUpdate()
    {
        var changes = new List<PlannedChange>();

        foreach (var (service, _, disabled) in UpdateServices)
        {
            changes.Add(new PlannedChange(
                ChangeTargetKind.ServiceStartMode, service, disabled.ToString(),
                $"Set service '{service}' to {disabled}."));
        }

        changes.Add(new PlannedChange(
            ChangeTargetKind.RegistryValue, NoAutoUpdateKey, "DWord:1",
            "Marks automatic updates as disabled via policy."));

        return new ChangePlan("disable-windows-update",
            "Disable Windows Update services and the automatic-update policy.", changes);
    }

    /// <summary>Plan that restores Windows Update to its default behavior.</summary>
    public static ChangePlan EnableWindowsUpdate()
    {
        var changes = new List<PlannedChange>();

        foreach (var (service, enabled, _) in UpdateServices)
        {
            changes.Add(new PlannedChange(
                ChangeTargetKind.ServiceStartMode, service, enabled.ToString(),
                $"Set service '{service}' to {enabled}."));
        }

        changes.Add(new PlannedChange(
            ChangeTargetKind.RegistryValue, NoAutoUpdateKey, "DWord:0",
            "Re-enables automatic updates via policy."));

        return new ChangePlan("enable-windows-update",
            "Restore Windows Update services and re-enable automatic updates.", changes);
    }

    /// <summary>Enumerates all built-in plans by id.</summary>
    public static IEnumerable<ChangePlan> All()
    {
        yield return DisableWindowsUpdate();
        yield return EnableWindowsUpdate();
    }

    /// <summary>Looks up a built-in plan by id (e.g. "disable-windows-update").</summary>
    public static ChangePlan? Find(string id) =>
        All().FirstOrDefault(p => string.Equals(p.Name, id, StringComparison.OrdinalIgnoreCase));
}
