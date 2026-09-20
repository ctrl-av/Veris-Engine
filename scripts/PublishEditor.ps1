param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$OutputDirectory = 'artifacts/publish/editor'
)
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$installation) { throw 'Visual Studio C++ tools are required to package the Release runtime.' }
$msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
& $msbuild (Join-Path $repository 'Runtime\VerisRuntime.vcxproj') /p:Configuration=Release /p:Platform=x64 /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Release runtime build failed.' }
dotnet publish (Join-Path $repository 'VerisEditor\VerisEditor.csproj') --configuration $Configuration --output $OutputDirectory --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Editor publication failed.' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
$runtime = Join-Path $output 'Runtime'
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
$native = Join-Path $repository 'artifacts\bin\VerisRuntime\x64\Release'
Copy-Item -LiteralPath (Join-Path $native 'VerisRuntime.exe') -Destination $runtime -Force
Get-ChildItem -LiteralPath $native -Filter '*.dll' | Copy-Item -Destination $runtime -Force
Copy-Item -LiteralPath (Join-Path $native 'licenses') -Destination $runtime -Recurse -Force
$redist = Get-ChildItem -LiteralPath (Join-Path $installation 'VC\Redist\MSVC') -Directory |
    Where-Object { $runtimeVersion = $null; [Version]::TryParse($_.Name, [ref]$runtimeVersion) } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$redist) { throw 'MSVC redistributable files are missing.' }
$crt = Join-Path $redist.FullName 'x64\Microsoft.VC143.CRT'
Get-ChildItem -LiteralPath $crt -Filter '*.dll' | Copy-Item -Destination $runtime -Force
foreach ($required in @('VerisRuntime.exe', 'sfml-graphics-3.dll', 'sfml-audio-3.dll', 'msvcp140.dll', 'vcruntime140.dll', 'vcruntime140_1.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $runtime $required))) { throw "Runtime package is incomplete: $required" }
}
Write-Output "PASS: Published $Configuration editor with a Release export runtime at $output"
