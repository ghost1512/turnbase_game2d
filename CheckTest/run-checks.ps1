$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$reportPath = Join-Path $PSScriptRoot 'latest-results.txt'
"Check started: $(Get-Date -Format o)" | Set-Content -LiteralPath $reportPath
& (Join-Path $repoRoot 'Tests/run-core-tests.ps1') 2>&1 | Tee-Object -FilePath $reportPath -Append
if ($LASTEXITCODE -ne 0) { throw 'Core/Enemy policy tests failed. See latest-results.txt.' }
$targetsPath = Join-Path $repoRoot 'Tests/IncludeAllScripts.targets'
dotnet build (Join-Path $repoRoot 'Assembly-CSharp.csproj') --no-restore --verbosity quiet "-p:CustomBeforeMicrosoftCommonTargets=$targetsPath" 2>&1 |
    Tee-Object -FilePath $reportPath -Append
if ($LASTEXITCODE -ne 0) { throw 'Unity C# build failed. See latest-results.txt.' }
'Automated scope: core logic + Enemy target policy + C# compilation. Unity Play Mode is not exercised.' |
    Tee-Object -FilePath $reportPath -Append
