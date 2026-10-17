using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using WinTuner.Core.Interop;
using WinTuner.Core.Models;

namespace WinTuner.Core.Services;

/// <summary>
/// Reads and writes Windows service configuration.
/// <para>
/// Reads come from the service registry key, which is the source of truth for
/// the configured start type. Writes go through the Service Control Manager so
/// the change is applied atomically and immediately visible to the SCM.
/// </para>
/// </summary>
public sealed class ServiceManager
{
    private const string ServicesKeyPath = @"SYSTEM\CurrentControlSet\Services";

    /// <summary>Observes a service's identity, configured start mode and runtime status.</summary>
    public ServiceState GetState(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var key = Registry.LocalMachine.OpenSubKey($@"{ServicesKeyPath}\{name}", writable: false);
        if (key is null)
        {
            return ServiceState.Missing(name);
        }

        var displayName = key.GetValue("DisplayName") as string;
        var startValue = key.GetValue("Start");
        ServiceStartMode? mode = startValue is int i && Enum.IsDefined(typeof(ServiceStartMode), i)
            ? (ServiceStartMode)i
            : null;

        return new ServiceState(name, Exists: true, displayName, mode, QueryRuntimeStatus(name));
    }

    /// <summary>Returns true when a service with this name is installed.</summary>
    public bool Exists(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        using var key = Registry.LocalMachine.OpenSubKey($@"{ServicesKeyPath}\{name}", writable: false);
        return key is not null;
    }

    /// <summary>
    /// Changes a service start type through the SCM. Requires administrative
    /// rights; throws <see cref="Win32Exception"/> when the change is refused.
    /// </summary>
    public void SetStartMode(string name, ServiceStartMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var scm = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"OpenSCManager failed for '{name}'.");
        }

        try
        {
            var service = NativeMethods.OpenService(scm, name, NativeMethods.SERVICE_CHANGE_CONFIG);
            if (service == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"OpenService failed for '{name}'.");
            }

            try
            {
                var ok = NativeMethods.ChangeServiceConfig(
                    service,
                    NativeMethods.SERVICE_NO_CHANGE,
                    (uint)mode,
                    NativeMethods.SERVICE_NO_CHANGE,
                    null, null, IntPtr.Zero, null, null, null, null);

                if (!ok)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"ChangeServiceConfig failed for '{name}'.");
                }
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    private static ServiceRuntimeStatus QueryRuntimeStatus(string name)
    {
        var scm = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_CONNECT);
        if (scm == IntPtr.Zero)
        {
            return ServiceRuntimeStatus.Unknown;
        }

        try
        {
            var service = NativeMethods.OpenService(scm, name, NativeMethods.SERVICE_QUERY_STATUS);
            if (service == IntPtr.Zero)
            {
                return ServiceRuntimeStatus.Unknown;
            }

            try
            {
                var status = default(NativeMethods.SERVICE_STATUS);
                if (!NativeMethods.QueryServiceStatus(service, ref status))
                {
                    return ServiceRuntimeStatus.Unknown;
                }

                return Enum.IsDefined(typeof(ServiceRuntimeStatus), (int)status.dwCurrentState)
                    ? (ServiceRuntimeStatus)status.dwCurrentState
                    : ServiceRuntimeStatus.Unknown;
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }
}
