using System.Globalization;
using Microsoft.Win32;

namespace WinTuner.Core.RegistryTools;

/// <summary>
/// Reads and writes registry values addressed as <c>HIVE\Path\ValueName</c>.
/// <para>
/// Values are encoded to a single human-readable string (<c>DWord:4</c>,
/// <c>String:hello</c>, ...) so they can flow through snapshots and journals
/// without widening the data schema.
/// </para>
/// </summary>
public sealed class RegistryToolkit
{
    /// <summary>Reads a value, or null when the key or value does not exist.</summary>
    public string? ReadValue(string address)
    {
        var (hive, path, name) = Parse(address);

        // Predefined base keys (LocalMachine, ...) are intentionally not disposed.
        var baseKey = OpenHive(hive);
        using var key = baseKey.OpenSubKey(path, writable: false);
        if (key is null)
        {
            return null;
        }

        var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return value is null ? null : Encode(key.GetValueKind(name), value);
    }

    /// <summary>Writes an encoded value, creating the key path when missing.</summary>
    public void WriteValue(string address, string encodedValue)
    {
        var (hive, path, name) = Parse(address);
        var (kind, value) = Decode(encodedValue);

        var baseKey = OpenHive(hive);
        using var key = baseKey.CreateSubKey(path, writable: true)
            ?? throw new InvalidOperationException($"Cannot open or create key '{hive}\\{path}'.");

        key.SetValue(name, value, kind);
    }

    private static (string Hive, string Path, string Name) Parse(string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var firstSlash = address.IndexOf('\\');
        var lastSlash = address.LastIndexOf('\\');
        if (firstSlash < 1 || lastSlash <= firstSlash)
        {
            throw new ArgumentException(
                $"Registry address must be 'HIVE\\Path\\ValueName', got '{address}'.", nameof(address));
        }

        return (address[..firstSlash], address[(firstSlash + 1)..lastSlash], address[(lastSlash + 1)..]);
    }

    private static RegistryKey OpenHive(string hive) => hive.ToUpperInvariant() switch
    {
        "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
        "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
        "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
        "HKU" or "HKEY_USERS" => Registry.Users,
        "HKCC" or "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
        _ => throw new ArgumentException($"Unknown registry hive '{hive}'.", nameof(hive)),
    };

    private static string Encode(RegistryValueKind kind, object value) => kind switch
    {
        RegistryValueKind.DWord => $"DWord:{Convert.ToInt32(value, CultureInfo.InvariantCulture)}",
        RegistryValueKind.QWord => $"QWord:{Convert.ToInt64(value, CultureInfo.InvariantCulture)}",
        RegistryValueKind.String => $"String:{value}",
        RegistryValueKind.ExpandString => $"ExpandString:{value}",
        RegistryValueKind.MultiString => $"MultiString:{string.Join("|", (string[])value)}",
        RegistryValueKind.Binary => $"Binary:{Convert.ToHexString((byte[])value)}",
        _ => $"Unknown:{value}",
    };

    private static (RegistryValueKind Kind, object Value) Decode(string encoded)
    {
        var sep = encoded.IndexOf(':');
        if (sep < 0)
        {
            return (RegistryValueKind.String, encoded);
        }

        var kindName = encoded[..sep];
        var payload = encoded[(sep + 1)..];
        return kindName switch
        {
            "DWord" => (RegistryValueKind.DWord, int.Parse(payload, CultureInfo.InvariantCulture)),
            "QWord" => (RegistryValueKind.QWord, long.Parse(payload, CultureInfo.InvariantCulture)),
            "String" => (RegistryValueKind.String, payload),
            "ExpandString" => (RegistryValueKind.ExpandString, payload),
            "MultiString" => (RegistryValueKind.MultiString, payload.Split('|')),
            "Binary" => (RegistryValueKind.Binary, Convert.FromHexString(payload)),
            _ => (RegistryValueKind.String, encoded),
        };
    }
}
