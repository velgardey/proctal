$ErrorActionPreference='Stop'
$proctalRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$proctalArchive=Join-Path $proctalRoot 'dist\Proctal.Windows.zip'
$proctalZip=[IO.Compression.ZipFile]::OpenRead($proctalArchive)
try {
    $proctalEntries=@($proctalZip.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach($proctalRequired in @('Proctal.exe','Proctal.dll','.runtime/dotnet.exe','Launch-Proctal.cmd','THIRD-PARTY-NOTICES.md')) {
        if($proctalRequired -notin $proctalEntries){throw "Package missing $proctalRequired"}
    }
    foreach($proctalFramework in @('Microsoft.NETCore.App','Microsoft.WindowsDesktop.App')) {
        if(-not @($proctalEntries | Where-Object { $_.StartsWith(".runtime/shared/$proctalFramework/") }).Count){throw "Package missing $proctalFramework runtime"}
    }
    if(@($proctalEntries | Where-Object { $_ -match '(^|/)(\.data|\.tools|\.cache|work|src|tests)(/|$)|(^|/)(settings\.json|answers\.json|documents\.json|azure-key\.bin)$' }).Count){throw 'Private or development files entered the release.'}
    $proctalChecksum=(Get-Content -LiteralPath (Join-Path $proctalRoot 'dist\Proctal.Windows.sha256') -Raw).Trim()
    if($proctalChecksum -ne ((Get-FileHash -LiteralPath $proctalArchive -Algorithm SHA256).Hash.ToLowerInvariant()+'  Proctal.Windows.zip')){throw 'Release checksum mismatch.'}
    'PASS: app, bundled runtimes, notices, private-data exclusion and SHA256.'
} finally {$proctalZip.Dispose()}
