$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$publishRoot = Join-Path $repositoryRoot 'artifacts\TV1\win-x64'
$serverOutput = Join-Path $publishRoot 'server'
$clientOutput = Join-Path $publishRoot 'client'

$serverProject = Join-Path $repositoryRoot 'server\ThuVienDAU.Server.csproj'
$clientProject = Join-Path $repositoryRoot 'client\ThuVienDAU.WinForms.csproj'

& dotnet publish $serverProject --configuration Release --runtime win-x64 --self-contained true --output $serverOutput -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Publish server thất bại.' }

& dotnet publish $clientProject --configuration Release --runtime win-x64 --self-contained true --output $clientOutput -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Publish client thất bại.' }

Copy-Item (Join-Path $repositoryRoot 'server\appsettings.example.json') (Join-Path $serverOutput 'appsettings.local.json') -Force
Copy-Item (Join-Path $repositoryRoot 'client\appsettings.example.json') (Join-Path $clientOutput 'appsettings.local.json') -Force

Write-Output "Server: $serverOutput"
Write-Output "Client: $clientOutput"
Write-Output 'Local configs created from examples. Set server MySQL and client server IP before running.'
