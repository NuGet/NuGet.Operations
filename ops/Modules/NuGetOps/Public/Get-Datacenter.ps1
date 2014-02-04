<#
.SYNOPSIS
Gets a list of available datacenters in the current environment

.PARAMETER Environment
Specifies the environment to search within. If not specified, the current environment is used.

.PARAMETER Id
If specified, retrieves only the Datacenter with that ID.
#>
function Get-Datacenter {
    param(
        [Parameter(Mandatory=$false, Position = 0)][int]$Id = -1,
        [Parameter(Mandatory=$false)][string]$Environment)
    if(!$NuOps) {
        throw "This command requires that an App Model file was located during startup"
    }
    $env = $null;
    if(!$Environment) {
        if(!$NuOps.CurrentEnvironment) {
            throw "This command requires a current environment"
        }
        $env = $NuOps.CurrentEnvironment;
    } else {
        $env = $NuOps[$Environment]
        if(!$env) {
            throw "Unknown environment: $Environment"
        }
    }
    if($Id -ge 0) {
        $env[$Id]
    } else {
        $env.Datacenters
    }
}
Set-Alias -Name dc -Value Get-Datacenter
Export-ModuleMember -Alias dc