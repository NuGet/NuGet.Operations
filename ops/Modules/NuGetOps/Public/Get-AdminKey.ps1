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
    (Get-ConfigValue $Service "Http.AdminKey" -Datacenter $Datacenter -Slot production).Trim()
}