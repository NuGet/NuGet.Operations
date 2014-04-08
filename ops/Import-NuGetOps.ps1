Import-Module -ErrorAction SilentlyContinue posh-git # Try to import posh-git
$poshGit = !!(Get-Command Write-VcsStatus -ErrorAction SilentlyContinue)

Import-Module -ErrorVariable moderr Azure
Import-Module -ErrorVariable moderr "$PSScriptRoot\Modules\NuGetOps"

if($moderr) {
    Write-Warning "There were fatal errors loading the ops console. I'm going to continue loading, but you have been warned :)"
    [Environment]::Exit(1)
}

function prompt() {
    $title = "<NuOps>"
    if($NuOpsVersion) {
        $title = "<NuOps $NuOpsVersion>"
    }

    Write-Host -NoNewLine -ForegroundColor Yellow "$title "
    Write-Host -NoNewLine -ForegroundColor Green (Get-Location)
    if($poshGit) {
        Write-VcsStatus
    }
    Write-Host
    Write-NuGetOpsPrompt
    " # "
}