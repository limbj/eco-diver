param([string]$CompilerPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

if (-not $CompilerPath) {
    $vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (Test-Path -LiteralPath $vsWhere) {
        $CompilerPath = & $vsWhere -latest -products '*' -find 'MSBuild\**\Bin\Roslyn\csc.exe' | Select-Object -First 1
    }
}
if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath)) {
    throw 'Pass -CompilerPath with the Roslyn csc.exe path from Visual Studio or Build Tools.'
}

$compilerDirectory = Split-Path -Parent $CompilerPath
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('EcoDiver-checks-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
try {
    $coreSources = @(
        'Assets/Script/Display/OxygenSupply.cs',
        'Assets/Script/UI/FailureMenuInput.cs',
        'Assets/Script/UI/SettingsValues.cs',
        'Assets/Script/Scene/SceneDetail.cs',
        'Assets/Script/Scene/SceneFlow.cs'
    ) | ForEach-Object { Join-Path $projectRoot $_ }
    $testSources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Standalone') -File -Filter '*.cs' | ForEach-Object { $_.FullName })
    $testsExecutable = Join-Path $temporaryRoot 'CoreTests.exe'
    & $CompilerPath -nologo -warnaserror+ -langversion:9.0 -target:exe "-out:$testsExecutable" @coreSources @testSources
    if ($LASTEXITCODE -ne 0) { throw 'Core test compilation failed.' }
    & $testsExecutable
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }

    $componentSources = @(
        'Assets/Script/Quest/Quest.cs',
        'Assets/Script/Quest/QuestObserver.cs',
        'Assets/Script/Quest/Quest3A.cs',
        'Assets/Script/Quest/Quest3B.cs',
        'Assets/Script/Quest/QuestLevel.cs',
        'Assets/Script/Environment/Collect/CollectManager.cs',
        'Assets/Script/Display/WaterDepthManager.cs',
        'Assets/Script/Display/OxygenManager.cs',
        'Assets/Script/Display/OxygenSupply.cs',
        'Assets/Script/Equipment/ToolSelector.cs',
        'Assets/Script/Environment/Optimizer/CullingObject.cs',
        'Assets/Script/Environment/Optimizer/CullingOptimizer.cs',
        'Assets/Script/Environment/Optimizer/MarkerObject.cs'
    ) | ForEach-Object { Join-Path $projectRoot $_ }
    $componentTests = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Components') -File -Filter '*.cs' |
        ForEach-Object { $_.FullName })
    $componentExecutable = Join-Path $temporaryRoot 'ComponentTests.exe'
    # Scene-populated and legacy unused Unity fields are outside this standalone compiler's view.
    & $CompilerPath -nologo -warnaserror+ -nowarn:0169,0414,0649 -langversion:9.0 -target:exe "-out:$componentExecutable" `
        @componentSources @componentTests (Join-Path $PSScriptRoot 'Standalone/Program.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Component test compilation failed.' }
    & $componentExecutable
    if ($LASTEXITCODE -ne 0) { throw 'Component tests failed.' }

    $roslynNames = @('Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll',
        'System.Collections.Immutable.dll','System.Reflection.Metadata.dll','System.Memory.dll',
        'System.Runtime.CompilerServices.Unsafe.dll','System.Threading.Tasks.Extensions.dll',
        'System.Buffers.dll','System.Numerics.Vectors.dll','System.Text.Encoding.CodePages.dll')
    foreach ($name in $roslynNames) {
        $library = Join-Path $compilerDirectory $name
        if (Test-Path -LiteralPath $library) { Copy-Item -LiteralPath $library -Destination $temporaryRoot }
    }
    $references = @('Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll','System.Collections.Immutable.dll') |
        ForEach-Object { '-r:' + (Join-Path $temporaryRoot $_) }
    $facades = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.8/Facades/netstandard.dll'),
        (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/Facades/netstandard.dll'),
        (Join-Path $compilerDirectory '../../../Microsoft/Microsoft.NET.Build.Extensions/net461/lib/netstandard.dll'),
        (Join-Path $compilerDirectory '../ref/netstandard.dll')
    )
    $standardFacade = $facades | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($standardFacade) {
        $references += '-r:' + $standardFacade
        Copy-Item -LiteralPath $standardFacade -Destination $temporaryRoot
    }
    $syntaxExecutable = Join-Path $temporaryRoot 'CSharpSyntaxCheck.exe'
    & $CompilerPath -nologo -target:exe "-out:$syntaxExecutable" @references (Join-Path $PSScriptRoot 'Tools/CSharpSyntaxCheck.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Syntax checker compilation failed.' }
    if (Test-Path -LiteralPath ($CompilerPath + '.config')) {
        Copy-Item -LiteralPath ($CompilerPath + '.config') -Destination ($syntaxExecutable + '.config')
    }
    $scripts = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Script') -Recurse -File -Filter '*.cs' | ForEach-Object { $_.FullName })
    & $syntaxExecutable @scripts
    if ($LASTEXITCODE -ne 0) { throw 'C# syntax checks failed.' }

    [xml]$settingsXml = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets/Art/UI/UXML/Settings.uxml') -Raw -Encoding UTF8
    if ($settingsXml.SelectNodes('//*[@name="ClearButton"]').Count -ne 1) { throw 'Expected one settings reset button.' }
    Write-Output 'Settings UXML: valid, reset button found.'
} finally {
    foreach ($temporaryFile in Get-ChildItem -LiteralPath $temporaryRoot -File) {
        Remove-Item -LiteralPath $temporaryFile.FullName
    }
    Remove-Item -LiteralPath $temporaryRoot
}
