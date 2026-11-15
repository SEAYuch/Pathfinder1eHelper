namespace Pathfinder1eHelper.Views;

/// <summary>Width hysteresis for the app shell's responsive Arc Sidebar.</summary>
public static class SidebarResponsivePolicy
{
    /// <summary>At or below this window width, automatically collapse to the icon rail.</summary>
    public const double CollapseAtWidth = 960;

    /// <summary>At or above this width, automatically expand; the gap prevents resize jitter.</summary>
    public const double ExpandAtWidth = 1088;

    /// <summary>
    /// Returns the auto-managed collapsed state. Between the two thresholds, keep the current state
    /// so resizing around a single breakpoint does not repeatedly collapse and expand the sidebar.
    /// </summary>
    public static bool ShouldCollapse(double windowWidth, bool isCurrentlyCollapsed)
    {
        if (!double.IsFinite(windowWidth) || windowWidth <= 0)
        {
            return isCurrentlyCollapsed;
        }

        if (windowWidth <= CollapseAtWidth)
        {
            return true;
        }

        return windowWidth < ExpandAtWidth && isCurrentlyCollapsed;
    }
}
