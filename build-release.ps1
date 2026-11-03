param([string]$NugetSource, [switch]$SkipWebBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
function Invoke-Checked([string]$File, [string[]]$Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE" }
}
$staging = Join-Path $projectRoot 'artifacts'
$portable = Join-Path $staging 'portable'
if (Test-Path -LiteralPath $staging) {
    $resolved = (Resolve-Path -LiteralPath $staging).Path
    if ($resolved -ne [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts'))) { throw 'Unexpected staging path' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
New-Item -ItemType Directory -Path $portable -Force | Out-Null
if (-not $SkipWebBuild) {
    Push-Location (Join-Path $projectRoot 'WebUI')
    try {
        Invoke-Checked 'npm.cmd' @('ci','--no-audit','--no-fund')
        Invoke-Checked 'npm.cmd' @('run','build')
    } finally { Pop-Location }
} elseif (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'WebUI/dist/index.html'))) {
    throw 'SkipWebBuild requires an existing verified WebUI/dist build.'
}
$restoreArgs = @()
if ($NugetSource) { $restoreArgs = @('--source',$NugetSource) }
Invoke-Checked 'dotnet' (@('publish',(Join-Path $projectRoot 'UzbekNewKeyboard.csproj'),'-c','Release','-o',$portable) + $restoreArgs)
Invoke-Checked 'dotnet' (@('publish',(Join-Path $projectRoot 'installer/uninstaller/Uninstall.csproj'),'-c','Release','-r','win-x64','--self-contained','false','-p:PublishSingleFile=true','-o',(Join-Path $staging 'uninstaller')) + $restoreArgs)
Copy-Item -LiteralPath (Join-Path $staging 'uninstaller/Uninstall.exe') -Destination $portable
Copy-Item -LiteralPath (Join-Path $projectRoot 'GLASS-BUILD.md') -Destination (Join-Path $portable 'README.md')
Get-ChildItem -LiteralPath $portable -Recurse -File | Where-Object { $_.Extension -in '.pdb','.xml' } | Remove-Item -Force
$payload = Join-Path $projectRoot 'installer/payload.zip'
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath $payload -Force
Invoke-Checked 'dotnet' (@('publish',(Join-Path $projectRoot 'installer/Installer.csproj'),'-c','Release','-r','win-x64','--self-contained','false','-p:PublishSingleFile=true','-o',(Join-Path $staging 'setup')) + $restoreArgs)
$distribution = Join-Path $projectRoot 'dist'
$distPortable = Join-Path $distribution 'portable'
if (Test-Path -LiteralPath $distPortable) {
    $resolved = (Resolve-Path -LiteralPath $distPortable).Path
    if ($resolved -ne [IO.Path]::GetFullPath((Join-Path $projectRoot 'dist/portable'))) { throw 'Unexpected portable path' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
New-Item -ItemType Directory -Path $distribution -Force | Out-Null
Copy-Item -LiteralPath $portable -Destination $distPortable -Recurse
Copy-Item -LiteralPath (Join-Path $staging 'setup/Yangi lotin klaviatura Setup.exe') -Destination $distribution
Compress-Archive -Path (Join-Path $distPortable '*') -DestinationPath (Join-Path $distribution 'Yangi-Lotin-Portable.zip') -Force
Get-ChildItem -LiteralPath $distribution
