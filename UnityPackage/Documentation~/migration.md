# Migration from built-in LayoutGroups

Open **Tools > TaffyUGUI > Migration Window**. The migration service analyzes legacy uGUI layout groups before changing anything and integrates with Unity Undo.

## Supported automatic migrations

- `HorizontalLayoutGroup` -> Taffy Flex row;
- `VerticalLayoutGroup` -> Taffy Flex column;
- deterministic `GridLayoutGroup` configurations using Fixed Column Count + Horizontal start axis or Fixed Row Count + Vertical start axis.

Padding, spacing, alignment, control/expand behavior, reverse arrangement where available, cell size, and safe child layout data are translated conservatively. Existing `TaffyLayoutItem` components are reused rather than replaced wholesale.

## Refused migrations

The tool intentionally refuses cases where automatic translation would change semantics, including:

- legacy child scale control;
- Flexible `GridLayoutGroup` constraints;
- non-Upper-Left Grid start corners;
- incompatible Grid start-axis/constraint combinations;
- invalid/negative Grid sizing values.

Fix or manually author those layouts rather than forcing migration.

## Prefabs and Undo

Migration uses Unity Undo and prefab modification APIs. Because Unity does not allow two `LayoutGroup` components on the same GameObject, the service snapshots the legacy group, removes it through Undo, adds `TaffyLayoutGroup`, then applies the captured settings. Prefab-instance overrides remain instance-safe; the prefab asset is not silently rewritten.

Use selection-only migration first on important scenes, inspect the result, then use the all-loaded-scenes batch option if appropriate.
