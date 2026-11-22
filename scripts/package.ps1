[CmdletBinding()]
param([switch]$NoZip)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'env.ps1')
$proctalInstalled=Join-Path $script:ProctalRoot 'dist\Proctal.Windows'
$proctalDistribution=Join-Path $script:ProctalRoot ('work\package-'+[Guid]::NewGuid().ToString('N'))
if(@(Get-Process Proctal -ErrorAction SilentlyContinue)){throw 'Close Proctal before updating its package.'}
New-Item -ItemType Directory -Path (Split-Path $proctalInstalled -Parent) -Force | Out-Null
New-Item -ItemType Directory -Path $proctalDistribution -Force | Out-Null
if((Get-Item -LiteralPath $proctalDistribution).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Package folder must not be a link.'}
Push-Location $script:ProctalRoot
try {
    & $script:ProctalDotnet publish 'src\Proctal.Desktop\Proctal.Desktop.csproj' -c Release --no-restore --self-contained false -p:AppHostDotNetSearch=AppRelative -p:AppHostRelativeDotNet=.runtime -o $proctalDistribution
    if($LASTEXITCODE -ne 0){throw 'Publishing failed.'}
    $proctalRuntime=Join-Path $proctalDistribution '.runtime'
    New-Item -ItemType Directory -Path $proctalRuntime -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $env:DOTNET_ROOT 'dotnet.exe') -Destination $proctalRuntime -Force
    Copy-Item -LiteralPath (Join-Path $env:DOTNET_ROOT 'host') -Destination $proctalRuntime -Recurse -Force
    foreach($proctalFramework in @('Microsoft.NETCore.App','Microsoft.WindowsDesktop.App')) {
        $proctalShared=Join-Path $proctalRuntime 'shared'
        New-Item -ItemType Directory -Path $proctalShared -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $env:DOTNET_ROOT "shared\$proctalFramework") -Destination $proctalShared -Recurse -Force
    }
    foreach($proctalLicense in @('LICENSE.txt','ThirdPartyNotices.txt')) {
        Copy-Item -LiteralPath (Join-Path $env:DOTNET_ROOT $proctalLicense) -Destination $proctalRuntime -Force
    }
    Copy-Item -LiteralPath (Join-Path $script:ProctalRoot 'README.md') -Destination $proctalDistribution -Force
    [IO.File]::WriteAllText((Join-Path $proctalDistribution 'Launch-Proctal.cmd'),"@echo off`r`nstart `"`" `"%~dp0Proctal.exe`"`r`nexit /b`r`n")
    Copy-Item -LiteralPath (Join-Path $script:ProctalRoot 'docs\THIRD-PARTY-NOTICES.md') -Destination $proctalDistribution -Force
    if(-not $NoZip) {
        $proctalArchive=Join-Path $script:ProctalRoot 'dist\Proctal.Windows.zip'
        # ZipFile includes the hidden .runtime folder; Compress-Archive omits hidden files.
        if(Test-Path -LiteralPath $proctalArchive){Remove-Item -LiteralPath $proctalArchive -Force}
        [IO.Compression.ZipFile]::CreateFromDirectory($proctalDistribution,$proctalArchive)
        $proctalHash=(Get-FileHash -LiteralPath $proctalArchive -Algorithm SHA256).Hash.ToLowerInvariant()
        [IO.File]::WriteAllText((Join-Path $script:ProctalRoot 'dist\Proctal.Windows.sha256'),"$proctalHash  Proctal.Windows.zip`n")
    }
    New-Item -ItemType Directory -Path $proctalInstalled -Force | Out-Null
    if((Get-Item -LiteralPath $proctalInstalled).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Installed package folder must not be a link.'}
    # Publish and archive a fresh folder so an existing portable profile never enters a release.
    Get-ChildItem -LiteralPath $proctalDistribution -Force | Copy-Item -Destination $proctalInstalled -Recurse -Force
    $proctalResolved=[IO.Path]::GetFullPath($proctalDistribution)
    if(-not $proctalResolved.StartsWith((Join-Path $script:ProctalRoot 'work')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe package cleanup path.'}
    Remove-Item -LiteralPath $proctalResolved -Recurse -Force
} finally {Pop-Location}
Write-Host "Portable Windows app ready: $proctalInstalled\Proctal.exe"
