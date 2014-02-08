Import-Module -ErrorAction SilentlyContinue posh-git # Try to import posh-git
$poshGit = !!(Get-Command Write-VcsStatus)

Import-Module -ErrorVariable moderr Azure
Import-Module -ErrorVariable moderr "$PSScriptRoot\Modules\NuGetOps"

if($moderr) {
    Write-Host "Exiting ops console due to errors..."
    [Environment]::Exit(1)
}

function prompt() {
    Write-Host -NoNewLine -ForegroundColor Yellow "<NuOps> "
    Write-Host -NoNewLine -ForegroundColor Green (Get-Location)
    if($poshGit) {
        Write-VcsStatus
    }
    Write-Host
    Write-NuGetOpsPrompt
    " # "
}