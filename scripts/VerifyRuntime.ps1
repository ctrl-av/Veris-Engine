param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repositoryDirectory = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$runtimeBuildDirectory = Join-Path $repositoryDirectory "artifacts\bin\VerisRuntime\x64\$Configuration"
$fixtureDirectory = Join-Path $repositoryDirectory "artifacts\runtime-checks\$Configuration\Run $([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
$runtimeDirectory = Join-Path $fixtureDirectory 'Standalone'
New-Item -ItemType Directory -Path $runtimeDirectory | Out-Null
Copy-Item -LiteralPath (Join-Path $runtimeBuildDirectory 'VerisRuntime.exe') -Destination $runtimeDirectory
Get-ChildItem -LiteralPath $runtimeBuildDirectory -Filter 'sfml-*.dll' | Copy-Item -Destination $runtimeDirectory
Copy-Item -LiteralPath (Join-Path $runtimeBuildDirectory 'licenses') -Destination $runtimeDirectory -Recurse
$runtimeExecutable = Join-Path $runtimeDirectory 'VerisRuntime.exe'
New-Item -ItemType Directory -Path (Join-Path $fixtureDirectory 'Content'), (Join-Path $fixtureDirectory 'GameCode') | Out-Null
$projectFile = Join-Path $fixtureDirectory ("Game " + [char]0x03A9 + '.veris')

function Invoke-RuntimeCheck {
    param([string]$CaseName, [int]$ExpectedExitCode, [string]$FrameCount = '2')
    $stdout = Join-Path $fixtureDirectory "$CaseName.stdout.txt"
    $stderr = Join-Path $fixtureDirectory "$CaseName.stderr.txt"
    $runtime = Start-Process -FilePath $runtimeExecutable -ArgumentList @(('"' + $projectFile + '"'), '--frames', $FrameCount) -WorkingDirectory $runtimeDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    # Windows PowerShell needs a retained handle to read the exit code after the process exits.
    $runtime.Handle | Out-Null
    if (!$runtime.WaitForExit(30000)) {
        $runtime.Kill()
        throw "Runtime check timed out: $CaseName"
    }
    if ($runtime.ExitCode -ne $ExpectedExitCode) { throw "Runtime check failed: $CaseName. $(Get-Content -LiteralPath $stderr -Raw)" }
    if ($ExpectedExitCode -eq 0 -and !(Get-Content -LiteralPath $stdout -Raw).Contains('Completed 2 frames.')) {
        throw "Runtime did not complete two frames: $CaseName"
    }
    if ($ExpectedExitCode -ne 0 -and [string]::IsNullOrWhiteSpace((Get-Content -LiteralPath $stderr -Raw))) {
        throw "Runtime failure did not produce a diagnostic: $CaseName"
    }
    Write-Output "PASS: Runtime $CaseName"
}

