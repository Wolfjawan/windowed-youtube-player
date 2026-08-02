$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$iconBytes = [Convert]::FromBase64String((Get-Content assets/app.ico.b64 -Raw).Trim())
[IO.File]::WriteAllBytes("src/WindowedYouTubePlayer/app.ico", $iconBytes)

New-Item -ItemType Directory -Force artifacts | Out-Null

dotnet publish src/WindowedYouTubePlayer/WindowedYouTubePlayer.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output artifacts/win-x64

$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
  throw "Inno Setup 6 is required to build the installer."
}

& $iscc installer/WindowedStreamingPlayer.iss
