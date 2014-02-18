<#
.SYNOPSIS
Gets the specified configuration value from the running service

.PARAMETER Service
The service to get the value for

.PARAMETER Key
The key to get the value of

.PARAMETER Datacenter
The datacenter in which that service runs (defaults to 0)

.PARAMETER Slot
The slot to get the configuration value from (defaults to "production")

#>
function Get-ConfigValue {
    param(
        [Parameter(Mandatory=$true,Position=0)][string]$Service,
        [Parameter(Mandatory=$true,Position=1)][string]$Key,
        [Parameter(Mandatory=$false)][int]$Datacenter = 0,
        [Parameter(Mandatory=$false)][string]$Slot = "production")
    if(!$NuOps -or !$NuOps.CurrentEnvironment) {
        throw "This operation requires a current environment. Use 'env' to set one."
    }

    $svc = $NuOps.CurrentEnvironment.GetService($Datacenter, $Service)
    if(!$svc) {
        throw "There is no $Service service in $($NuOps.CurrentEnvironment.Name) datacenter $Datacenter!"
    }
    Write-Host "Fetching configuration for Azure Service '$($svc.Value)'"
    $dep = Get-AzureDeployment -Service $svc.Value -Slot $Slot
    if(!$dep) {
        throw "Unable to load configuration for Azure Service '$($svc.Value)'"
    }
    $x = [xml]$dep.Configuration
    $x.ServiceConfiguration.Role.ConfigurationSettings.Setting | where { $_.name -eq $Key } | select -ExpandProperty value -first 1
}