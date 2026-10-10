param([Parameter(Mandatory = $true)][string]$CandidateDirectory)
$ErrorActionPreference = 'Stop'
$candidate = (Resolve-Path -LiteralPath $CandidateDirectory).Path
$scripts = Join-Path $PSScriptRoot '../scripts'
$checksums = Join-Path $candidate 'SHA256SUMS.txt'
$originalHash = (Get-FileHash -LiteralPath $checksums).Hash
try {
    & (Join-Path $scripts 'PrepareRelease.ps1') -OutputDirectory $candidate
    throw 'Existing release output was accepted.'
}
catch {
    if (!$_.Exception.Message.Contains('existing releases are never overwritten')) { throw }
}
if ((Get-FileHash -LiteralPath $checksums).Hash -ne $originalHash) { throw 'Rejected preparation changed existing release checksums.' }
$fixture = Join-Path $candidate "Guard checks $([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $fixture | Out-Null
$entry = Get-Content -LiteralPath $checksums | Select-Object -First 1
$name = $entry.Substring(66)
$entry | Set-Content -LiteralPath (Join-Path $fixture 'SHA256SUMS.txt') -Encoding ASCII
Copy-Item -LiteralPath (Join-Path $candidate $name) -Destination (Join-Path $fixture $name)
Add-Content -LiteralPath (Join-Path $fixture $name) -Value 'Altered release evidence.'
try {
    & (Join-Path $scripts 'VerifyRelease.ps1') -CandidateDirectory $fixture
    throw 'Altered release evidence was accepted.'
}
catch {
    if (!$_.Exception.Message.Contains('Release checksum mismatch')) { throw }
}
Write-Output 'PASS: Absolute output paths reject existing releases without changes; corrupted release evidence is rejected.'
