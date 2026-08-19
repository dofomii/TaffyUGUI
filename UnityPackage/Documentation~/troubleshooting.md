# Troubleshooting and Diagnostics

## `DllNotFoundException` / native library does not load

v1.1.1 bundles native plugins for **Android ARM64, Windows x86/x64, and Linux x86/x64**. Unity should select the binary that matches the active Editor/Player platform and architecture. macOS, iOS, and WebGL binaries are not bundled in this release.

If loading fails, confirm the expected file exists under `Plugins/<platform>/<architecture>/`, then inspect Unity's Plugin Importer settings. Windows x86 and Linux x86 are legacy binaries; current Unity desktop Editors are 64-bit.

## Layout does not move or size children

Check that:

- the container has `TaffyLayoutGroup` and no other `LayoutGroup` on the same GameObject;
- children are direct layout children and have `RectTransform`;
- explicit child styles are on `TaffyLayoutItem`;
- another component is not rewriting the same RectTransform axes after layout;
- Grid/Calc validation errors are empty.

Use **Tools > TaffyUGUI > Layout Debugger** and inspect `GridValidationError` / `GetIntegrationWarnings()`.

## TMP text has the wrong intrinsic size

Ensure TextMeshPro is installed/resolved and the `TMP_Text` has a valid font. Taffy caches intrinsic measurements; normal TMP change/font events invalidate the cache, but a custom runtime provider must increment `MeasurementVersion` or call `InvalidateMeasurement()` when its data changes.

## ScrollRect rebuild loop or content fights another fitter

Use `GetIntegrationWarnings()`. `ContentSizeFitter` and `AspectRatioFitter` can own the same axes that Taffy wants to control. Configure one owner per axis. `SuppressedRebuildRequestCount` indicates that the per-frame rebuild guard has been triggered.

## Responsive profile does not activate

Call `ValidateResponsiveProfiles(out error)` and confirm profile names are unique, bounds are finite/non-negative, and max bounds are zero/unbounded or >= min bounds. A runtime profile override takes precedence until cleared.

## Grid placement or Calc fails

Grid placement names/spans and Calc expression trees are validated before native marshalling. Calc cycles, missing operands, incorrect operand counts, and NaN/Infinity values are rejected. The custom property drawers surface these structures directly in the Inspector.

## Migration refuses a layout

That is deliberate when a legacy uGUI behavior cannot be translated deterministically. Read the analysis message in the Migration Window and manually author the unsupported semantics rather than bypassing the guard.

## Native diagnostic information

The managed ABI validates native ABI version/stage, exact Taffy version, capabilities, struct sizes, offsets, and enum numeric contracts during context startup. ABI mismatch exceptions mean the managed package and native `.so` do not belong to the same release payload.
