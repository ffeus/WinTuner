using System.Diagnostics;

namespace WinTuner.Core.Tasks;

/// <summary>
/// Enables, disables and inspects scheduled tasks through <c>schtasks.exe</c>.
/// <para>
/// Output parsing is deliberately permissive (keyword-based) to tolerate
/// localized Windows installs; the raw output is returned in exceptions so
/// callers can surface it as evidence.
/// </para>
/// </summary>
public sealed class ScheduledTaskManager
{
    /// <summary>
    /// Returns "Enabled", "Disabled", "Unknown", or null when the task is absent.
    /// </summary>
    public string? GetState(string taskPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskPath);

        var (exitCode, output) = Run("schtasks", $"/query /tn \"{taskPath}\" /fo LIST /v");
        if (exitCode != 0)
        {
            return null;
        }

        var lower = output.ToLowerInvariant();
        if (lower.Contains("disabled"))
        {
            return "Disabled";
        }

        if (lower.Contains("enabled") || lower.Contains("ready") || lower.Contains("running"))
        {
            return "Enabled";
        }

        return "Unknown";
    }

    /// <summary>
    /// Enables or disables one task. Throws <see cref="InvalidOperationException"/>
    /// with the raw schtasks output when the change fails.
    /// </summary>
    public void SetState(string taskPath, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskPath);

        var verb = enabled ? "/enable" : "/disable";
        var (exitCode, output) = Run("schtasks", $"/change /tn \"{taskPath}\" {verb}");
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"schtasks {verb} failed for '{taskPath}' (exit {exitCode}): {output.Trim()}");
        }
    }

    private static (int ExitCode, string Output) Run(string file, string args)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                return (-1, $"Failed to start {file}.");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, stdout + stderr);
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
    }
}
