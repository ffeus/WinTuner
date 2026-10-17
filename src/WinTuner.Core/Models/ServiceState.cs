namespace WinTuner.Core.Models;

/// <summary>
/// An immutable observation of a single service: its identity, configured start
/// mode and current runtime status. <see cref="Exists"/> is false when the
/// service is not installed, in which case the other fields are null/unknown.
/// </summary>
public sealed record ServiceState(
    string Name,
    bool Exists,
    string? DisplayName,
    ServiceStartMode? StartMode,
    ServiceRuntimeStatus Status)
{
    public static ServiceState Missing(string name) =>
        new(name, Exists: false, DisplayName: null, StartMode: null, Status: ServiceRuntimeStatus.Unknown);
}
