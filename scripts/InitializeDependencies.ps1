$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'DependencyDownloads.ps1')
$dependencyDirectory = Join-Path $PSScriptRoot '..\.dependencies'
$archivePath = Join-Path $dependencyDirectory 'SFML-3.0.1-windows-vc17-64-bit.zip'
$expectedHash = '2DA4B95BAB898686E06C6DD7F2333BCAAD572CB70DEB96C41D52321DF90AF48B'

try {
    New-Item -ItemType Directory -Path $dependencyDirectory -Force | Out-Null
    Get-VerifiedDependencyFile -Sources 'https://www.sfml-dev.org/files/SFML-3.0.1-windows-vc17-64-bit.zip' -Destination $archivePath -Sha256 $expectedHash

    $graphicsHeader = Join-Path $dependencyDirectory 'SFML-3.0.1\include\SFML\Graphics.hpp'
    $graphicsLibrary = Join-Path $dependencyDirectory 'SFML-3.0.1\lib\sfml-graphics.lib'
    if (!(Test-Path -LiteralPath $graphicsHeader) -or !(Test-Path -LiteralPath $graphicsLibrary)) {
        Expand-Archive -LiteralPath $archivePath -DestinationPath $dependencyDirectory -Force
    }

    Write-Output 'SFML 3.0.1 (MSVC v143, x64) is ready.'
    $jsonDirectory = Join-Path $dependencyDirectory 'json-3.12.0'
    $jsonFiles = @(
        @{ Path = 'include\nlohmann\json.hpp'; Url = 'https://raw.githubusercontent.com/nlohmann/json/v3.12.0/single_include/nlohmann/json.hpp'; Hash = 'AAF127C04CB31C406E5B04A63F1AE89369FCCDE6D8FA7CDDA1ED4F32DFC5DE63' },
        @{ Path = 'LICENSE.MIT'; Url = 'https://raw.githubusercontent.com/nlohmann/json/v3.12.0/LICENSE.MIT'; Hash = '46A65CFFD1EA955132D95A8DD921640714A8D6B537D2E4E482D31145AE95B603' }
    )
    foreach ($jsonFile in $jsonFiles) {
        $destination = Join-Path $jsonDirectory $jsonFile.Path
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Get-VerifiedDependencyFile -Sources $jsonFile.Url -Destination $destination -Sha256 $jsonFile.Hash
    }
    Write-Output 'nlohmann/json 3.12.0 is ready.'
    $physicsArchive = Join-Path $dependencyDirectory 'box2d-2.4.1.zip'
    Get-VerifiedDependencyFile -Sources 'https://codeload.github.com/erincatto/box2d/zip/refs/tags/v2.4.1' -Destination $physicsArchive -Sha256 '0CB512DFA5BE79CA227CD881B279ADEE61249C85C8B51CAF5AA036B71E943002'
    if (!(Test-Path -LiteralPath (Join-Path $dependencyDirectory 'box2d-2.4.1\src\dynamics\b2_world.cpp'))) {
        Expand-Archive -LiteralPath $physicsArchive -DestinationPath $dependencyDirectory -Force
    }
    Write-Output 'Box2D 2.4.1 is ready.'
    $luaArchive = Join-Path $dependencyDirectory 'lua-5.4.8.tar.gz'
    $luaSources = @('https://www.lua.org/ftp/lua-5.4.8.tar.gz', 'https://distfiles.macports.org/lua/lua-5.4.8.tar.gz')
    Get-VerifiedDependencyFile -Sources $luaSources -Destination $luaArchive -Sha256 '4F18DDAE154E793E46EEAB727C59EF1C0C0C2B744E7B94219710D76F530629AE'
    if (!(Test-Path -LiteralPath (Join-Path $dependencyDirectory 'lua-5.4.8\src\lua.h'))) {
        tar -xzf $luaArchive -C $dependencyDirectory
        if ($LASTEXITCODE -ne 0) { throw 'Lua extraction failed.' }
    }
    Write-Output 'Lua 5.4.8 is ready.'
}
catch {
    Write-Error "Native dependency setup failed: $_"
    exit 1
}
