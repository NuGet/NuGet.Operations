<#
.SYNOPSIS
Checks if the specified environment is the current environment, or if it exists in the App Model

.PARAMETER Environment
The environment to check for

.PARAMETER Exists
If this switch is set, the command will return true if the environment exists in the App Model and false if it does not.
If this switch is NOT set, the command will return true if the specified environment is the current environment and false if it is not.
#>

function Test-Environment([Parameter(Mandatory=$true)][String]$Environment, [Switch]$Exists) {
    if($Exists) {
        return !!($NuOps[$Environment])
    } else {
        [String]::Equals($NuOps.CurrentEnvironment.Name, $Environment, "OrdinalIgnoreCase");
    }
}