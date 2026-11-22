[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
if (-not (Test-Path -LiteralPath $script:ProctalDotnet)) { throw 'Run scripts/setup.ps1 first.' }
Push-Location $script:ProctalRoot
try {
    foreach ($project in @('src\Proctal.Desktop\Proctal.Desktop.csproj','tests\Proctal.Tests\Proctal.Tests.csproj')) {
        & $script:ProctalDotnet restore $project --configfile NuGet.Config --locked-mode
        if ($LASTEXITCODE -ne 0) { throw "Dependency restore failed: $project" }
    }
} finally { Pop-Location }
Write-Host 'Dependencies downloaded locally and locked. Ready to build and test.'
