param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$installation = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$installation) { throw 'Visual Studio C++ tools are required.' }
$msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
Push-Location $repository
try {
    & $msbuild Veris.sln /restore "/p:Configuration=$Configuration" /p:Platform=x64 /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
    & "./artifacts/bin/RenderingDependency/x64/$Configuration/RenderingDependency.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Native behavior checks failed.' }
    foreach ($suite in @('EditorBehavior', 'ResourceDiscovery', 'InteropBehavior', 'EditorUi')) {
        dotnet run --project "Tests/$suite/$suite.csproj" --configuration $Configuration --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "$suite failed." }
    }
    & ./scripts/VerifyRuntime.ps1 -Configuration $Configuration
    & ./scripts/PublishEditor.ps1 -Configuration $Configuration -OutputDirectory "artifacts/publish/$Configuration"
    & ./scripts/VerifyEditorResources.ps1 -EditorDirectory "artifacts/publish/$Configuration" -Configuration $Configuration
    dotnet run --project Tests/InteropBehavior/InteropBehavior.csproj --configuration $Configuration --verbosity quiet -- --runtime "artifacts/publish/$Configuration/Runtime"
    if ($LASTEXITCODE -ne 0) { throw 'Export execution checks failed.' }
    dotnet run --project Tests/Workflow/Workflow.csproj --configuration $Configuration --verbosity quiet -- "artifacts/publish/$Configuration/Runtime"
    if ($LASTEXITCODE -ne 0) { throw 'Workflow business-rule checks failed.' }
    git diff --check
    if ($LASTEXITCODE -ne 0) { throw 'Whitespace checks failed.' }
    Write-Output "PASS: Complete $Configuration validation."
}
finally { Pop-Location }
