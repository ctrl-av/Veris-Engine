function Get-VerifiedDependencyFile {
    param([string[]]$Sources, [string]$Destination, [string]$Sha256)
    if ((Test-Path -LiteralPath $Destination -PathType Leaf) -and (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash -eq $Sha256) { return }
    New-Item -ItemType Directory -Path (Split-Path $Destination) -Force | Out-Null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        foreach ($source in $Sources) {
            try {
                Write-Host "Downloading $(Split-Path $Destination -Leaf) from $source (attempt $attempt/3)."
                Save-DependencyDownload -Source $source -Destination $Destination -Sha256 $Sha256
                return
            }
            catch {
                $lastFailure = $_.Exception.Message
                Write-Warning "Download failed for ${source}: $lastFailure"
            }
        }
        if ($attempt -lt 3) { Start-Sleep -Seconds (2 * $attempt) }
    }
    throw "Could not obtain verified dependency $(Split-Path $Destination -Leaf). Last failure: $lastFailure"
}

function Save-DependencyDownload {
    param([string]$Source, [string]$Destination, [string]$Sha256)
    $download = "$Destination.$([Guid]::NewGuid().ToString('N')).download"
    try {
        Invoke-WebRequest -UseBasicParsing -Uri $Source -OutFile $download -TimeoutSec 30 -ErrorAction Stop
        if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $Sha256) {
            throw "Checksum mismatch for $(Split-Path $Destination -Leaf)."
        }
        Move-Item -LiteralPath $download -Destination $Destination -Force
    }
    finally {
        if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download -Force }
    }
}
