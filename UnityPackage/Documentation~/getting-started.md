# Getting Started

## Requirements

TaffyUGUI 1.1.2 targets Unity **2021.3 or newer**. The package declares uGUI and TextMeshPro dependencies. Native plugins are bundled for Android ARM64, Windows x86/x64, and Linux x86/x64.

## Install

After the repository owner intentionally creates the `v1.1.2` tag, choose **Add package from git URL...** in Package Manager and enter:

```text
https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#v1.1.2
```

For a local checkout, choose **Add package from disk...** and select `UnityPackage/package.json`.

## Create your first layout

The fastest path is the guided Editor workflow; you do not need to configure raw Flex properties first.

1. Create a Canvas if the scene does not already have one.
2. In the Hierarchy, choose **GameObject > TaffyUGUI > Horizontal Layout**.
3. Select the created object. The Inspector opens in **Simple** mode by default.
4. Use **Quick Layout** to switch between common layouts such as Horizontal, Vertical, Centered Panel, Toolbar, Cards, or Grid.
5. Add or select child UI objects. Add **Taffy Layout Item** only when a child needs explicit sizing, Flex growth, Grid placement, spacing, or measurement behavior.
6. Use the Item quick actions for common intent: **Fill Width**, **Fill Parent**, **Fit Content**, **Fixed Size**, **Flexible Item**, **Spacer**, or contextual centering.

Simple mode edits the same serialized `TaffyLayoutGroup` and `TaffyLayoutItem` fields as Advanced mode. Switch to **Advanced** when you need the complete Flex, Grid, Block, Calc, measurement, responsive, or integration surface.

## Start from a recipe or preset

For common UI structures, use one of these paths instead of building the hierarchy manually:

- **GameObject > TaffyUGUI**: Horizontal Layout, Vertical Layout, Centered Panel, Toolbar, Sidebar + Content, Scrollable List, Responsive Cards, Modal, Form, Grid Layout, and Spacer.
- **Window > TaffyUGUI > UI Builder**: searchable access to the same ordinary scene-object recipes.
- **Window > TaffyUGUI > Preset Browser**: apply reusable built-in or project presets to an existing Group or Item.

Recipes and presets write ordinary TaffyUGUI serialized state. They do not introduce a separate runtime document or live preset model.

## Understand a selected layout

The Inspector provides three increasingly detailed levels of feedback:

- **Computed Layout** shows current RectTransform geometry, responsive profile, parent/effective layout context, measured content information where available, and Grid validation state.
- **Layout Health** reports common setup conflicts and offers explicit fixes where safe.
- **Explain Layout** summarizes deterministic reasons for fixed/percent/content sizing, padding, active responsive overrides, common Flex Grow behavior, and Grid placement. It avoids claiming internal reasoning when the available state is insufficient.

For a broader view, open **Tools > TaffyUGUI > Layout Debugger**. The Debugger reuses the same diagnostic and computed-layout data as the Inspector.

## Responsive and Scene workflows

Use the visual responsive and Grid controls in the Group Inspector for normal authoring. For non-destructive responsive inspection, choose **Tools > TaffyUGUI > Responsive Preview** and select Desktop, Tablet, Mobile, or a custom size.

Scene visualization is controlled from **Tools > TaffyUGUI > Scene Overlays**. Container/child bounds, padding, margins, Flex axes, gaps, Grid tracks, responsive-profile labels, and computed-size labels can be enabled independently. Optional padding and gap handles are under **Tools > TaffyUGUI > Scene Handles** and use normal serialized editing with Undo.

## Import samples

Open Package Manager, select **TaffyUGUI**, expand **Samples**, and import **Flex Quick Start**, **Grid and Responsive**, **Custom Measurement**, or **Responsive Dashboard**. The dashboard sample is a complete serialized scene that reflows from desktop to mobile using the same Taffy hierarchy; the sample READMEs point back to the recommended Inspector, recipe, preview, and debugging workflows.and debugging workflows.

## Important ownership rule

Let one system own each axis. Do not keep Unity `HorizontalLayoutGroup`, `VerticalLayoutGroup`, or `GridLayoutGroup` on the same object as `TaffyLayoutGroup`. `ContentSizeFitter`, `AspectRatioFitter`, ScrollRect content sizing, and Taffy can also request competing geometry. Layout Health surfaces common conflicts; see [ScrollRect and Responsive Integration](responsive-and-scrollrect.md) for integration details.

For the complete Editor workflow reference, see [Editor Workflows](editor-workflows.md).
