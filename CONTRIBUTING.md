# Contributing

TaffyUGUI development is local-first. Before backing up a change to the repository mirror:

```bash
python3 build/build.py static-gate
python3 build/build.py verify-abi-rc
```

For native platform work, build and verify the affected target locally with `build/build.py native <target>` and keep its manifest/checksum evidence. Do not treat a remote CI result as a substitute for the local gate.

Keep Unity rendering/input behavior separate from native geometry computation, preserve the `tu_*` ABI unless a deliberate ABI version change is documented, and do not silently break Unity serialized component data.
