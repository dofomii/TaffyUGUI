# Editor Workflows

This guide describes the recommended TaffyUGUI authoring workflow. All Editor tools ultimately edit the existing serialized `TaffyLayoutGroup` and `TaffyLayoutItem` components; no separate runtime configuration model is introduced.

## Inspector modes

TaffyUGUI inspectors open in **Simple** mode by default.

Simple mode keeps the common path small: Group layout type, direction or Grid flow, main/cross-axis alignment, gaps, padding, and the common Item sizing/alignment controls. Parent-aware Item controls appear only when they are meaningful for the current Flex, Grid, or Block context.

Switch to **Advanced** for the complete serialized authoring surface. Advanced mode adds:

- Search across Advanced sections, including aliases such as `clip` for Overflow and `center` for alignment.
- **Essentials / Modified / All** filtering.
- Section-level Reset.
- Comfortable or Compact Inspector density.
- Full Flex, Grid, Block, Calc, measurement, responsive, and integration values, including variants intentionally omitted from Simple visual controls.

Simple and Advanced modes edit the same serialized fields, so switching modes does not convert or discard data.

## Quick Layout and Item actions

A selected `TaffyLayoutGroup` exposes one-click common layouts including Horizontal, Vertical, Centered Panel, Toolbar, wrapping Cards, and a basic Grid. Group initialization helpers can preserve child sizes, stretch children, or initialize them for content fitting.

A selected `TaffyLayoutItem` provides intent-based actions including Fill Width, Fill Parent, Fit Content, Fixed Size, Flexible Item, Spacer, and context-aware centering. If the Item has no Taffy parent, the Inspector can add `TaffyLayoutGroup` to its parent directly.

All built-in actions use Unity Undo, support multi-selection where the operation is meaningful, preserve prefab-instance connection, and only change the serialized fields owned by the action.

## Layout Health

**Layout Health** is the setup-diagnostics layer shown in Taffy inspectors. It is read-only until you explicitly choose a fix.

It detects common problems such as:

- Missing Taffy parent for a Layout Item.
- Competing Unity `HorizontalLayoutGroup`, `VerticalLayoutGroup`, or `GridLayoutGroup` ownership.
- `ContentSizeFitter` and `AspectRatioFitter` ownership conflicts.
- ScrollRect content-ownership conflicts.
- Missing intrinsic measurement source where determinable.
- Responsive-profile and Grid validation errors.

Safe fixes use the same centralized action infrastructure as the rest of the Editor and remain Undo/prefab aware.

## Presets

Open **Window > TaffyUGUI > Preset Browser** to browse built-in and project presets. Presets are apply-once authoring helpers: applying a preset writes ordinary serialized TaffyUGUI values and does not leave a live link or override-mask runtime model.

Use presets when you already have the target Group or Item and want a reusable configuration. Use recipes when you want a complete hierarchy structure.

## Visual Grid and responsive authoring

Grid and responsive profiles have visual authoring surfaces in the Group Inspector.

For Grid, use the visual row/column track editors, gap controls, common 2/3/4-column starters, and Item placement/span controls. Advanced mode keeps named-line/area and raw Grid data available.

For responsive authoring, edit breakpoint cards and enabled overrides rather than raw serialized profile internals. The Inspector reports overlapping breakpoints and shows the currently resolved profile. Advanced raw profile access remains available for expert cases.

## Scene View and responsive preview

TaffyUGUI Scene visualization is optional and independently configurable from **Tools > TaffyUGUI > Scene Overlays**. Available overlays include container bounds, child bounds, padding bounds, selected-item margins, Flex axes, gap markers, Grid tracks, the active responsive profile, and computed-size labels.

Optional editing handles are under **Tools > TaffyUGUI > Scene Handles**. Padding and gap handles write normal serialized component state with Unity Undo. Ambiguous direct size handles are intentionally omitted for values such as Auto, Percent, and Calc.

For non-destructive viewport checks, use **Tools > TaffyUGUI > Responsive Preview** with Desktop (1440×900), Tablet (1024×768), Mobile (390×844), or a custom size. Previewing does not rewrite the component's responsive data.

## UI Builder and hierarchy recipes

Use **GameObject > TaffyUGUI** for direct Hierarchy creation, or **Window > TaffyUGUI > UI Builder** for searchable recipe discovery. Both surfaces reuse the same recipe catalog.

Available recipes include Horizontal Layout, Vertical Layout, Centered Panel, Toolbar, Sidebar + Content, Scrollable List, Responsive Cards, Modal, Form, Grid Layout, and Spacer.

The Builder creates ordinary Unity/Taffy scene objects in one Undoable operation. There is no separate Builder document or runtime representation after creation.

## Computed Layout and Explain Layout

The shared **Computed Layout** view reports current RectTransform position/size, resolved responsive profile, parent/effective display context, Flex direction, intrinsic measurement information where available, and relevant Grid validation state.

**Explain Layout** turns deterministic current state into human-readable explanations. Representative explanations include:

- A fixed size came from an explicit point/pixel value.
- A percentage size resolves relative to its containing size.
- Auto/content sizing depends on intrinsic measurement when a source exists.
- Padding contributes to the container's content box.
- An active responsive profile overrides specific base values.
- Flex Grow participates in remaining-space allocation at a high level.
- Grid placement and track information is reported when deterministically available.

Explain Layout deliberately stops short of inventing exact native-engine reasoning when the available Editor state cannot prove it.

## Expert copy/paste and reset

Advanced Item workflows can Copy/Paste Size, Spacing, Flex, and safe Grid placement settings. Values are deep-copied where necessary, including Calc data, and Paste remains Undoable. Section Reset changes only the fields owned by that section.

## Layout Debugger

Open **Tools > TaffyUGUI > Layout Debugger** for a broader debugging view and selection/navigation helpers. The Debugger reuses formal Layout Health rules and the same computed-layout snapshot used by the Inspector, preventing diagnostic drift between tools.

## Contextual documentation

Inspector sections expose direct documentation links relevant to the selected feature. Use these when you need deeper Flex, Grid/Calc, responsive/ScrollRect, measurement, migration, or troubleshooting detail without leaving the current authoring context.
