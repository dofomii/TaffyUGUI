# Flexbox and Block

## Container configuration

Set `TaffyLayoutGroup.containerDisplay` to `Flex`, `Block`, or `FlowRoot`.

For Flex containers the primary controls are:

- `direction`: Row, Column, RowReverse, or ColumnReverse;
- `wrap`: NoWrap, Wrap, or WrapReverse;
- `horizontalGap` / `verticalGap`;
- `justifyContent` along the main axis;
- `alignItems` and `alignContent` on the cross axis.

Container padding comes from the inherited uGUI `LayoutGroup.padding`. `TaffyLayoutGroup.border`, overflow, writing direction, box sizing, and scrollbar width map to the native style surface.

## Child sizing and flex behavior

Add `TaffyLayoutItem` when a child needs explicit Taffy style. Supported sizing forms include:

```csharp
item.width = TaffyLength.Points(180f);
item.minWidth = TaffyLength.Percent(0.25f); // fractions: 0.25 = 25%
item.maxWidth = TaffyLength.Points(320f);
item.flexBasis = TaffyLength.Auto;
item.flexGrow = 1f;
item.flexShrink = 1f;
item.margin = TaffyEdges.Points(8f);
```

`TaffyLength.Percent` takes a fraction, not a percentage number.

## Position, box model, and display

A `TaffyLayoutItem` exposes relative/absolute positioning, inset edges, margin, padding, border, box sizing, overflow, aspect ratio, display, alignment overrides, and writing direction. `display = None` removes the item and descendants from layout.

## Block/Float

For a Block or FlowRoot container, child `floatMode` supports Left/Right and `clearMode` supports Left/Right/Both. FlowRoot is useful when floats must be contained by a new formatting context.

## Unity interaction

Taffy writes child `RectTransform` layout geometry through Unity's layout pass. Anchors/size controllers that also rewrite the same axes can conflict. Prefer Taffy's `aspectRatio`, width/height, and flex properties over adding another component that continuously owns the same dimensions.
