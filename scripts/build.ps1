param([string]$IsccPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    dotnet publish src/OLA3D.Calculator/OLA3D.Calculator.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish
    if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
    if (-not $IsccPath) {
        $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($compiler) { $IsccPath = $compiler.Source }
        else { $IsccPath = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe' }
    }
    if (-not (Test-Path -LiteralPath $IsccPath)) { throw 'Inno Setup 6 is required. Pass -IsccPath with the path to ISCC.exe.' }
    & $IsccPath installer/OLA3D.Calculator.iss
    if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
    Get-ChildItem artifacts/release/*.exe | Get-FileHash -Algorithm SHA256 | ForEach-Object {
        '{0}  {1}' -f $_.Hash.ToLowerInvariant(), (Split-Path $_.Path -Leaf)
    } | Set-Content artifacts/release/SHA256SUMS.txt -Encoding ascii
} finally { Pop-Location }
