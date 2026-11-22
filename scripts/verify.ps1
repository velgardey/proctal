[CmdletBinding()]
param([switch]$SkipAzure)
$ErrorActionPreference='Stop'
$proctalProject=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$proctalApp=Join-Path $proctalProject 'dist\Proctal.Windows\Proctal.exe'
if(-not(Test-Path -LiteralPath $proctalApp)){throw 'Run package.ps1 first.'}
if(@(Get-Process Proctal -ErrorAction SilentlyContinue)){throw 'Close Proctal before verification.'}
$proctalReports=Join-Path $proctalProject 'work\verification'
New-Item -ItemType Directory -Path $proctalReports -Force | Out-Null
$proctalFailures=@()
foreach($proctalMode in $(if($SkipAzure){@('self-check')}else{@('self-check','check-azure')})) {
    $proctalReport=Join-Path $proctalReports "$proctalMode.json"
    if(Test-Path -LiteralPath $proctalReport){Remove-Item -LiteralPath $proctalReport -Force}
    $proctalArguments=@("--$proctalMode",('"'+$proctalReport+'"'))
    $proctalCheck=Start-Process -FilePath $proctalApp -ArgumentList $proctalArguments -WorkingDirectory $proctalProject -WindowStyle Hidden -PassThru
    if(-not $proctalCheck.WaitForExit(70000)){ $proctalCheck.Kill();$proctalFailures+="$proctalMode timed out";continue }
    if(-not(Test-Path -LiteralPath $proctalReport)){$proctalFailures+="$proctalMode produced no report";continue}
    $proctalResult=Get-Content -LiteralPath $proctalReport -Raw | ConvertFrom-Json
    Write-Host "$proctalMode report: $proctalReport"
    $proctalResult | ConvertTo-Json -Depth 6 | Write-Host
    if(-not $proctalResult.passed){$proctalFailures+=$proctalMode}
}
if($proctalFailures){throw ('Checks need attention: '+($proctalFailures -join ', '))}
Write-Host 'Native UI, screen capture, and requested Azure checks passed. Receiver-side sharing and a microphone rehearsal still require manual checks.'
