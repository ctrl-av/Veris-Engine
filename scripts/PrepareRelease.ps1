param([string]$OutputDirectory = 'artifacts/releases/1.0.0-rc.1')
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$output = [IO.Path]::GetFullPath([IO.Path]::Combine($repository, $OutputDirectory))
if (Test-Path -LiteralPath $output) { throw 'Choose a new release directory; existing releases are never overwritten.' }
New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $repository
try {
    foreach ($configuration in @('Debug', 'Release')) {
        $verification = Start-Process powershell -WorkingDirectory $repository -WindowStyle Hidden -Wait -PassThru -ArgumentList @('-NoProfile', '-File', 'scripts/VerifyProject.ps1', '-Configuration', $configuration) -RedirectStandardOutput (Join-Path $output "$configuration-validation.log") -RedirectStandardError (Join-Path $output "$configuration-diagnostics.log")
        if ($verification.ExitCode -ne 0) { throw "$configuration validation failed; inspect its release logs." }
        Write-Output "PASS: $configuration release qualification on the development machine."
    }
    $performance = Join-Path $output 'performance.json'
    dotnet run --project Tools/Performance/Performance.csproj --configuration Release --verbosity quiet -- $performance
    if ($LASTEXITCODE -ne 0) { throw 'Release performance budgets failed.' }
    $editor = Join-Path $output 'Editor'
    & ./scripts/PublishEditor.ps1 -Configuration Release -OutputDirectory $editor
    & ./scripts/VerifyEditorResources.ps1 -EditorDirectory $editor -Configuration Release
    $game = Join-Path $output 'BeaconRun'
    dotnet run --project Tools/ExportProject/ExportProject.csproj --configuration Release --verbosity quiet -- Samples/BeaconRun/BeaconRun.veris $game (Join-Path $editor 'Runtime')
    if ($LASTEXITCODE -ne 0) { throw 'Sample export failed.' }
    & (Join-Path $game 'Game.exe') --frames 3
    if ($LASTEXITCODE -ne 0) { throw 'Packaged sample launch failed.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    foreach ($package in @('Editor', 'BeaconRun')) {
        [IO.Compression.ZipFile]::CreateFromDirectory((Join-Path $output $package), (Join-Path $output "Veris-1.0.0-rc.1-$package-win-x64.zip"))
    }
    $sourceFiles = @()
    foreach ($relative in (& git ls-files --cached --others --exclude-standard)) {
        if (Test-Path -LiteralPath $relative -PathType Leaf) {
            $sourceFiles += @{ path = $relative; sha256 = (Get-FileHash -LiteralPath $relative -Algorithm SHA256).Hash.ToLowerInvariant() }
        }
    }
    $sourceFiles | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'source-manifest.json') -Encoding UTF8
    @{
        version = '1.0.0-rc.1'; preparedUtc = [DateTime]::UtcNow.ToString('o'); sourceCommit = (& git rev-parse HEAD)
        workingTreeStatus = @(& git status --porcelain); debugValidation = 'passed'; releaseValidation = 'passed'
        performance = 'passed'; localSampleLaunch = 'passed'; cleanWindows10 = 'pending'; cleanWindows11 = 'pending'
        qualification = 'release candidate; clean-install validation pending'
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'release.json') -Encoding UTF8
    $checksums = foreach ($file in Get-ChildItem -LiteralPath $output -File | Sort-Object Name) {
        "{0}  {1}" -f (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $file.Name
    }
    $checksums | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ASCII
    Write-Output "PASS: Release candidate prepared at $output. Clean Windows qualification remains pending."
}
finally { Pop-Location }
