$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Toolchain = Join-Path $Root ".toolchain"
$env:CARGO_HOME = $Toolchain
$env:RUSTUP_HOME = Join-Path $Toolchain "rustup"
$env:PATH = (Join-Path $Toolchain "bin") + ";" + $env:PATH
New-Item -ItemType Directory -Force -Path $Toolchain | Out-Null

if (-not (Get-Command rustup -ErrorAction SilentlyContinue)) {
    $Init = Join-Path $env:TEMP "rustup-init.exe"
    Invoke-WebRequest "https://win.rustup.rs/x86_64" -OutFile $Init
    & $Init -y --no-modify-path --profile minimal --default-toolchain 1.97.1
}

rustup toolchain install 1.97.1 --profile minimal --component rustfmt --component clippy
rustup override set 1.97.1
if (-not (Get-Command cbindgen -ErrorAction SilentlyContinue) -or -not ((cbindgen --version) -match "0.29.2")) {
    cargo install cbindgen --version 0.29.2 --locked
}
python "$Root/build/build.py" doctor
