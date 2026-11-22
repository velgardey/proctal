[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param([ValidateSet('Build','Toolchain','Data','All')][string]$Mode='Build',[switch]$RemoveProject)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\','/')
$marker=Join-Path $root '.proctal-project'
if ($root -eq [IO.Path]::GetPathRoot($root).TrimEnd('\','/') -or -not (Test-Path -LiteralPath $marker) -or (Get-Content -LiteralPath $marker -Raw).Trim() -ne 'proctal-project-v1') { throw 'Cleanup requires a marked Proctal project, never a drive root.' }
if ((Get-Item -LiteralPath $root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Cleanup refuses a linked project directory.' }
foreach($process in @(Get-Process Proctal -ErrorAction SilentlyContinue)) {
    try { $processPath=$process.Path } catch { throw 'Close Proctal before cleanup.' }
    if ($processPath -and $processPath.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Close this Proctal application before cleanup.' }
}
$buildTargets=@('.cache','dist','work','TestResults','src\Proctal.Core\bin','src\Proctal.Core\obj','src\Proctal.Desktop\bin','src\Proctal.Desktop\obj','tests\Proctal.Tests\bin','tests\Proctal.Tests\obj')
$targets=switch($Mode){ 'Build' {$buildTargets}; 'Toolchain' {$buildTargets+'.tools'}; 'Data' {'.data'}; 'All' {$buildTargets+'.tools'+'.data'} }
if($RemoveProject){$targets=@('.')}
$verifiedTargets=@()
foreach($relative in $targets) {
    $target=[IO.Path]::GetFullPath((Join-Path $root $relative)).TrimEnd('\','/')
    if ($target -ne $root -and -not $target.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)) { throw "Cleanup target leaves the project: $relative" }
    if ($target -eq $root -and -not $RemoveProject) { throw 'Removing the project requires -RemoveProject.' }
    if (-not (Test-Path -LiteralPath $target)) { continue }
    $linked=@(Get-Item -LiteralPath $target -Force; Get-ChildItem -LiteralPath $target -Force -Recurse -ErrorAction Stop) | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }
    if($linked){throw "Cleanup refuses linked files or folders under: $relative. Remove those links manually first."}
    $verifiedTargets+=$target
}
foreach($target in $verifiedTargets) {
    if ($PSCmdlet.ShouldProcess($target,'Remove Proctal files recursively')) { Remove-Item -LiteralPath $target -Recurse -Force }
}
if(-not $RemoveProject){Write-Host "Proctal cleanup complete ($Mode). Source files are retained."}
