[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
Push-Location $script:ProctalRoot
try {
    & $script:ProctalDotnet build 'src\Proctal.Desktop\Proctal.Desktop.csproj' -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Build failed. If packages are missing, run restore.ps1 first.'}
} finally {Pop-Location}
