using Pathfinder1eHelper.Views;

namespace Pathfinder1eHelper.Test;

public class SidebarResponsivePolicyTests
{
    [Theory]
    [InlineData(960, false, true)]
    [InlineData(960.01, false, false)]
    [InlineData(1024, false, false)]
    [InlineData(1024, true, true)]
    [InlineData(1087.99, true, true)]
    [InlineData(1088, true, false)]
    [InlineData(1200, true, false)]
    public void Width_hysteresis_collapses_and_expands_at_separate_thresholds(
        double width,
        bool isCurrentlyCollapsed,
        bool expected)
    {
        Assert.Equal(expected, SidebarResponsivePolicy.ShouldCollapse(width, isCurrentlyCollapsed));
    }

    [Theory]
    [InlineData(double.NaN, true)]
    [InlineData(double.PositiveInfinity, false)]
    [InlineData(0, true)]
    public void Invalid_width_keeps_the_current_sidebar_state(double width, bool isCurrentlyCollapsed)
    {
        Assert.Equal(isCurrentlyCollapsed, SidebarResponsivePolicy.ShouldCollapse(width, isCurrentlyCollapsed));
    }
}
