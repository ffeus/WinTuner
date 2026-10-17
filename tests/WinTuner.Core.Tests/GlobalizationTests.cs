using System.Windows.Markup;
using Xunit;

namespace WinTuner.Core.Tests;

/// <summary>
/// Guards the WPF startup path against invariant globalization.
/// <para>
/// WPF resolves the XAML language (default <c>en-us</c>) to a concrete
/// <see cref="System.Globalization.CultureInfo"/> while a window is created. When
/// <c>InvariantGlobalization</c> is enabled the runtime ships no globalization
/// data, so that lookup throws
/// <c>InvalidOperationException: Cannot find non-neutral culture related to 'en-us'</c>
/// and the GUI crashes before it ever appears. This test fails fast on that
/// misconfiguration.
/// </para>
/// </summary>
public sealed class GlobalizationTests
{
    [Fact]
    public void XamlLanguage_ResolvesToSpecificCulture()
    {
        var language = XmlLanguage.GetLanguage("en-us");

        var culture = language.GetSpecificCulture();

        Assert.False(culture.IsNeutralCulture);
        Assert.Equal("en-US", culture.Name);
    }
}
