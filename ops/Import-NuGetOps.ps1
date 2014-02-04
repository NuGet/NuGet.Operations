Import-Module -ErrorAction SilentlyContinue posh-git # Try to import posh-git
Import-Module Azure
Import-Module "$PSScriptRoot\Modules\NuGetOps"

$poshGit = !!(Get-Command Write-VcsStatus)

function prompt() {
    Write-Host -NoNewLine -ForegroundColor Yellow "<NuGet Ops> "
    Write-Host -NoNewLine -ForegroundColor Green (Get-Location)
    if($poshGit) {
        Write-VcsStatus
    }
    Write-Host
    Write-NuGetOpsPrompt
    " # "
}