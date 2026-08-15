# Contributing

Contributions are welcome.

## Development contract

Before starting work, read:

1. [docs/PROJECT_DECISIONS.md](docs/PROJECT_DECISIONS.md) — normative engineering decisions.
2. [docs/TASK_TRACKER.md](docs/TASK_TRACKER.md) — current phase and next task.
3. [docs/DEVELOPMENT_PLAN.md](docs/DEVELOPMENT_PLAN.md) — end-to-end implementation plan.

Do not bypass phase gates or introduce a competing ABI/build strategy without explicitly updating the normative decisions.

## Native development

1. Fork or branch from `main`.
2. Install Rustup; the repository `rust-toolchain.toml` selects the normal Rust toolchain automatically.
3. Run the canonical quality command:

   ```bash
   python build/build.py quality
   ```

4. Keep `native/Cargo.lock` synchronized and use locked dependency resolution.
5. Add deterministic tests/verification with new native behavior.
6. Keep changes focused and update `docs/TASK_TRACKER.md` when completing tracked tasks.

The project MSRV is Rust 1.82.0 and CI validates it separately from the pinned normal/release toolchain.

## ABI changes

The current bootstrap ABI is version 0. The production ABI lifecycle is defined in `PROJECT_DECISIONS.md`.

Once ABI v1 is frozen, binary-incompatible changes require an explicit ABI version increment and the corresponding rebuild/migration work. Do not expose Rust/Taffy internal types directly across the C boundary.

## Unity changes

User-facing Unity feature work is gated by the task tracker. When the relevant Unity phase is active, changes must be tested against the matching staged native artifact and the required Unity Edit/Play/player validation lane.

## Licensing

By contributing, you agree that your contribution is licensed under the repository's MIT License.
