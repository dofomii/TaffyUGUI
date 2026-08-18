# DX0 — Editor Compatibility Baseline

**Program:** TaffyUGUI Developer Experience Improvement
**Purpose:** freeze the serialized authoring contract and current Editor responsibilities before the inspector architecture is restructured.

This document is the compatibility reference for DX1 and later phases. The Developer Experience program may change labels, grouping, visual controls, presets, diagnostics, and workflow, but the existing runtime components remain the source of truth.

---

## Source-of-Truth Rule

All Editor authoring paths must ultimately edit the existing serialized runtime state:

```text
Inspector / Quick Action / Preset / Builder / Scene Handle
                         │
                         ▼
          TaffyLayoutGroup / TaffyLayoutItem
                         │
                         ▼
                  Existing Runtime
                         │
                         ▼
                  Native ABI / Taffy
```

Simple and Advanced inspector modes are presentation modes only. They must not create separate layout configuration data.

---

# Serialized `TaffyLayoutGroup` Contract

Declared TaffyUGUI authoring fields that must remain serialization-compatible:

```text
containerDisplay
boxSizing
writingDirection
overflowX
overflowY
scrollbarWidth
border
textAlign

direction
wrap
horizontalGap
verticalGap
justifyContent
alignItems
alignContent
justifyItems

gridAutoFlow
gridRows
gridColumns
gridAutoRows
gridAutoColumns
gridNamedLines
gridAreas
gridAreaRows
gridAreaColumns

responsiveProfiles
safeAreaMode
scrollRectContentMode
pixelRounding
maxRebuildRequestsPerFrame
```

`TaffyLayoutGroup` also deliberately uses the inherited Unity `LayoutGroup.padding` serialized backing field:

```text
m_Padding
```

The custom inspector currently accesses that property directly. Future inspectors may relabel it as `Padding`, use a visual box-model control, or move it into an Essentials section, but the runtime/base serialized data must remain compatible.

The inherited Unity script/component metadata is not part of the TaffyUGUI-owned authoring contract.

---

# Serialized `TaffyLayoutItem` Contract

Declared authoring fields that must remain serialization-compatible:

```text
display
boxSizing
writingDirection
overflowX
overflowY
scrollbarWidth

position
inset
width
height
minWidth
minHeight
maxWidth
maxHeight
aspectRatio

margin
padding
border

flexBasis
flexGrow
flexShrink
alignSelf

gridRowStart
gridRowEnd
gridColumnStart
gridColumnEnd
justifySelf

floatMode
clearMode
textAlign

measurement
forceReplacedElement
itemIsTable
```

Future Editor labels may use intent-first language such as `Fill Parent`, `Fit Content`, `Flexible`, `Main Axis Size`, or `Alignment Override`. Those labels must still map to these existing serialized fields.

---

# Nested Serializable Authoring Contract

## `TaffyLength`

```text
unit
value
calc
```

## `TaffyEdges`

```text
left
right
top
bottom
```

## `TaffyCalcExpression`

```text
operation
value
operands
```

`operands` uses managed-reference serialization and can recursively contain `TaffyCalcExpression` objects.

## `TaffyGridTrackBreadth`

```text
kind
value
calc
```

## `TaffyGridTrack`

```text
kind
value
calc
min
max
repeatMode
repeatCount
repeatTracks
```

`repeatTracks` uses managed-reference serialization and can recursively contain `TaffyGridTrack` objects.

## `TaffyGridNamedLine`

```text
axis
lineIndex
name
```

## `TaffyGridArea`

```text
name
rowStart
rowEnd
columnStart
columnEnd
```

## `TaffyGridPlacement`

```text
kind
line
span
name
occurrence
```

## `TaffyResponsiveProfile`

```text
name
priority
minWidth
maxWidth
minHeight
maxHeight

overrideContainerDisplay
containerDisplay

overrideFlexDirection
direction
overrideFlexWrap
wrap
overrideGaps
horizontalGap
verticalGap
overrideAlignment
justifyContent
alignItems
alignContent
justifyItems

overrideGridAutoFlow
gridAutoFlow

overridePadding
padding
```

## `TaffyPixelInsets`

```text
left
right
top
bottom
```

## `TaffyMeasurementSample`

```text
availableWidth
size
```

`TaffyMeasurementData` is runtime transfer data rather than a `[Serializable]` scene/prefab authoring object and is not part of this serialized contract.

---

# Enum Numeric Compatibility Contract

The following values are intentionally pinned. Inspector display order or user-facing labels may change, but these numeric values must not change without an explicit serialized-data migration.

## Core style enums

