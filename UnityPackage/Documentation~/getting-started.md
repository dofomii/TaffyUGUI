# Getting Started

## Requirements

TaffyUGUI 1.0.0 targets Unity **2021.3 or newer**. The package declares uGUI and TextMeshPro dependencies; Unity 6 resolves the modern uGUI/TMP shim versions automatically. The v1.0 Player target is Android ARM64.

## Install from Git

After the repository owner intentionally creates the `v1.0.0` tag, choose **Add package from git URL...** and enter:

```text
https://github.com/dofomii/TaffyUGUI.git?path=/UnityPackage#v1.0.0
```

For a local checkout, choose **Add package from disk...** and select `UnityPackage/package.json`.

## First layout

1. Create or select a uGUI object with a `RectTransform` under a Canvas.
2. Remove any `HorizontalLayoutGroup`, `VerticalLayoutGroup`, or `GridLayoutGroup` from that same object. Unity layout groups are mutually exclusive with `TaffyLayoutGroup`.
3. Add **Taffy Layout Group**.
4. Leave **Formatting Context = Flex** and choose a Flex Direction.
5. Add child `RectTransform` objects.
6. Add `TaffyLayoutItem` to children that need explicit width/height, margin, flex growth/shrink, Grid placement, aspect ratio, or measurement control.

Example:

```csharp
var group = root.gameObject.AddComponent<TaffyLayoutGroup>();
group.direction = TaffyFlexDirection.Row;
group.horizontalGap = 12f;
group.alignItems = TaffyAlign.Center;

var item = child.gameObject.AddComponent<TaffyLayoutItem>();
item.width = TaffyLength.Points(160f);
item.height = TaffyLength.Points(48f);
item.flexShrink = 1f;
```

`TaffyLayoutGroup` participates in Unity's normal `ILayoutElement` / `ILayoutController` rebuild flow. Do not call the native library directly for normal uGUI use.

## Import samples

Open Package Manager, select **TaffyUGUI**, expand **Samples**, and import **Flex Quick Start**, **Grid and Responsive**, or **Custom Measurement**. Each sample is a small runtime bootstrap that can be placed on an empty `RectTransform` under a Canvas.

## Editor tools

- **Tools > TaffyUGUI > Layout Debugger** shows resolved layout and diagnostics.
- **Tools > TaffyUGUI > Migration Window** analyzes and migrates supported built-in uGUI layout groups.
- Selecting a Taffy layout in Scene view enables layout visualization supplied by the Editor assembly.

## Important ownership rule

Let one system own each axis. `ContentSizeFitter`, `AspectRatioFitter`, ScrollRect content sizing, and Taffy can otherwise request competing geometry. Taffy contains explicit bridges and warnings for common combinations; see [ScrollRect and Responsive Integration](responsive-and-scrollrect.md).
