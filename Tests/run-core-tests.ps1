$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$testOutput = Join-Path $repoRoot 'Temp/CoreTests'
New-Item -ItemType Directory -Force $testOutput | Out-Null
$coreSources = [System.Security.SecurityElement]::Escape((Join-Path $repoRoot 'Assets/Scripts/Core/*.cs'))
$testSources = [System.Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'Core/Program.cs'))
$enemyChecks = [System.Security.SecurityElement]::Escape((Join-Path $repoRoot 'CheckTest/EnemyPolicyTests.cs'))
$testProject = Join-Path $testOutput 'CoreTests.csproj'
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$coreSources" />
    <Compile Include="$testSources" />
    <Compile Include="$enemyChecks" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $testProject
dotnet run --project $testProject --verbosity quiet
exit $LASTEXITCODE
