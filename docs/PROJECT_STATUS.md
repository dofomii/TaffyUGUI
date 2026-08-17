# TaffyUGUI Project Status

**Status date:** 2026-08-17
**Canonical workflow:** local development, local build, local verification
**Active release scope:** Android ARM64 only
**Native ABI source state:** final ABI v1 (`version=1`, `stage=2`) on exact Taffy `0.13.0`

## Current state

- Phase 0 foundation: **complete**.
- Phase 1 native engine implementation: **complete**.
- Phase 2 production C ABI implementation: **complete**.
- Phase 3 native verification: **complete**.
- Phase 4 Android ARM64 native artifact: **complete at final ABI v1 `1/2`**.
- Phase 5 Android Unity native payload: **complete at final ABI v1 `1/2`**.
- Phase 6 managed ABI conformance/final freeze: **complete**.
- Phase 7 minimal Unity uGUI product: **active; P7.1 is next**.

## Final Phase 6 release evidence

The final release chain is content-addressed rather than requiring a Git commit. The following all bind to source snapshot `sha256:68fb502c6bc48c83b2239f5212d98fd6a7f3f777c587cb286876121c58752731`:

- Phase 3 local evidence;
- Phase 4 Android ARM64 manifest and `phase4-index.json`;
- Phase 5 Unity payload provenance.

The accepted Android binary SHA-256 is `7bdca92aae2939e5098292294ee7f7d730d5eee1c718d87f65a3f22349338f66`, ABI v1 `1/2`. `verify-abi-final`, Android native build/verification, `verify-phase4`, `stage-phase5`, and `verify-phase5` all passed.

A fresh Unity `6000.4.3f1` ARM64 IL2CPP Player was also built from a local-only package snapshot of the accepted Phase 5 payload. The APK contained exactly one `lib/arm64-v8a/libtaffy_ugui.so`; its ELF program headers and both `PT_LOAD` runtime segments matched the accepted staged library. Earlier Phase 6 physical-device validation on CPH2723 / Android 16 had already proven final ABI `1/2`, library loading, layout round trips, and last-error diagnostics for the frozen runtime source.

Disposable harness/probe material remains local-only under ignored `.build/` paths and is never part of tracked project source.

## Next authoritative work

Phase 7 is now open. The next tracker task is **P7.1 — production persistent managed/native context lifecycle**.

Windows, macOS, iOS, and WebGL remain deferred outside the active Android ARM64 release scope.
