# Grid and Responsive

This sample demonstrates Grid tracks, Calc sizing, and a responsive profile that changes the container layout when it becomes narrow.

## Recommended Editor workflow

For normal authoring:

1. Choose **GameObject > TaffyUGUI > Grid Layout** or create **Responsive Cards** from **GameObject > TaffyUGUI** / **Window > TaffyUGUI > UI Builder**.
2. Select the Group and use the visual Grid row/column controls and common track starters in the Inspector.
3. Configure responsive breakpoint cards and enabled overrides in the responsive section.
4. Use **Tools > TaffyUGUI > Responsive Preview** to inspect Desktop, Tablet, Mobile, or a custom size without rewriting responsive data.
5. Enable **Tools > TaffyUGUI > Scene Overlays > Grid Tracks** or **Responsive Profile Label** when visual context helps.

Advanced mode keeps raw Grid named lines/areas, Calc values, and responsive profile data available.

## Runtime sample

Attach `GridAndResponsiveSample` to an empty UI `RectTransform` under a Canvas and enter Play Mode. It creates a two-column Grid, a Calc-sized item, and a `Compact` breakpoint that switches the container to a Flex column when the rect becomes narrow. Resize the Game view or parent rect to observe the breakpoint.

Use **Computed Layout**, **Explain Layout**, or **Tools > TaffyUGUI > Layout Debugger** to inspect the resolved responsive profile and Grid state.
