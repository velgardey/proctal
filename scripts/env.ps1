$script:ProctalRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$env:DOTNET_ROOT = Join-Path $script:ProctalRoot '.tools\dotnet'
$env:DOTNET_CLI_HOME = Join-Path $script:ProctalRoot '.tools\cli-home'
$env:NUGET_PACKAGES = Join-Path $script:ProctalRoot '.cache\nuget-packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $script:ProctalRoot '.cache\nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $script:ProctalRoot '.cache\nuget-plugins'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:TEMP = Join-Path $script:ProctalRoot 'work\tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Path $env:TEMP,$env:DOTNET_CLI_HOME -Force | Out-Null
$script:ProctalDotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