```text
TaffyUnit
Auto=0, Points=1, Percent=2, Calc=3

TaffyContainerDisplay
Flex=1, Grid=2, Block=3, FlowRoot=4

TaffyDisplay
None=0, Flex=1, Grid=2, Block=3, FlowRoot=4

TaffyBoxSizing
BorderBox=0, ContentBox=1

TaffyWritingDirection
LeftToRight=0, RightToLeft=1

TaffyOverflow
Visible=0, Clip=1, Hidden=2, Scroll=3

TaffyPosition
Relative=0, Absolute=1

TaffyFlexDirection
Row=0, Column=1, RowReverse=2, ColumnReverse=3

TaffyFlexWrap
NoWrap=0, Wrap=1, WrapReverse=2

TaffyFloat
None=0, Left=1, Right=2

TaffyClear
None=0, Left=1, Right=2, Both=3

TaffyTextAlign
Auto=0, LegacyLeft=1, LegacyRight=2, LegacyCenter=3

TaffyMeasurementMode
Auto=0, Disabled=1
```

## Alignment enums

```text
TaffyAlign
Auto=-1
Start=0
End=1
Center=2
Stretch=3
Baseline=4
FlexStart=5
FlexEnd=6
SelfStart=7
SelfEnd=8
SafeStart=9
SafeEnd=10
SafeCenter=11
SafeFlexStart=12
SafeFlexEnd=13
SafeSelfStart=14
SafeSelfEnd=15

TaffyJustify
Auto=-1
Start=0
End=1
Center=2
SpaceBetween=3
SpaceAround=4
SpaceEvenly=5
FlexStart=6
FlexEnd=7
SafeStart=8
SafeEnd=9
SafeCenter=10
SafeFlexStart=11
SafeFlexEnd=12

TaffyAlignContent
Auto=-1
Start=0
End=1
Center=2
Stretch=3
SpaceBetween=4
SpaceAround=5
SpaceEvenly=6
FlexStart=7
FlexEnd=8
SafeStart=9
SafeEnd=10
SafeCenter=11
SafeFlexStart=12
SafeFlexEnd=13
```

`TaffyJustify` is particularly sensitive because its existing numeric values intentionally preserve previously serialized data and do not exactly mirror the native `AlignContent` numbering.

## Grid enums

```text
TaffyGridAutoFlow
Row=0, Column=1, RowDense=2, ColumnDense=3

TaffyGridAxis
Row=0, Column=1

TaffyGridRepeatMode
Count=0, AutoFill=1, AutoFit=2

TaffyGridTrackKind
Auto=0, Points=1, Percent=2, Fraction=3, MinMax=4,
MinContent=5, MaxContent=6, Calc=7, Repeat=8

TaffyGridTrackBreadthKind
Auto=0, Points=1, Percent=2, Fraction=3,
MinContent=5, MaxContent=6, Calc=7

TaffyGridPlacementKind
Auto=0, Line=1, Span=2, NamedLine=3, NamedSpan=4
```

## Calc enum

```text
TaffyCalcOperation
Length=0, Percent=1, Add=2, Subtract=3,
Scale=4, Min=5, Max=6, Clamp=7
```

## Responsive/integration enums

```text
TaffySafeAreaMode
Disabled=0, Padding=1

TaffyScrollRectContentMode
Disabled=0, AutoExpandContent=1

TaffyPixelRounding
None=0, Round=1, Floor=2, Ceil=3, CanvasPixel=4
```

---

# Current Editor Surface Baseline

## `TaffyLayoutGroupEditor`

Current inspector sections:

```text
Formatting Context
Flex / Alignment
Grid Authoring
Responsive / Integration
Live Diagnostics
```

Current serialized-property coverage:

### Formatting Context

```text
containerDisplay
boxSizing
writingDirection
overflowX
overflowY
scrollbarWidth
m_Padding
border
textAlign
```

### Flex / Alignment

```text
direction
wrap
horizontalGap
verticalGap
justifyContent
alignItems
alignContent
justifyItems
```

### Grid Authoring

```text
gridAutoFlow
gridRows
gridColumns
gridAutoRows
gridAutoColumns
gridNamedLines
gridAreas
gridAreaRows
gridAreaColumns
```

### Responsive / Integration

```text
responsiveProfiles
safeAreaMode
scrollRectContentMode
pixelRounding
maxRebuildRequestsPerFrame
```

### Live Diagnostics

Read-only/actions:

```text
Active responsive profile
Suppressed rebuild count
Grid validation status
Force Rebuild
Reset Rebuild Counters
Read Grid Diagnostics
Open Debugger
Show/Hide Scene Overlay
```

The current inspector also displays responsive/Grid validation errors and integration warnings.

---

## `TaffyLayoutItemEditor`

Current inspector sections and property coverage:

