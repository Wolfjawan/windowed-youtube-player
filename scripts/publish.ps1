$ErrorActionPreference = "Stop"

$Project = Join-Path $PSScriptRoot "..\src\WindowedYouTubePlayer\WindowedYouTubePlayer.csproj"
$Output = Join-Path $PSScriptRoot "..\artifacts\win-x64"

Remove-Item $Output -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish $Project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $Output

Write-Host "Published to $Output"
