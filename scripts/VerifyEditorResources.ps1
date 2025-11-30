param(
    [Parameter(Mandatory = $true)]
    [string]$EditorDirectory
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
