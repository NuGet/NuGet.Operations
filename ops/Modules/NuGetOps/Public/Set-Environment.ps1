<#
.SYNOPSIS
Sets the current environment

.PARAMETER Name
The name of the environment to set as current
#>
function Set-Environment {
    param([Parameter(Mandatory=$true)][string]$Name)
    if(!$NuOps) {
        throw "This command requires that an App Model file was located during startup"
    }
    $NuOps.SetCurrentEnvironment($Name)
    $env:NUOPS_CURRENT_ENVIRONMENT = $Name

    Select-AzureSubscription $NuOps.CurrentEnvironment.Subscription.Name
}