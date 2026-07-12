param(
    [Parameter(Mandatory = $true)]
    [string]$EditorDirectory,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$sourceDirectory = (Resolve-Path (Join-Path $PSScriptRoot '..\VerisEditor\ProjectTemplates')).Path
$sourceAssets = Get-ChildItem -LiteralPath $sourceDirectory -Recurse -File
foreach ($sourceAsset in $sourceAssets) {
    $relativeAssetPath = $sourceAsset.FullName.Substring($sourceDirectory.Length + 1)
    $deployedAsset = Join-Path $EditorDirectory "ProjectTemplates\$relativeAssetPath"
    if (!(Test-Path -LiteralPath $deployedAsset)) {
        throw "Missing editor resource: $relativeAssetPath"
    }

    if ((Get-FileHash -LiteralPath $sourceAsset.FullName).Hash -ne (Get-FileHash -LiteralPath $deployedAsset).Hash) {
        throw "Editor resource content differs: $relativeAssetPath"
    }
}

Write-Output "PASS: All $($sourceAssets.Count) editor resources are deployed unchanged."

$repositoryDirectory = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sfmlSuffix = if ($Configuration -eq 'Debug') { '-d' } else { '' }
$nativeFiles = @(
    @{ Source = ".dependencies\lua-5.4.8\doc\readme.html"; Destination = 'licenses\Lua.html' },
    @{ Source = ".dependencies\SFML-3.0.1\license.md"; Destination = 'licenses\SFML.md' },
    @{ Source = ".dependencies\box2d-2.4.1\LICENSE"; Destination = 'licenses\Box2D.txt' },
    @{ Source = ".dependencies\json-3.12.0\LICENSE.MIT"; Destination = 'licenses\nlohmann-json.txt' },
    @{ Source = "artifacts\bin\VerisInterop\x64\$Configuration\VerisInterop.dll"; Destination = 'VerisInterop.dll' }
)
foreach ($module in @('graphics', 'window', 'system', 'audio')) {
    $name = "sfml-$module$sfmlSuffix-3.dll"
    $nativeFiles += @{ Source = ".dependencies\SFML-3.0.1\bin\$name"; Destination = $name }
}
foreach ($nativeFile in $nativeFiles) {
    $source = Join-Path $repositoryDirectory $nativeFile.Source
    $destination = Join-Path $EditorDirectory $nativeFile.Destination
    if (!(Test-Path -LiteralPath $destination) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) {
        throw "Missing or mismatched native editor dependency: $($nativeFile.Destination)"
    }
}
Write-Output "PASS: $Configuration interop, four SFML DLLs, and four dependency licenses match their build sources."
