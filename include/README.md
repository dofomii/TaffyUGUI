# Native C API Header

`taffy_ugui.h` is the generated public C ABI header for TaffyUGUI.

The production header is generated from the Rust FFI surface with:

```text
python build/build.py header
```

Generation uses the repository `cbindgen.toml`. The generated header becomes authoritative when the production ABI candidate is implemented; it must not be edited by hand.
