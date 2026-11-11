$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../scripts/DependencyDownloads.ps1')
$fixture = Join-Path $PSScriptRoot "../artifacts/dependency-checks/$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
$script:payload = 'Verified dependency fixture'
$source = Join-Path $fixture 'source.bin'
[IO.File]::WriteAllText($source, $script:payload)
$sha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$script:transientAttempts = 0

# The transport is deterministic; assertions cover actual cache bytes, publication and staging cleanup.
function Invoke-WebRequest {
    [CmdletBinding()]
    param([string]$Uri, [string]$OutFile, [int]$TimeoutSec, [switch]$UseBasicParsing)
    if ($TimeoutSec -ne 30 -or !$UseBasicParsing) { throw 'Downloads must use bounded requests without browser parsing.' }
    [IO.File]::WriteAllText($OutFile, 'Partial or invalid response')
    if ($Uri -eq 'fixture://offline') { throw 'Connection timed out.' }
    if ($Uri -eq 'fixture://corrupt') { return }
    if ($Uri -eq 'fixture://transient') {
        $script:transientAttempts++
        if ($script:transientAttempts -lt 3) { throw 'Temporary server failure.' }
    }
    if ($Uri -eq 'fixture://cached') { throw 'A verified cache must not access the network.' }
    [IO.File]::WriteAllText($OutFile, $script:payload)
}

$destination = Join-Path $fixture 'dependency.bin'
Get-VerifiedDependencyFile -Sources @('fixture://offline', 'fixture://valid') -Destination $destination -Sha256 $sha256
if ([IO.File]::ReadAllText($destination) -ne $script:payload) { throw 'A failed primary must fall back to verified mirror bytes.' }
Write-Output 'PASS: Failed primary download recovers through a mirror.'

Get-VerifiedDependencyFile -Sources 'fixture://cached' -Destination $destination -Sha256 $sha256
if ([IO.File]::ReadAllText($destination) -ne $script:payload) { throw 'Verified cached bytes must remain unchanged.' }
Write-Output 'PASS: Verified cache is reused without downloading.'

[IO.File]::WriteAllText($destination, 'Corrupt cached archive')
Get-VerifiedDependencyFile -Sources @('fixture://corrupt', 'fixture://valid') -Destination $destination -Sha256 $sha256
if ((Get-FileHash -LiteralPath $destination).Hash -ne $sha256) { throw 'Corrupt cache and mirror responses must be replaced only by verified bytes.' }
Write-Output 'PASS: Corrupt cache and invalid mirror responses recover safely.'

$retryDestination = Join-Path $fixture 'retry.bin'
Get-VerifiedDependencyFile -Sources 'fixture://transient' -Destination $retryDestination -Sha256 $sha256
if ([IO.File]::ReadAllText($retryDestination) -ne $script:payload -or $script:transientAttempts -ne 3) { throw 'Transient failures must retry and publish verified bytes.' }
Write-Output 'PASS: Transient failures recover on a later attempt.'

$failedDestination = Join-Path $fixture 'failed.bin'
try {
    Get-VerifiedDependencyFile -Sources 'fixture://offline' -Destination $failedDestination -Sha256 $sha256
    throw 'Failed downloads were accepted.'
}
catch { if (!$_.Exception.Message.Contains('Could not obtain verified dependency failed.bin')) { throw } }
if (Test-Path -LiteralPath $failedDestination) { throw 'Partial downloads must not become cache entries.' }

[IO.File]::WriteAllText($destination, 'Preserve this rejected cache')
try {
    Get-VerifiedDependencyFile -Sources 'fixture://corrupt' -Destination $destination -Sha256 $sha256
    throw 'Invalid hashes were accepted.'
}
catch { if (!$_.Exception.Message.Contains('Could not obtain verified dependency dependency.bin')) { throw } }
if ([IO.File]::ReadAllText($destination) -ne 'Preserve this rejected cache') { throw 'Failed replacements must preserve existing files.' }
if (Get-ChildItem -LiteralPath $fixture -Filter '*.download') { throw 'Download failures must remove temporary files.' }
Write-Output 'PASS: Permanent failures preserve existing files, reject partial bytes and clean staging files.'
