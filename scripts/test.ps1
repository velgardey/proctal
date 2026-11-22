[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
Push-Location $script:ProctalRoot
try {
    & $script:ProctalDotnet test 'tests\Proctal.Tests\Proctal.Tests.csproj' -c Release --no-restore --logger 'trx;LogFileName=core.trx' --results-directory 'work\test-results'
    if($LASTEXITCODE -ne 0){throw 'Core tests failed.'}
    & (Join-Path $script:ProctalRoot 'tests\cleanup-tests.ps1')
} finally {Pop-Location}
