param([Parameter(Mandatory = $true)][string]$CandidateDirectory)
$ErrorActionPreference = 'Stop'
$candidate = (Resolve-Path -LiteralPath $CandidateDirectory).Path
foreach ($line in Get-Content -LiteralPath (Join-Path $candidate 'SHA256SUMS.txt')) {
    $separator = $line.IndexOf('  ')
    if ($separator -ne 64) { throw 'Malformed release checksum entry.' }
    $name = $line.Substring(66)
    if ([IO.Path]::GetFileName($name) -ne $name) { throw 'Release checksum paths must be filenames.' }
    if ((Get-FileHash -LiteralPath (Join-Path $candidate $name) -Algorithm SHA256).Hash -ne $line.Substring(0, 64)) {
        throw "Release checksum mismatch: $name"
    }
}
$release = Get-Content -LiteralPath (Join-Path $candidate 'release.json') -Raw | ConvertFrom-Json
if ($release.debugValidation -ne 'passed' -or $release.releaseValidation -ne 'passed' -or $release.performance -ne 'passed') {
    throw 'The candidate does not contain passing local qualification results.'
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$extracted = Join-Path $candidate "Verification $([Guid]::NewGuid().ToString('N'))"
foreach ($package in @('Editor', 'BeaconRun')) {
    [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $candidate "Veris-1.0.0-rc.1-$package-win-x64.zip"), (Join-Path $extracted $package))
}
$game = Join-Path $extracted 'BeaconRun'
$manifest = Get-Content -LiteralPath (Join-Path $game 'export-manifest.json') -Raw | ConvertFrom-Json
foreach ($file in $manifest.files) {
    $path = [IO.Path]::GetFullPath((Join-Path $game $file.path))
    if (!$path.StartsWith($game + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Export manifest escapes the game directory.' }
    if ((Get-Item -LiteralPath $path).Length -ne $file.bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256) {
        throw "Extracted game content mismatch: $($file.path)"
    }
}
$editor = Join-Path $extracted 'Editor'
foreach ($name in @('VerisEditor.exe', 'VerisInterop.dll', 'msvcp140.dll', 'vcruntime140.dll', 'vcruntime140_1.dll', 'Runtime/VerisRuntime.exe', 'ProjectTemplates/TopDownProject/template.xml')) {
    if (!(Test-Path -LiteralPath (Join-Path $editor $name) -PathType Leaf)) { throw "Extracted editor is incomplete: $name" }
}
$runtime = Start-Process -FilePath (Join-Path $game 'Game.exe') -ArgumentList @('--frames', '3') -WorkingDirectory $candidate -WindowStyle Hidden -PassThru
if (!$runtime.WaitForExit(30000)) { $runtime.Kill(); throw 'Extracted game launch timed out.' }
if ($runtime.ExitCode -ne 0) { throw 'Extracted game launch failed.' }
$shell = Start-Process -FilePath (Join-Path $editor 'VerisEditor.exe') -WorkingDirectory $candidate -WindowStyle Hidden -PassThru
try {
    if ($shell.WaitForExit(5000)) { throw "Extracted editor exited during startup: $($shell.ExitCode)" }
    $shell.Refresh()
    if ($shell.MainWindowHandle -eq [IntPtr]::Zero) { throw 'Extracted editor did not create its project browser.' }
}
finally {
    if (!$shell.HasExited) {
        $null = $shell.CloseMainWindow()
        if (!$shell.WaitForExit(5000)) { $shell.Kill() }
    }
}
Write-Output "PASS: Archive checksums, extracted manifest, game launch from another directory and editor startup. Extracted files: $extracted"
Write-Output 'Clean Windows 10/11 qualification is a separate pending check.'
