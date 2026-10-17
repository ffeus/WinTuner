using WinTuner.Core.Models;
using WinTuner.Core.Profiles;
using Xunit;

namespace WinTuner.Core.Tests;

/// <summary>
/// Pure-logic tests over the built-in plans. They assert the declarative shape
/// of the plans without touching the machine.
/// </summary>
public sealed class BuiltInProfilesTests
{
    [Fact]
    public void DisableWindowsUpdate_TargetsWuauservAsDisabled()
    {
        var plan = BuiltInProfiles.DisableWindowsUpdate();
        var change = plan.Changes.First(c => c.TargetId == "wuauserv");

        Assert.Equal(ChangeTargetKind.ServiceStartMode, change.Kind);
        Assert.Equal(ServiceStartMode.Disabled.ToString(), change.DesiredValue);
    }

    [Fact]
    public void EnableWindowsUpdate_TargetsWuauservAsAutomatic()
    {
        var plan = BuiltInProfiles.EnableWindowsUpdate();
        var change = plan.Changes.First(c => c.TargetId == "wuauserv");

        Assert.Equal(ServiceStartMode.Automatic.ToString(), change.DesiredValue);
    }

    [Fact]
    public void DisableAndEnable_AreStructurallyInverse()
    {
        var disable = BuiltInProfiles.DisableWindowsUpdate();
        var enable = BuiltInProfiles.EnableWindowsUpdate();

        Assert.Equal(
            disable.Changes.Select(c => c.TargetId).OrderBy(x => x),
            enable.Changes.Select(c => c.TargetId).OrderBy(x => x));
    }

    [Fact]
    public void Every_Plan_Is_NonEmpty_And_Named()
    {
        var plans = BuiltInProfiles.All().ToList();

        Assert.NotEmpty(plans);
        Assert.All(plans, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.NotEmpty(p.Changes);
        });
    }

    [Theory]
    [InlineData("DISABLE-WINDOWS-UPDATE")]
    [InlineData("disable-windows-update")]
    public void Find_IsCaseInsensitive(string id)
    {
        Assert.NotNull(BuiltInProfiles.Find(id));
    }

    [Fact]
    public void Find_ReturnsNull_ForUnknownId()
    {
        Assert.Null(BuiltInProfiles.Find("no-such-plan"));
    }
}
