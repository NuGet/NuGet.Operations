<#
.SYNOPSIS
Gets a list of available environments

.PARAMETER Name
An optional wildcard filter to use to filter environments
#>
function Get-Environment {
    param([Parameter(Mandatory=$false)][string]$Name)
    if(!$NuOps) {
        throw "This command requires that an App Model file was located during startup"
    }
    $envs = $NuOps.Model.Environments
    if(![String]::IsNullOrEmpty($Name)) {
        $envs = $envs | where { $_.Name -like $Name }
    }
    $envs
}