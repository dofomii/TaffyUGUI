$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $Root

& (Join-Path $Root "scripts/bootstrap-local-toolchain.ps1")
rustup target add x86_64-pc-windows-msvc

if (-not (Get-Command cl.exe -ErrorAction SilentlyContinue) -and
    -not (Get-Command dumpbin.exe -ErrorAction SilentlyContinue) -and
    -not (Get-Command llvm-objdump.exe -ErrorAction SilentlyContinue) -and
    -not (Get-Command objdump.exe -ErrorAction SilentlyContinue)) {
    throw "Run this script from a Visual Studio/MSVC developer environment (or provide LLVM/objdump tooling)."
}

python build/build.py prepare
python build/build.py verify-abi-rc
python build/build.py phase4-host
python build/build.py phase4-status
