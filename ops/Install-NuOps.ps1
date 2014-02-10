Write-Host "NuOps Installer"
Write-Host "This installer will ask you a few small questions, then set up the NuOps environment on your machine"

function Confirm() {
    param($Message, [bool]$Default = $true)

    $choices = "[Y/n]"
    if(!$Default) {
        $choices = "[y/N]"
    }

    $val = $null
    do {
        $str = Read-Host "$Message $choices"
        if([String]::IsNullOrWhitespace($str)) {
            $val = $Default
        } elseif([String]::Equals($str, "y", "OrdinalIgnoreCase") -or [String]::Equals($str, "yes", "OrdinalIgnoreCase")) {
            $val = $true
        } elseif([String]::Equals($str, "n", "OrdinalIgnoreCase") -or [String]::Equals($str, "no", "OrdinalIgnoreCase")) {
            $val = $false
        } else {
            $val = $null
        }
    } while($val -eq $null)
    $val
}

if([String]::IsNullOrWhitespace($env:NUOPS_APP_MODEL) -or (!(Test-Path $env:NUOPS_APP_MODEL))) {
    $DefaultInternalRepository = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "Deployment"
    $appModel = Join-Path $DefaultInternalRepository "AppModel.xml"

    Write-Host "In order to use NuOps, you must clone your application's deployment configuration repository."
    if($appModel -and (Test-Path $appModel)) {
        Write-Host "It looks like you've cloned the deployment configuration repository to $DefaultInternalRepository."
        if(!(Confirm "Is this correct?")) {
            $appModel = $null
        }
    }
    
    if(!$appModel) {
        $location = Read-Host "Where have you cloned this Repository?"
        $appModel = Join-Path $location "AppModel.xml"
    }

    if(!(Test-Path $appModel)) {
        throw "Could not find AppModel.xml file in this repository! Make sure you've cloned the config repository first!"
    }

    Write-Host "Setting NUOPS_APP_MODEL environment variable to $appModel"
    [Environment]::SetEnvironmentVariable("NUOPS_APP_MODEL", $appModel, "User")
    $env:NUOPS_APP_MODEL = $appModel
}

# Check for Azure
Import-Module -ErrorVariable moderr Azure

if($moderr) {
    Write-Host "Azure PowerShell is not installed."
    Write-Host "Go to https://go.microsoft.com/fwlink/p/?linkid=320376&clcid=0x409 to install it"
    if(Confirm "Launch browser to that URL?") {
        Start-Process "https://go.microsoft.com/fwlink/p/?linkid=320376&clcid=0x409"
    }
}