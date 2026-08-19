# Responsive Dashboard

A complete TaffyUGUI dashboard scene inspired by a responsive web admin interface. The same Unity hierarchy adapts from a wide desktop layout to a narrow mobile layout without maintaining separate desktop and phone scenes.

## What this sample demonstrates

- Flex rows, columns, wrapping, gaps, growth, minimum sizes, and nested layout groups.
- Responsive profiles for compact/mobile padding and lower-content direction changes.
- A scrollable page whose `ScrollRect.content` is owned by `TaffyLayoutGroup` with `AutoExpandContent`.
- Reusable usage cards, metric cards, progress panels, a data-table-like panel, and a dark summary panel.
- Desktop navigation that switches to a hamburger affordance below the mobile breakpoint.
- Normal uGUI `Image`, `Text`, `Shadow`, `ScrollRect`, and `RectMask2D` components for presentation while Taffy owns geometry.

## Try it

1. Open `ResponsiveDashboard.unity`.
2. Use a Game view close to `1440x900` for the desktop composition.
3. Resize below roughly `1100` pixels to see cards wrap naturally.
4. Resize below `700` pixels to see the mobile header, tighter page padding, and vertically stacked content.
5. Try `430x932` or `375x812` to compare with a phone viewport.

The Canvas intentionally uses **Constant Pixel Size** so the Game view width maps directly to the responsive breakpoints used by the sample.

The scene is designed as a reference implementation rather than a pixel-perfect recreation of any particular web framework. The important part is the hierarchy and responsive behavior: the desktop and phone views are produced by the same TaffyUGUI layout tree.
