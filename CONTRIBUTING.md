# Contributing

TaffyUGUI development is local-first. Before backing up a change to the repository mirror, run the permanent project checks that apply to the change. For the native ABI/release path, use:

```bash
python3 build/build.py quality
python3 build/build.py verify-abi-final
```

For native platform work, build and verify the affected target locally with `build/build.py native <target>` and keep its manifest/checksum evidence. Do not treat a remote CI result as a substitute for local verification.

Keep Unity rendering/input behavior separate from native geometry computation, preserve the `tu_*` ABI unless a deliberate ABI version change is documented, and do not silently break Unity serialized component data.

## Repository rule: no harness/probe code

Disposable test harnesses, one-off probes, temporary Unity projects, device runners, diagnostic executables, generated validation evidence, and exploratory verification scripts are **local-only** and must never be committed to Git.

Create such material only under ignored `.build/` paths (for example `.build/local-validation/`) or outside the repository. Do not add temporary harness/probe files under `scripts/`, `tests/`, `UnityPackage/`, `native/`, or other tracked project directories.

Permanent product tests are allowed when they are maintainable regression/unit/integration tests that belong to the project itself; disposable reproduction/probe code is not.
