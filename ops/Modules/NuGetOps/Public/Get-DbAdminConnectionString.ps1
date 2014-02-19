<#
.SYNOPSIS
Gets the admin connection string for the specified database by querying the Work Service's running CSCFG

.PARAMETER Database
The database to get the connection string for (Primary, Legacy or Warehouse). Defaults to "Legacy"

.PARAMETER Datacenter
The datacenter in which that service runs (defaults to 0)

#>
function Get-DbAdminConnectionString {
    param(
        [Parameter(Mandatory=$false,Position=0)][string]$Database = "legacy",
        [Parameter(Mandatory=$false,Position=1)][int]$Datacenter = 0)
    Get-ConfigValue "Work" "Sql.$Database" -Datacenter $Datacenter -Slot production
}