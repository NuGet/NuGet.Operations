<#
.SYNOPSIS
Gets the HTTP admin key for the specified service

.PARAMETER Service
The service to get the admin key for

.PARAMETER Datacenter
The datacenter in which that service runs (defaults to 0)

#>
function Get-AdminKey {
    param(
        [Parameter(Mandatory=$true,Position=0)][string]$Service,
        [Parameter(Mandatory=$false,Position=1)][int]$Datacenter = 0)
    if(!$NuOps -or !$NuOps.CurrentEnvironment) {
        throw "This operation requires a current environment. Use 'env' to set one."
    }

    $svc = $NuOps.CurrentEnvironment.GetService($Datacenter, $Service)
    if(!$svc) {
        throw "There is no $Service service in $($NuOps.CurrentEnvironment.Name) datacenter $Datacenter!"
    }
    Write-Host "Fetching configuration for Azure Service '$($svc.Value)'"
    $dep = Get-AzureDeployment -Service $svc.Value -Slot production
    if(!$dep) {
        throw "Unable to load configuration for Azure Service '$($svc.Value)'"
    }
    $x = [xml]$dep.Configuration
    $x.ServiceConfiguration.Role.ConfigurationSettings.Setting | where { $_.name -eq "Http.AdminKey" } | select -ExpandProperty value -first 1
}