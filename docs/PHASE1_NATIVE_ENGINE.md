# Phase 1 — Complete Rust/Taffy Native Engine

**Implementation:** COMPLETE
**Verification inventory:** COMPLETE
**Compiled re-validation under current local-only policy:** inherited by Phase 3 gate

## Goal

Implement the complete persistent native layout engine required by the v1 Unity package before freezing the production ABI.

## Delivered engine architecture

The engine uses Taffy 0.13's low-level/custom-tree integration instead of relying on the high-level `TaffyTree` wrapper. This gives TaffyUGUI ownership of persistent topology, cached measurements, resource lifetime, layout caches, and diagnostic extraction.

Implemented native subsystems include:

- persistent context registry;
- thread-local context ownership compatible with Taffy's non-`Send` tree internals;
- generation-safe 64-bit context, node, and resource handles;
- stale-handle and cross-context rejection;
- persistent parent/child topology;
- dirty-state/cache invalidation;
- bulk style updates;
- bulk topology updates;
- bulk measurement updates;
- bulk layout result retrieval;
- duplicate-compute/cache behavior;
- typed Calc resource graph;
- Grid template/resource ownership;
- cached intrinsic measurement records with no managed callback during layout;
- detailed Grid diagnostics.

## Layout surface implemented natively

### Core box/layout behavior

- display modes required for v1;
- width/height/min/max sizing;
- margin/padding/border;
- percent and length units;
- positioning/insets;
- overflow and scrollbar width;
- box sizing;
- direction;
- aspect ratio;
- display none.

### Flexbox

- row / column / reverse directions;
- wrap / wrap-reverse;
- grow / shrink / basis;
- align-items / align-self / align-content;
- justify-content;
- gaps;
- auto margins.

### Block / FlowRoot / Float

- Block and FlowRoot dispatch;
- float left/right;
- clear behavior;
- text-align surface required by Taffy block style.

### Grid

- explicit rows/columns;
- implicit/auto tracks;
- fixed, percent, fractional, auto, min/max-content, minmax, repeat, auto-fill/auto-fit track forms;
- named lines;
- named spans;
- named template areas;
- numeric line/span placement;
- grid-auto-flow row/column/dense variants;
- Grid alignment/justification fields;
- detailed track, gutter, and item placement diagnostics.

### Calc

Typed native Calc resources support the v1 expression model without a CSS text parser. Resource lifetime is owned by the native context.

### Measurement

Caller-supplied cached measurement supports:

- min-content size;
- max-content size;
- preferred size;
- width-dependent samples;
- replaced-element intrinsic aspect ratio;
- no managed callback during Taffy computation.

## Safety/invariant work delivered

- handle generations do not wrap into stale-handle resurrection;
- capacity exhaustion fails explicitly;
- cross-thread context access is rejected;
- topology validation rejects invalid/self-parenting structures;
- bulk operations reject malformed/duplicate updates instead of silently applying ambiguous last-write-wins behavior;
- Calc evaluation/resource depth is bounded;
- true-root layout computation semantics are enforced.

## Test inventory

The repository contains native unit/integration/golden verification covering the above subsystems. Under the current local-first policy, the authoritative compiled execution of that inventory is part of `build/build.py verify-abi-rc` in Phase 3.

Therefore Phase 1 implementation is complete, while its current-machine compiled proof is intentionally inherited by the still-pending Phase 3 local gate rather than being falsely re-declared as freshly verified in this sandbox.
