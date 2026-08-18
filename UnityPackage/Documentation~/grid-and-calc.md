# Grid and Calc

## Create a Grid container

Set `TaffyLayoutGroup.containerDisplay = TaffyContainerDisplay.Grid` and populate `gridColumns` / `gridRows`.

```csharp
group.containerDisplay = TaffyContainerDisplay.Grid;
group.gridColumns.Add(TaffyGridTrack.Fraction(1f));
group.gridColumns.Add(TaffyGridTrack.Fraction(2f));
group.gridRows.Add(TaffyGridTrack.Auto());
group.horizontalGap = 12f;
group.verticalGap = 12f;
```

Track factories include Auto, Points, Percent, Fraction, MinContent, MaxContent, MinMax, Repeat, and Calc. `Repeat` supports fixed count, AutoFill, and AutoFit modes where accepted by the native Taffy surface.

## Placement

`TaffyLayoutItem` exposes row/column start/end placements. Placement helpers include:

```csharp
item.gridColumnStart = TaffyGridPlacement.Line(1);
item.gridColumnEnd = TaffyGridPlacement.Span(2);
item.gridRowStart = TaffyGridPlacement.NamedLine("content-start");
```

Named lines and named areas are authored on `TaffyLayoutGroup`. The custom inspector and property drawers validate malformed placement and track definitions before native marshalling.

## Calc expressions

Calc is a typed expression tree used by `TaffyLength` and Grid track breadths. Percent values are fractions.

```csharp
var width = TaffyCalcExpression.Clamp(
    TaffyCalcExpression.Length(120f),
    TaffyCalcExpression.Percent(0.50f),
    TaffyCalcExpression.Length(360f));

item.width = TaffyLength.Calc(width);
```

Available operations are Length, Percent, Add, Subtract, Scale, Min, Max, and Clamp. Cycles, null operands, wrong operand counts, and non-finite values are rejected before upload.

## Diagnostics

`TaffyLayoutGroup.GridValidationError` exposes the most recent Grid authoring validation message. The **Tools > TaffyUGUI > Layout Debugger** window can also inspect active layout state.