$projectSettings = @{
    formatVersion = 1; projectId = [Guid]::NewGuid().ToString(); name = 'Runtime Smoke'; templateId = 'empty'
    contentDirectory = 'Content'; gameCodeDirectory = 'GameCode'; startupScene = $null; windowWidth = 32; windowHeight = 32
}
[IO.File]::WriteAllText($projectFile, ($projectSettings | ConvertTo-Json))
Invoke-RuntimeCheck 'valid-project' 0
Invoke-RuntimeCheck 'zero-frame-limit' 1 '0'
Invoke-RuntimeCheck 'negative-frame-limit' 1 '-1'
Invoke-RuntimeCheck 'nonnumeric-frame-limit' 1 'invalid'
Invoke-RuntimeCheck 'overflowing-frame-limit' 1 '18446744073709551616'
$invalidProjects = @(
    @{ Name = 'invalid-window-mode'; Key = 'windowMode'; Value = 'invalid' },
    @{ Name = 'invalid-vsync-type'; Key = 'verticalSync'; Value = 1 },
    @{ Name = 'unsupported-fullscreen'; Key = 'windowMode'; Value = 'fullscreen' },
    @{ Name = 'future-version'; Key = 'formatVersion'; Value = 2 },
    @{ Name = 'invalid-width'; Key = 'windowWidth'; Value = 0 },
    @{ Name = 'fractional-width'; Key = 'windowWidth'; Value = 1.5 },
    @{ Name = 'empty-guid'; Key = 'projectId'; Value = '00000000-0000-0000-0000-000000000000' },
    @{ Name = 'escaping-path'; Key = 'contentDirectory'; Value = '../Outside' },
    @{ Name = 'reserved-path'; Key = 'contentDirectory'; Value = 'CON' },
    @{ Name = 'missing-directory'; Key = 'gameCodeDirectory'; Value = 'Missing' },
    @{ Name = 'unknown-field'; Key = 'unknownField'; Value = 1 },
    @{ Name = 'blank-name'; Key = 'name'; Value = ([string][char]0x2003) }
)
foreach ($invalidProject in $invalidProjects) {
    $invalidSettings = $projectSettings.Clone()
    $invalidSettings[$invalidProject.Key] = $invalidProject.Value
    [IO.File]::WriteAllText($projectFile, ($invalidSettings | ConvertTo-Json))
    Invoke-RuntimeCheck $invalidProject.Name 1
}
$missingSettings = $projectSettings.Clone()
$missingSettings.Remove('templateId')
[IO.File]::WriteAllText($projectFile, ($missingSettings | ConvertTo-Json))
Invoke-RuntimeCheck 'missing-required-field' 1
[IO.File]::WriteAllText($projectFile, (($projectSettings | ConvertTo-Json) + ' {}'))
Invoke-RuntimeCheck 'trailing-json' 1
[IO.File]::WriteAllText($projectFile, 'invalid json')
Invoke-RuntimeCheck 'malformed-json' 1
[IO.File]::WriteAllText($projectFile, ($projectSettings | ConvertTo-Json))
$metadataDirectory = Join-Path $fixtureDirectory '.Veris'
New-Item -ItemType Directory -Path $metadataDirectory -Force | Out-Null
$inputConfigurationFile = Join-Path $metadataDirectory 'input-actions.json'
$validInputConfiguration = '{"formatVersion":1,"actions":{"Quit":["Key:Escape"],"Fire":["Mouse:Left","Key:Space"]}}'
[IO.File]::WriteAllText($inputConfigurationFile, $validInputConfiguration)
Invoke-RuntimeCheck 'valid-action-configuration' 0
$invalidInputConfigurations = @(
    @{ Name = 'unknown-input-key'; Json = '{"formatVersion":1,"actions":{"Fire":["Key:NotAKey"]}}' },
    @{ Name = 'duplicate-input-binding'; Json = '{"formatVersion":1,"actions":{"Fire":["Key:A","Key:A"]}}' },
    @{ Name = 'empty-input-binding'; Json = '{"formatVersion":1,"actions":{"Fire":[]}}' },
    @{ Name = 'invalid-action-name'; Json = '{"formatVersion":1,"actions":{"Bad Name":["Key:A"]}}' },
    @{ Name = 'invalid-input-version'; Json = '{"formatVersion":2,"actions":{}}' },
    @{ Name = 'invalid-input-type'; Json = '{"formatVersion":1,"actions":{"Fire":[1]}}' },
    @{ Name = 'malformed-input-configuration'; Json = 'invalid json' }
)
foreach ($invalidInputConfiguration in $invalidInputConfigurations) {
    [IO.File]::WriteAllText($inputConfigurationFile, $invalidInputConfiguration.Json)
    Invoke-RuntimeCheck $invalidInputConfiguration.Name 1
}
[IO.File]::WriteAllText($inputConfigurationFile, $validInputConfiguration)
Write-Output "Runtime check fixtures and diagnostics: $fixtureDirectory"
