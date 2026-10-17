namespace WinTuner.Core.Models;

/// <summary>
/// Windows service start type. Values intentionally match the registry
/// <c>Start</c> value under <c>HKLM\SYSTEM\CurrentControlSet\Services\&lt;name&gt;</c>.
/// </summary>
public enum ServiceStartMode
{
    Boot = 0,
    System = 1,
    Automatic = 2,
    Manual = 3,
    Disabled = 4,
}

/// <summary>Runtime service state as reported by the Service Control Manager.</summary>
public enum ServiceRuntimeStatus
{
    Unknown = 0,
    Stopped = 1,
    StartPending = 2,
    StopPending = 3,
    Running = 4,
    ContinuePending = 5,
    PausePending = 6,
    Paused = 7,
}

/// <summary>Categories of machine settings WinTuner can snapshot and roll back.</summary>
public enum ChangeTargetKind
{
    ServiceStartMode,
    RegistryValue,
    ScheduledTaskState,
}
