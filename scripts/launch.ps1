[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$proctalProject=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$proctalApp=Join-Path $proctalProject 'dist\Proctal.Windows\Proctal.exe'
if(-not(Test-Path -LiteralPath $proctalApp)){throw 'Run package.ps1 first.'}
Start-Process -FilePath $proctalApp -WorkingDirectory (Split-Path -Parent $proctalApp) -WindowStyle Hidden
