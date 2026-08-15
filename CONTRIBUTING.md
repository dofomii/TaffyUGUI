# Contributing

Contributions are welcome.

## Development

1. Fork or branch from `main`.
2. Install stable Rust.
3. Run `cargo fmt --manifest-path native/Cargo.toml --all -- --check`.
4. Run `cargo clippy --manifest-path native/Cargo.toml --all-targets -- -D warnings`.
5. Run `cargo test --manifest-path native/Cargo.toml`.
6. For Unity changes, test in a supported Unity project with the matching native library present.

Keep changes focused and preserve the stable C ABI unless the change explicitly requires an ABI version bump.

## Licensing

By contributing, you agree that your contribution is licensed under the repository's MIT License.
