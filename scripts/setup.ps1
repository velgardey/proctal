[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
if (-not (Test-Path -LiteralPath (Join-Path $script:ProctalRoot '.proctal-project'))) { throw 'Project marker is missing.' }
if (Test-Path -LiteralPath $script:ProctalDotnet) {
    & $script:ProctalDotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'The local SDK could not start.' }
    Write-Host 'Proctal SDK already installed locally.'
    return
}
Write-Host 'Downloading the official .NET 10 SDK into Proctal. No global installation or PATH change.'
$metadata = Invoke-RestMethod 'https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json'
$version = (Get-Content -LiteralPath (Join-Path $script:ProctalRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
$sdk = $metadata.releases | ForEach-Object { $_.sdks } | Where-Object { $_.version -eq $version } | Select-Object -First 1
$file = $sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.name.EndsWith('.zip') } | Select-Object -First 1
if (-not $file -or -not $file.hash -or -not $file.url.StartsWith('https://')) { throw 'Official SDK metadata was incomplete.' }
$archive = Join-Path $script:ProctalRoot 'work\dotnet-sdk.zip'
Invoke-WebRequest -Uri $file.url -OutFile $archive
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $file.hash) { throw 'SDK checksum mismatch. Installation stopped.' }
New-Item -ItemType Directory -Path $env:DOTNET_ROOT -Force | Out-Null
Expand-Archive -LiteralPath $archive -DestinationPath $env:DOTNET_ROOT -Force
Remove-Item -LiteralPath $archive -Force
@{version=$version;url=$file.url;sha512=$file.hash;installedLocally=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:ProctalRoot 'docs\sdk.json') -Encoding utf8
& $script:ProctalDotnet --version
if ($LASTEXITCODE -ne 0) { throw 'The downloaded SDK could not start.' }
Write-Host 'Local SDK ready. Proctal dependencies and caches will stay in this folder.'
