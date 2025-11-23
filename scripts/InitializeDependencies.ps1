$ErrorActionPreference = 'Stop'
$dependencyDirectory = Join-Path $PSScriptRoot '..\.dependencies'
$archivePath = Join-Path $dependencyDirectory 'SFML-3.0.1-windows-vc17-64-bit.zip'
$expectedHash = '2DA4B95BAB898686E06C6DD7F2333BCAAD572CB70DEB96C41D52321DF90AF48B'

try {
    New-Item -ItemType Directory -Path $dependencyDirectory -Force | Out-Null
    if (!(Test-Path -LiteralPath $archivePath)) {
        Invoke-WebRequest -Uri 'https://www.sfml-dev.org/files/SFML-3.0.1-windows-vc17-64-bit.zip' -OutFile $archivePath
    }

    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $expectedHash) {
        throw "SFML archive checksum mismatch. Remove '$archivePath' and run setup again."
    }

    $graphicsHeader = Join-Path $dependencyDirectory 'SFML-3.0.1\include\SFML\Graphics.hpp'
    $graphicsLibrary = Join-Path $dependencyDirectory 'SFML-3.0.1\lib\sfml-graphics.lib'
    if (!(Test-Path -LiteralPath $graphicsHeader) -or !(Test-Path -LiteralPath $graphicsLibrary)) {
        Expand-Archive -LiteralPath $archivePath -DestinationPath $dependencyDirectory -Force
    }

    Write-Output 'SFML 3.0.1 (MSVC v143, x64) is ready.'
}
catch {
    Write-Error "Native dependency setup failed: $_"
    exit 1
}
