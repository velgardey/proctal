$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$fixtureRoot=Join-Path $project ('work\cleanup-test-'+[Guid]::NewGuid().ToString('N'))
if (-not $fixtureRoot.StartsWith((Join-Path $project 'work')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test fixture path.' }
New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'scripts'),(Join-Path $fixtureRoot '.cache'),(Join-Path $fixtureRoot '.data'),(Join-Path $fixtureRoot 'src'),(Join-Path $fixtureRoot 'dist') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $project 'scripts\cleanup.ps1') -Destination (Join-Path $fixtureRoot 'scripts\cleanup.ps1')
'proctal-project-v1' | Set-Content -LiteralPath (Join-Path $fixtureRoot '.proctal-project')
'keep source' | Set-Content -LiteralPath (Join-Path $fixtureRoot 'src\keep.txt')
'secret' | Set-Content -LiteralPath (Join-Path $fixtureRoot '.data\private.txt')
'cached' | Set-Content -LiteralPath (Join-Path $fixtureRoot '.cache\package.txt')
try {
    & (Join-Path $fixtureRoot 'scripts\cleanup.ps1') -Mode All -WhatIf
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot '.data\private.txt'))) { throw 'WhatIf removed private data.' }
    & (Join-Path $fixtureRoot 'scripts\cleanup.ps1') -Mode Build -Confirm:$false
    if (Test-Path -LiteralPath (Join-Path $fixtureRoot '.cache')) { throw 'Build cleanup left its cache.' }
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot '.data\private.txt'))) { throw 'Build cleanup removed private data.' }
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'src\keep.txt'))) { throw 'Build cleanup removed source.' }
    & (Join-Path $fixtureRoot 'scripts\cleanup.ps1') -Mode All -Confirm:$false
    if (Test-Path -LiteralPath (Join-Path $fixtureRoot '.data')) { throw 'All cleanup left private data.' }
    if (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'src\keep.txt'))) { throw 'All cleanup removed source.' }
    Remove-Item -LiteralPath (Join-Path $fixtureRoot '.proctal-project') -Force
    $refused=$false
    try { & (Join-Path $fixtureRoot 'scripts\cleanup.ps1') -Mode All -Confirm:$false } catch { $refused=$true }
    if (-not $refused) { throw 'Cleanup accepted an unmarked directory.' }
    'PASS: preview, build-only cleanup, data cleanup, source preservation, and marker guard.'
} finally {
    $resolved=[IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolved.StartsWith((Join-Path $project 'work')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture removal.' }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