```text
Display
- display
- boxSizing
- writingDirection
- overflowX
- overflowY
- scrollbarWidth

Position and Size
- position
- inset
- width
- height
- minWidth
- minHeight
- maxWidth
- maxHeight
- aspectRatio

Box Model
- margin
- padding
- border

Flex Item
- flexBasis
- flexGrow
- flexShrink
- alignSelf

Grid Item
- gridRowStart
- gridRowEnd
- gridColumnStart
- gridColumnEnd
- justifySelf

Block / Float
- floatMode
- clearMode
- textAlign

Intrinsic Measurement
- measurement
- forceReplacedElement
- itemIsTable
```

Current item action:

```text
Invalidate Measurement
```

When the parent is Grid, the inspector also surfaces parent Grid-authoring validation errors.

---

# Current Property Drawer Ownership

Current custom drawers:

```text
TaffyLengthDrawer               -> TaffyLength
TaffyEdgesDrawer                -> TaffyEdges
TaffyPixelInsetsDrawer          -> TaffyPixelInsets
TaffyCalcExpressionDrawer       -> TaffyCalcExpression
TaffyGridTrackBreadthDrawer     -> TaffyGridTrackBreadth
TaffyGridTrackDrawer            -> TaffyGridTrack
TaffyGridPlacementDrawer        -> TaffyGridPlacement
TaffyGridNamedLineDrawer        -> TaffyGridNamedLine
TaffyGridAreaDrawer             -> TaffyGridArea
```

DX1 may move these into a clearer folder/module structure, but their serialized semantics must remain unchanged.

---

# Current Editor Tool Responsibilities

## Layout Debugger

`TaffyLayoutDebuggerWindow` currently provides:

```text
All loaded groups or Selection Only filtering
Group selection/ping
Display mode
Rect size
Active responsive profile
Minimum/preferred layout input
Suppressed rebuild count
Responsive validation
Grid validation
Detailed Grid row/column/item diagnostics
Integration warnings
Force rebuild
Reset rebuild counters
```

## Scene Visualization

`TaffySceneVisualization` currently provides:

```text
Editor preference-backed enable/disable state
Selected TaffyLayoutGroup bounds
Child bounds
Display/profile label
Detailed Grid track lines when diagnostics are available
```

## Migration

`TaffyMigration` / `TaffyMigrationService` currently owns:

```text
HorizontalLayoutGroup analysis/migration
VerticalLayoutGroup analysis/migration
Conservative GridLayoutGroup analysis/migration
Undo isolation
Prefab-instance-safe changes
Batch migration
Unsupported-case diagnostics
```

The DX program should reuse these responsibilities rather than duplicating them inside inspector drawing code.

---

# Editor / Runtime Assembly Boundary

Current assemblies:

```text
TaffyUGUI.Runtime
- Player-capable
- references Unity.ugui and Unity.TextMeshPro
- must not reference TaffyUGUI.Editor

TaffyUGUI.Editor
- includePlatforms: Editor
- references TaffyUGUI.Runtime and Unity.ugui
- must remain excluded from Player assemblies
```

Permanent DX0 tests protect this boundary.

---

# Allowed Editor Presentation Changes

Without a runtime migration, future DX phases may safely change:

```text
Inspector labels
Tooltip text
Field grouping
Foldout organization
Simple/Advanced visibility
Visual alignment selectors
Intent-first length labels
Box-model presentation
Search/filtering
Preset application workflows
Quick actions
Diagnostics presentation
Scene overlays
Builder/onboarding workflows
```

They must continue writing the same serialized runtime fields.

---

# Changes Requiring Explicit Compatibility Work

The following are **not** ordinary Editor presentation changes:

```text
Renaming/removing serialized runtime fields
Renumbering serialized enums
Changing a serialized field type
Changing managed-reference structure
Moving source-of-truth values into Editor-only assets
Introducing linked preset state that overrides runtime fields
Changing runtime/native layout semantics
```

Any such change requires a separately designed migration and regression gate.

---

# Explicit Non-Goal: 32-bit ABI

The Developer Experience program does not include the separate 32-bit managed/native ABI redesign.

The current C# ABI validation contains pointer-width-sensitive structure-size assumptions. Native x86 binaries may exist, but full 32-bit managed Unity support requires its own ABI project and validation.

DX work must not weaken or bypass current ABI validation merely to make x86 packaging appear supported.

---

# DX0 Validation Contract

Permanent Edit Mode coverage added by DX0 protects:

```text
Declared Group field names
Declared Item field names
Nested serialized authoring field names
Pinned enum numeric values
Representative Group serialization round-trip
Representative Item serialization round-trip
Runtime/Editor assembly separation
Custom editor creation
Multi-object custom editor creation
```

The purpose is not to prevent future evolution. It is to ensure future evolution is deliberate rather than accidental.
